#![allow(dead_code)]

use serde_json::{json, Value};

#[derive(Debug, Clone)]
pub struct MepRerouteProposal {
    pub strategy: String,
    pub bend_angle_deg: f64,
    pub offset_distance_m: f64,
    pub total_bypass_length_m: f64,
    pub head_loss_increase_pa: f64,
    pub requires_structural_sleeve: bool,
    pub sleeve_diameter_mm: Option<f64>,
    pub waypoints: Vec<[f64; 3]>,
}

pub fn resolve_mep_clash_offline(params: &Value) -> Result<Value, String> {
    let clash_id = params.get("clash_id").and_then(|v| v.as_str()).unwrap_or("CLASH-001");
    let primary_elem_id = params.get("primary_element_id").and_then(|v| v.as_i64()).unwrap_or(101234);
    let secondary_elem_id = params.get("secondary_element_id").and_then(|v| v.as_i64()).unwrap_or(205678);
    let elem_type = params.get("element_type").and_then(|v| v.as_str()).unwrap_or("Duct"); // "Duct", "Pipe", "CableTray"
    let obstacle_type = params.get("obstacle_type").and_then(|v| v.as_str()).unwrap_or("StructuralBeam"); // "StructuralBeam", "Duct", "Pipe"
    let clearance_required_m = params.get("clearance_m").and_then(|v| v.as_f64()).unwrap_or(0.10); // 10 cm margin
    let obstacle_depth_m = params.get("obstacle_depth_m").and_then(|v| v.as_f64()).unwrap_or(0.40); // 40 cm beam

    // Coordonnées du point de clash
    let clash_point = if let Some(arr) = params.get("clash_point").and_then(|v| v.as_array()) {
        [
            arr.first().and_then(|v| v.as_f64()).unwrap_or(12.5),
            arr.get(1).and_then(|v| v.as_f64()).unwrap_or(8.2),
            arr.get(2).and_then(|v| v.as_f64()).unwrap_or(2.8),
        ]
    } else {
        [12.5, 8.2, 2.8]
    };

    // Calcul de la géométrie de dévoiement optimal (Dévoiement 45° pour minimiser les pertes de charge aérauliques/hydrauliques)
    let offset_z = obstacle_depth_m + clearance_required_m;
    let angle_deg = 45.0;
    let angle_rad = angle_deg * std::f64::consts::PI / 180.0;
    let transition_length = offset_z / angle_rad.sin(); // L = Delta_Z / sin(45°) = Delta_Z * sqrt(2)
    let horizontal_run = offset_z / angle_rad.tan(); // Run = Delta_Z / tan(45°) = Delta_Z

    // Estimation de la perte de charge singulière supplémentaire (rho = 1.2 kg/m3 pour l'air, v = 4.5 m/s)
    let rho: f64 = 1.204;
    let velocity: f64 = 4.5;
    let dynamic_pressure: f64 = 0.5 * rho * velocity * velocity;
    // Deux coudes à 45° ont un coefficient zeta d'environ 0.20 chacun (contre 0.70 pour un coude 90°)
    let delta_p_45: f64 = 2.0 * 0.20 * dynamic_pressure;
    let delta_p_90: f64 = 2.0 * 0.70 * dynamic_pressure;

    // Calcul des waypoints pour le reroute (contournement par le bas z - offset_z)
    let wp_start = [clash_point[0] - horizontal_run - 0.5, clash_point[1], clash_point[2]];
    let wp_down1 = [clash_point[0] - 0.5, clash_point[1], clash_point[2] - offset_z];
    let wp_down2 = [clash_point[0] + 0.5, clash_point[1], clash_point[2] - offset_z];
    let wp_end = [clash_point[0] + horizontal_run + 0.5, clash_point[1], clash_point[2]];

    let reroute_45 = MepRerouteProposal {
        strategy: "BYPASS_UNDER_45_DEGREE".to_string(),
        bend_angle_deg: 45.0,
        offset_distance_m: offset_z,
        total_bypass_length_m: transition_length * 2.0 + 1.0,
        head_loss_increase_pa: (delta_p_45 * 10.0).round() / 10.0,
        requires_structural_sleeve: false,
        sleeve_diameter_mm: None,
        waypoints: vec![wp_start, wp_down1, wp_down2, wp_end],
    };

    // Option trémie / manchon structurel si passage direct à travers poutre
    let sleeve_option = json!({
        "strategy": "STRUCTURAL_SLEEVE_THROUGH_WEB",
        "diameterMm": 350.0,
        "acousticFirestopClass": "EI 120",
        "requiresStructuralEngineerValidation": true,
        "clashAvoidedWithoutReroute": true
    });

    Ok(json!({
        "success": true,
        "clashId": clash_id,
        "primaryElementId": primary_elem_id,
        "secondaryElementId": secondary_elem_id,
        "elementType": elem_type,
        "obstacleType": obstacle_type,
        "recommendedReroute": {
            "strategy": reroute_45.strategy,
            "bendAngleDegrees": reroute_45.bend_angle_deg,
            "verticalOffsetM": (reroute_45.offset_distance_m * 100.0).round() / 100.0,
            "headLossIncreasePa": reroute_45.head_loss_increase_pa,
            "pressureSavingComparedTo90DegPa": ((delta_p_90 - delta_p_45) * 10.0).round() / 10.0,
            "waypoints": reroute_45.waypoints,
            "fittingsRequired": [
                { "type": "Elbow_45", "count": 4, "angle": 45.0 }
            ]
        },
        "alternativeOption": sleeve_option,
        "revitActionInstructions": {
            "action": "reroute_mep_element",
            "elementId": primary_elem_id,
            "splitPoints": [wp_start, wp_end],
            "newSegments": [
                { "from": wp_start, "to": wp_down1 },
                { "from": wp_down1, "to": wp_down2 },
                { "from": wp_down2, "to": wp_end }
            ]
        }
    }))
}
