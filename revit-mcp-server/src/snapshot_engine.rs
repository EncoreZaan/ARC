// ARC (Architecture & Revit Copilot) - Lightweight In-Memory Snapshot & Time-Travel Engine
// Tracks project deltas, category totals and geometric signatures with sub-microsecond latency.

use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::collections::HashMap;
use std::time::{SystemTime, UNIX_EPOCH};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ModelSnapshot {
    pub id: String,
    pub timestamp_ms: u64,
    pub label: String,
    pub element_count: usize,
    pub categories: HashMap<String, usize>,
    pub hash_signature: String,
}

pub struct SnapshotStore {
    snapshots: Vec<ModelSnapshot>,
}

impl Default for SnapshotStore {
    fn default() -> Self {
        Self::new()
    }
}

impl SnapshotStore {
    pub fn new() -> Self {
        Self { snapshots: Vec::new() }
    }

    pub fn take_snapshot(&mut self, label: &str, elements: &[Value]) -> ModelSnapshot {
        let ts = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap_or_default()
            .as_millis() as u64;

        let mut cat_counts = HashMap::new();
        let mut id_sum: i64 = 0;

        for el in elements {
            let cat = el.get("category").and_then(|v| v.as_str()).unwrap_or("Autre").to_string();
            *cat_counts.entry(cat).or_insert(0) += 1;
            if let Some(id) = el.get("id").and_then(|v| v.as_i64()) {
                id_sum = (id_sum + id) % 1_000_000_007;
            }
        }

        let snap_id = format!("snap_{}_{}", ts, self.snapshots.len() + 1);
        let hash = format!("{:x}_{:x}", id_sum, elements.len());

        let snap = ModelSnapshot {
            id: snap_id,
            timestamp_ms: ts,
            label: label.to_string(),
            element_count: elements.len(),
            categories: cat_counts,
            hash_signature: hash,
        };

        self.snapshots.push(snap.clone());
        if self.snapshots.len() > 20 {
            self.snapshots.remove(0); // Keep max 20 snapshots
        }

        snap
    }

    pub fn list_snapshots(&self) -> Vec<ModelSnapshot> {
        self.snapshots.clone()
    }

    pub fn compare(&self, id_a: Option<&str>, id_b: Option<&str>) -> Value {
        if self.snapshots.is_empty() {
            return json!({ "success": false, "error": "No snapshots recorded in this session" });
        }

        let snap_b = match id_b {
            Some(id) => self.snapshots.iter().find(|s| s.id == id).cloned(),
            None => self.snapshots.last().cloned(),
        };

        let snap_a = match id_a {
            Some(id) => self.snapshots.iter().find(|s| s.id == id).cloned(),
            None => if self.snapshots.len() >= 2 {
                Some(self.snapshots[self.snapshots.len() - 2].clone())
            } else {
                self.snapshots.first().cloned()
            },
        };

        match (snap_a, snap_b) {
            (Some(a), Some(b)) => {
                let delta_elements = (b.element_count as i64) - (a.element_count as i64);
                let mut cat_deltas = HashMap::new();

                for (cat, count_b) in &b.categories {
                    let count_a = a.categories.get(cat).cloned().unwrap_or(0);
                    let diff = (*count_b as i64) - (count_a as i64);
                    if diff != 0 {
                        cat_deltas.insert(cat.clone(), diff);
                    }
                }

                for (cat, count_a) in &a.categories {
                    if !b.categories.contains_key(cat) {
                        cat_deltas.insert(cat.clone(), -(*count_a as i64));
                    }
                }

                json!({
                    "success": true,
                    "engine": "Rust In-Memory Time-Travel Diff Engine",
                    "snapshotA": { "id": a.id, "label": a.label, "elementCount": a.element_count },
                    "snapshotB": { "id": b.id, "label": b.label, "elementCount": b.element_count },
                    "deltaElements": delta_elements,
                    "categoryDeltas": cat_deltas,
                    "identical": a.hash_signature == b.hash_signature,
                    "summary": format!("Diff: {:+} éléments entre [{}] et [{}]", delta_elements, a.label, b.label)
                })
            }
            _ => json!({ "success": false, "error": "Target snapshots could not be resolved" }),
        }
    }
}
