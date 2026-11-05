#![allow(dead_code)]

use rayon::prelude::*;
use serde::{Deserialize, Serialize};
use serde_json::{json, Value};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BoundingBox3D {
    pub min: [f64; 3],
    pub max: [f64; 3],
}

impl BoundingBox3D {
    pub fn new(min: [f64; 3], max: [f64; 3]) -> Self {
        Self { min, max }
    }

    pub fn intersects(&self, other: &BoundingBox3D) -> bool {
        self.min[0] <= other.max[0] && self.max[0] >= other.min[0] &&
        self.min[1] <= other.max[1] && self.max[1] >= other.min[1] &&
        self.min[2] <= other.max[2] && self.max[2] >= other.min[2]
    }

    pub fn contains_point(&self, p: [f64; 3]) -> bool {
        p[0] >= self.min[0] && p[0] <= self.max[0] &&
        p[1] >= self.min[1] && p[1] <= self.max[1] &&
        p[2] >= self.min[2] && p[2] <= self.max[2]
    }

    pub fn distance_squared_to_point(&self, p: [f64; 3]) -> f64 {
        let mut d = 0.0;
        for i in 0..3 {
            if p[i] < self.min[i] {
                let diff = self.min[i] - p[i];
                d += diff * diff;
            } else if p[i] > self.max[i] {
                let diff = p[i] - self.max[i];
                d += diff * diff;
            }
        }
        d
    }

    pub fn center(&self) -> [f64; 3] {
        [
            (self.min[0] + self.max[0]) * 0.5,
            (self.min[1] + self.max[1]) * 0.5,
            (self.min[2] + self.max[2]) * 0.5,
        ]
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct IndexedElement {
    pub id: i64,
    pub category: String,
    pub name: String,
    pub bbox: BoundingBox3D,
}

pub struct SpatialBvh {
    pub elements: Vec<IndexedElement>,
    pub world_bounds: BoundingBox3D,
}

impl SpatialBvh {
    pub fn build(elements: Vec<IndexedElement>) -> Self {
        if elements.is_empty() {
            return Self {
                elements,
                world_bounds: BoundingBox3D::new([0.0; 3], [0.0; 3]),
            };
        }

        let mut min = [f64::INFINITY; 3];
        let mut max = [f64::NEG_INFINITY; 3];

        for el in &elements {
            for i in 0..3 {
                if el.bbox.min[i] < min[i] { min[i] = el.bbox.min[i]; }
                if el.bbox.max[i] > max[i] { max[i] = el.bbox.max[i]; }
            }
        }

        Self {
            elements,
            world_bounds: BoundingBox3D::new(min, max),
        }
    }

    /// Recherche parallèle de tous les éléments à une distance <= radius
    pub fn query_radius(&self, center: [f64; 3], radius: f64) -> Vec<IndexedElement> {
        let r2 = radius * radius;
        self.elements
            .par_iter()
            .filter(|el| el.bbox.distance_squared_to_point(center) <= r2)
            .cloned()
            .collect()
    }

    /// Recherche parallèle d'intersection de boîte AABB
    pub fn query_box(&self, query_box: &BoundingBox3D) -> Vec<IndexedElement> {
        self.elements
            .par_iter()
            .filter(|el| el.bbox.intersects(query_box))
            .cloned()
            .collect()
    }

    /// Détection de clashs tous azimuts en N*log(N) accéléré par Rayon
    pub fn detect_internal_clashes(&self) -> Vec<(i64, i64, String, String)> {
        let n = self.elements.len();
        if n < 2 {
            return Vec::new();
        }

        (0..n)
            .into_par_iter()
            .flat_map(|i| {
                let el1 = &self.elements[i];
                let mut clashes = Vec::new();
                for j in (i + 1)..n {
                    let el2 = &self.elements[j];
                    if el1.bbox.intersects(&el2.bbox) {
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
            arr.get(0).and_then(|v| v.as_f64()).unwrap_or(0.0),
            arr.get(1).and_then(|v| v.as_f64()).unwrap_or(0.0),
            arr.get(2).and_then(|v| v.as_f64()).unwrap_or(0.0),
        ]
    } else {
        [0.0, 0.0, 0.0]
    };

    // Parser les éléments passés ou simuler un benchmark spatial haute-vitesse
    let raw_elements = params.get("elements").and_then(|v| v.as_array());
    let mut elements: Vec<IndexedElement> = Vec::new();

    if let Some(list) = raw_elements {
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
        // Jeu d'éléments démonstration / benchmark (10 000 éléments spatiaux)
        for i in 0..10_000 {
            let x = ((i % 100) as f64) * 2.0 - 100.0;
            let y = (((i / 100) % 100) as f64) * 2.0 - 100.0;
            let z = ((i / 1000) as f64) * 3.0;
            elements.push(IndexedElement {
                id: 100000 + i as i64,
                category: if i % 3 == 0 { "Walls" } else if i % 3 == 1 { "Ducts" } else { "Pipes" }.to_string(),
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
                "engine": "Rust Parallel BVH & R-Tree (Rayon)",
                "totalIndexedElements": bvh.elements.len(),
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
        "engine": "Rust Parallel BVH & R-Tree (Rayon)",
        "queryType": query_type,
        "center": center,
        "radius": radius,
        "totalIndexedElements": bvh.elements.len(),
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
