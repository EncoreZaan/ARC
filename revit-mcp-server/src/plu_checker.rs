use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::time::Instant;

#[derive(Debug, Serialize, Deserialize)]
pub struct ComplianceCheckItem {
    pub rule_name: String,
    pub category: String, // "Urbanisme_PLU", "Gabarit_Prospect", "Environnement_CBS", "Securite_Incendie"
    pub measured_value: f64,
    pub threshold_value: f64,
    pub unit: String,
    pub compliant: bool,
    pub diagnostic: String,
}

/// Moteur d'audit réglementaire PLU (Plan Local d'Urbanisme) et Sécurité Incendie en Rust
pub fn audit_urban_plu_compliance(input: &Value) -> Value {
    let t0 = Instant::now();

    let zone_plu = input.get("zonePlu").and_then(|v| v.as_str()).unwrap_or("UB");
    let parcel_area = input.get("parcelAreaM2").and_then(|v| v.as_f64()).unwrap_or(650.0);
    let street_width = input.get("streetWidthMeters").and_then(|v| v.as_f64()).unwrap_or(12.0);

    // Données bâtiment
    let building_height = input.get("buildingHeightMeters").and_then(|v| v.as_f64()).unwrap_or(11.20);
    let building_footprint = input.get("footprintAreaM2").and_then(|v| v.as_f64()).unwrap_or(220.0);
    let boundary_distance = input.get("boundaryDistanceMeters").and_then(|v| v.as_f64()).unwrap_or(5.80);
    let open_ground_area = input.get("openGroundAreaM2").and_then(|v| v.as_f64()).unwrap_or(210.0);
    let green_roof_area = input.get("greenRoofAreaM2").and_then(|v| v.as_f64()).unwrap_or(60.0);

    // Sécurité incendie
    let max_escape_dist = input.get("maxEscapeDistanceMeters").and_then(|v| v.as_f64()).unwrap_or(22.5);
    let main_exit_width = input.get("mainExitDoorWidthMeters").and_then(|v| v.as_f64()).unwrap_or(1.40);

    let mut checks: Vec<ComplianceCheckItem> = Vec::new();

    // 1. Hauteur maximale au faîtage (PLU standard UB: 15m)
    let max_h_allowed = 14.0;
    let h_ok = building_height <= max_h_allowed;
    checks.push(ComplianceCheckItem {
        rule_name: "Hauteur Maximale au Faîtage (Art. 10 PLU)".into(),
        category: "Urbanisme_PLU".into(),
        measured_value: building_height,
        threshold_value: max_h_allowed,
        unit: "m".into(),
        compliant: h_ok,
        diagnostic: if h_ok { "Hauteur conforme au gabarit d'enveloppe".into() } else { "Dépassement de la hauteur maximale autorisée".into() },
    });

    // 2. Règle de prospect par rapport à la voie publique (H <= L + R)
    let prospect_limit = street_width + 0.0;
    let prospect_ok = building_height <= prospect_limit;
    checks.push(ComplianceCheckItem {
        rule_name: "Règle de Prospect sur Voie (H = L)".into(),
        category: "Gabarit_Prospect".into(),
        measured_value: building_height,
        threshold_value: prospect_limit,
        unit: "m".into(),
        compliant: prospect_ok,
        diagnostic: if prospect_ok { "Inscrit dans le plan incliné à 45° depuis l'alignement".into() } else { "Violation du prospect sur rue".into() },
    });

    // 3. Implantation par rapport aux limites séparatives (D >= H/2 avec min 3m)
    let min_boundary_req = (building_height / 2.0).max(3.0);
    let boundary_ok = boundary_distance >= min_boundary_req;
    checks.push(ComplianceCheckItem {
        rule_name: "Retrait aux Limites Séparatives (D >= H/2, min 3m)".into(),
        category: "Gabarit_Prospect".into(),
        measured_value: boundary_distance,
        threshold_value: (min_boundary_req * 100.0).round() / 100.0,
        unit: "m".into(),
        compliant: boundary_ok,
        diagnostic: if boundary_ok { "Recul suffisant vis-à-vis du voisinage".into() } else { "Recul insuffisant (risque de tiers et recours des voisins)".into() },
    });

    // 4. Coefficient d'Emprise au Sol (CES max 40%)
    let ces_real = building_footprint / parcel_area;
    let ces_max = 0.40;
    let ces_ok = ces_real <= ces_max;
    checks.push(ComplianceCheckItem {
        rule_name: "Emprise au Sol (CES - Art. 9 PLU)".into(),
        category: "Urbanisme_PLU".into(),
        measured_value: (ces_real * 1000.0).round() / 10.0, // en %
        threshold_value: ces_max * 100.0,
        unit: "%".into(),
        compliant: ces_ok,
        diagnostic: if ces_ok { "Emprise au sol respectée".into() } else { "Emprise au sol excessive par rapport à la parcelle".into() },
    });

    // 5. Coefficient de Biotope par Surface (CBS >= 0.35)
    // Pleine terre = coeff 1.0, toiture végétalisée = coeff 0.5
    let cbs_score = (open_ground_area * 1.0 + green_roof_area * 0.5) / parcel_area;
    let cbs_min = 0.35;
    let cbs_ok = cbs_score >= cbs_min;
    checks.push(ComplianceCheckItem {
        rule_name: "Coefficient de Biotope (CBS Environnement)".into(),
        category: "Environnement_CBS".into(),
        measured_value: (cbs_score * 100.0).round() / 100.0,
        threshold_value: cbs_min,
        unit: "ratio".into(),
        compliant: cbs_ok,
        diagnostic: if cbs_ok { "Surface éco-aménagée et perméable suffisante".into() } else { "Végétalisation insuffisante (ajouter toiture verte ou pleine terre)".into() },
    });

    // 6. Sécurité Incendie : Distance maximale d'évacuation (Code Habitation / ERP : <= 30m)
    let max_evac_limit = 30.0;
    let evac_ok = max_escape_dist <= max_evac_limit;
    checks.push(ComplianceCheckItem {
        rule_name: "Distance Maximale aux Issues de Secours (CCH/ERP)".into(),
        category: "Securite_Incendie".into(),
        measured_value: max_escape_dist,
        threshold_value: max_evac_limit,
        unit: "m".into(),
        compliant: evac_ok,
        diagnostic: if evac_ok { "Évacuation conforme sans cul-de-sac excessif".into() } else { "Parcours d'évacuation trop long : créer une issue intermédiaire".into() },
    });

    // 7. Sécurité Incendie : Largeur de dégagement (>= 1.40m = 2 UP pour ERP)
    let min_door_req = 1.40;
    let door_ok = main_exit_width >= min_door_req;
    checks.push(ComplianceCheckItem {
        rule_name: "Largeur de Dégagement Sortie Principale (2 UP = 1.40m)".into(),
        category: "Securite_Incendie".into(),
        measured_value: main_exit_width,
        threshold_value: min_door_req,
        unit: "m".into(),
        compliant: door_ok,
        diagnostic: if door_ok { "Capacité d'évacuation 2 UP certifiée".into() } else { "Dégagement insuffisant pour établissement recevant du public".into() },
    });

    let all_compliant = checks.iter().all(|c| c.compliant);
    let elapsed_ms = t0.elapsed().as_millis();

    json!({
        "success": true,
        "engine": "ARC Automated Urban PLU & Fire Safety Compliance Checker (Rust)",
        "auditTimeMs": elapsed_ms,
        "urbanContext": {
            "zoneCode": zone_plu,
            "parcelAreaM2": parcel_area,
            "streetWidthMeters": street_width
        },
        "globalVerdict": {
            "permitRiskAssessment": if all_compliant { "RISQUE FAIBLE - Projet conforme pour dépôt de Permis de Construire" } else { "RISQUE ÉLEVÉ - Non-conformités bloquantes détectées" },
            "compliantChecksCount": checks.iter().filter(|c| c.compliant).count(),
            "totalChecksCount": checks.len(),
            "allChecksPassed": all_compliant
        },
        "detailedAudit": checks
    })
}
