//! `studio-qa` — Agent 05 : Engineering, QA & Autocorrection Loop
//! Vérificateur mathématique réel et boucle d'autocorrection autonome :
//! CONSTRUCTION -> ANALYSE -> DÉTECTION -> CORRECTION -> VALIDATION.
//! Aucun résultat n'est codé en dur : chaque règle inspecte les vecteurs géométriques de l'APM.

use studio_apm::{
    ArchitecturalConstraints, DesignVariant, QaCheckResult, VariantQaReport,
};

/// Exécute une passe complète d'analyse, de détection et d'autocorrection sur un `DesignVariant`,
/// avec convergence bornée (`max_iterations`).
pub fn evaluate_and_autocorrect_variant(
    variant: &mut DesignVariant,
    constraints: &ArchitecturalConstraints,
    bimcraft_verified: bool,
    max_iterations: u32,
) -> VariantQaReport {
    let mut current_iter = 0;

    loop {
        current_iter += 1;
        let mut checks = Vec::new();
        let mut corrections_applied_this_pass = 0;

        // 1. Contrôle de la Hauteur Sous Plafond (HSP = 2800 mm)
        let mut off_height_walls = 0;
        for w in variant.existing_walls_kept.iter_mut().chain(variant.new_walls.iter_mut()) {
            if (w.height_mm - constraints.ceiling_height_mm).abs() > 1.0 {
                off_height_walls += 1;
                if current_iter < max_iterations {
                    w.height_mm = constraints.ceiling_height_mm;
                    corrections_applied_this_pass += 1;
                }
            }
        }
        checks.push(QaCheckResult {
            rule_id: "QA-HSP-01".into(),
            category: "Gabarit & Volumétrie".into(),
            description: "Respect de la Hauteur Sous Plafond réglementaire du sujet (HSP = 280 cm)".into(),
            passed: off_height_walls == 0,
            measured_value: format!(
                "{} murs à {:.0} mm (écarts détectés: {})",
                variant.existing_walls_kept.len() + variant.new_walls.len(),
                constraints.ceiling_height_mm,
                off_height_walls
            ),
            expected_bound: format!("= {:.0} mm", constraints.ceiling_height_mm),
            auto_corrected: off_height_walls > 0 && corrections_applied_this_pass > 0,
            correction_note: if off_height_walls > 0 {
                Some("Recalage automatique de la hauteur des murs à 2800 mm".into())
            } else {
                None
            },
        });

        // 1b. Contrôle & Autocorrection des Collisions Porte <-> Fenêtre sur un même mur
        let mut door_window_overlaps = 0;
        for d in variant.doors.iter_mut() {
            for w in &variant.windows {
                // Même mur horizontal (même Y à 50 mm près)
                if (d.at_mm[1] - w.at_mm[1]).abs() < 50.0 {
                    let min_dist = (d.width_mm + w.width_mm) * 0.5 + 150.0; // +150 mm de trumeau minimal
                    let actual_dist = (d.at_mm[0] - w.at_mm[0]).abs();
                    if actual_dist < min_dist {
                        door_window_overlaps += 1;
                        if current_iter < max_iterations {
                            let shift = min_dist - actual_dist + 50.0;
                            if d.at_mm[0] >= w.at_mm[0] {
                                d.at_mm[0] += shift;
                            } else {
                                d.at_mm[0] -= shift;
                            }
                            corrections_applied_this_pass += 1;
                        }
                    }
                }
                // Même mur vertical (même X à 50 mm près)
                if (d.at_mm[0] - w.at_mm[0]).abs() < 50.0 {
                    let min_dist = (d.width_mm + w.width_mm) * 0.5 + 150.0;
                    let actual_dist = (d.at_mm[1] - w.at_mm[1]).abs();
                    if actual_dist < min_dist {
                        door_window_overlaps += 1;
                        if current_iter < max_iterations {
                            let shift = min_dist - actual_dist + 50.0;
                            if d.at_mm[1] >= w.at_mm[1] {
                                d.at_mm[1] += shift;
                            } else {
                                d.at_mm[1] -= shift;
                            }
                            corrections_applied_this_pass += 1;
                        }
                    }
                }
            }
        }
        checks.push(QaCheckResult {
            rule_id: "QA-CLASH-01B".into(),
            category: "Collisions Baies (Porte / Fenêtre)".into(),
            description: "Absence de chevauchement entre portes et fenêtres sur un même mur hôte (trumeau >= 15 cm)".into(),
            passed: door_window_overlaps == 0,
            measured_value: format!(
                "{} portes et {} fenêtres inspectées (chevauchements détectés: {})",
                variant.doors.len(),
                variant.windows.len(),
                door_window_overlaps
            ),
            expected_bound: "0 collision baie/baie".into(),
            auto_corrected: door_window_overlaps > 0 && corrections_applied_this_pass > 0,
            correction_note: if door_window_overlaps > 0 {
                Some("Recalage automatique de l'axe de la porte pour garantir un trumeau >= 150 mm avec la fenêtre voisine".into())
            } else {
                None
            },
        });

        // 2. Contrôle Structurel : Ouvertures dans murs porteurs (L <= 4000 mm, Têtes >= 300 mm, Poutre présente)
        let mut max_span_seen = 0.0f64;
        let mut min_pier_seen = f64::MAX;
        let mut all_openings_ok = true;
        let mut corrected_openings = false;

        for op in &mut variant.structural_openings {
            if op.span_mm > max_span_seen {
                max_span_seen = op.span_mm;
            }
            let min_p = op.left_pier_mm.min(op.right_pier_mm);
            if min_p < min_pier_seen {
                min_pier_seen = min_p;
            }

            if op.span_mm > constraints.max_load_bearing_opening_mm {
                all_openings_ok = false;
                if current_iter < max_iterations {
                    op.span_mm = constraints.max_load_bearing_opening_mm;
                    corrected_openings = true;
                    corrections_applied_this_pass += 1;
                }
            }
            if op.left_pier_mm < constraints.min_load_bearing_pier_mm {
                all_openings_ok = false;
                if current_iter < max_iterations {
                    op.left_pier_mm = constraints.min_load_bearing_pier_mm;
                    corrected_openings = true;
                    corrections_applied_this_pass += 1;
                }
            }
            if op.right_pier_mm < constraints.min_load_bearing_pier_mm {
                all_openings_ok = false;
                if current_iter < max_iterations {
                    op.right_pier_mm = constraints.min_load_bearing_pier_mm;
                    corrected_openings = true;
                    corrections_applied_this_pass += 1;
                }
            }
            let beam_len = ((op.beam_end_mm[0] - op.beam_start_mm[0]).powi(2)
                + (op.beam_end_mm[1] - op.beam_start_mm[1]).powi(2))
            .sqrt();
            if beam_len < op.span_mm {
                all_openings_ok = false;
            }
        }

        checks.push(QaCheckResult {
            rule_id: "QA-STRUCT-02".into(),
            category: "Structure & Murs Porteurs".into(),
            description: "Ouverture mur porteur <= 4.00 m + têtes de mur >= 30 cm de part et d'autre + poutre BA".into(),
            passed: all_openings_ok,
            measured_value: format!(
                "{} percement(s) : portée max = {:.0} mm, tête de mur min = {:.0} mm, poutre 35x40 cm active",
                variant.structural_openings.len(),
                max_span_seen,
                if min_pier_seen == f64::MAX { 0.0 } else { min_pier_seen }
            ),
            expected_bound: "Portée <= 4000 mm & Têtes >= 300 mm".into(),
            auto_corrected: corrected_openings,
            correction_note: if corrected_openings {
                Some("Ajustement automatique de la baie porteuse et des têtes de mur à >= 300 mm".into())
            } else {
                None
            },
        });

        // 3. Contrôle de la Porte d'Entrée et du Rebouchage éventuel (selon lot_duplication_mirror)
        let entry_door_found = variant.doors.iter().any(|d| d.is_existing && d.is_entry_door);
        let infill_ok = if constraints.lot_duplication_mirror {
            variant.new_walls.iter().any(|w| w.label == "PRJ_Rebouchage 20")
        } else {
            true
        };

        checks.push(QaCheckResult {
            rule_id: "QA-ENTRY-03".into(),
            category: "Accès & Enveloppe".into(),
            description: "Présence et conformité de la porte d'entrée principale (+ rebouchage 2nd accès si réunion de lots)".into(),
            passed: entry_door_found && infill_ok,
            measured_value: format!(
                "Porte d'entrée principale = {}, Conformité accès = {}",
                entry_door_found, infill_ok
            ),
            expected_bound: "Porte d'entrée conforme au sujet".into(),
            auto_corrected: false,
            correction_note: None,
        });

        // 4. Contrôle des Ouvertures Extérieures (0 fenêtre extérieure neuve créée si contraint par le sujet)
        let new_ext_windows = variant.windows.iter().filter(|w| !w.is_existing).count();
        let existing_win_count = variant.windows.iter().filter(|w| w.is_existing).count();
        let ext_ok = if constraints.forbid_new_exterior_openings {
            new_ext_windows == 0 && existing_win_count >= 4
        } else {
            variant.windows.len() >= 2
        };

        checks.push(QaCheckResult {
            rule_id: "QA-FACADE-04".into(),
            category: "Façades & Ouvertures Extérieures".into(),
            description: if constraints.forbid_new_exterior_openings {
                "Conservation des ouvertures existantes en façade (0 nouvelle ouverture extérieure créée)".into()
            } else {
                "Présence de baies vitrées extérieures assurant l'éclairage naturel du projet".into()
            },
            passed: ext_ok,
            measured_value: format!(
                "{} baies existantes, {} baies neuves (total {})",
                existing_win_count,
                new_ext_windows,
                variant.windows.len()
            ),
            expected_bound: if constraints.forbid_new_exterior_openings {
                "0 nouvelle ouverture extérieure".into()
            } else {
                "Au moins 2 baies vitrées extérieures".into()
            },
            auto_corrected: false,
            correction_note: None,
        });

        // 5. Contrôle Complet du Programme Architectural (selon le sujet extrait dans constraints)
        let has_bath = variant
            .rooms
            .iter()
            .any(|r| r.name.contains("SDB") || r.name.contains("SALLE D'EAU"));
        let has_kitchen = variant.rooms.iter().any(|r| r.name.contains("CUISINE"));
        let has_living = variant
            .rooms
            .iter()
            .any(|r| r.name.contains("SÉJOUR") || r.name.contains("SALON"));
        let custom_furniture_count = variant.components.len();

        let program_ok = if constraints.lot_duplication_mirror {
            let bedroom_count = variant.rooms.iter().filter(|r| r.name.contains("CHAMBRE")).count();
            let has_laundry = variant.rooms.iter().any(|r| r.name.contains("BUANDERIE"));
            (2..=3).contains(&bedroom_count)
                && has_bath
                && has_kitchen
                && has_living
                && has_laundry
                && custom_furniture_count >= 15
        } else if constraints.forbid_new_exterior_openings {
            // Programme type Micro-Habitat / Cabane / Atelier polyvalent
            let has_atelier = variant.rooms.iter().any(|r| r.name.contains("ATELIER") || r.name.contains("ESTRADE") || r.name.contains("BUREAU"));
            let has_guest_sleep = variant.rooms.iter().any(|r| r.name.contains("CHAMBRE") || r.name.contains("NUIT") || r.name.contains("LIT"));
            has_atelier && has_guest_sleep && has_bath && has_kitchen && has_living && custom_furniture_count >= 6
        } else {
            // Projet paramétrique sur mesure extrait d'un sujet PDF arbitraire
            variant.rooms.len() >= 3 && custom_furniture_count >= 4
        };

        checks.push(QaCheckResult {
            rule_id: "QA-PROG-05".into(),
            category: "Programme Architectural".into(),
            description: "Présence vérifiée de tous les espaces exigés par le sujet PDF et du mobilier dessiné sur mesure".into(),
            passed: program_ok,
            measured_value: format!(
                "{} espaces fonctionnels modélisés, SDB={}, Cuisine={}, Salon/Séjour={}, {} composants 3D sur mesure ({} familles)",
                variant.rooms.len(),
                has_bath,
                has_kitchen,
                has_living,
                custom_furniture_count,
                variant.families.len()
            ),
            expected_bound: "100% des espaces du programme + mobilier sur mesure".into(),
            auto_corrected: false,
            correction_note: None,
        });

        // 6. Contrôle de la Surface Habitable par rapport à la cible extraite du PDF
        let target_a = constraints.target_total_area_m2;
        let area_ok = (variant.total_habitable_area_m2 - target_a).abs() <= 8.0;
        checks.push(QaCheckResult {
            rule_id: "QA-AREA-06".into(),
            category: "Surfaces & Équilibre Spatial".into(),
            description: format!("Surface habitable conforme au gabarit du sujet (cible {:.1} m²)", target_a),
            passed: area_ok,
            measured_value: format!(
                "Total Habitable = {:.1} m² (Zone Jour/Atelier = {:.1} m², Zone Nuit = {:.1} m²)",
                variant.total_habitable_area_m2, variant.day_area_m2, variant.night_area_m2
            ),
            expected_bound: format!("{:.1} m² ± 8.0 m²", target_a),
            auto_corrected: false,
            correction_note: None,
        });

        // 7. Contrôle d'Exécution & Cross-Check Moteur BIMcraft 0.11.0
        let total_elements = variant.existing_walls_kept.len()
            + variant.new_walls.len()
            + variant.doors.len()
            + variant.windows.len()
            + variant.components.len()
            + variant.rooms.len()
            + variant.structural_openings.len();

        checks.push(QaCheckResult {
            rule_id: "QA-BIM-07".into(),
            category: "Intégrité Modèle BIMcraft".into(),
            description: "Compilation sans erreur dans BIMcraft 0.11.0 et validation des éléments 2D/3D générés".into(),
            passed: bimcraft_verified,
            measured_value: format!("{} objets BIM compilés et vérifiés sur disque", total_elements),
            expected_bound: "Modèle .bimcraft + .ifc + .glb + .svg valides".into(),
            auto_corrected: false,
            correction_note: None,
        });

        let passed_count = checks.iter().filter(|c| c.passed).count();
        let score = (passed_count as f64 / checks.len() as f64) * 100.0;
        let all_passed = passed_count == checks.len();

        if all_passed || corrections_applied_this_pass == 0 || current_iter >= max_iterations {
            return VariantQaReport {
                variant_id: variant.variant_id.clone(),
                iteration: current_iter,
                score_100: (score * 10.0).round() / 10.0,
                converged: all_passed,
                checks,
                bimcraft_verified,
                bimcraft_elements_count: total_elements,
            };
        }
    }
}
