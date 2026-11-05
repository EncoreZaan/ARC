use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::collections::HashMap;
use std::time::Instant;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DiffElement {
    pub id: String,
    pub category: String,
    pub name: String,
    pub status: String, // "Demolished", "Created", "Preserved", "Modified"
    pub color_hex: String,
    pub x1: f64,
    pub y1: f64,
    pub x2: f64,
    pub y2: f64,
    pub thickness: f64,
    pub length_m: f64,
}

fn parse_line_element(val: &Value) -> Option<(String, String, String, f64, f64, f64, f64, f64)> {
    let id = val.get("id")
        .or_else(|| val.get("elementId"))
        .and_then(|v| v.as_str().map(|s| s.to_string()).or_else(|| v.as_i64().map(|n| n.to_string())))?;

    let cat = val.get("category").and_then(|v| v.as_str()).unwrap_or("Walls").to_string();
    let name = val.get("name").and_then(|v| v.as_str()).unwrap_or("Mur").to_string();

    let x1 = val.get("x1").and_then(|v| v.as_f64()).unwrap_or(0.0);
    let y1 = val.get("y1").and_then(|v| v.as_f64()).unwrap_or(0.0);
    let x2 = val.get("x2").and_then(|v| v.as_f64()).unwrap_or(x1 + 1.0);
    let y2 = val.get("y2").and_then(|v| v.as_f64()).unwrap_or(y1);
    let th = val.get("thickness").and_then(|v| v.as_f64()).unwrap_or(0.15);

    Some((id, cat, name, x1, y1, x2, y2, th))
}

