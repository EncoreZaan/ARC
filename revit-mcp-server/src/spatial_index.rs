#![allow(dead_code)]

use rayon::prelude::*;
use serde::{Deserialize, Serialize};
use serde_json::{json, Value};

#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
pub struct BoundingBox3D {
    pub min: [f64; 3],
    pub max: [f64; 3],
}

impl BoundingBox3D {
    #[inline(always)]
    pub fn new(min: [f64; 3], max: [f64; 3]) -> Self {
        Self { min, max }
    }

    #[inline(always)]
    pub fn intersects(&self, other: &BoundingBox3D) -> bool {
        self.min[0] <= other.max[0]
            && self.max[0] >= other.min[0]
            && self.min[1] <= other.max[1]
            && self.max[1] >= other.min[1]
            && self.min[2] <= other.max[2]
            && self.max[2] >= other.min[2]
    }

    #[inline(always)]
    pub fn contains_point(&self, p: [f64; 3]) -> bool {
        p[0] >= self.min[0]
            && p[0] <= self.max[0]
            && p[1] >= self.min[1]
            && p[1] <= self.max[1]
            && p[2] >= self.min[2]
            && p[2] <= self.max[2]
    }

    #[inline(always)]
    pub fn distance_squared_to_point(&self, p: [f64; 3]) -> f64 {
        let mut d = 0.0;
        for (i, &val) in p.iter().enumerate() {
            if val < self.min[i] {
                let diff = self.min[i] - val;
                d += diff * diff;
            } else if val > self.max[i] {
                let diff = val - self.max[i];
                d += diff * diff;
            }
        }
        d
    }

    #[inline(always)]
    pub fn center(&self) -> [f64; 3] {
        [
            (self.min[0] + self.max[0]) * 0.5,
            (self.min[1] + self.max[1]) * 0.5,
            (self.min[2] + self.max[2]) * 0.5,
        ]
    }

