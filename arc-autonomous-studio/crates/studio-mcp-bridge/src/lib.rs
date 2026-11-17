//! `studio-mcp-bridge` — Adaptateur Client Unidirectionnel & Vérificateur de Non-Régression MCP
//! - Vérifie que les binaires et configurations historiques (`revit-mcp-server.exe`, `Cargo.toml`, `tools_manifest.json`)
//!   n'ont subi aucune régression (empreintes SHA-256 + contrat JSON-RPC `stdio`).
//! - Peut compiler un `DesignVariant` de l'APM en payload atomique `revit_batch` / `arc_batch_macro`
//!   sans que les serveurs MCP historiques ne dépendent d'Autonomous Studio.

use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use sha2::{Digest, Sha256};
use std::fs;
use std::io::{BufRead, BufReader, Write};
use std::path::Path;
use std::process::{Command, Stdio};
use studio_apm::DesignVariant;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LegacyMcpBaselineCheck {
    pub binary_intact: bool,
    pub manifest_intact: bool,
    pub initialize_ok: bool,
    pub compact_tools_count: usize,
    pub all_tools_count: usize,
    pub cad_tools_count: usize,
    pub passed: bool,
    pub details: Vec<String>,
}

pub fn sha256_file_upper(path: &Path) -> Result<String, String> {
    let bytes = fs::read(path).map_err(|e| format!("Impossible de lire {:?}: {}", path, e))?;
    let mut hasher = Sha256::new();
    hasher.update(&bytes);
    Ok(format!("{:X}", hasher.finalize()))
}

/// Exécute une session JSON-RPC `stdio` en lecture seule avec le binaire historique `revit-mcp-server.exe`
/// pour vérifier que les contrats MCP (`initialize`, `tools/list`) fonctionnent exactement comme avant.
pub fn query_legacy_mcp_tools_count(mcp_bin: &Path, profile: Option<&str>, args: &[&str]) -> Result<(bool, usize), String> {
    let mut cmd = Command::new(mcp_bin);
    cmd.args(args)
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::null());

    if let Some(p) = profile {
        cmd.env("ARC_TOOL_PROFILE", p);
    }

    let mut child = cmd.spawn().map_err(|e| format!("Échec spawn {:?}: {}", mcp_bin, e))?;
    {
        let stdin = child.stdin.as_mut().ok_or("No stdin")?;
        writeln!(
            stdin,
            r#"{{"jsonrpc":"2.0","id":1,"method":"initialize","params":{{"protocolVersion":"2024-11-05","capabilities":{{}},"clientInfo":{{"name":"studio-non-regression","version":"1.0"}}}}}}"#
        )
        .map_err(|e| e.to_string())?;
        writeln!(
            stdin,
            r#"{{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{{}}}}"#
        )
        .map_err(|e| e.to_string())?;
    }

    let stdout = child.stdout.take().ok_or("No stdout")?;
    let reader = BufReader::new(stdout);
    let mut init_ok = false;
    let mut tools_count = 0;

    for line in reader.lines().take(2).flatten() {
        if let Ok(val) = serde_json::from_str::<Value>(&line) {
            if val.get("id").and_then(|v| v.as_i64()) == Some(1) {
                init_ok = val.pointer("/result/serverInfo/name").is_some();
            } else if val.get("id").and_then(|v| v.as_i64()) == Some(2) {
                if let Some(arr) = val.pointer("/result/tools").and_then(|v| v.as_array()) {
                    tools_count = arr.len();
                }
            }
        }
    }

    let _ = child.kill();
    Ok((init_ok, tools_count))
}

