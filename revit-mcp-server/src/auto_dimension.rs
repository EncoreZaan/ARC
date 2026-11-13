use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::time::Instant;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DimensionSegment {
    pub start: [f64; 2],
    pub end: [f64; 2],
    pub length_mm: f64,
    pub label: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DimensionChain {
    pub chain_type: String, // "OpeningsAndPiers", "InteriorClearance", "OverallTotal"
    pub orientation: String, // "Horizontal", "Vertical"
    pub offset_m: f64,
    pub segments: Vec<DimensionSegment>,
    pub total_length_mm: f64,
}

/// Moteur de Cotation Automatique Intelligente des Plans en Rust natif
pub fn generate_auto_dimensions(input: &Value) -> Value {
    let t0 = Instant::now();

    let offset_base = input.get("offsetBaseMeters").and_then(|v| v.as_f64()).unwrap_or(0.80);
    let offset_spacing = input.get("offsetSpacingMeters").and_then(|v| v.as_f64()).unwrap_or(0.50);

    // Extraction des murs et des ouvertures
    let mut x_points: Vec<f64> = Vec::new();
    let mut y_points: Vec<f64> = Vec::new();

    if let Some(walls_arr) = input.get("walls").and_then(|v| v.as_array()) {
        for w in walls_arr {
            if let (Some(x1), Some(y1), Some(x2), Some(y2)) = (
                w.get("x1").and_then(|v| v.as_f64()),
                w.get("y1").and_then(|v| v.as_f64()),
                w.get("x2").and_then(|v| v.as_f64()),
                w.get("y2").and_then(|v| v.as_f64()),
            ) {
                x_points.push(x1);
                x_points.push(x2);
                y_points.push(y1);
                y_points.push(y2);
            }
        }
    }

    if let Some(openings_arr) = input.get("openings").and_then(|v| v.as_array()) {
        for op in openings_arr {
            let x = op.get("x").and_then(|v| v.as_f64()).unwrap_or(0.0);
            let y = op.get("y").and_then(|v| v.as_f64()).unwrap_or(0.0);
            let w = op.get("width").and_then(|v| v.as_f64()).unwrap_or(0.90);
            x_points.push(x - w / 2.0);
            x_points.push(x + w / 2.0);
            y_points.push(y - w / 2.0);
            y_points.push(y + w / 2.0);
        }
    }

    // Déduplication et tri avec tolérance (5 cm)
    x_points.sort_by(|a, b| a.partial_cmp(b).unwrap_or(std::cmp::Ordering::Equal));
    y_points.sort_by(|a, b| a.partial_cmp(b).unwrap_or(std::cmp::Ordering::Equal));

    let mut clean_x: Vec<f64> = Vec::new();
    for x in x_points {
        if clean_x.is_empty() || clean_x.last().is_none_or(|last_x| (last_x - x).abs() > 0.05) {
            clean_x.push((x * 100.0).round() / 100.0);
        }
    }

    let mut clean_y: Vec<f64> = Vec::new();
    for y in y_points {
        if clean_y.is_empty() || clean_y.last().is_none_or(|last_y| (last_y - y).abs() > 0.05) {
            clean_y.push((y * 100.0).round() / 100.0);
        }
    }

    // Si aucune géométrie transmise, générer une cotation type salon londonien
    if clean_x.len() < 2 {
        clean_x = vec![0.0, 1.20, 2.10, 4.50, 6.20];
    }
    if clean_y.len() < 2 {
        clean_y = vec![0.0, 1.80, 2.70, 5.00];
    }

    let min_y = *clean_y.first().unwrap_or(&0.0);
    let min_x = *clean_x.first().unwrap_or(&0.0);

    let mut chains: Vec<DimensionChain> = Vec::new();

    // 1. Ligne Horizontale 1 : Cotes de détails (ouvertures & trumeaux)
    let mut segs_h1 = Vec::new();
    let mut total_h1 = 0.0;
    for i in 0..clean_x.len() - 1 {
        let x1 = clean_x[i];
        let x2 = clean_x[i + 1];
        let len_mm = ((x2 - x1) * 1000.0).round();
        total_h1 += len_mm;
        segs_h1.push(DimensionSegment {
            start: [x1, min_y - offset_base],
            end: [x2, min_y - offset_base],
            length_mm: len_mm,
            label: format!("{:.0}", len_mm),
        });
    }
    chains.push(DimensionChain {
        chain_type: "OpeningsAndPiers".into(),
        orientation: "Horizontal".into(),
        offset_m: offset_base,
        segments: segs_h1,
        total_length_mm: total_h1,
    });

    // 2. Ligne Horizontale 2 : Cote générale hors-tout
    let x_first = *clean_x.first().unwrap_or(&0.0);
    let x_last = *clean_x.last().unwrap_or(&x_first);
    let total_h_mm = ((x_last - x_first) * 1000.0).round();
    chains.push(DimensionChain {
        chain_type: "OverallTotal".into(),
        orientation: "Horizontal".into(),
        offset_m: offset_base + offset_spacing,
        segments: vec![DimensionSegment {
            start: [x_first, min_y - (offset_base + offset_spacing)],
            end: [x_last, min_y - (offset_base + offset_spacing)],
            length_mm: total_h_mm,
            label: format!("{:.0}", total_h_mm),
        }],
        total_length_mm: total_h_mm,
    });

    // 3. Ligne Verticale 1 : Cotes de détails Ouest
    let mut segs_v1 = Vec::new();
    let mut total_v1 = 0.0;
    for i in 0..clean_y.len() - 1 {
        let y1 = clean_y[i];
        let y2 = clean_y[i + 1];
        let len_mm = ((y2 - y1) * 1000.0).round();
        total_v1 += len_mm;
        segs_v1.push(DimensionSegment {
            start: [min_x - offset_base, y1],
            end: [min_x - offset_base, y2],
            length_mm: len_mm,
            label: format!("{:.0}", len_mm),
        });
    }
    chains.push(DimensionChain {
        chain_type: "OpeningsAndPiers".into(),
        orientation: "Vertical".into(),
        offset_m: offset_base,
        segments: segs_v1,
        total_length_mm: total_v1,
    });

    // 4. Ligne Verticale 2 : Cote générale hors-tout
    let y_first = *clean_y.first().unwrap_or(&0.0);
    let y_last = *clean_y.last().unwrap_or(&y_first);
    let total_v_mm = ((y_last - y_first) * 1000.0).round();
    chains.push(DimensionChain {
        chain_type: "OverallTotal".into(),
        orientation: "Vertical".into(),
        offset_m: offset_base + offset_spacing,
        segments: vec![DimensionSegment {
            start: [min_x - (offset_base + offset_spacing), y_first],
            end: [min_x - (offset_base + offset_spacing), y_last],
            length_mm: total_v_mm,
            label: format!("{:.0}", total_v_mm),
        }],
        total_length_mm: total_v_mm,
    });

    let elapsed_us = t0.elapsed().as_micros();

    json!({
        "success": true,
        "engine": "ARC Architectural Auto-Dimensioning Engine (Rust)",
        "calculationTimeUs": elapsed_us,
        "unit": "Millimeters (mm)",
        "summary": {
            "totalChains": chains.len(),
            "totalSegments": chains.iter().map(|c| c.segments.len()).sum::<usize>(),
            "horizontalExtentMm": total_h_mm,
            "verticalExtentMm": total_v_mm
        },
        "dimensionChains": chains,
        "revitTransactionPlan": {
            "method": "revit_create_linear_dimension",
            "chainsCount": chains.len(),
            "readyToCommit": true
        }
    })
}
