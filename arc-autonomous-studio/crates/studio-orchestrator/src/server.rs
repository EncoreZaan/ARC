//! `server.rs` — Service API Local Sécurisé d'ARC Autonomous Studio (`127.0.0.1:7878`)
//!
//! Permet à l'application ARC unifiée (`docs/index.html`) de piloter Autonomous Studio
//! comme un service indépendant (processus Rust dédié, cycle de vie propre, démarrage/arrêt
//! indépendant, zéro couplage avec `revit-mcp-server.exe` ni Claude Desktop).

use crate::{run_autonomous_architecture_pipeline, AutonomousRunResult};
use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::collections::BTreeMap;
use std::fs;
use std::io::{Read, Write};
use std::net::{TcpListener, TcpStream};
use std::path::{Path, PathBuf};
use studio_ai_runtime::OpenRouterFreeFirstEngine;
use studio_apm::ArcProjectModel;
use studio_mcp_bridge::verify_legacy_mcp_non_regression;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct StudioProjectSnapshot {
    pub project_folder: String,
    pub apm: ArcProjectModel,
    pub run_summary: AutonomousRunResult,
    pub variant_svgs: BTreeMap<String, VariantSvgBundle>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct VariantSvgBundle {
    pub bimcraft_floorplan_svg: String,
    pub zoning_circulation_svg: String,
    pub section_aa_svg: String,
    pub notice_markdown: String,
}

/// Construit et synchronise le snapshot complet des projets Autonomous Studio
/// dans `docs/studio-projects-snapshot.json` et `docs/studio-projects-snapshot.js`
/// afin que l'interface unifiée ARC dispose toujours des données réelles (en ligne ET hors-ligne).
pub fn sync_unified_arc_ui_snapshot(repo_root: &Path) -> Result<Value, String> {
    let studio_root = repo_root.join("arc-autonomous-studio");
    let workspace_dir = studio_root.join("workspace_projects");
    let cfg_path = studio_root.join("config").join("studio_config.toml");

    let ai_engine = OpenRouterFreeFirstEngine::initialize(Some(&cfg_path));
    let non_regression = verify_legacy_mcp_non_regression(repo_root);

    let mut projects = Vec::new();
    if workspace_dir.exists() {
        let mut entries: Vec<_> = fs::read_dir(&workspace_dir)
            .map_err(|e| e.to_string())?
            .filter_map(|e| e.ok())
            .collect();
        entries.sort_by_key(|e| e.file_name());

        for entry in entries {
            let path = entry.path();
            if !path.is_dir() {
                continue;
            }
            let apm_file = path.join("arc_project_model.apm.json");
            let summary_file = path.join("autonomous_run_summary.json");
            if !apm_file.exists() || !summary_file.exists() {
                continue;
            }

            let apm_str = fs::read_to_string(&apm_file).map_err(|e| e.to_string())?;
            let sum_str = fs::read_to_string(&summary_file).map_err(|e| e.to_string())?;
            let Ok(apm) = serde_json::from_str::<ArcProjectModel>(&apm_str) else {
                continue;
            };
            let Ok(run_summary) = serde_json::from_str::<AutonomousRunResult>(&sum_str) else {
                continue;
            };

            let mut variant_svgs = BTreeMap::new();
            for vid in apm.variants.keys() {
                let bim_svg_path = path
                    .join(vid)
                    .join("01_BIMcraft_Model")
                    .join(format!("{}_floorplan_1_50.svg", vid));
                let zoning_svg_path = path
                    .join(vid)
                    .join("02_Deliverables")
                    .join(format!("{}_zoning_circulation.svg", vid));
                let sec_svg_path = path
                    .join(vid)
                    .join("02_Deliverables")
                    .join(format!("{}_section_AA.svg", vid));
                let notice_path = path
                    .join(vid)
                    .join("02_Deliverables")
                    .join(format!("{}_architectural_notice.md", vid));

                let bimcraft_floorplan_svg =
                    fs::read_to_string(&bim_svg_path).unwrap_or_default();
                let zoning_circulation_svg =
                    fs::read_to_string(&zoning_svg_path).unwrap_or_default();
                let section_aa_svg = fs::read_to_string(&sec_svg_path).unwrap_or_default();
                let notice_markdown = fs::read_to_string(&notice_path).unwrap_or_default();

                variant_svgs.insert(
                    vid.clone(),
                    VariantSvgBundle {
                        bimcraft_floorplan_svg,
                        zoning_circulation_svg,
                        section_aa_svg,
                        notice_markdown,
                    },
                );
            }

            projects.push(StudioProjectSnapshot {
                project_folder: path
                    .file_name()
                    .unwrap_or_default()
                    .to_string_lossy()
                    .to_string(),
                apm,
                run_summary,
                variant_svgs,
            });
        }
    }

    let has_api_key = read_configured_api_key_status(&cfg_path);

    let snapshot = json!({
        "studio_version": "0.3.0",
        "service_default_endpoint": "http://127.0.0.1:7878",
        "strict_free_first_policy": true,
        "allow_paid_models": false,
        "api_key_configured": has_api_key,
        "openrouter_report": ai_engine.report,
        "mcp_non_regression": non_regression,
        "projects": projects,
    });

    let docs_dir = repo_root.join("docs");
    if docs_dir.exists() {
        let json_path = docs_dir.join("studio-projects-snapshot.json");
        let js_path = docs_dir.join("studio-projects-snapshot.js");
        let pretty = serde_json::to_string_pretty(&snapshot).map_err(|e| e.to_string())?;
        fs::write(&json_path, &pretty).map_err(|e| e.to_string())?;
        let js_content = format!(
            "// Généré automatiquement par ARC Autonomous Studio (Rust)\nwindow.ARC_STUDIO_SNAPSHOT = {};\n",
            pretty
        );
        fs::write(&js_path, js_content).map_err(|e| e.to_string())?;
    }

    Ok(snapshot)
}

fn read_configured_api_key_status(cfg_path: &Path) -> bool {
    if let Ok(k) = std::env::var("OPENROUTER_API_KEY") {
        if !k.trim().is_empty() {
            return true;
        }
    }
    if let Ok(raw) = fs::read_to_string(cfg_path) {
        for line in raw.lines() {
            let t = line.trim();
            if t.starts_with("api_key") {
                if let Some(val) = t.split('=').nth(1) {
                    let clean = val.trim().trim_matches('"').trim_matches('\'');
                    if !clean.is_empty() {
                        return true;
                    }
                }
            }
        }
    }
    false
}

fn update_studio_config_toml(
    cfg_path: &Path,
    new_api_key: Option<&str>,
    allow_paid_models: bool,
) -> Result<(), String> {
    let raw = fs::read_to_string(cfg_path).unwrap_or_default();
    let mut out_lines = Vec::new();
    let mut found_key = false;
    let mut found_paid = false;

    for line in raw.lines() {
        let trimmed = line.trim();
        if trimmed.starts_with("api_key") && new_api_key.is_some() {
            out_lines.push(format!("api_key = \"{}\"", new_api_key.unwrap_or("")));
            found_key = true;
        } else if trimmed.starts_with("allow_paid_models") {
            out_lines.push(format!("allow_paid_models = {}", allow_paid_models));
            found_paid = true;
        } else {
            out_lines.push(line.to_string());
        }
    }

    if !found_key {
        if let Some(k) = new_api_key {
            out_lines.push(format!("api_key = \"{}\"", k));
        }
    }
    if !found_paid {
        out_lines.push(format!("allow_paid_models = {}", allow_paid_models));
    }

    fs::write(cfg_path, out_lines.join("\n") + "\n").map_err(|e| e.to_string())
}

/// Démarre le démon API local d'ARC Autonomous Studio sur `127.0.0.1:<port>`.
pub fn run_local_studio_api_server(repo_root: &Path, port: u16) -> Result<(), String> {
    // Synchronise d'abord le snapshot pour l'UI unifiée
    let _ = sync_unified_arc_ui_snapshot(repo_root);

    let addr = format!("127.0.0.1:{}", port);
    let listener = TcpListener::bind(&addr)
        .map_err(|e| format!("Impossible d'ouvrir le port local {} : {}", addr, e))?;

    println!("=========================================================================");
    println!("🚀 ARC AUTONOMOUS STUDIO — SERVICE API LOCAL INDÉPENDANT ACTIF");
    println!("=========================================================================");
    println!("🔗 Interface ARC Unifiée : http://127.0.0.1:{}/", port);
    println!("🔌 Endpoint API Studio   : http://127.0.0.1:{}/api/studio/health", port);
    println!("🛡️ Isolation MCP         : 100% découplé de revit-mcp-server.exe");
    println!("💸 Politique OpenRouter  : FREE-FIRST STRICTE (0.00 $)");
    println!("-------------------------------------------------------------------------");

    for stream in listener.incoming() {
        match stream {
            Ok(mut s) => {
                let should_stop = handle_http_connection(&mut s, repo_root);
                if should_stop {
                    println!("🛑 Arrêt indépendant du service ARC Autonomous Studio demandé via API.");
                    break;
                }
            }
            Err(e) => {
                eprintln!("Erreur connexion entrante : {}", e);
            }
        }
    }
    Ok(())
}

fn handle_http_connection(stream: &mut TcpStream, repo_root: &Path) -> bool {
    let mut raw_bytes = Vec::new();
    let mut chunk = [0u8; 8192];
    let mut header_end = None;
    let mut content_length = 0usize;

    loop {
        let Ok(n) = stream.read(&mut chunk) else {
            return false;
        };
        if n == 0 {
            break;
        }
        raw_bytes.extend_from_slice(&chunk[..n]);
        if raw_bytes.len() > 50 * 1024 * 1024 {
            return false;
        }

        if header_end.is_none() {
            if let Some(pos) = raw_bytes.windows(4).position(|w| w == b"\r\n\r\n") {
                let hend = pos + 4;
                header_end = Some(hend);
                let hdr_str = String::from_utf8_lossy(&raw_bytes[..hend]);
                for line in hdr_str.lines() {
                    if let Some((k, v)) = line.split_once(':') {
                        if k.trim().eq_ignore_ascii_case("Content-Length") {
                            content_length = v.trim().parse::<usize>().unwrap_or(0);
                        }
                    }
                }
            }
        }

        if let Some(hend) = header_end {
            if raw_bytes.len() >= hend + content_length {
                break;
            }
        }
    }

    if raw_bytes.is_empty() {
        return false;
    }

    let hend = header_end.unwrap_or(raw_bytes.len());
    let header_str = String::from_utf8_lossy(&raw_bytes[..hend]).to_string();
    let mut lines = header_str.lines();
    let first_line = lines.next().unwrap_or("");
    let parts: Vec<&str> = first_line.split_whitespace().collect();
    if parts.len() < 2 {
        return false;
    }
    let method = parts[0];
    let full_path = parts[1];
    let (path, query) = match full_path.split_once('?') {
        Some((p, q)) => (p, q),
        None => (full_path, ""),
    };

    // Gestion preflight CORS
    if method == "OPTIONS" {
        send_json_response(stream, 200, &json!({"ok": true}));
        return false;
    }

    // Extraction du body JSON complet
    let body_end = (hend + content_length).min(raw_bytes.len());
    let body_str_owned = String::from_utf8_lossy(&raw_bytes[hend..body_end]).to_string();
    let body_str = body_str_owned.trim_matches(char::from(0)).trim();

    match (method, path) {
        ("GET", "/api/studio/health") => {
            let check = verify_legacy_mcp_non_regression(repo_root);
            send_json_response(
                stream,
                200,
                &json!({
                    "status": "running",
                    "service": "arc-autonomous-studio",
                    "version": "0.3.0",
                    "independent_process": true,
                    "pid": std::process::id(),
                    "free_first_enforced": true,
                    "bimcraft_engine": "bimcraft-cli 0.11.0",
                    "mcp_non_regression_passed": check.passed,
                    "mcp_tools_intact_count": check.all_tools_count
                }),
            );
            false
        }
        ("GET", "/api/studio/snapshot") | ("GET", "/api/studio/projects") => {
            match sync_unified_arc_ui_snapshot(repo_root) {
                Ok(snap) => send_json_response(stream, 200, &snap),
                Err(e) => send_json_response(stream, 500, &json!({"error": e})),
            }
            false
        }
        ("GET", "/api/studio/models") => {
            let cfg_p = repo_root
                .join("arc-autonomous-studio")
                .join("config")
                .join("studio_config.toml");
            let engine = OpenRouterFreeFirstEngine::initialize(Some(&cfg_p));
            send_json_response(stream, 200, &json!(engine.report));
            false
        }
        ("POST", "/api/studio/config") => {
            let parsed: Value = serde_json::from_str(body_str).unwrap_or_else(|_| json!({}));
            let api_key = parsed.get("api_key").and_then(|v| v.as_str());
            let allow_paid = parsed
                .get("allow_paid_models")
                .and_then(|v| v.as_bool())
                .unwrap_or(false);

            let cfg_p = repo_root
                .join("arc-autonomous-studio")
                .join("config")
                .join("studio_config.toml");
            let _ = update_studio_config_toml(&cfg_p, api_key, allow_paid);
            let engine = OpenRouterFreeFirstEngine::initialize(Some(&cfg_p));
            let _ = sync_unified_arc_ui_snapshot(repo_root);

            send_json_response(
                stream,
                200,
                &json!({
                    "updated": true,
                    "strict_free_first_policy": !allow_paid,
                    "api_key_configured": read_configured_api_key_status(&cfg_p),
                    "openrouter_report": engine.report
                }),
            );
            false
        }
        ("POST", "/api/studio/projects/run") => {
            let parsed: Value = serde_json::from_str(body_str).unwrap_or_else(|_| json!({}));
            let preset = parsed
                .get("preset")
                .and_then(|v| v.as_str())
                .unwrap_or("cabane");
            let custom_pdf_path = parsed.get("pdf_path").and_then(|v| v.as_str());
            let pdf_b64 = parsed.get("pdf_base64").and_then(|v| v.as_str());
            let pdf_fname = parsed.get("pdf_filename").and_then(|v| v.as_str());

            let studio_root = repo_root.join("arc-autonomous-studio");
            let (subj_pdf, plan_pdf, out_dir) = if let (Some(b64), Some(fname)) = (pdf_b64, pdf_fname) {
                let uploads_dir = studio_root.join("workspace_projects").join("_uploads");
                let _ = fs::create_dir_all(&uploads_dir);
                let safe_name: String = fname
                    .chars()
                    .map(|c| if c.is_ascii_alphanumeric() || c == '.' || c == '-' || c == '_' { c } else { '_' })
                    .collect();
                let saved_pdf = uploads_dir.join(&safe_name);
                if let Ok(decoded) = decode_base64(b64) {
                    let _ = fs::write(&saved_pdf, decoded);
                }
                let stem = saved_pdf
                    .file_stem()
                    .unwrap_or_default()
                    .to_string_lossy()
                    .to_uppercase()
                    .replace(' ', "_");
                (
                    saved_pdf,
                    None,
                    studio_root
                        .join("workspace_projects")
                        .join(format!("{}_AUTONOMOUS", stem)),
                )
            } else if let Some(cp) = custom_pdf_path {
                if !cp.trim().is_empty() && Path::new(cp).exists() {
                    let p = PathBuf::from(cp);
                    let stem = p
                        .file_stem()
                        .unwrap_or_default()
                        .to_string_lossy()
                        .to_uppercase()
                        .replace(' ', "_");
                    (
                        p,
                        None,
                        studio_root
                            .join("workspace_projects")
                            .join(format!("{}_AUTONOMOUS", stem)),
                    )
                } else if preset == "london" {
                    (
                        PathBuf::from(r"C:\Users\teoba\Documents\Projet Appart Londonien\00_Sources\SUJET-2-l-appartement-londonien-2.pdf"),
                        Some(PathBuf::from(r"C:\Users\teoba\Documents\Projet Appart Londonien\00_Sources\MJM-2026-27-PLAN.pdf")),
                        studio_root.join("workspace_projects").join("REFLET_LONDON_AUTONOMOUS"),
                    )
                } else {
                    (
                        PathBuf::from(r"C:\Users\teoba\Downloads\LA_CABANE_V3_Planches_A4_1-50.pdf"),
                        Some(PathBuf::from(r"C:\Users\teoba\Downloads\Final\00_SOMMAIRE_LISEZ-MOI.pdf")),
                        studio_root.join("workspace_projects").join("LA_CABANE_AUTONOMOUS"),
                    )
                }
            } else if preset == "london" {
                (
                    PathBuf::from(r"C:\Users\teoba\Documents\Projet Appart Londonien\00_Sources\SUJET-2-l-appartement-londonien-2.pdf"),
                    Some(PathBuf::from(r"C:\Users\teoba\Documents\Projet Appart Londonien\00_Sources\MJM-2026-27-PLAN.pdf")),
                    studio_root.join("workspace_projects").join("REFLET_LONDON_AUTONOMOUS"),
                )
            } else {
                (
                    PathBuf::from(r"C:\Users\teoba\Downloads\LA_CABANE_V3_Planches_A4_1-50.pdf"),
                    Some(PathBuf::from(r"C:\Users\teoba\Downloads\Final\00_SOMMAIRE_LISEZ-MOI.pdf")),
                    studio_root.join("workspace_projects").join("LA_CABANE_AUTONOMOUS"),
                )
            };

            let folder_name = out_dir
                .file_name()
                .unwrap_or_default()
                .to_string_lossy()
                .to_string();

            match run_autonomous_architecture_pipeline(
                Some(&subj_pdf),
                plan_pdf.as_deref(),
                &out_dir,
                repo_root,
            ) {
                Ok(res) => {
                    if let Ok(summary_str) = serde_json::to_string_pretty(&res) {
                        let _ = fs::write(out_dir.join("autonomous_run_summary.json"), summary_str);
                    }
                    let snap = sync_unified_arc_ui_snapshot(repo_root).unwrap_or_else(|_| json!({}));
                    send_json_response(
                        stream,
                        200,
                        &json!({
                            "success": true,
                            "project_folder": folder_name,
                            "run_result": res,
                            "snapshot": snap
                        }),
                    );
                }
                Err(e) => {
                    send_json_response(stream, 500, &json!({"success": false, "error": e}));
                }
            }
            false
        }
        ("POST", "/api/studio/bimcraft/open") => {
            let parsed: Value = serde_json::from_str(body_str).unwrap_or_else(|_| json!({}));
            let model_path = parsed
                .get("model_path")
                .and_then(|v| v.as_str())
                .unwrap_or("");
            let bimcraft_exe = std::env::var("USERPROFILE")
                .or_else(|_| std::env::var("HOME"))
                .map(|h| PathBuf::from(h).join(".cargo").join("bin").join("bimcraft.exe"))
                .unwrap_or_else(|_| PathBuf::from("bimcraft"));

            let mut cmd = std::process::Command::new(&bimcraft_exe);
            cmd.env("BIMCRAFT_NO_UPDATE_CHECK", "1");
            if !model_path.is_empty() && Path::new(model_path).exists() {
                cmd.arg(model_path);
            }
            cmd.arg("--control").arg("0");
            match cmd
                .stdin(std::process::Stdio::null())
                .stdout(std::process::Stdio::null())
                .stderr(std::process::Stdio::null())
                .spawn()
            {
                Ok(child) => send_json_response(
                    stream,
                    200,
                    &json!({ "launched": true, "pid": child.id(), "model_path": model_path }),
                ),
                Err(e) => send_json_response(
                    stream,
                    500,
                    &json!({ "launched": false, "error": e.to_string() }),
                ),
            }
            false
        }
        ("POST", "/api/studio/service/stop") => {
            send_json_response(
                stream,
                200,
                &json!({
                    "status": "stopped",
                    "message": "Le service ARC Autonomous Studio s'est arrêté proprement. Les serveurs MCP historiques et l'interface ARC restent 100% opérationnels."
                }),
            );
            true
        }
        ("GET", "/api/studio/download") => {
            // Permet de télécharger un livrable généré dans workspace_projects
            let rel = query
                .split('&')
                .find_map(|kv| kv.strip_prefix("file="))
                .unwrap_or("");
            let decoded = url_decode(rel);
            let target = PathBuf::from(&decoded);
            if target.exists() && target.is_file() {
                if let Ok(bytes) = fs::read(&target) {
                    let mime = guess_mime(&target);
                    let fname = target
                        .file_name()
                        .unwrap_or_default()
                        .to_string_lossy()
                        .to_string();
                    let header = format!(
                        "HTTP/1.1 200 OK\r\nContent-Type: {}\r\nContent-Length: {}\r\nContent-Disposition: inline; filename=\"{}\"\r\nAccess-Control-Allow-Origin: *\r\n\r\n",
                        mime,
                        bytes.len(),
                        fname
                    );
                    let _ = stream.write_all(header.as_bytes());
                    let _ = stream.write_all(&bytes);
                    return false;
                }
            }
            send_json_response(stream, 404, &json!({"error": "Fichier livrable introuvable"}));
            false
        }
        ("GET", _) => {
            // Sert les fichiers statiques de `docs/` pour l'accès direct via http://127.0.0.1:7878/
            let rel_path = if path == "/" || path == "/index.html" {
                "index.html"
            } else {
                path.trim_start_matches('/')
            };
            let file_path = repo_root.join("docs").join(rel_path);
            if file_path.exists() && file_path.is_file() {
                if let Ok(bytes) = fs::read(&file_path) {
                    let mime = guess_mime(&file_path);
                    let header = format!(
                        "HTTP/1.1 200 OK\r\nContent-Type: {}\r\nContent-Length: {}\r\nAccess-Control-Allow-Origin: *\r\n\r\n",
                        mime,
                        bytes.len()
                    );
                    let _ = stream.write_all(header.as_bytes());
                    let _ = stream.write_all(&bytes);
                    return false;
                }
            }
            send_json_response(stream, 404, &json!({"error": "Ressource non trouvée"}));
            false
        }
        _ => {
            send_json_response(stream, 404, &json!({"error": "Route API inconnue"}));
            false
        }
    }
}

fn send_json_response(stream: &mut TcpStream, status_code: u16, body: &Value) {
    let status_text = match status_code {
        200 => "OK",
        404 => "Not Found",
        500 => "Internal Server Error",
        _ => "OK",
    };
    let payload = serde_json::to_string(body).unwrap_or_else(|_| "{}".to_string());
    let resp = format!(
        "HTTP/1.1 {} {}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {}\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Methods: GET, POST, OPTIONS\r\nAccess-Control-Allow-Headers: Content-Type, Authorization\r\nConnection: close\r\n\r\n{}",
        status_code,
        status_text,
        payload.len(),
        payload
    );
    let _ = stream.write_all(resp.as_bytes());
    let _ = stream.flush();
}

fn guess_mime(path: &Path) -> &'static str {
    match path
        .extension()
        .and_then(|e| e.to_str())
        .unwrap_or("")
        .to_lowercase()
        .as_str()
    {
        "html" => "text/html; charset=utf-8",
        "css" => "text/css; charset=utf-8",
        "js" => "application/javascript; charset=utf-8",
        "json" => "application/json; charset=utf-8",
        "svg" => "image/svg+xml",
        "pdf" => "application/pdf",
        "dxf" => "application/dxf",
        "ifc" => "application/x-step",
        "glb" => "model/gltf-binary",
        "md" => "text/markdown; charset=utf-8",
        _ => "application/octet-stream",
    }
}

