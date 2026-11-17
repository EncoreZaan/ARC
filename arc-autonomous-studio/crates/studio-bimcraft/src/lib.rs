//! `studio-bimcraft` — Compilateur & Pilote Autonome BIMcraft (Priorité N°1)
//! Traduit un `DesignVariant` de l'`ArcProjectModel` (APM) en un script transactionnel
//! BIMcraft 0.11+, exécute `bimcraft-cli` en mode headless sans aucune dépendance à Revit,
//! génère `.bimcraft`, `.ifc`, `.glb`, `.svg`, `.pdf` et inspecte le modèle produit.

use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::collections::BTreeSet;
use std::env;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;
use studio_apm::DesignVariant;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BimcraftBuildArtifacts {
    pub variant_id: String,
    pub script_path: String,
    pub bimcraft_model_path: String,
    pub ifc_path: String,
    pub glb_path: String,
    pub svg_plan_path: String,
    pub pdf_sheet_path: String,
    pub cli_version: String,
    pub inspection_raw: String,
    pub elements_verified: bool,
    pub build_duration_ms: u128,
}

pub fn resolve_bimcraft_cli() -> PathBuf {
    if let Ok(home) = env::var("USERPROFILE").or_else(|_| env::var("HOME")) {
        let p = PathBuf::from(&home).join(".cargo").join("bin").join("bimcraft-cli.exe");
        if p.exists() {
            return p;
        }
        let p_unix = PathBuf::from(&home).join(".cargo").join("bin").join("bimcraft-cli");
        if p_unix.exists() {
            return p_unix;
        }
    }
    PathBuf::from("bimcraft-cli")
}

fn fmt_num(v: f64) -> Value {
    let r = (v * 10.0).round() / 10.0;
    if (r - r.round()).abs() < 1e-6 {
        json!(r.round() as i64)
    } else {
        json!(r)
    }
}

fn fmt_pt(pt: [f64; 2]) -> Value {
    json!([fmt_num(pt[0]), fmt_num(pt[1])])
}