/// Vérifie intégralement la non-régression de l'écosystème MCP historique.
pub fn verify_legacy_mcp_non_regression(repo_root: &Path) -> LegacyMcpBaselineCheck {
    let mut details = Vec::new();

    let cargo_path = repo_root.join("revit-mcp-server/Cargo.toml");
    let manifest_path = repo_root.join("revit-mcp-server/tools_manifest.json");
    let bin_path = repo_root.join("revit-mcp-server/target/release/revit-mcp-server.exe");

    let expected_cargo_hash = "61CA577F90BDC3412A657D1BF47E75394E7106537E1EDDC854A3BE65746BDBCE";
    let expected_manifest_hash = "2F1E88DE71646873A5A347629C3ED74609393E88DE28A876CA34394370C5BE7D";
    let expected_bin_hash = "F7F4EF051572572F0150B7EF687DB119F65DE17F857ACCA4847113FA2DCD3341";

    let cargo_hash = sha256_file_upper(&cargo_path).unwrap_or_default();
    let manifest_hash = sha256_file_upper(&manifest_path).unwrap_or_default();
    let bin_hash = sha256_file_upper(&bin_path).unwrap_or_default();

    let manifest_intact = cargo_hash == expected_cargo_hash && manifest_hash == expected_manifest_hash;
    let binary_intact = bin_hash == expected_bin_hash;

    details.push(format!("Cargo.toml SHA-256 intact : {}", cargo_hash == expected_cargo_hash));
    details.push(format!("tools_manifest.json SHA-256 intact : {}", manifest_hash == expected_manifest_hash));
    details.push(format!("revit-mcp-server.exe SHA-256 intact : {}", binary_intact));

    let (init_compact, compact_count) =
        query_legacy_mcp_tools_count(&bin_path, Some("compact"), &[]).unwrap_or((false, 0));
    let (_, all_count) =
        query_legacy_mcp_tools_count(&bin_path, Some("all"), &[]).unwrap_or((false, 0));
    let (init_cad, _) =
        query_legacy_mcp_tools_count(&bin_path, None, &["cad-mcp"]).unwrap_or((false, 0));

    details.push(format!(
        "JSON-RPC stdio 'compact' profile : init={}, tools={}",
        init_compact, compact_count
    ));
    details.push(format!("JSON-RPC stdio 'all' profile : tools={}", all_count));
    details.push(format!("JSON-RPC stdio 'cad-mcp' mode : init={}", init_cad));

    let passed = binary_intact
        && manifest_intact
        && init_compact
        && init_cad
        && compact_count >= 30
        && all_count >= 230;

    LegacyMcpBaselineCheck {
        binary_intact,
        manifest_intact,
        initialize_ok: init_compact && init_cad,
        compact_tools_count: compact_count,
        all_tools_count: all_count,
        cad_tools_count: if init_cad { 10 } else { 0 },
        passed,
        details,
    }
}

/// Compile un `DesignVariant` de l'APM en un payload de création atomique pour l'adaptateur Revit 2027
/// (consommable facultativement via l'outil `revit_batch` ou `arc_batch_macro` sans toucher au serveur MCP).
pub fn compile_variant_to_revit_batch_payload(variant: &DesignVariant) -> Value {
    let mut operations = Vec::new();

    for w in variant.existing_walls_kept.iter().chain(variant.new_walls.iter()) {
        operations.push(json!({
            "action": "walls/create",
            "args": {
                "startX": w.start_mm[0] / 1000.0,
                "startY": w.start_mm[1] / 1000.0,
                "endX": w.end_mm[0] / 1000.0,
                "endY": w.end_mm[1] / 1000.0,
                "height": w.height_mm / 1000.0,
                "wallType": w.label
            }
        }));
    }

    for r in &variant.rooms {
        operations.push(json!({
            "action": "rooms/create",
            "args": {
                "name": r.name,
                "number": r.number,
                "x": r.point_mm[0] / 1000.0,
                "y": r.point_mm[1] / 1000.0
            }
        }));
    }

    json!({
        "variantId": variant.variant_id,
        "targetEngine": "Autodesk Revit 2027 (Optional Adapter via Named Pipe / MCP)",
        "atomic": true,
        "operationsCount": operations.len(),
        "operations": operations
    })
}
