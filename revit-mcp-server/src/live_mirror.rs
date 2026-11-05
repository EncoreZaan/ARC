use rayon::prelude::*;
use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::collections::HashMap;
use std::time::Instant;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ElementFingerprint {
    pub id: String,
    pub category: String,
    pub name: String,
    pub x: f64,
    pub y: f64,
    pub z: f64,
    pub width: Option<f64>,
    pub height: Option<f64>,
    pub length: Option<f64>,
    pub parameters: HashMap<String, String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct DifferentialDelta {
    pub change_type: String, // "Added", "Modified", "Deleted", "Identical", "Conflict"
    pub element_id: String,
    pub category: String,
    pub source_data: Option<ElementFingerprint>,
    pub target_data: Option<ElementFingerprint>,
    pub delta_translation: Option<[f64; 3]>,
    pub parameter_diffs: HashMap<String, (String, String)>,
    pub resolution_action: String,
}

fn parse_element(val: &Value) -> Option<ElementFingerprint> {
    let id = val.get("id")
        .or_else(|| val.get("elementId"))
        .or_else(|| val.get("uniqueId"))
        .and_then(|v| v.as_str().map(|s| s.to_string()).or_else(|| v.as_i64().map(|n| n.to_string())))?;

    let category = val.get("category")
        .and_then(|v| v.as_str())
        .unwrap_or("GenericModel")
        .to_string();

    let name = val.get("name")
        .and_then(|v| v.as_str())
        .unwrap_or("Unnamed")
        .to_string();

    let x = val.get("x").and_then(|v| v.as_f64()).unwrap_or(0.0);
    let y = val.get("y").and_then(|v| v.as_f64()).unwrap_or(0.0);
    let z = val.get("z").and_then(|v| v.as_f64()).unwrap_or(0.0);

    let width = val.get("width").and_then(|v| v.as_f64());
    let height = val.get("height").and_then(|v| v.as_f64());
    let length = val.get("length").and_then(|v| v.as_f64());

    let mut parameters = HashMap::new();
    if let Some(params_obj) = val.get("parameters").and_then(|v| v.as_object()) {
        for (k, v) in params_obj {
            let s = match v {
                Value::String(st) => st.clone(),
                Value::Number(num) => num.to_string(),
                Value::Bool(b) => b.to_string(),
                _ => v.to_string(),
            };
            parameters.insert(k.clone(), s);
        }
    }

    Some(ElementFingerprint {
        id,
        category,
        name,
        x,
        y,
        z,
        width,
        height,
        length,
        parameters,
    })
}

/// Moteur de comparaison différentielle et fusion bidirectionnelle Revit <-> BIMcraft
pub fn compute_live_mirror_sync(
    source_val: &[Value],
    target_val: &[Value],
    strategy: &str,
) -> Value {
    let t0 = Instant::now();

    // Analyse parallèle avec rayon
    let source_elements: Vec<ElementFingerprint> = source_val
        .par_iter()
        .filter_map(parse_element)
        .collect();

    let target_elements: Vec<ElementFingerprint> = target_val
        .par_iter()
        .filter_map(parse_element)
        .collect();

    let mut source_map: HashMap<String, ElementFingerprint> = HashMap::with_capacity(source_elements.len());
    for el in source_elements {
        source_map.insert(el.id.clone(), el);
    }

    let mut target_map: HashMap<String, ElementFingerprint> = HashMap::with_capacity(target_elements.len());
    for el in target_elements {
        target_map.insert(el.id.clone(), el);
    }

    let mut deltas: Vec<DifferentialDelta> = Vec::new();
    let mut added_count = 0usize;
    let mut modified_count = 0usize;
    let mut deleted_count = 0usize;
    let conflict_count = 0usize;
    let mut identical_count = 0usize;

    // 1. Examiner les éléments source
    for (id, src) in &source_map {
        if let Some(tgt) = target_map.get(id) {
            // Comparaison spatiale et paramétrique
            let dx = src.x - tgt.x;
            let dy = src.y - tgt.y;
            let dz = src.z - tgt.z;
            let dist = (dx * dx + dy * dy + dz * dz).sqrt();

            let mut diffs: HashMap<String, (String, String)> = HashMap::new();
            for (k, v_src) in &src.parameters {
                if let Some(v_tgt) = tgt.parameters.get(k) {
                    if v_src != v_tgt {
                        diffs.insert(k.clone(), (v_src.clone(), v_tgt.clone()));
                    }
                } else {
                    diffs.insert(k.clone(), (v_src.clone(), "NON_DEFINI".to_string()));
                }
            }

            if dist > 0.001 || !diffs.is_empty() {
                let resolution = match strategy {
                    "SourceWins" => "ApplySourceOverride",
                    "TargetWins" => "PreserveTargetState",
                    "UnionMerge" => "CreateDesignOptionVariant",
                    _ => "SmartGeometricSnap",
                };
                modified_count += 1;
                deltas.push(DifferentialDelta {
                    change_type: "Modified".to_string(),
                    element_id: id.clone(),
                    category: src.category.clone(),
                    source_data: Some(src.clone()),
                    target_data: Some(tgt.clone()),
                    delta_translation: Some([dx, dy, dz]),
                    parameter_diffs: diffs,
                    resolution_action: resolution.to_string(),
                });
            } else {
                identical_count += 1;
            }
        } else {
            // Élément présent dans source, absent dans target -> Ajout à propager
            added_count += 1;
            deltas.push(DifferentialDelta {
                change_type: "Added".to_string(),
                element_id: id.clone(),
                category: src.category.clone(),
                source_data: Some(src.clone()),
                target_data: None,
                delta_translation: None,
                parameter_diffs: HashMap::new(),
                resolution_action: "PushNewElementToTarget".to_string(),
            });
        }
    }

    // 2. Examiner les éléments absents de la source mais présents dans target -> Supprimés
    for (id, tgt) in &target_map {
        if !source_map.contains_key(id) {
            deleted_count += 1;
            deltas.push(DifferentialDelta {
                change_type: "Deleted".to_string(),
                element_id: id.clone(),
                category: tgt.category.clone(),
                source_data: None,
                target_data: Some(tgt.clone()),
                delta_translation: None,
                parameter_diffs: HashMap::new(),
                resolution_action: if strategy == "SourceWins" { "DeleteFromTarget" } else { "RetainInTarget" }.to_string(),
            });
        }
    }

    let elapsed_us = t0.elapsed().as_micros();

    json!({
        "success": true,
        "engine": "ARC Differential Live Mirror Engine (Rust Rayon)",
        "resolutionStrategy": strategy,
        "executionTimeUs": elapsed_us,
        "summary": {
            "sourceTotal": source_map.len(),
            "targetTotal": target_map.len(),
            "addedToTarget": added_count,
            "modified": modified_count,
            "deletedFromTarget": deleted_count,
            "identical": identical_count,
            "conflicts": conflict_count
        },
        "synchronizationPlan": {
            "createQueue": added_count,
            "updateQueue": modified_count,
            "deleteQueue": deleted_count,
            "readyToCommit": true
        },
        "deltas": deltas
    })
}
