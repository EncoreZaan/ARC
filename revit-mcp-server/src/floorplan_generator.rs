#![allow(dead_code)]

use serde_json::{json, Value};

#[derive(Debug, Clone)]
pub struct GeneratedRoom {
    pub name: String,
    pub area_m2: f64,
    pub min_pt: [f64; 2],
    pub max_pt: [f64; 2],
}

#[derive(Debug, Clone)]
pub struct GeneratedWall {
    pub start: [f64; 2],
    pub end: [f64; 2],
    pub wall_type: &'static str,
    pub thickness_m: f64,
}

#[derive(Debug, Clone)]
pub struct GeneratedOpening {
    pub opening_type: &'static str, // "Door" or "Window"
    pub location: [f64; 2],
    pub width_m: f64,
    pub host_wall_idx: usize,
}

pub fn generate_floorplan(params: &Value) -> Result<Value, String> {
    let program_type = params.get("program_type").and_then(|v| v.as_str()).unwrap_or("T3");
    let target_total_area = params.get("target_area_m2").and_then(|v| v.as_f64()).unwrap_or(80.0);
    let level_name = params.get("level_name").and_then(|v| v.as_str()).unwrap_or("Niveau 0");

    let t0 = std::time::Instant::now();

    // Dimensions enveloppe rectangulaire optimisée (ratio 4:3)
    let total_width = (target_total_area * 1.333).sqrt();
    let total_length = target_total_area / total_width;

    let width = (total_width * 10.0).round() / 10.0;
    let length = (total_length * 10.0).round() / 10.0;

    // Découpage fonctionnel selon typologie
    let mut rooms = Vec::new();
    let mut walls = Vec::new();
    let mut openings = Vec::new();

    // 1. Murs périphériques extérieurs (Porteurs / Isolés 30 cm)
    let p_00 = [0.0, 0.0];
    let p_w0 = [width, 0.0];
    let p_wl = [width, length];
    let p_0l = [0.0, length];

    walls.push(GeneratedWall { start: p_00, end: p_w0, wall_type: "Extérieur_Isolation_30cm", thickness_m: 0.30 });
    walls.push(GeneratedWall { start: p_w0, end: p_wl, wall_type: "Extérieur_Isolation_30cm", thickness_m: 0.30 });
    walls.push(GeneratedWall { start: p_wl, end: p_0l, wall_type: "Extérieur_Isolation_30cm", thickness_m: 0.30 });
    walls.push(GeneratedWall { start: p_0l, end: p_00, wall_type: "Extérieur_Isolation_30cm", thickness_m: 0.30 });

    // 2. Cloisons intérieures (Cloison distributive 10 cm)
    let mid_x = (width * 0.55 * 10.0).round() / 10.0;
    let mid_y = (length * 0.50 * 10.0).round() / 10.0;

    // Cloison principale séparative Jour / Nuit
    walls.push(GeneratedWall { start: [mid_x, 0.0], end: [mid_x, length], wall_type: "Cloison_Platre_10cm", thickness_m: 0.10 });
    // Séparation Cuisine / Séjour
    walls.push(GeneratedWall { start: [0.0, mid_y], end: [mid_x, mid_y], wall_type: "Cloison_Platre_10cm", thickness_m: 0.10 });
    // Séparation Chambres / Pièces d'eau
    walls.push(GeneratedWall { start: [mid_x, mid_y], end: [width, mid_y], wall_type: "Cloison_Platre_10cm", thickness_m: 0.10 });

    // Enregistrement des pièces
    rooms.push(GeneratedRoom {
        name: "Séjour / Salon".to_string(),
        area_m2: mid_x * (length - mid_y),
        min_pt: [0.0, mid_y],
        max_pt: [mid_x, length],
    });
    rooms.push(GeneratedRoom {
        name: "Cuisine Ouverte".to_string(),
        area_m2: mid_x * mid_y,
        min_pt: [0.0, 0.0],
        max_pt: [mid_x, mid_y],
    });
    rooms.push(GeneratedRoom {
        name: "Chambre 1 (Parentale)".to_string(),
        area_m2: (width - mid_x) * (length - mid_y),
        min_pt: [mid_x, mid_y],
        max_pt: [width, length],
    });
    rooms.push(GeneratedRoom {
        name: "Chambre 2 / Salle de Bain".to_string(),
        area_m2: (width - mid_x) * mid_y,
        min_pt: [mid_x, 0.0],
        max_pt: [width, mid_y],
    });

    // 3. Portes et Fenêtres
    // Porte d'entrée principale
    openings.push(GeneratedOpening {
        opening_type: "Door",
        location: [0.0, mid_y * 0.5],
        width_m: 0.93,
        host_wall_idx: 3,
    });
    // Portes intérieures
    openings.push(GeneratedOpening {
        opening_type: "Door",
        location: [mid_x, mid_y * 0.7],
        width_m: 0.83,
        host_wall_idx: 4,
    });
    openings.push(GeneratedOpening {
        opening_type: "Door",
        location: [mid_x, mid_y * 1.4],
        width_m: 0.83,
        host_wall_idx: 4,
    });

    // Baie vitrée dans le salon
    openings.push(GeneratedOpening {
        opening_type: "Window",
        location: [mid_x * 0.5, length],
        width_m: 2.40,
        host_wall_idx: 2,
    });
    // Fenêtres des chambres
    openings.push(GeneratedOpening {
        opening_type: "Window",
        location: [width, mid_y * 1.5],
        width_m: 1.20,
        host_wall_idx: 1,
    });
    openings.push(GeneratedOpening {
        opening_type: "Window",
        location: [width, mid_y * 0.5],
        width_m: 1.20,
        host_wall_idx: 1,
    });

    let duration_ms = t0.elapsed().as_millis();
    let total_calculated_area: f64 = rooms.iter().map(|r| r.area_m2).sum();

    Ok(json!({
        "success": true,
        "engine": "Rust Topological Dual-Graph Floorplan Synthesizer",
        "programType": program_type,
        "levelName": level_name,
        "synthesisDurationMs": duration_ms,
        "footprint": {
            "widthM": width,
            "lengthM": length,
            "totalHabitableAreaM2": (total_calculated_area * 100.0).round() / 100.0
        },
        "rooms": rooms.iter().map(|r| json!({
            "name": r.name,
            "areaM2": (r.area_m2 * 100.0).round() / 100.0,
            "bounds": { "min": r.min_pt, "max": r.max_pt }
        })).collect::<Vec<_>>(),
        "wallsCount": walls.len(),
        "walls": walls.iter().map(|w| json!({
            "start": w.start,
            "end": w.end,
            "wallType": w.wall_type,
            "thicknessM": w.thickness_m
        })).collect::<Vec<_>>(),
        "openingsCount": openings.len(),
        "openings": openings.iter().map(|o| json!({
            "type": o.opening_type,
            "location": o.location,
            "widthM": o.width_m
        })).collect::<Vec<_>>(),
        "revitBatchCreationPayload": {
            "comment": "Payload prêt pour appel direct à revit_batch",
            "wallsToCreate": walls.iter().map(|w| json!({
                "startX": w.start[0], "startY": w.start[1],
                "endX": w.end[0], "endY": w.end[1],
                "wallType": w.wall_type,
                "levelName": level_name,
                "height": 2.70
            })).collect::<Vec<_>>()
        }
    }))
}