/// Moteur de Comparaison Graphique de Variantes (Plan Démolition/Création Rouge-Vert)
pub fn compare_variants_visual(input: &Value) -> Value {
    let t0 = Instant::now();

    let view_title = input.get("viewTitle").and_then(|v| v.as_str()).unwrap_or("Plan de Démolition & Création");
    let existing_raw = input.get("existingElements").and_then(|v| v.as_array()).cloned().unwrap_or_default();
    let project_raw = input.get("projectElements").and_then(|v| v.as_array()).cloned().unwrap_or_default();

    let mut existing_map = HashMap::new();
    for el in &existing_raw {
        if let Some(parsed) = parse_line_element(el) {
            existing_map.insert(parsed.0.clone(), parsed);
        }
    }

    let mut project_map = HashMap::new();
    for el in &project_raw {
        if let Some(parsed) = parse_line_element(el) {
            project_map.insert(parsed.0.clone(), parsed);
        }
    }

    // Données d'exemple représentatives si entrées vides
    if existing_map.is_empty() && project_map.is_empty() {
        existing_map.insert("W-EXT-01".into(), ("W-EXT-01".into(), "Walls".into(), "Mur Porteur Sud".into(), 0.0, 0.0, 8.0, 0.0, 0.30));
        existing_map.insert("W-EXT-02".into(), ("W-EXT-02".into(), "Walls".into(), "Mur Porteur Nord".into(), 0.0, 6.0, 8.0, 6.0, 0.30));
        existing_map.insert("W-DEMO-01".into(), ("W-DEMO-01".into(), "Walls".into(), "Ancienne Cloison Cuisine".into(), 2.8, 0.0, 2.8, 4.0, 0.10));
        existing_map.insert("W-DEMO-02".into(), ("W-DEMO-02".into(), "Walls".into(), "Ancien Couloir".into(), 2.8, 4.0, 0.0, 4.0, 0.10));

        project_map.insert("W-EXT-01".into(), ("W-EXT-01".into(), "Walls".into(), "Mur Porteur Sud".into(), 0.0, 0.0, 8.0, 0.0, 0.30));
        project_map.insert("W-EXT-02".into(), ("W-EXT-02".into(), "Walls".into(), "Mur Porteur Nord".into(), 0.0, 6.0, 8.0, 6.0, 0.30));
        project_map.insert("W-NEW-01".into(), ("W-NEW-01".into(), "Walls".into(), "Nouvelle Cloison Dressing".into(), 5.2, 0.0, 5.2, 3.5, 0.10));
        project_map.insert("W-NEW-02".into(), ("W-NEW-02".into(), "Walls".into(), "Verrière Atelier".into(), 3.5, 2.0, 3.5, 6.0, 0.08));
    }

    let mut diff_elements: Vec<DiffElement> = Vec::new();
    let mut linear_demolished_m = 0.0;
    let mut linear_created_m = 0.0;
    let mut linear_preserved_m = 0.0;

    // 1. Examiner les éléments existants
    for (id, ex) in &existing_map {
        let dx = ex.5 - ex.3;
        let dy = ex.6 - ex.4;
        let len = (dx * dx + dy * dy).sqrt();

        if let Some(prj) = project_map.get(id) {
            let pos_diff = ((ex.3 - prj.3).powi(2) + (ex.4 - prj.4).powi(2) + (ex.5 - prj.5).powi(2) + (ex.6 - prj.6).powi(2)).sqrt();

            if pos_diff < 0.05 {
                linear_preserved_m += len;
                diff_elements.push(DiffElement {
                    id: id.clone(),
                    category: ex.1.clone(),
                    name: ex.2.clone(),
                    status: "Preserved".into(),
                    color_hex: "#BDBDBD".into(),
                    x1: ex.3,
                    y1: ex.4,
                    x2: ex.5,
                    y2: ex.6,
                    thickness: ex.7,
                    length_m: (len * 100.0).round() / 100.0,
                });
            } else {
                linear_demolished_m += len;
                diff_elements.push(DiffElement {
                    id: id.clone(),
                    category: ex.1.clone(),
                    name: ex.2.clone(),
                    status: "Modified".into(),
                    color_hex: "#FB8C00".into(),
                    x1: ex.3,
                    y1: ex.4,
                    x2: ex.5,
                    y2: ex.6,
                    thickness: ex.7,
                    length_m: (len * 100.0).round() / 100.0,
                });
            }
        } else {
            linear_demolished_m += len;
            diff_elements.push(DiffElement {
                id: id.clone(),
                category: ex.1.clone(),
                name: ex.2.clone(),
                status: "Demolished".into(),
                color_hex: "#E53935".into(),
                x1: ex.3,
                y1: ex.4,
                x2: ex.5,
                y2: ex.6,
                thickness: ex.7,
                length_m: (len * 100.0).round() / 100.0,
            });
        }
    }

    // 2. Examiner les nouveaux éléments dans le projet
    for (id, prj) in &project_map {
        if !existing_map.contains_key(id) {
            let dx = prj.5 - prj.3;
            let dy = prj.6 - prj.4;
            let len = (dx * dx + dy * dy).sqrt();
            linear_created_m += len;

            diff_elements.push(DiffElement {
                id: id.clone(),
                category: prj.1.clone(),
                name: prj.2.clone(),
                status: "Created".into(),
                color_hex: "#43A047".into(),
                x1: prj.3,
                y1: prj.4,
                x2: prj.5,
                y2: prj.6,
                thickness: prj.7,
                length_m: (len * 100.0).round() / 100.0,
            });
        }
    }

    // Génération du SVG vectoriel de superposition
    let svg_scale = 80.0;
    let pad = 60.0;
    let svg_w = 900.0;
    let svg_h = 650.0;

    let mut svg = format!(
        "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {:.0} {:.0}\" width=\"100%\" height=\"100%\" style=\"background:#121015; font-family:'Segoe UI',sans-serif;\">",
        svg_w, svg_h
    );

    svg.push_str(&format!(
        "<text x=\"{:.0}\" y=\"35\" fill=\"#F6F4EF\" font-size=\"18\" font-weight=\"bold\">{}</text>",
        pad, view_title
    ));
    svg.push_str(&format!(
        "<text x=\"{:.0}\" y=\"52\" fill=\"#D7B98C\" font-size=\"12\">ARC Diff Visual Engine | Demolition (Rouge) / Creation (Vert) / Conserve (Gris)</text>",
        pad
    ));

    // Légende
    svg.push_str("<g transform=\"translate(60, 580)\">");
    svg.push_str("<rect x=\"0\" y=\"0\" width=\"16\" height=\"6\" fill=\"#E53935\" stroke=\"#E53935\" stroke-dasharray=\"3,2\"/>");
    svg.push_str("<text x=\"25\" y=\"7\" fill=\"#E53935\" font-size=\"12\">Demoli / Depose</text>");
    svg.push_str("<rect x=\"180\" y=\"0\" width=\"16\" height=\"6\" fill=\"#43A047\"/>");
    svg.push_str("<text x=\"205\" y=\"7\" fill=\"#43A047\" font-size=\"12\">Cree / Neuf</text>");
    svg.push_str("<rect x=\"340\" y=\"0\" width=\"16\" height=\"6\" fill=\"#BDBDBD\"/>");
    svg.push_str("<text x=\"365\" y=\"7\" fill=\"#BDBDBD\" font-size=\"12\">Conserve</text>");
    svg.push_str("</g>");

    // Lignes de murs
    for el in &diff_elements {
        let sx1 = pad + el.x1 * svg_scale;
        let sy1 = pad + 60.0 + el.y1 * svg_scale;
        let sx2 = pad + el.x2 * svg_scale;
        let sy2 = pad + 60.0 + el.y2 * svg_scale;
        let stroke_w = (el.thickness * svg_scale).max(4.0);

        let dash = if el.status == "Demolished" { "stroke-dasharray=\"6,4\"" } else { "" };
        svg.push_str(&format!(
            "<line x1=\"{:.1}\" y1=\"{:.1}\" x2=\"{:.1}\" y2=\"{:.1}\" stroke=\"{}\" stroke-width=\"{:.1}\" stroke-linecap=\"round\" {} opacity=\"0.9\"/>",
            sx1, sy1, sx2, sy2, el.color_hex, stroke_w, dash
        ));
    }

    svg.push_str("</svg>");

    let elapsed_ms = t0.elapsed().as_millis();

    json!({
        "success": true,
        "engine": "ARC Visual Demolition & Construction Comparator (Rust)",
        "comparisonTimeMs": elapsed_ms,
        "metrics": {
            "linearDemolishedMeters": (linear_demolished_m * 100.0).round() / 100.0,
            "linearCreatedMeters": (linear_created_m * 100.0).round() / 100.0,
            "linearPreservedMeters": (linear_preserved_m * 100.0).round() / 100.0,
            "demolitionRatePercent": ((linear_demolished_m / (linear_demolished_m + linear_preserved_m).max(0.1)) * 1000.0).round() / 10.0
        },
        "elementsDiffCount": {
            "demolished": diff_elements.iter().filter(|e| e.status == "Demolished").count(),
            "created": diff_elements.iter().filter(|e| e.status == "Created").count(),
            "preserved": diff_elements.iter().filter(|e| e.status == "Preserved").count(),
            "modified": diff_elements.iter().filter(|e| e.status == "Modified").count()
        },
        "diffElements": diff_elements,
        "vectorSvgVisualPlan": svg
    })
}
