use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::time::Instant;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CaseworkModule {
    pub index: usize,
    pub module_name: String,
    pub width_mm: f64,
    pub depth_mm: f64,
    pub height_mm: f64,
    pub position_x_m: f64,
    pub equipment: Vec<String>,
}

/// Moteur de Calepinage Paramétrique d'Agencement Intérieur (Placards, Cuisines, Dressings)
pub fn generate_casework_layout(input: &Value) -> Value {
    let t0 = Instant::now();

    let layout_type = input.get("layoutType").and_then(|v| v.as_str()).unwrap_or("DressingPlacard");
    let total_length_m = input.get("totalLengthMeters").and_then(|v| v.as_f64()).unwrap_or(3.20);
    let depth_m = input.get("depthMeters").and_then(|v| v.as_f64()).unwrap_or(0.60);
    let height_m = input.get("heightMeters").and_then(|v| v.as_f64()).unwrap_or(2.50);
    let finish = input.get("finishMaterial").and_then(|v| v.as_str()).unwrap_or("Chêne Naturel Verni Mat");

    // Détermination du nombre de caissons (standard 60cm à 90cm)
    let target_module_width = match layout_type {
        "CuisineLineaire" | "CuisineEnL" => 0.60, // Standard électroménager 600mm
        "Bibliotheque" => 0.50,
        "MeubleVasque" => 0.70,
        _ => 0.80, // Dressing / Placard 800mm
    };

    let requested_modules = input.get("numModules").and_then(|v| v.as_u64()).map(|n| n as usize);
    let num_modules = requested_modules.unwrap_or_else(|| {
        ((total_length_m / target_module_width).round() as usize).max(1)
    });

    let module_width_m = total_length_m / num_modules as f64;
    let module_width_mm = (module_width_m * 1000.0).round();

    let mut modules: Vec<CaseworkModule> = Vec::new();

    for i in 0..num_modules {
        let pos_x = (i as f64) * module_width_m;
        let (name, equip) = match layout_type {
            "CuisineLineaire" | "CuisineEnL" => {
                if i == 0 {
                    ("Caisson Colonne Frigo Encastré", vec!["Réfrigérateur intégrable", "Niche ventilation haute"])
                } else if i == 1 {
                    ("Caisson Évier & Tri Sélectif", vec!["Sous-évier 2 bacs", "Tiroir coulissant poubelles"])
                } else if i == 2 {
                    ("Caisson Lave-Vaisselle Intégré", vec!["Porte d'habillage lave-vaisselle 60cm"])
                } else if i == 3 {
                    ("Caisson Plaque Induction & Four", vec!["Four pyrolyse encastré", "Tiroir range-casseroles"])
                } else {
                    ("Caisson Rangement Coulissant", vec!["3 tiroirs à amortisseurs Blumotion"])
                }
            },
            "MeubleVasque" => {
                ("Module Meuble Vasque Suspendu", vec!["Vasque à encastrer", "Tiroir découpe siphon", "Miroir rétroéclairé LED"])
            },
            "Bibliotheque" => {
                ("Colonne Bibliothèque Etagères", vec!["5 tablettes réglables pas de 32mm", "Fond plein 8mm"])
            },
            _ => {
                // Dressing / Placard
                if i % 2 == 0 {
                    ("Module Dressing Penderie Double", vec!["2 tringles basculantes chromées", "Étagère haute valises"])
                } else {
                    ("Module Dressing Lingère & Tiroirs", vec!["4 étagères réglables", "2 tiroirs coulissants bijoux/ceintures"])
                }
            }
        };

        modules.push(CaseworkModule {
            index: i + 1,
            module_name: name.into(),
            width_mm: module_width_mm,
            depth_mm: (depth_m * 1000.0).round(),
            height_mm: (height_m * 1000.0).round(),
            position_x_m: (pos_x * 100.0).round() / 100.0,
            equipment: equip.into_iter().map(String::from).collect(),
        });
    }

    // Métriques artisan
    let facade_area_m2 = (total_length_m * height_m * 100.0).round() / 100.0;
    let plinth_linear_m = (total_length_m * 100.0).round() / 100.0;
    let hinges_count = num_modules * 4;

    let elapsed_us = t0.elapsed().as_micros();

    json!({
        "success": true,
        "engine": "ARC Parametric Millwork & Casework Layout Engine (Rust)",
        "calculationTimeUs": elapsed_us,
        "configuration": {
            "layoutType": layout_type,
            "totalLinearMeters": total_length_m,
            "depthMeters": depth_m,
            "heightMeters": height_m,
            "finishMaterial": finish,
            "modulesCount": num_modules,
            "moduleWidthMm": module_width_mm
        },
        "artisanBillOfMaterials": {
            "facadesAreaM2": facade_area_m2,
            "linearPlinthMeters": plinth_linear_m,
            "estimatedHingesCount": hinges_count,
            "slidesCount": num_modules * 2
        },
        "modules": modules,
        "bimPlacementTransactions": {
            "revit": {
                "category": "Casework / Agencement",
                "readyToCommit": true
            },
            "bimcraft": {
                "command": "furnish",
                "readyToCommit": true
            }
        }
    })
}