/// Compile un `DesignVariant` de l'APM en script natif BIMcraft 0.11.0.
pub fn compile_variant_to_bimscript(variant: &DesignVariant, hsp_mm: f64, slab_mm: f64) -> String {
    let mut lines = Vec::with_capacity(250);

    let emit = |cmd: &str, payload: Value| -> String {
        format!("{} {}", cmd, serde_json::to_string(&payload).unwrap_or_default())
    };

    // 1. Niveaux et unités
    lines.push(emit("modify", json!({"id": 100000, "name": "N0 - Appartement"})));
    lines.push(emit(
        "modify",
        json!({
            "id": 100001,
            "name": format!("N+1 - Sous-face dalle haute (HSP {:.0} + {:.0})", hsp_mm / 10.0, slab_mm / 10.0),
            "elevation": fmt_num(hsp_mm + slab_mm)
        }),
    ));
    lines.push(emit("units", json!({"length": "cm"})));
    lines.push(emit("room_area", json!({"at": "finish"})));

    // 2. Types de murs et plancher
    for (name, thick) in [
        ("EXI_Mur porteur 35", 350),
        ("EXI_Mur palier 20", 200),
        ("EXI_Mur mitoyen 105", 1050),
        ("EXI_Cloison 7", 70),
        ("PRJ_Cloison 7", 70),
        ("PRJ_Rebouchage 20", 200),
        ("PRJ_Rebouchage 35", 350),
        ("PRJ_Meuble-cloison 60", 600),
    ] {
        lines.push(emit(
            "type",
            json!({"category": "wall", "name": name, "thickness": thick}),
        ));
    }
    lines.push(emit(
        "type",
        json!({"category": "floor", "name": "EXI_Plancher existant 25", "thickness": slab_mm.round() as i64}),
    ));

    // 3. Types de portes et fenêtres utilisés dans la variante
    let mut door_widths = BTreeSet::new();
    for d in &variant.doors {
        door_widths.insert(d.width_mm.round() as i64);
    }
    for w_mm in door_widths {
        let tname = format!("Porte {} x 204", w_mm / 10);
        lines.push(emit(
            "type",
            json!({"category": "door", "name": tname, "width": w_mm, "height": 2040}),
        ));
    }

    let mut win_widths = BTreeSet::new();
    for w in &variant.windows {
        win_widths.insert(w.width_mm.round() as i64);
    }
    for w_mm in win_widths {
        let tname = format!("Fenêtre {} x 170", w_mm / 10);
        lines.push(emit(
            "type",
            json!({"category": "window", "name": tname, "width": w_mm, "height": 1700, "sill": 800}),
        ));
    }

    lines.push(emit(
        "type",
        json!({"category": "beam", "name": "PRJ_Poutre 35 x 40", "width": 350, "depth": 400, "material": "Concrete"}),
    ));
    lines.push(emit(
        "line_style",
        json!({"name": "Démolition", "pen": 3, "color": "#c8281e", "pattern": "Dash"}),
    ));

    // 4. Phase Existant : Dalle, murs existants conservés, fenêtres et portes existantes
    if !variant.floor_boundary_mm.is_empty() {
        let b_pts: Vec<Value> = variant.floor_boundary_mm.iter().map(|p| fmt_pt(*p)).collect();
        lines.push(emit(
            "floor",
            json!({"boundary": b_pts, "type": "EXI_Plancher existant 25"}),
        ));
    }

    for w in &variant.existing_walls_kept {
        lines.push(emit(
            "wall",
            json!({
                "points": [fmt_pt(w.start_mm), fmt_pt(w.end_mm)],
                "type": w.category.bimcraft_type_name(true),
                "height": fmt_num(w.height_mm)
            }),
        ));
    }

    for win in &variant.windows {
        lines.push(emit(
            "window",
            json!({"at": fmt_pt(win.at_mm), "type": win.type_name}),
        ));
    }

    for d in variant.doors.iter().filter(|d| d.is_existing) {
        lines.push(emit(
            "door",
            json!({
                "at": fmt_pt(d.at_mm),
                "type": d.type_name,
                "flip_hand": d.flip_hand,
                "flip_facing": d.flip_facing
            }),
        ));
    }

    // 5. Phase Projet Nouveau : Cloisons neuves, portes neuves, poutres de reprise, mobilier 3D, pièces
    lines.push(emit("phase", json!({"now": "New Construction"})));

    for w in &variant.new_walls {
        lines.push(emit(
            "wall",
            json!({
                "points": [fmt_pt(w.start_mm), fmt_pt(w.end_mm)],
                "type": w.category.bimcraft_type_name(false),
                "height": fmt_num(w.height_mm)
            }),
        ));
    }

    for d in variant.doors.iter().filter(|d| !d.is_existing) {
        lines.push(emit(
            "door",
            json!({
                "at": fmt_pt(d.at_mm),
                "type": d.type_name,
                "flip_hand": d.flip_hand,
                "flip_facing": d.flip_facing
            }),
        ));
    }

    for op in &variant.structural_openings {
        lines.push(emit(
            "beam",
            json!({
                "points": [fmt_pt(op.beam_start_mm), fmt_pt(op.beam_end_mm)],
                "type": "PRJ_Poutre 35 x 40",
                "offset": fmt_num(op.beam_offset_mm)
            }),
        ));
    }

    for fam in &variant.families {
        lines.push(emit("family", fam.raw_bimcraft_json.clone()));
    }

    for comp in &variant.components {
        lines.push(emit(
            "component",
            json!({
                "family": comp.family_name,
                "at": fmt_pt(comp.at_mm),
                "angle": fmt_num(comp.angle_deg)
            }),
        ));
    }

    for sep in &variant.room_separators_mm {
        lines.push(emit(
            "room_separator",
            json!({"points": [fmt_pt(sep[0]), fmt_pt(sep[1])]}),
        ));
    }

    for r in &variant.rooms {
        lines.push(emit(
            "room",
            json!({
                "point": fmt_pt(r.point_mm),
                "name": r.name,
                "number": r.number
            }),
        ));
    }

    for poly in &variant.demolished_polygons_mm {
        let pts: Vec<Value> = poly.iter().map(|p| fmt_pt(*p)).collect();
        lines.push(emit(
            "detail_line",
            json!({"points": pts, "closed": true, "style": "Démolition"}),
        ));
    }

    // 6. Vues, mise à l'échelle 1:50, feuille A3 et nomenclature
    lines.push(emit("scale", json!({"view": "N0 - Appartement", "scale": 50})));
    lines.push(emit(
        "graphics",
        json!({
            "view": "N0 - Appartement",
            "phase": "New Construction",
            "phase_filter": "Show Previous + New"
        }),
    ));
    lines.push(emit(
        "sheet",
        json!({
            "number": "A101",
            "name": format!("REFLET - {} - Plan projet 1:50", variant.variant_id),
            "size": "A3"
        }),
    ));
    lines.push(emit(
        "place",
        json!({"sheet": "A101", "view": "N0 - Appartement", "scale": 50}),
    ));
    lines.push(emit(
        "schedule",
        json!({
            "name": "Surfaces des pièces",
            "category": "rooms",
            "fields": ["Number", "Name", {"name": "Area", "total": true}]
        }),
    ));

    lines.join("\n") + "\n"
}

