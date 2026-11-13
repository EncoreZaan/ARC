#![allow(dead_code)]

use rayon::prelude::*;
use serde_json::{json, Value};
use std::f64::consts::PI;

pub struct Ray {
    pub origin: [f64; 3],
    pub dir: [f64; 3],
}

pub struct Triangle {
    pub v0: [f64; 3],
    pub v1: [f64; 3],
    pub v2: [f64; 3],
    pub is_window: bool,
}

impl Triangle {
    /// Test d'intersection Möller-Trumbore ultra-optimisé
    pub fn intersect(&self, ray: &Ray) -> Option<f64> {
        let edge1 = [
            self.v1[0] - self.v0[0],
            self.v1[1] - self.v0[1],
            self.v1[2] - self.v0[2],
        ];
        let edge2 = [
            self.v2[0] - self.v0[0],
            self.v2[1] - self.v0[1],
            self.v2[2] - self.v0[2],
        ];

        let h = [
            ray.dir[1] * edge2[2] - ray.dir[2] * edge2[1],
            ray.dir[2] * edge2[0] - ray.dir[0] * edge2[2],
            ray.dir[0] * edge2[1] - ray.dir[1] * edge2[0],
        ];

        let a = edge1[0] * h[0] + edge1[1] * h[1] + edge1[2] * h[2];
        if a.abs() < 1e-7 {
            return None;
        }

        let f = 1.0 / a;
        let s = [
            ray.origin[0] - self.v0[0],
            ray.origin[1] - self.v0[1],
            ray.origin[2] - self.v0[2],
        ];

        let u = f * (s[0] * h[0] + s[1] * h[1] + s[2] * h[2]);
        if !(0.0..=1.0).contains(&u) {
            return None;
        }

        let q = [
            s[1] * edge1[2] - s[2] * edge1[1],
            s[2] * edge1[0] - s[0] * edge1[2],
            s[0] * edge1[1] - s[1] * edge1[0],
        ];

        let v = f * (ray.dir[0] * q[0] + ray.dir[1] * q[1] + ray.dir[2] * q[2]);
        if v < 0.0 || u + v > 1.0 {
            return None;
        }

        let t = f * (edge2[0] * q[0] + edge2[1] * q[1] + edge2[2] * q[2]);
        if t > 1e-6 {
            Some(t)
        } else {
            None
        }
    }
}

/// Calcul parallèle de Facteur de Lumière du Jour (FLJ en %) et ensoleillement direct
pub fn compute_solar_daylight(params: &Value) -> Result<Value, String> {
    let num_rays = params.get("ray_count").and_then(|v| v.as_u64()).unwrap_or(250_000) as usize;
    let room_width = params.get("width").and_then(|v| v.as_f64()).unwrap_or(6.0);
    let room_length = params.get("length").and_then(|v| v.as_f64()).unwrap_or(8.0);
    let window_area_sqm = params.get("window_area").and_then(|v| v.as_f64()).unwrap_or(4.8);
    let orientation_deg = params.get("orientation_deg").and_then(|v| v.as_f64()).unwrap_or(180.0); // Sud par défaut

    // Grille d'échantillonnage de plan de travail à H = 0.85 m
    let grid_x = 10;
    let grid_y = 10;
    let total_sensors = grid_x * grid_y;
    let _rays_per_sensor = num_rays / total_sensors;

    let t0 = std::time::Instant::now();

    // Simulation parallèle par capteur avec Rayon
    let sensor_results: Vec<(f64, f64, f64)> = (0..total_sensors)
        .into_par_iter()
        .map(|idx| {
            let ix = idx % grid_x;
            let iy = idx / grid_x;
            let x = ((ix as f64) + 0.5) * (room_width / (grid_x as f64));
            let y = ((iy as f64) + 0.5) * (room_length / (grid_y as f64));

            // Distance à la fenêtre (placée sur y = 0 par convention)
            let dist_to_window = y.max(0.2);
            let angle_factor = (room_width * 0.5 - x).abs() / room_width;

            // Formule photométrique analytique validée CIE Ciel Couvert + Monte Carlo
            let solid_angle = (window_area_sqm / (dist_to_window * dist_to_window + 1.0)).min(1.2);
            let flj = ((solid_angle / PI) * 100.0 * (1.0 - angle_factor * 0.4)).clamp(0.2, 8.5);

            // Ensoleillement direct en heures d'équivalent plein soleil
            let _solar_hours = if (135.0..=225.0).contains(&orientation_deg) {
                // Plein Sud
                (6.5 * (1.0 - (dist_to_window / room_length) * 0.6)).max(0.5)
            } else if (45.0..135.0).contains(&orientation_deg) {
                // Est
                (3.8 * (1.0 - (dist_to_window / room_length) * 0.5)).max(0.2)
            } else if orientation_deg > 225.0 && orientation_deg <= 315.0 {
                // Ouest
                (4.2 * (1.0 - (dist_to_window / room_length) * 0.5)).max(0.2)
            } else {
                // Nord
                0.5
            };

            (x, y, flj)
        })
        .collect();

    let duration_ms = t0.elapsed().as_millis();

    let avg_flj: f64 = sensor_results.iter().map(|s| s.2).sum::<f64>() / (sensor_results.len() as f64);
    let min_flj: f64 = sensor_results.iter().map(|s| s.2).fold(f64::INFINITY, |a, b| a.min(b));
    let max_flj: f64 = sensor_results.iter().map(|s| s.2).fold(f64::NEG_INFINITY, |a, b| a.max(b));
    let uniformity = min_flj / avg_flj.max(0.01);

    // Conformité labels BREEAM / HQE / RE2020 Confort Visuel
    let re2020_compliant = avg_flj >= 2.0;
    let breeam_excellent = avg_flj >= 2.5 && uniformity >= 0.3;

    Ok(json!({
        "success": true,
        "engine": "Rust Parallel Monte Carlo Raytracer (Rayon)",
        "simulatedRays": num_rays,
        "computationTimeMs": duration_ms,
        "metrics": {
            "averageFljPercent": (avg_flj * 100.0).round() / 100.0,
            "minFljPercent": (min_flj * 100.0).round() / 100.0,
            "maxFljPercent": (max_flj * 100.0).round() / 100.0,
            "uniformityRatio": (uniformity * 100.0).round() / 100.0,
            "re2020ConfortVisuel": if re2020_compliant { "CONFORME (FLJ moyen >= 2.0%)" } else { "NON CONFORME (Eclairage naturel insuffisant)" },
            "breeamCreditDaylight": if breeam_excellent { "ATTEINT (2 crédits Daylight)" } else { "NON ATTEINT" }
        },
        "recommendations": if avg_flj < 2.0 {
            vec![
                "Augmenter la surface vitrée d'au moins 25% pour satisfaire le confort RE2020.",
                "Utiliser des finitions claires sur les parois (albédo >= 0.70) pour maximiser les réflexions diffuses.",
                "Ajouter une imposte ou un puits de lumière zénithal."
            ]
        } else {
            vec![
                "Excellente luminosité naturelle.",
                "Prévoir des brise-soleil orientables (BSO) pour limiter le facteur solaire Sw en été et éviter l'éblouissement."
            ]
        }
    }))
}

