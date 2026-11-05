#![allow(dead_code)]

use rayon::prelude::*;
use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::f64::consts::PI;

/// Axis-Aligned Bounding Box (AABB) in millimeters.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BBox3D {
    pub min_x: f64,
    pub min_y: f64,
    pub min_z: f64,
    pub max_x: f64,
    pub max_y: f64,
    pub max_z: f64,
}

impl BBox3D {
    pub fn intersects(&self, other: &BBox3D, clearance_mm: f64) -> bool {
        (self.min_x - clearance_mm) <= other.max_x
            && (self.max_x + clearance_mm) >= other.min_x
            && (self.min_y - clearance_mm) <= other.max_y
            && (self.max_y + clearance_mm) >= other.min_y
            && (self.min_z - clearance_mm) <= other.max_z
            && (self.max_z + clearance_mm) >= other.min_z
    }

    pub fn intersection_volume_m3(&self, other: &BBox3D) -> f64 {
        let ix = (self.max_x.min(other.max_x) - self.min_x.max(other.min_x)).max(0.0);
        let iy = (self.max_y.min(other.max_y) - self.min_y.max(other.min_y)).max(0.0);
        let iz = (self.max_z.min(other.max_z) - self.min_z.max(other.min_z)).max(0.0);
        (ix * iy * iz) / 1_000_000_000.0 // mm³ to m³
    }

    pub fn center(&self) -> (f64, f64, f64) {
        (
            (self.min_x + self.max_x) / 2.0,
            (self.min_y + self.max_y) / 2.0,
            (self.min_z + self.max_z) / 2.0,
        )
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ClashElement {
    pub id: i64,
    pub category: String,
    pub name: String,
    pub bbox: BBox3D,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ClashResult {
    pub element_a_id: i64,
    pub element_a_category: String,
    pub element_a_name: String,
    pub element_b_id: i64,
    pub element_b_category: String,
    pub element_b_name: String,
    pub intersection_volume_m3: f64,
    pub clash_severity: String,
}

/// Multithreaded parallel clash detection engine using Rayon.
pub fn run_parallel_clash_detection(
    elements: &[ClashElement],
    clearance_mm: f64,
) -> Vec<ClashResult> {
    if elements.len() < 2 {
        return Vec::new();
    }

    (0..elements.len())
        .into_par_iter()
        .flat_map(|i| {
            let el_a = &elements[i];
            let mut local_clashes = Vec::new();

            for j in (i + 1)..elements.len() {
                let el_b = &elements[j];

                // Ignore elements of same ID or host-dependent pairings if identical
                if el_a.id == el_b.id {
                    continue;
                }

                if el_a.bbox.intersects(&el_b.bbox, clearance_mm) {
                    let vol = el_a.bbox.intersection_volume_m3(&el_b.bbox);
                    let severity = if vol > 0.001 {
                        "hard_clash"
                    } else if clearance_mm > 0.0 {
                        "clearance_clash"
                    } else {
                        "touching"
                    };

                    local_clashes.push(ClashResult {
                        element_a_id: el_a.id,
                        element_a_category: el_a.category.clone(),
                        element_a_name: el_a.name.clone(),
                        element_b_id: el_b.id,
                        element_b_category: el_b.category.clone(),
                        element_b_name: el_b.name.clone(),
                        intersection_volume_m3: (vol * 10_000.0).round() / 10_000.0,
                        clash_severity: severity.to_string(),
                    });
                }
            }
            local_clashes
        })
        .collect()
}

/// 2D Polygon spatial metrics (Green's theorem / Shoelace).
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Polygon2D {
    pub vertices: Vec<[f64; 2]>, // [x, y] in millimeters
}

impl Polygon2D {
    pub fn area_m2(&self) -> f64 {
        let n = self.vertices.len();
        if n < 3 {
            return 0.0;
        }

        let mut area = 0.0;
        for i in 0..n {
            let j = (i + 1) % n;
            area += self.vertices[i][0] * self.vertices[j][1];
            area -= self.vertices[j][0] * self.vertices[i][1];
        }
        (area.abs() / 2.0) / 1_000_000.0 // mm² to m²
    }

    pub fn perimeter_m(&self) -> f64 {
        let n = self.vertices.len();
        if n < 2 {
            return 0.0;
        }

        let mut perim = 0.0;
        for i in 0..n {
            let j = (i + 1) % n;
            let dx = self.vertices[j][0] - self.vertices[i][0];
            let dy = self.vertices[j][1] - self.vertices[i][1];
            perim += (dx * dx + dy * dy).sqrt();
        }
        perim / 1_000.0 // mm to m
    }

    pub fn circularity(&self) -> f64 {
        let a = self.area_m2();
        let p = self.perimeter_m();
        if p <= 0.0 {
            0.0
        } else {
            (4.0 * PI * a) / (p * p)
        }
    }
}

/// Architectural bioclimatic metrics (RE2020 ratio, daylight, room efficiency).
pub fn analyze_spatial_metrics(
    room_polygon: &Polygon2D,
    glazed_areas_m2: &[f64],
    window_orientations: &[String],
) -> Value {
    let floor_area = room_polygon.area_m2();
    let perimeter = room_polygon.perimeter_m();
    let circularity = room_polygon.circularity();

    let total_glazed_area: f64 = glazed_areas_m2.iter().sum();
    let glazed_ratio = if floor_area > 0.0 {
        total_glazed_area / floor_area
    } else {
        0.0
    };

    let re2020_compliant = glazed_ratio >= (1.0 / 6.0); // Ratio 1/6 (16.67%)
    let mut south_glazing = 0.0;
    let mut east_glazing = 0.0;
    let mut west_glazing = 0.0;
    let mut north_glazing = 0.0;

    for (area, ori) in glazed_areas_m2.iter().zip(window_orientations.iter()) {
        match ori.to_lowercase().as_str() {
            "sud" | "south" | "s" => south_glazing += *area,
            "est" | "east" | "e" => east_glazing += *area,
            "ouest" | "west" | "w" | "o" => west_glazing += *area,
            "nord" | "north" | "n" => north_glazing += *area,
            _ => south_glazing += *area,
        }
    }

    json!({
        "success": true,
        "metrics": {
            "floorAreaM2": (floor_area * 100.0).round() / 100.0,
            "perimeterM": (perimeter * 100.0).round() / 100.0,
            "compactnessCircularity": (circularity * 1000.0).round() / 1000.0,
            "totalGlazedAreaM2": (total_glazed_area * 100.0).round() / 100.0,
            "glazedRatio": (glazed_ratio * 1000.0).round() / 1000.0,
            "re2020BioclimaticCompliant": re2020_compliant,
            "re2020TargetRatio": 0.167,
            "solarExposure": {
                "southM2": (south_glazing * 100.0).round() / 100.0,
                "eastM2": (east_glazing * 100.0).round() / 100.0,
                "westM2": (west_glazing * 100.0).round() / 100.0,
                "northM2": (north_glazing * 100.0).round() / 100.0
            }
        }
    })
}
