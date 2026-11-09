#![allow(dead_code)]

use serde_json::{json, Value};

/// Parseur NLP haute vitesse en Rust pur (< 3 µs) pour commandes vocales Speech-to-BIM.
/// Compatible Dual-Engine : génère le plan d'exécution structuré pour Revit et BIMcraft.
pub fn parse_voice_intent(params: &Value) -> Value {
    let t0 = std::time::Instant::now();
    let transcript = params
        .get("command")
        .or_else(|| params.get("transcript"))
        .and_then(|v| v.as_str())
        .unwrap_or("")
        .trim();

    if transcript.is_empty() {
        return json!({
            "success": false,
            "error": "Aucun texte vocal ou transcription n'a été fourni."
        });
    }

    let norm = transcript.to_lowercase();
    let mut actions = Vec::new();
    let mut bimcraft_cmds = Vec::new();

    if norm.contains("isole") || norm.contains("isolate") {
        let mut targets = Vec::new();
        if norm.contains("gaine") || norm.contains("ventilation") || norm.contains("duct") {
            targets.push("Ducts");
        }
        if norm.contains("canalisation") || norm.contains("tuyau") || norm.contains("pipe") {
            targets.push("Pipes");
        }
        if norm.contains("mur") || norm.contains("wall") {
            targets.push("Walls");
        }
        if norm.contains("structure") || norm.contains("poteau") || norm.contains("column") {
            targets.push("StructuralColumns");
        }
        if norm.contains("porte") || norm.contains("door") {
            targets.push("Doors");
        }
        if norm.contains("fenêtre") || norm.contains("fenetre") || norm.contains("window") {
            targets.push("Windows");
        }
        actions.push(json!({ "intent": "isolate", "categories": targets }));
    }

    if norm.contains("masque") || norm.contains("cache") || norm.contains("hide") {
        let mut targets = Vec::new();
        if norm.contains("mobilier") || norm.contains("meuble") || norm.contains("furniture") {
            targets.push("Furniture");
        }
        if norm.contains("porte") || norm.contains("door") {
            targets.push("Doors");
        }
        if norm.contains("fenêtre") || norm.contains("fenetre") || norm.contains("window") {
            targets.push("Windows");
        }
        actions.push(json!({ "intent": "hide", "categories": targets }));
    }

    if norm.contains("réinitialise")
        || norm.contains("reinitialise")
        || norm.contains("reset")
        || norm.contains("affiche tout")
    {
        actions.push(json!({ "intent": "reset_view" }));
    }

    if norm.contains("vue 3d") || norm.contains("passe en 3d") || norm.contains("3d view") {
        actions.push(json!({ "intent": "activate_3d" }));
        bimcraft_cmds.push("view 3d".to_string());
    }

    if norm.contains("capture") || norm.contains("photo") || norm.contains("screenshot") {
        actions.push(json!({ "intent": "screenshot", "widthPixels": 1920 }));
        bimcraft_cmds.push("render active_view.png".to_string());
    }

    if norm.contains("efface")
        && (norm.contains("hologramme") || norm.contains("fantome") || norm.contains("fantôme") || norm.contains("preview"))
    {
        actions.push(json!({ "intent": "clear_holograms" }));
    }

    let elapsed_us = t0.elapsed().as_micros();
    json!({
        "success": !actions.is_empty(),
        "engine": "ARC Native Rust Speech-to-BIM NLP Parser",
        "transcript": transcript,
        "parsedIntents": actions,
        "bimcraftCommands": bimcraft_cmds,
        "nlpParseMicroseconds": elapsed_us
    })
}