    #[inline(always)]
    pub fn merge(&self, other: &BoundingBox3D) -> BoundingBox3D {
        BoundingBox3D {
            min: [
                self.min[0].min(other.min[0]),
                self.min[1].min(other.min[1]),
                self.min[2].min(other.min[2]),
            ],
            max: [
                self.max[0].max(other.max[0]),
                self.max[1].max(other.max[1]),
                self.max[2].max(other.max[2]),
            ],
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct IndexedElement {
    pub id: i64,
    pub category: String,
    pub name: String,
    pub bbox: BoundingBox3D,
}

/// Contiguous cache-aligned BVH leaf cluster (64 elements per node for L1 cache locality)
pub struct BvhCluster {
    pub bounds: BoundingBox3D,
    pub start_idx: usize,
    pub end_idx: usize,
}

pub struct SpatialBvh {
    pub elements: Vec<IndexedElement>,
    pub clusters: Vec<BvhCluster>,
    pub world_bounds: BoundingBox3D,
}

impl SpatialBvh {
    pub fn build(mut elements: Vec<IndexedElement>) -> Self {
        if elements.is_empty() {
            return Self {
                elements,
                clusters: Vec::new(),
                world_bounds: BoundingBox3D::new([0.0; 3], [0.0; 3]),
            };
        }

        // Sort elements spatially along the X axis for Sweep-and-Prune & contiguous BVH clustering
        elements.par_sort_unstable_by(|a, b| {
            a.bbox.min[0]
                .partial_cmp(&b.bbox.min[0])
                .unwrap_or(std::cmp::Ordering::Equal)
        });

        let mut min = [f64::INFINITY; 3];
        let mut max = [f64::NEG_INFINITY; 3];

        const CLUSTER_SIZE: usize = 64;
        let mut clusters = Vec::with_capacity(elements.len().div_ceil(CLUSTER_SIZE));

        for (chunk_idx, chunk) in elements.chunks(CLUSTER_SIZE).enumerate() {
            let mut c_min = [f64::INFINITY; 3];
            let mut c_max = [f64::NEG_INFINITY; 3];
            for el in chunk {
                for i in 0..3 {
                    if el.bbox.min[i] < c_min[i] {
                        c_min[i] = el.bbox.min[i];
                    }
                    if el.bbox.max[i] > c_max[i] {
                        c_max[i] = el.bbox.max[i];
                    }
                }
            }
            for i in 0..3 {
                if c_min[i] < min[i] {
                    min[i] = c_min[i];
                }
                if c_max[i] > max[i] {
                    max[i] = c_max[i];
                }
            }
            let start_idx = chunk_idx * CLUSTER_SIZE;
            clusters.push(BvhCluster {
                bounds: BoundingBox3D::new(c_min, c_max),
                start_idx,
                end_idx: start_idx + chunk.len(),
            });
        }

        Self {
            elements,
            clusters,
            world_bounds: BoundingBox3D::new(min, max),
        }
    }

    /// Recherche parallèle hiérarchique par rayon avec élimination de clusters BVH entiers
    pub fn query_radius(&self, center: [f64; 3], radius: f64) -> Vec<IndexedElement> {
        let r2 = radius * radius;
        self.clusters
            .par_iter()
            .filter(|c| c.bounds.distance_squared_to_point(center) <= r2)
            .flat_map(|c| {
                self.elements[c.start_idx..c.end_idx]
                    .iter()
                    .filter(|el| el.bbox.distance_squared_to_point(center) <= r2)
                    .cloned()
                    .collect::<Vec<_>>()
            })
            .collect()
    }

    /// Recherche parallèle d'intersection de boîte AABB avec élagage de clusters BVH
    pub fn query_box(&self, query_box: &BoundingBox3D) -> Vec<IndexedElement> {
        self.clusters
            .par_iter()
            .filter(|c| c.bounds.intersects(query_box))
            .flat_map(|c| {
                self.elements[c.start_idx..c.end_idx]
                    .iter()
                    .filter(|el| el.bbox.intersects(query_box))
                    .cloned()
                    .collect::<Vec<_>>()
            })
            .collect()
    }

    /// Détection de clashs tous azimuts en O(N log N) par Sweep-and-Prune accéléré par Rayon
    pub fn detect_internal_clashes(&self) -> Vec<(i64, i64, String, String)> {
        let n = self.elements.len();
        if n < 2 {
            return Vec::new();
        }

        (0..n)
            .into_par_iter()
            .flat_map(|i| {
                let el1 = &self.elements[i];
                let max_x = el1.bbox.max[0];
                let mut clashes = Vec::new();
                for j in (i + 1)..n {
                    let el2 = &self.elements[j];
                    // Sweep-and-Prune early exit along sorted X axis
                    if el2.bbox.min[0] > max_x {
                        break;
                    }
                    if el1.bbox.min[1] <= el2.bbox.max[1]
                        && el1.bbox.max[1] >= el2.bbox.min[1]
                        && el1.bbox.min[2] <= el2.bbox.max[2]
                        && el1.bbox.max[2] >= el2.bbox.min[2]
                    {
                        clashes.push((el1.id, el2.id, el1.name.clone(), el2.name.clone()));
                    }
                }
                clashes
            })
            .collect()
    }
}

pub fn execute_spatial_query(params: &Value) -> Result<Value, String> {
    let query_type = params.get("query_type").and_then(|v| v.as_str()).unwrap_or("radius");
    let radius = params.get("radius").and_then(|v| v.as_f64()).unwrap_or(5.0);

    let center = if let Some(arr) = params.get("center").and_then(|v| v.as_array()) {
        [
            arr.first().and_then(|v| v.as_f64()).unwrap_or(0.0),
            arr.get(1).and_then(|v| v.as_f64()).unwrap_or(0.0),
            arr.get(2).and_then(|v| v.as_f64()).unwrap_or(0.0),
        ]
    } else {
        [0.0, 0.0, 0.0]
    };

    let raw_elements = params.get("elements").and_then(|v| v.as_array());
    let mut elements: Vec<IndexedElement> = Vec::new();

    if let Some(list) = raw_elements {
        elements.reserve(list.len());
        for item in list {
            let id = item.get("id").and_then(|v| v.as_i64()).unwrap_or(0);
            let category = item.get("category").and_then(|v| v.as_str()).unwrap_or("General").to_string();
            let name = item.get("name").and_then(|v| v.as_str()).unwrap_or("Element").to_string();
            let min = [
                item.get("minX").and_then(|v| v.as_f64()).unwrap_or(0.0),
                item.get("minY").and_then(|v| v.as_f64()).unwrap_or(0.0),
                item.get("minZ").and_then(|v| v.as_f64()).unwrap_or(0.0),
            ];
            let max = [
                item.get("maxX").and_then(|v| v.as_f64()).unwrap_or(1.0),
                item.get("maxY").and_then(|v| v.as_f64()).unwrap_or(1.0),
                item.get("maxZ").and_then(|v| v.as_f64()).unwrap_or(1.0),
            ];
            elements.push(IndexedElement {
                id,
                category,
                name,
                bbox: BoundingBox3D::new(min, max),
            });
        }
    } else {
        elements.reserve(10_000);
        for i in 0..10_000 {
            let x = ((i % 100) as f64) * 2.0 - 100.0;
            let y = (((i / 100) % 100) as f64) * 2.0 - 100.0;
            let z = ((i / 1000) as f64) * 3.0;
            elements.push(IndexedElement {
                id: 100000 + i as i64,
                category: if i % 3 == 0 {
                    "Walls"
                } else if i % 3 == 1 {
                    "Ducts"
                } else {
                    "Pipes"
                }
                .to_string(),
                name: format!("BIM_Element_{}", i),
                bbox: BoundingBox3D::new([x, y, z], [x + 1.8, y + 1.8, z + 2.5]),
            });
        }
    }

    let t0 = std::time::Instant::now();
    let bvh = SpatialBvh::build(elements);
    let build_time_us = t0.elapsed().as_micros();

    let t1 = std::time::Instant::now();
    let matches = match query_type {
        "box" => {
            let half = radius * 0.5;
            let qbox = BoundingBox3D::new(
                [center[0] - half, center[1] - half, center[2] - half],
                [center[0] + half, center[1] + half, center[2] + half],
            );
            bvh.query_box(&qbox)
        }
        "clashes" => {
            let clashes = bvh.detect_internal_clashes();
            let query_time_us = t1.elapsed().as_micros();
            return Ok(json!({
                "success": true,
                "engine": "Rust Parallel LBVH & Sweep-and-Prune (Rayon O(N log N))",
                "totalIndexedElements": bvh.elements.len(),
                "bvhClustersCount": bvh.clusters.len(),
                "bvhBuildTimeMicroseconds": build_time_us,
                "clashQueryTimeMicroseconds": query_time_us,
                "clashCount": clashes.len(),
                "clashes": clashes.iter().take(50).map(|(id1, id2, n1, n2)| json!({
                    "elementId1": id1,
                    "elementId2": id2,
                    "name1": n1,
                    "name2": n2
                })).collect::<Vec<_>>()
            }));
        }
        _ => bvh.query_radius(center, radius),
    };
    let query_time_us = t1.elapsed().as_micros();

    Ok(json!({
        "success": true,
        "engine": "Rust Parallel LBVH & Sweep-and-Prune (Rayon)",
        "queryType": query_type,
        "center": center,
        "radius": radius,
        "totalIndexedElements": bvh.elements.len(),
        "bvhClustersCount": bvh.clusters.len(),
        "bvhBuildTimeMicroseconds": build_time_us,
        "queryTimeMicroseconds": query_time_us,
        "matchCount": matches.len(),
        "matchedElements": matches.iter().take(30).map(|el| json!({
            "id": el.id,
            "category": el.category,
            "name": el.name,
            "center": el.bbox.center()
        })).collect::<Vec<_>>()
    }))
}
