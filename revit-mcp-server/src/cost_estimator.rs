#![allow(dead_code)]

use serde_json::{json, Value};

#[derive(Debug, Clone)]
pub struct CctpTradeLot {
    pub lot_number: &'static str,
    pub lot_name: &'static str,
    pub unit: &'static str,
    pub quantity: f64,
    pub unit_price_eur: f64,
    pub total_ht_eur: f64,
}

pub fn compute_cctp_budget(params: &Value) -> Result<Value, String> {
    let project_name = params.get("project_name").and_then(|v| v.as_str()).unwrap_or("Estimation CCTP Chantier");
    let gross_floor_area_m2 = params.get("gross_floor_area_m2").and_then(|v| v.as_f64()).unwrap_or(150.0);
    let wall_area_m2 = params.get("wall_area_m2").and_then(|v| v.as_f64()).unwrap_or(gross_floor_area_m2 * 2.2);
    let concrete_volume_m3 = params.get("concrete_volume_m3").and_then(|v| v.as_f64()).unwrap_or(gross_floor_area_m2 * 0.35);
    let doors_count = params.get("doors_count").and_then(|v| v.as_f64()).unwrap_or(12.0);
    let windows_count = params.get("windows_count").and_then(|v| v.as_f64()).unwrap_or(10.0);
    let tva_rate = params.get("tva_rate").and_then(|v| v.as_f64()).unwrap_or(0.20); // 20% standard

    let t0 = std::time::Instant::now();

    // Bordereau de prix unitaires (BPU) du Bâtiment France 2025/2026
    let mut lots = Vec::new();

    // Lot 01 - Terrassements / Gros-Œuvre / Fondations
    let lot01_price = 380.0; // 380 €/m3 béton banché ferraillé
    lots.push(CctpTradeLot {
        lot_number: "LOT 01",
        lot_name: "Gros-Œuvre & Dalles Béton Armé",
        unit: "m³",
        quantity: concrete_volume_m3,
        unit_price_eur: lot01_price,
        total_ht_eur: concrete_volume_m3 * lot01_price,
    });

    // Lot 02 - Charpente & Étanchéité
    let lot02_price = 110.0;
    lots.push(CctpTradeLot {
        lot_number: "LOT 02",
        lot_name: "Étanchéité Toiture-Terrasse & Isolant",
        unit: "m²",
        quantity: gross_floor_area_m2 * 0.5,
        unit_price_eur: lot02_price,
        total_ht_eur: gross_floor_area_m2 * 0.5 * lot02_price,
    });

    // Lot 03 - Menuiseries Extérieures Aluminium / Double vitrage
    let lot03_price = 750.0;
    lots.push(CctpTradeLot {
        lot_number: "LOT 03",
        lot_name: "Menuiseries Extérieures Alu RPT",
        unit: "unité",
        quantity: windows_count,
        unit_price_eur: lot03_price,
        total_ht_eur: windows_count * lot03_price,
    });

    // Lot 04 - Plâtrerie / Doublages & Cloisons
    let lot04_price = 55.0; // 55 €/m2 Placostil BA13 isolée
    lots.push(CctpTradeLot {
        lot_number: "LOT 04",
        lot_name: "Plâtrerie / Cloisons & Faux-Plafonds",
        unit: "m²",
        quantity: wall_area_m2,
        unit_price_eur: lot04_price,
        total_ht_eur: wall_area_m2 * lot04_price,
    });

    // Lot 05 - Menuiseries Intérieures (Blocs-portes âme pleine)
    let lot05_price = 320.0;
    lots.push(CctpTradeLot {
        lot_number: "LOT 05",
        lot_name: "Menuiseries Intérieures (Blocs-Portes)",
        unit: "unité",
        quantity: doors_count,
        unit_price_eur: lot05_price,
        total_ht_eur: doors_count * lot05_price,
    });

    // Lot 06 - Revêtements de Sols (Grès cérame / Parquet contrecollé)
    let lot06_price = 68.0;
    lots.push(CctpTradeLot {
        lot_number: "LOT 06",
        lot_name: "Revêtements de Sol (Carrelage & Parquet)",
        unit: "m²",
        quantity: gross_floor_area_m2,
        unit_price_eur: lot06_price,
        total_ht_eur: gross_floor_area_m2 * lot06_price,
    });

    // Lot 07 - Peinture & Finitions
    let lot07_price = 28.0;
    lots.push(CctpTradeLot {
        lot_number: "LOT 07",
        lot_name: "Peinture Intérieure 2 couches & Ratissage",
        unit: "m²",
        quantity: wall_area_m2 + gross_floor_area_m2,
        unit_price_eur: lot07_price,
        total_ht_eur: (wall_area_m2 + gross_floor_area_m2) * lot07_price,
    });

    // Lot 08 - CVC & Ventilation double flux
    let lot08_price = 85.0;
    lots.push(CctpTradeLot {
        lot_number: "LOT 08",
        lot_name: "Génie Climatique / Pompe à Chaleur & VMC",
        unit: "m² SHAB",
        quantity: gross_floor_area_m2,
        unit_price_eur: lot08_price,
        total_ht_eur: gross_floor_area_m2 * lot08_price,
    });

    // Lot 09 - Plomberie / Sanitaires
    let lot09_price = 950.0;
    lots.push(CctpTradeLot {
        lot_number: "LOT 09",
        lot_name: "Plomberie & Appareils Sanitaires",
        unit: "poste",
        quantity: 6.0,
        unit_price_eur: lot09_price,
        total_ht_eur: 6.0 * lot09_price,
    });

    // Lot 10 - Électricité CFO/CFA
    let lot10_price = 75.0;
    lots.push(CctpTradeLot {
        lot_number: "LOT 10",
        lot_name: "Électricité Courants Forts / Courants Faibles",
        unit: "m² SHAB",
        quantity: gross_floor_area_m2,
        unit_price_eur: lot10_price,
        total_ht_eur: gross_floor_area_m2 * lot10_price,
    });

    let debourse_sec: f64 = lots.iter().map(|l| l.total_ht_eur).sum();
    let aleas_imprevus = debourse_sec * 0.05; // 5% d'imprévus
    let total_ht = debourse_sec + aleas_imprevus;
    let montant_tva = total_ht * tva_rate;
    let total_ttc = total_ht + montant_tva;
    let ratio_m2_ht = total_ht / gross_floor_area_m2.max(1.0);

    let duration_us = t0.elapsed().as_micros();

    Ok(json!({
        "success": true,
        "engine": "Rust Real-Time CCTP Construction Budget Estimator",
        "projectName": project_name,
        "computationTimeMicroseconds": duration_us,
        "surfaceShabM2": gross_floor_area_m2,
        "financialSummary": {
            "debourseSecHtEur": (debourse_sec * 100.0).round() / 100.0,
            "provisionsAleas5PercentEur": (aleas_imprevus * 100.0).round() / 100.0,
            "totalChantierHtEur": (total_ht * 100.0).round() / 100.0,
            "tvaAmountEur": (montant_tva * 100.0).round() / 100.0,
            "totalChantierTtcEur": (total_ttc * 100.0).round() / 100.0,
            "ratioPrixM2Ht": (ratio_m2_ht * 100.0).round() / 100.0
        },
        "breakdownByTrades": lots.iter().map(|l| json!({
            "lot": l.lot_number,
            "label": l.lot_name,
            "unit": l.unit,
            "quantity": (l.quantity * 10.0).round() / 10.0,
            "unitPriceEur": l.unit_price_eur,
            "subtotalHtEur": (l.total_ht_eur * 100.0).round() / 100.0,
            "percentOfTotal": ((l.total_ht_eur / total_ht) * 1000.0).round() / 10.0
        })).collect::<Vec<_>>()
    }))
}
