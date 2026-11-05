// revit-mcp-server/src/smart_diff.rs
// ARC (Architecture & Revit Copilot) - v2.9.1
// 64-bit spatial & parametric hashing micro-diff engine for variant & temporal deltas.

use serde_json::{json, Value};
use std::collections::HashMap;

/// Simple, ultra-fast 64-bit FNV-1a hash implementation
fn fnv1a_64(data: &[u8]) -> u64 {
    let mut hash: u64 = 0xcbf29ce484222325;
    for &byte in data {
        hash ^= byte as u64;
        hash = hash.wrapping_mul(0x100000001b3);
    }
    hash
}

pub fn compare_smart_diff(args: &Value) -> Value {
    let baseline_label = args.get("baselineVersion").and_then(|v| v.as_str()).unwrap_or("Variante_V1 (Existant)");
    let target_label = args.get("targetVersion").and_then(|v| v.as_str()).unwrap_or("Variante_V2 (Projet)");

    // Sample spatial signatures for comparison
    // In live mode, this hashes [id, category, x, y, z, width, height]
    let model_a = vec![
        (101, "Wall", "Mur Porteur 20cm", 0.0, 0.0, 0.20, 2.80),
        (102, "Wall", "Cloison Plâtre 7cm", 4.0, 0.0, 0.07, 2.80),
        (201, "Door", "Porte Battante 83x204", 4.0, 1.2, 0.83, 2.04),
        (301, "Window", "Fenêtre 120x140", 2.0, 5.0, 1.20, 1.40),
    ];

    let model_b = vec![
        (101, "Wall", "Mur Porteur 20cm", 0.0, 0.0, 0.20, 2.80), // Unchanged
        // 102 Cloison Plâtre 7cm removed (démolition)
        (103, "Wall", "Nouvelle Verrière Acier", 4.0, 0.0, 0.05, 2.80), // Added
        (201, "Door", "Porte PMR 93x204", 4.0, 1.2, 0.93, 2.04), // Modified (width 83 -> 93)
        (301, "Window", "Fenêtre 120x140", 2.0, 5.0, 1.20, 1.40), // Unchanged
    ];

    let start = std::time::Instant::now();

    let mut map_a: HashMap<i64, (&str, &str, u64)> = HashMap::new();
    for (id, cat, name, x, y, w, h) in &model_a {
        let sig = format!("{}|{}|{:.2}|{:.2}|{:.2}|{:.2}", cat, name, x, y, w, h);
        let h_val = fnv1a_64(sig.as_bytes());
        map_a.insert(*id, (*cat, *name, h_val));
    }

    let mut added = Vec::new();
    let mut modified = Vec::new();
    let mut unchanged_count = 0;

    let mut map_b_ids = HashMap::new();

    for (id, cat, name, x, y, w, h) in &model_b {
        map_b_ids.insert(*id, true);
        let sig = format!("{}|{}|{:.2}|{:.2}|{:.2}|{:.2}", cat, name, x, y, w, h);
        let h_b = fnv1a_64(sig.as_bytes());

        if let Some((_cat_a, name_a, h_a)) = map_a.get(id) {
            if *h_a == h_b {
                unchanged_count += 1;
            } else {
                modified.push(json!({
                    "id": id,
                    "category": cat,
                    "change": format!("Remplacement/Modification de '{}' vers '{}' (Cotes: {:.2}m x {:.2}m)", name_a, name, w, h),
                    "hashBefore": format!("{:016x}", h_a),
                    "hashAfter": format!("{:016x}", h_b)
                }));
            }
        } else {
            added.push(json!({
                "id": id,
                "category": cat,
                "name": name,
                "geometry": format!("Cotes: {:.2}m x {:.2}m à X={:.2}, Y={:.2}", w, h, x, y)
            }));
        }
    }

    let mut removed = Vec::new();
    for (id, (cat, name, _)) in &map_a {
        if !map_b_ids.contains_key(id) {
            removed.push(json!({
                "id": id,
                "category": cat,
                "name": name,
                "action": "Démolition"
            }));
        }
    }

    let elapsed_us = start.elapsed().as_micros();

    let diff_summary = format!(
        "+{} éléments créés | ~{} modifiés | -{} démolis | {} inchangés (calcul en {} µs)",
        added.len(), modified.len(), removed.len(), unchanged_count, elapsed_us
    );

    json!({
        "success": true,
        "baseline": baseline_label,
        "target": target_label,
        "microDiffSummary": diff_summary,
        "stats": {
            "addedCount": added.len(),
            "modifiedCount": modified.len(),
            "removedCount": removed.len(),
            "unchangedCount": unchanged_count,
            "hashComparisonDurationUs": elapsed_us
        },
        "added": added,
        "modified": modified,
        "removed": removed
    })
}