/// Construit le projet complet dans BIMcraft 0.11.0 à partir du `DesignVariant` de l'APM,
/// sans nécessiter Revit, et exporte `.bimcraft`, `.ifc`, `.glb`, `.svg` et `.pdf`.
pub fn build_variant_in_bimcraft(
    variant: &DesignVariant,
    hsp_mm: f64,
    slab_mm: f64,
    output_dir: &Path,
) -> Result<BimcraftBuildArtifacts, String> {
    let t0 = std::time::Instant::now();
    fs::create_dir_all(output_dir).map_err(|e| e.to_string())?;

    let cli = resolve_bimcraft_cli();
    if !cli.exists() {
        return Err(format!("bimcraft-cli introuvable à : {:?}", cli));
    }

    let script_content = compile_variant_to_bimscript(variant, hsp_mm, slab_mm);
    let script_path = output_dir.join(format!("{}.bimscript", variant.variant_id));
    fs::write(&script_path, &script_content).map_err(|e| e.to_string())?;

    let bimcraft_model_path = output_dir.join(format!("{}.bimcraft", variant.variant_id));
    let ifc_path = output_dir.join(format!("{}.ifc", variant.variant_id));
    let glb_path = output_dir.join(format!("{}.glb", variant.variant_id));
    let svg_plan_path = output_dir.join(format!("{}_plan.svg", variant.variant_id));
    let pdf_sheet_path = output_dir.join(format!("{}_sheet_A3.pdf", variant.variant_id));

    let run_out = Command::new(&cli)
        .arg("run")
        .arg("--script")
        .arg(&script_path)
        .arg("--save")
        .arg(&bimcraft_model_path)
        .arg("--save")
        .arg(&ifc_path)
        .arg("--save")
        .arg(&glb_path)
        .arg("--save")
        .arg(&svg_plan_path)
        .arg("--save")
        .arg(&pdf_sheet_path)
        .output()
        .map_err(|e| format!("Échec exécution bimcraft-cli run: {}", e))?;

    if !run_out.status.success() {
        return Err(format!(
            "Erreur bimcraft-cli run ({}): {}",
            run_out.status,
            String::from_utf8_lossy(&run_out.stderr)
        ));
    }

    // Inspection analytique réelle via `bimcraft-cli info` sur le fichier .bimcraft généré
    let info_out = Command::new(&cli)
        .arg("info")
        .arg(&bimcraft_model_path)
        .output()
        .map_err(|e| format!("Échec bimcraft-cli info: {}", e))?;

    let inspection_raw = String::from_utf8_lossy(&info_out.stdout).trim().to_string();
    let ver_out = Command::new(&cli)
        .arg("--version")
        .output()
        .ok()
        .map(|o| String::from_utf8_lossy(&o.stdout).trim().to_string())
        .unwrap_or_else(|| "bimcraft-cli 0.11.0".into());

    let elements_verified = bimcraft_model_path.exists()
        && fs::metadata(&bimcraft_model_path).map(|m| m.len() > 1000).unwrap_or(false)
        && ifc_path.exists()
        && glb_path.exists()
        && svg_plan_path.exists();

    Ok(BimcraftBuildArtifacts {
        variant_id: variant.variant_id.clone(),
        script_path: script_path.to_string_lossy().to_string(),
        bimcraft_model_path: bimcraft_model_path.to_string_lossy().to_string(),
        ifc_path: ifc_path.to_string_lossy().to_string(),
        glb_path: glb_path.to_string_lossy().to_string(),
        svg_plan_path: svg_plan_path.to_string_lossy().to_string(),
        pdf_sheet_path: pdf_sheet_path.to_string_lossy().to_string(),
        cli_version: ver_out,
        inspection_raw,
        elements_verified,
        build_duration_ms: t0.elapsed().as_millis(),
    })
}