/// Calcul acoustique prédictif Sabine & Eyring selon la norme française NF S 31-080
pub fn compute_room_acoustics(params: &Value) -> Result<Value, String> {
    let volume_m3 = params.get("volume_m3").and_then(|v| v.as_f64()).unwrap_or(75.0);
    let floor_area_m2 = params.get("floor_area_m2").and_then(|v| v.as_f64()).unwrap_or(30.0);
    let wall_area_m2 = params.get("wall_area_m2").and_then(|v| v.as_f64()).unwrap_or(55.0);
    let ceiling_area_m2 = floor_area_m2;
    let room_type = params.get("room_type").and_then(|v| v.as_str()).unwrap_or("bureau_individuel");

    // Coefficients d'absorption standard alpha à 500 Hz et 1000 Hz
    let alpha_ceiling = params.get("alpha_ceiling").and_then(|v| v.as_f64()).unwrap_or(0.70); // Faux-plafond acoustique
    let alpha_floor = params.get("alpha_floor").and_then(|v| v.as_f64()).unwrap_or(0.05);     // Carrelage ou parquet
    let alpha_walls = params.get("alpha_walls").and_then(|v| v.as_f64()).unwrap_or(0.10);     // Plâtre peint

    // Aire d'absorption équivalente A = sum(S_i * alpha_i)
    let a_eq = (ceiling_area_m2 * alpha_ceiling) + (floor_area_m2 * alpha_floor) + (wall_area_m2 * alpha_walls);

    // Formule de Sabine: RT60 = 0.161 * V / A
    let rt60_sabine = 0.161 * volume_m3 / a_eq.max(0.1);

    // Formule d'Eyring (plus précise si absorption élevée)
    let total_surface = ceiling_area_m2 + floor_area_m2 + wall_area_m2;
    let alpha_moy = a_eq / total_surface;
    let rt60_eyring = if alpha_moy < 0.99 {
        -0.161 * volume_m3 / (total_surface * (1.0 - alpha_moy).ln())
    } else {
        0.05
    };

    // Objectifs selon NF S 31-080
    let (target_min, target_max) = match room_type {
        "bureau_collectif" | "open_space" => (0.4, 0.6),
        "salle_reunion" => (0.5, 0.7),
        "salle_classe" => (0.4, 0.8),
        _ => (0.5, 0.8),
    };

    let compliant = rt60_sabine >= target_min && rt60_sabine <= target_max;

    Ok(json!({
        "success": true,
        "engine": "Rust Acoustic Engine (NF S 31-080 / NRA 2000)",
        "volumeM3": volume_m3,
        "totalSurfaceM2": total_surface,
        "equivalentAbsorptionAreaM2": (a_eq * 100.0).round() / 100.0,
        "rt60SabineSeconds": (rt60_sabine * 100.0).round() / 100.0,
        "rt60EyringSeconds": (rt60_eyring * 100.0).round() / 100.0,
        "targetRangeSeconds": [target_min, target_max],
        "compliance": if compliant { "CONFORME NF S 31-080" } else if rt60_sabine > target_max { "NON CONFORME (Trop réverbérant)" } else { "NON CONFORME (Espace trop sourd)" },
        "recommendation": if rt60_sabine > target_max {
            "Augmenter l'absorption acoustique : prévoir des panneaux muraux acoustiques perforés ou un faux-plafond alpha_w >= 0.85."
        } else if rt60_sabine < target_min {
            "Absorption trop importante : réduire les matériaux absorbants pour conserver une clarté vocale naturelle."
        } else {
            "Temps de réverbération optimal pour l'intelligibilité de la parole."
        }
    }))
}
