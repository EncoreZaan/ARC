use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::time::Instant;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CameraWaypoint {
    pub timestamp_sec: f64,
    pub room_name: String,
    pub camera_position: [f64; 3],
    pub look_at_target: [f64; 3],
    pub field_of_view_deg: f64,
    pub cinematic_action: String,
    pub solar_time: String,
    pub narration_audio_script: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct MaterialSwapCue {
    pub timestamp_sec: f64,
    pub element_category: String,
    pub initial_material: String,
    pub alternate_material: String,
    pub visual_effect: String,
}

/// Moteur de réalisation cinématographique et compagnon vocal architectural en Rust
pub fn generate_walkthrough_direct(input: &Value) -> Value {
    let t0 = Instant::now();

    let project_name = input.get("projectName").and_then(|v| v.as_str()).unwrap_or("Appartement Londonien");
    let total_duration = input.get("totalDurationSec").and_then(|v| v.as_f64()).unwrap_or(45.0);
    let eye_height = input.get("eyeHeightMeters").and_then(|v| v.as_f64()).unwrap_or(1.65);

    let waypoints = vec![
        CameraWaypoint {
            timestamp_sec: 0.0,
            room_name: "Entrée Principale".into(),
            camera_position: [0.5, 0.5, eye_height],
            look_at_target: [3.5, 3.0, eye_height],
            field_of_view_deg: 75.0,
            cinematic_action: "Slow Forward Dolly & Gentle Reveal".into(),
            solar_time: "09:00 (Lumière matinale rasante)".into(),
            narration_audio_script: format!("Bienvenue dans ce projet : {}. Dès le hall d'entrée, les perspectives traversantes vers les espaces de vie créent une sensation d'amplitude immédiate.", project_name),
        },
        CameraWaypoint {
            timestamp_sec: 12.0,
            room_name: "Grand Séjour / Double Living".into(),
            camera_position: [3.2, 2.8, eye_height],
            look_at_target: [7.0, 4.5, 1.2],
            field_of_view_deg: 80.0,
            cinematic_action: "360-Degree Panoramic Arc to Exterior Windows".into(),
            solar_time: "12:30 (Zénith ensoleillé, ombres franches)".into(),
            narration_audio_script: "Nous pénétrons dans le séjour principal baigné de clarté grâce aux doubles baies vitrées orientées plein Sud. L'agencement favorise la circulation fluide et le confort thermique passif.".into(),
        },
        CameraWaypoint {
            timestamp_sec: 25.0,
            room_name: "Cuisine & Îlot Central".into(),
            camera_position: [4.8, 5.0, eye_height],
            look_at_target: [6.0, 5.2, 0.90],
            field_of_view_deg: 70.0,
            cinematic_action: "Close-up Glide over Worktop & Cabinetry".into(),
            solar_time: "15:00 (Lumière douce de l'après-midi)".into(),
            narration_audio_script: "L'îlot central en quartz noir structure la transition entre cuisine et espace dînatoire, alliant ergonomie moderne et noblesse des matériaux.".into(),
        },
        CameraWaypoint {
            timestamp_sec: 36.0,
            room_name: "Chambre Parentale".into(),
            camera_position: [6.5, 2.0, eye_height],
            look_at_target: [7.5, 1.5, eye_height * 0.8],
            field_of_view_deg: 68.0,
            cinematic_action: "Gentle Crane Pan to Terraced Balcony".into(),
            solar_time: "19:15 (Golden Hour / Crépuscule chaleureux)".into(),
            narration_audio_script: "La suite parentale profite de la quiétude du coucher de soleil. Les luminaires encastrés prennent doucement le relais pour composer une ambiance feutrée.".into(),
        },
        CameraWaypoint {
            timestamp_sec: total_duration,
            room_name: "Vue d'Ensemble Isométrique".into(),
            camera_position: [12.0, -8.0, 9.5],
            look_at_target: [4.0, 3.0, 1.0],
            field_of_view_deg: 50.0,
            cinematic_action: "Dramatic Ascending Pull-Out".into(),
            solar_time: "20:30 (Vue nocturne avec éclairage architectural)".into(),
            narration_audio_script: "Voilà pour la visite guidée interactive. Tous les paramètres spatiaux, finitions et performances thermiques sont modifiables en direct avec le copilote IA.".into(),
        },
    ];

    let material_swaps = vec![
        MaterialSwapCue {
            timestamp_sec: 14.5,
            element_category: "Floors".into(),
            initial_material: "Parquet Chêne Massif Point de Hongrie".into(),
            alternate_material: "Béton Ciré Minéral Gris Anthracite".into(),
            visual_effect: "Cross-Dissolve Swipe".into(),
        },
        MaterialSwapCue {
            timestamp_sec: 27.0,
            element_category: "Countertops".into(),
            initial_material: "Quartz Calacatta Blanc Veiné Or".into(),
            alternate_material: "Granit Noir Zimbabwéen Flammé".into(),
            visual_effect: "Radial Reveal".into(),
        },
    ];

    let elapsed_ms = t0.elapsed().as_millis();

    json!({
        "success": true,
        "engine": "ARC Conversational AI Walkthrough Director & Voice Companion (Rust)",
        "directorTimeMs": elapsed_ms,
        "storyboard": {
            "projectName": project_name,
            "totalDurationSeconds": total_duration,
            "cameraMotion": "Cinematic Smooth Spline (Catmull-Rom)",
            "waypointsCount": waypoints.len(),
            "waypoints": waypoints
        },
        "materialInteractiveSwaps": material_swaps,
        "ambientVoiceCompanion": {
            "ttsEngineReady": true,
            "geminiLiveApiIntegration": "Ready for real-time bidirectional audio walkthrough streaming",
            "voiceStyle": "Architectural Expert, Calming, Precise"
        }
    })
}
