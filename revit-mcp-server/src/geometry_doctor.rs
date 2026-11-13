use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::time::Instant;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GeometryAnomaly {
    pub element_id: String,
    pub anomaly_type: String, // "OffAxisAngle", "MicroGap", "MicroSegment", "OverlappingDuplicate"
    pub severity: String,     // "Warning", "Error", "Info"
    pub original_value: String,
    pub corrected_value: String,
    pub auto_healed: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HealedWall {
    pub id: String,
    pub original_start: [f64; 2],
    pub original_end: [f64; 2],
    pub healed_start: [f64; 2],
    pub healed_end: [f64; 2],
    pub angle_deg: f64,
}

/// Docteur Géométrique & Nettoyeur d'Avertissements Revit/BIM en Rust natif
pub fn diagnose_and_heal_geometry(input: &Value) -> Value {
    let t0 = Instant::now();

    let angle_tol_deg = input.get("angleToleranceDeg").and_then(|v| v.as_f64()).unwrap_or(0.85);
    let gap_tol_m = input.get("gapToleranceMm").and_then(|v| v.as_f64()).unwrap_or(25.0) / 1000.0;

    let mut walls_to_check = Vec::new();
    if let Some(arr) = input.get("walls").and_then(|v| v.as_array()) {
        for w in arr {
            let id = w.get("id").or_else(|| w.get("elementId")).and_then(|v| v.as_str().map(|s| s.to_string()).or_else(|| v.as_i64().map(|n| n.to_string()))).unwrap_or("W-01".into());
            let x1 = w.get("x1").and_then(|v| v.as_f64()).unwrap_or(0.0);
            let y1 = w.get("y1").and_then(|v| v.as_f64()).unwrap_or(0.0);
            let x2 = w.get("x2").and_then(|v| v.as_f64()).unwrap_or(x1 + 2.0);
            let y2 = w.get("y2").and_then(|v| v.as_f64()).unwrap_or(y1);
            walls_to_check.push((id, [x1, y1], [x2, y2]));
        }
    } else {
        walls_to_check.push(("WALL-ANOMALY-01".into(), [0.0, 0.0], [5.0, 0.025]));
        walls_to_check.push(("WALL-ANOMALY-02".into(), [5.012, 0.0], [5.012, 4.0]));
        walls_to_check.push(("WALL-ANOMALY-03".into(), [5.012, 4.0], [0.0, 4.0]));
        walls_to_check.push(("WALL-MICRO-04".into(), [2.0, 2.0], [2.02, 2.01]));
    }

    let mut anomalies: Vec<GeometryAnomaly> = Vec::new();
    let mut healed_walls: Vec<HealedWall> = Vec::new();
    let mut elements_to_delete: Vec<String> = Vec::new();

    for (id, mut p1, mut p2) in walls_to_check {
        let dx = p2[0] - p1[0];
        let dy = p2[1] - p1[1];
        let len = (dx * dx + dy * dy).sqrt();

        // 1. Détection de micro-segments parasites (< 50mm)
        if len < 0.05 {
            anomalies.push(GeometryAnomaly {
                element_id: id.clone(),
                anomaly_type: "MicroSegment".into(),
                severity: "Warning".into(),
                original_value: format!("Longueur = {:.1} mm", len * 1000.0),
                corrected_value: "Marqué pour suppression immédiate".into(),
                auto_healed: true,
            });
            elements_to_delete.push(id.clone());
            continue;
        }

        let angle_rad = dy.atan2(dx);
        let mut angle_deg = angle_rad.to_degrees().abs() % 180.0;

        // 2. Détection de léger hors-axe orthogonal (proche de 0° ou 180°)
        if angle_deg < angle_tol_deg || (180.0 - angle_deg) < angle_tol_deg {
            if angle_deg > 0.001 && (180.0 - angle_deg) > 0.001 {
                let avg_y = (p1[1] + p2[1]) / 2.0;
                let orig_angle = angle_deg;
                p1[1] = avg_y;
                p2[1] = avg_y;
                angle_deg = 0.0;

                anomalies.push(GeometryAnomaly {
                    element_id: id.clone(),
                    anomaly_type: "OffAxisAngle".into(),
                    severity: "Warning".into(),
                    original_value: format!("{:.2}°", orig_angle),
                    corrected_value: "Parfaitement horizontalisé à 0.00°".into(),
                    auto_healed: true,
                });
            }
        } else if (angle_deg - 90.0).abs() < angle_tol_deg
            && (angle_deg - 90.0).abs() > 0.001 {
                let avg_x = (p1[0] + p2[0]) / 2.0;
                let orig_angle = angle_deg;
                p1[0] = avg_x;
                p2[0] = avg_x;
                angle_deg = 90.0;

                anomalies.push(GeometryAnomaly {
                    element_id: id.clone(),
                    anomaly_type: "OffAxisAngle".into(),
                    severity: "Warning".into(),
                    original_value: format!("{:.2}°", orig_angle),
                    corrected_value: "Parfaitement verticalisé à 90.00°".into(),
                    auto_healed: true,
                });
            }

        healed_walls.push(HealedWall {
            id,
            original_start: [dx, dy],
            original_end: [p2[0], p2[1]],
            healed_start: p1,
            healed_end: p2,
            angle_deg: (angle_deg * 100.0).round() / 100.0,
        });
    }

    // 3. Détection des micro-gaps entre extrémités de murs consécutifs
    for i in 0..healed_walls.len() {
        for j in (i + 1)..healed_walls.len() {
            let p1_end = healed_walls[i].healed_end;
            let p2_start = healed_walls[j].healed_start;
            let dist = ((p1_end[0] - p2_start[0]).powi(2) + (p1_end[1] - p2_start[1]).powi(2)).sqrt();

            if dist > 0.001 && dist <= gap_tol_m {
                let snap_x = (p1_end[0] + p2_start[0]) / 2.0;
                let snap_y = (p1_end[1] + p2_start[1]) / 2.0;
                healed_walls[i].healed_end = [snap_x, snap_y];
                healed_walls[j].healed_start = [snap_x, snap_y];

                anomalies.push(GeometryAnomaly {
                    element_id: format!("{} <-> {}", healed_walls[i].id, healed_walls[j].id),
                    anomaly_type: "MicroGap".into(),
                    severity: "Error".into(),
                    original_value: format!("Écart = {:.1} mm", dist * 1000.0),
                    corrected_value: "Jonction fusionnée par magnétisme".into(),
                    auto_healed: true,
                });
            }
        }
    }

    let initial_anomalies_count = anomalies.len();
    let health_score = ((1.0 - (initial_anomalies_count as f64 / (healed_walls.len() + 1) as f64).min(1.0)) * 100.0).round();
    let elapsed_us = t0.elapsed().as_micros();

    json!({
        "success": true,
        "engine": "ARC Geometry Doctor & Wall Cleaner (Rust)",
        "diagnosticsTimeUs": elapsed_us,
        "modelHealthScore": health_score,
        "summary": {
            "totalWallsInspected": healed_walls.len() + elements_to_delete.len(),
            "anomaliesDetected": initial_anomalies_count,
            "anomaliesAutoHealed": anomalies.iter().filter(|a| a.auto_healed).count(),
            "microSegmentsMarkedForDeletion": elements_to_delete.len()
        },
        "anomaliesList": anomalies,
        "healingTransaction": {
            "updateWallCoordinatesCount": healed_walls.len(),
            "deleteElementIds": elements_to_delete,
            "readyToCommit": true
        }
    })
}