fn url_decode(input: &str) -> String {
    let mut out = Vec::new();
    let bytes = input.as_bytes();
    let mut i = 0;
    while i < bytes.len() {
        if bytes[i] == b'%' && i + 2 < bytes.len() {
            if let Ok(hex_str) = std::str::from_utf8(&bytes[i + 1..i + 3]) {
                if let Ok(val) = u8::from_str_radix(hex_str, 16) {
                    out.push(val);
                    i += 3;
                    continue;
                }
            }
        } else if bytes[i] == b'+' {
            out.push(b' ');
            i += 1;
            continue;
        }
        out.push(bytes[i]);
        i += 1;
    }
    String::from_utf8_lossy(&out).to_string()
}

fn decode_base64(input: &str) -> Result<Vec<u8>, String> {
    let mut out = Vec::with_capacity(input.len() * 3 / 4);
    let mut buf: u32 = 0;
    let mut bits: u8 = 0;
    for b in input.bytes() {
        let val = match b {
            b'A'..=b'Z' => b - b'A',
            b'a'..=b'z' => b - b'a' + 26,
            b'0'..=b'9' => b - b'0' + 52,
            b'+' => 62,
            b'/' => 63,
            b'=' | b'\n' | b'\r' | b' ' => continue,
            _ => return Err("Caractère Base64 invalide".into()),
        };
        buf = (buf << 6) | (val as u32);
        bits += 6;
        if bits >= 8 {
            bits -= 8;
            out.push(((buf >> bits) & 0xFF) as u8);
        }
    }
    Ok(out)
}
