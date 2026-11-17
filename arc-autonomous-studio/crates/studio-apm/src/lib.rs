//! `studio-apm` — ARC Project Model (APM)
//! Représentation intermédiaire typée, persistante et versionnée d'un projet architectural.
//! Totalement indépendante des moteurs BIMcraft et Revit.

use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use std::collections::BTreeMap;
use std::fs;
use std::path::Path;

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ProjectMetadata {
    pub project_id: String,
    pub title: String,
    pub subtitle: String,
    pub client_profile: String,
    pub source_brief_pdf: Option<String>,
    pub source_plan_pdf: Option<String>,
    pub canonical_unit: String, // "mm"
    pub reference_scale: String, // "1:50"
    pub version: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct SpaceRequirement {
    pub category: String,
    pub min_count: usize,
    pub max_count: usize,
    pub mandatory: bool,
    pub description: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ArchitecturalConstraints {
    pub ceiling_height_mm: f64,
    pub slab_thickness_mm: f64,
    pub lot_duplication_mirror: bool,
    pub target_total_area_m2: f64,
    pub max_load_bearing_opening_mm: f64,
    pub min_load_bearing_pier_mm: f64,
    pub require_lintel_beam: bool,
    pub preserve_entry_door_strictly: bool,
    pub forbid_new_exterior_openings: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct BlockingAmbiguity {
    pub id: String,
    pub topic: String,
    pub description: String,
    pub resolution_strategy: String,
    pub requires_user_arbitration: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ArchitecturalBrief {
    pub context_summary: String,
    pub design_objectives: Vec<String>,
    pub required_spaces: Vec<SpaceRequirement>,
    pub constraints: ArchitecturalConstraints,
    pub deliverables_esq: Vec<String>,
    pub deliverables_aps: Vec<String>,
    pub deliverables_apd: Vec<String>,
    pub deliverables_jury: Vec<String>,
    pub detected_ambiguities: Vec<BlockingAmbiguity>,
}

#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq)]
pub enum MirrorMode {
    LeftMirrorG,  // Axe x = -175 mm (Lot B à gauche du Lot A)
    RightMirrorD, // Axe x = 5575 mm (Lot B à droite du Lot A)
}

#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq)]
pub enum WallCategory {
    LoadBearing350, // P : Mur porteur 350 mm
    Landing200,     // F : Mur palier 200 mm
    PartyWall1050,  // X : Mur mitoyen 1050 mm
    Partition70,    // C : Cloison 70 mm
    Millwork600,    // Meuble-cloison 600 mm
    Infill200,      // Rebouchage porte palière 200 mm
    Infill350,      // Rebouchage porteur 350 mm
}

impl WallCategory {
    pub fn thickness_mm(&self) -> f64 {
        match self {
            WallCategory::LoadBearing350 | WallCategory::Infill350 => 350.0,
            WallCategory::Landing200 | WallCategory::Infill200 => 200.0,
            WallCategory::PartyWall1050 => 1050.0,
            WallCategory::Partition70 => 70.0,
            WallCategory::Millwork600 => 600.0,
        }
    }

    pub fn bimcraft_type_name(&self, is_existing: bool) -> &'static str {
        match (self, is_existing) {
            (WallCategory::LoadBearing350, true) => "EXI_Mur porteur 35",
            (WallCategory::Landing200, true) => "EXI_Mur palier 20",
            (WallCategory::PartyWall1050, true) => "EXI_Mur mitoyen 105",
            (WallCategory::Partition70, true) => "EXI_Cloison 7",
            (WallCategory::Partition70, false) => "PRJ_Cloison 7",
            (WallCategory::Infill200, _) => "PRJ_Rebouchage 20",
            (WallCategory::Infill350, _) => "PRJ_Rebouchage 35",
            (WallCategory::Millwork600, _) => "PRJ_Meuble-cloison 60",
            (WallCategory::LoadBearing350, false) => "PRJ_Rebouchage 35",
            (WallCategory::Landing200, false) => "PRJ_Rebouchage 20",
            (WallCategory::PartyWall1050, false) => "EXI_Mur mitoyen 105",
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ApmWallSegment {
    pub id: String,
    pub category: WallCategory,
    pub is_existing: bool,
    pub start_mm: [f64; 2],
    pub end_mm: [f64; 2],
    pub height_mm: f64,
    pub label: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ApmDoor {
    pub id: String,
    pub is_existing: bool,
    pub is_entry_door: bool,
    pub at_mm: [f64; 2],
    pub width_mm: f64,
    pub height_mm: f64,
    pub flip_hand: bool,
    pub flip_facing: bool,
    pub type_name: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ApmWindow {
    pub id: String,
    pub is_existing: bool,
    pub at_mm: [f64; 2],
    pub width_mm: f64,
    pub height_mm: f64,
    pub sill_mm: f64,
    pub type_name: String,
    pub label: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct StructuralOpening {
    pub id: String,
    pub host_wall_id: String,
    pub bbox_cm: [f64; 4], // [x0, y0, x1, y1]
    pub span_mm: f64,
    pub left_pier_mm: f64,
    pub right_pier_mm: f64,
    pub beam_start_mm: [f64; 2],
    pub beam_end_mm: [f64; 2],
    pub beam_width_mm: f64,
    pub beam_depth_mm: f64,
    pub beam_offset_mm: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ApmRoom {
    pub number: String,
    pub name: String,
    pub zone_type: String, // "Jour", "Nuit", "Services/Circulation"
    pub target_area_m2: f64,
    pub point_mm: [f64; 2],
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ApmFurnitureFamily {
    pub name: String,
    pub category: String,
    pub kind: Option<String>,
    pub raw_bimcraft_json: serde_json::Value,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ApmPlacedComponent {
    pub family_name: String,
    pub at_mm: [f64; 2],
    pub angle_deg: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct MaterialSpec {
    pub space_category: String,
    pub element: String,
    pub material_name: String,
    pub finish_description: String,
    pub color_hex: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct DesignVariant {
    pub variant_id: String, // "V1_ECLIPSE", "V2_ORBITE", "V3_PASSAGE"
    pub name: String,
    pub mirror_mode: MirrorMode,
    pub architectural_concept: String,
    pub spatial_justification: String,
    pub day_area_m2: f64,
    pub night_area_m2: f64,
    pub total_habitable_area_m2: f64,
    pub floor_boundary_mm: Vec<[f64; 2]>,
    pub existing_walls_kept: Vec<ApmWallSegment>,
    pub new_walls: Vec<ApmWallSegment>,
    pub demolished_polygons_mm: Vec<Vec<[f64; 2]>>,
    pub structural_openings: Vec<StructuralOpening>,
    pub windows: Vec<ApmWindow>,
    pub doors: Vec<ApmDoor>,
    pub room_separators_mm: Vec<[[f64; 2]; 2]>,
    pub rooms: Vec<ApmRoom>,
    pub families: Vec<ApmFurnitureFamily>,
    pub components: Vec<ApmPlacedComponent>,
    pub materials_palette: Vec<MaterialSpec>,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct QaCheckResult {
    pub rule_id: String,
    pub category: String,
    pub description: String,
    pub passed: bool,
    pub measured_value: String,
    pub expected_bound: String,
    pub auto_corrected: bool,
    pub correction_note: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct VariantQaReport {
    pub variant_id: String,
    pub iteration: u32,
    pub score_100: f64,
    pub converged: bool,
    pub checks: Vec<QaCheckResult>,
    pub bimcraft_verified: bool,
    pub bimcraft_elements_count: usize,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ApmVersionCommit {
    pub version: u32,
    pub step_name: String,
    pub sha256_digest: String,
    pub summary: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ArcProjectModel {
    pub metadata: ProjectMetadata,
    pub brief: ArchitecturalBrief,
    pub variants: BTreeMap<String, DesignVariant>,
    pub selected_variant_id: String,
    pub qa_reports: BTreeMap<String, VariantQaReport>,
    pub history: Vec<ApmVersionCommit>,
}

impl ArcProjectModel {
    pub fn compute_digest(&self) -> String {
        let serialized = serde_json::to_vec(&(&self.metadata, &self.brief, &self.variants, &self.selected_variant_id))
            .unwrap_or_default();
        let mut hasher = Sha256::new();
        hasher.update(&serialized);
        format!("{:x}", hasher.finalize())
    }

    pub fn record_commit(&mut self, step_name: &str, summary: &str) {
        self.metadata.version += 1;
        let digest = self.compute_digest();
        self.history.push(ApmVersionCommit {
            version: self.metadata.version,
            step_name: step_name.to_string(),
            sha256_digest: digest,
            summary: summary.to_string(),
        });
    }

    pub fn save_to_file(&self, path: &Path) -> Result<(), String> {
        if let Some(parent) = path.parent() {
            fs::create_dir_all(parent).map_err(|e| e.to_string())?;
        }
        let json = serde_json::to_string_pretty(self).map_err(|e| e.to_string())?;
        fs::write(path, json).map_err(|e| e.to_string())
    }

    pub fn load_from_file(path: &Path) -> Result<Self, String> {
        let content = fs::read_to_string(path).map_err(|e| e.to_string())?;
        serde_json::from_str(&content).map_err(|e| e.to_string())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_apm_versioning_and_digest() {
        let mut apm = ArcProjectModel {
            metadata: ProjectMetadata {
                project_id: "PROJ-LONDON-01".into(),
                title: "REFLET — L'Appartement Londonien".into(),
                subtitle: "Réunion de 2 lots en miroir".into(),
                client_profile: "Famille contemporaine londonienne (couple + 2 enfants)".into(),
                source_brief_pdf: None,
                source_plan_pdf: None,
                canonical_unit: "mm".into(),
                reference_scale: "1:50".into(),
                version: 0,
            },
            brief: ArchitecturalBrief {
                context_summary: "Réinventer un appartement londonien en réunissant 2 lots.".into(),
                design_objectives: vec!["Équilibre intimité / convivialité".into()],
                required_spaces: vec![],
                constraints: ArchitecturalConstraints {
                    ceiling_height_mm: 2800.0,
                    slab_thickness_mm: 250.0,
                    lot_duplication_mirror: true,
                    target_total_area_m2: 70.0,
                    max_load_bearing_opening_mm: 4000.0,
                    min_load_bearing_pier_mm: 300.0,
                    require_lintel_beam: true,
                    preserve_entry_door_strictly: true,
                    forbid_new_exterior_openings: true,
                },
                deliverables_esq: vec![],
                deliverables_aps: vec![],
                deliverables_apd: vec![],
                deliverables_jury: vec![],
                detected_ambiguities: vec![],
            },
            variants: BTreeMap::new(),
            selected_variant_id: "V1_ECLIPSE".into(),
            qa_reports: BTreeMap::new(),
            history: vec![],
        };

        apm.record_commit("INIT", "Initialisation du projet dans l'APM");
        assert_eq!(apm.metadata.version, 1);
        assert_eq!(apm.history.len(), 1);
        assert_eq!(apm.history[0].sha256_digest.len(), 64);
    }
}
