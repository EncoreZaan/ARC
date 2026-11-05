mod geometry;
mod solver;

use geometry::{ClashElement, Polygon2D};
use reqwest::Client;
use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::collections::HashMap;
use std::env;
use std::sync::Arc;
use std::time::Duration;
use tokio::io::{AsyncBufReadExt, AsyncWriteExt, BufReader};
use url::Url;

const TOOLS_MANIFEST_RAW: &str = include_str!("../tools_manifest.json");

const DEFAULT_REVIT_HOST: &str = "http://localhost:6543";
const DEFAULT_TIMEOUT_MS: u64 = 120_000;
const MIN_TIMEOUT_MS: u64 = 1_000;
const MAX_TIMEOUT_MS: u64 = 600_000;

#[derive(Debug, Clone, Deserialize, Serialize)]
struct ManifestTool {
    name: String,
    description: String,
    #[serde(rename = "inputSchema")]
    input_schema: Value,
    action: String,
    #[serde(rename = "isHealthCheck")]
    is_health_check: bool,
}

#[derive(Debug, Deserialize)]
struct ManifestRoot {
    tools: Vec<ManifestTool>,
}

#[derive(Debug, Clone)]
struct AppConfig {
    host: String,
    timeout: Duration,
}

struct AppState {
    config: AppConfig,
    client: Client,
    tools_map: HashMap<String, ManifestTool>,
    tools_list_json: Value,
}

#[derive(Debug, Deserialize)]
struct JsonRpcRequest {
    id: Option<Value>,
    method: String,
    params: Option<Value>,
}

fn resolve_config() -> AppConfig {
    let raw_host = env::var("REVIT_HOST").unwrap_or_else(|_| DEFAULT_REVIT_HOST.to_string());
    let allow_remote = env::var("REVIT_ALLOW_REMOTE").map(|v| v == "1").unwrap_or(false);

    let parsed_url = Url::parse(&raw_host).unwrap_or_else(|_| {
        eprintln!("[WARN] Invalid REVIT_HOST '{}'. Falling back to default.", raw_host);
        Url::parse(DEFAULT_REVIT_HOST).unwrap()
    });

    let host = if !allow_remote {
        let is_loopback = match parsed_url.host_str() {
            Some(h) => h == "localhost" || h == "127.0.0.1" || h == "::1" || h == "[::1]",
            None => false,
        };
        if !is_loopback {
            eprintln!("[WARN] Remote REVIT_HOST detected without REVIT_ALLOW_REMOTE=1. Falling back to localhost.");
            DEFAULT_REVIT_HOST.to_string()
        } else {
            parsed_url.origin().ascii_serialization()
        }
    } else {
        parsed_url.origin().ascii_serialization()
    };

    let timeout_ms = env::var("REVIT_TIMEOUT_MS")
        .ok()
        .and_then(|v| v.parse::<u64>().ok())
        .map(|ms| ms.clamp(MIN_TIMEOUT_MS, MAX_TIMEOUT_MS))
        .unwrap_or(DEFAULT_TIMEOUT_MS);

    AppConfig {
        host,
        timeout: Duration::from_millis(timeout_ms),
    }
}

/// Zero-latency Windows Named Pipe IPC client.
#[cfg(windows)]
async fn try_pipe_call(action: &str, mut payload: Value) -> Result<Value, String> {
    use tokio::net::windows::named_pipe::ClientOptions;

    let pipe_path = r"\\.\pipe\revit_mcp_ipc";
    let client = ClientOptions::new().open(pipe_path).map_err(|e| e.to_string())?;
    let (reader, mut writer) = tokio::io::split(client);
    let mut buf_reader = BufReader::new(reader);

    if let Value::Object(ref mut map) = payload {
        map.insert("action".to_string(), json!(action));
    } else {
        payload = json!({ "action": action });
    }

    let mut line = serde_json::to_string(&payload).map_err(|e| e.to_string())?;
    line.push('\n');

    writer.write_all(line.as_bytes()).await.map_err(|e| e.to_string())?;
    writer.flush().await.map_err(|e| e.to_string())?;

    let mut response_line = String::new();
    buf_reader.read_line(&mut response_line).await.map_err(|e| e.to_string())?;

    let resp_val: Value = serde_json::from_str(&response_line).map_err(|e| e.to_string())?;
    Ok(resp_val)
}

#[cfg(not(windows))]
async fn try_pipe_call(_action: &str, _payload: Value) -> Result<Value, String> {
    Err("Named pipe IPC is Windows-only".to_string())
}

/// Dispatches a request to Revit, prioritizing ultra-low latency Named Pipes and falling back to HTTP.
async fn dispatch_bridge_request(
    state: &AppState,
    action: &str,
    payload: Value,
) -> Result<Value, String> {
    // 1. Try zero-latency Named Pipe first
    if let Ok(pipe_resp) = try_pipe_call(action, payload.clone()).await {
        return Ok(pipe_resp);
    }

    // 2. Fallback to HTTP Bridge
    let api_url = format!("{}/api/{}", state.config.host, action);
    match state.client.post(&api_url).json(&payload).send().await {
        Ok(resp) => {
            let body: Value = match resp.json().await {
                Ok(val) => val,
                Err(_) => json!({ "error": "Non-JSON response from Revit bridge" }),
            };
            Ok(body)
        }
        Err(e) => Err(format!("Bridge error: {}", e)),
    }
}

async fn handle_tools_call(
    state: &AppState,
    tool_name: &str,
    args: Option<Value>,
) -> (Value, bool) {
    let tool = match state.tools_map.get(tool_name) {
        Some(t) => t,
        None => {
            return (
                json!({
                    "content": [{
                        "type": "text",
                        "text": format!("Unknown tool '{}'", tool_name)
                    }],
                    "isError": true
                }),
                true,
            );
        }
    };

    let post_body = args.unwrap_or_else(|| json!({}));

    // ── NATIVE RUST GEOMETRY TOOLS ───────────────────────────────────────────
    if tool_name == "revit_fast_clash_detection" {
        let clearance_mm = post_body.get("clearanceMm").and_then(|v| v.as_f64()).unwrap_or(0.0);
        let category = post_body.get("category").and_then(|v| v.as_str());

        let mut query_payload = json!({ "limit": 2000 });
        if let Some(cat) = category {
            query_payload["category"] = json!(cat);
        }

        match dispatch_bridge_request(state, "elements/get", query_payload).await {
            Ok(elements_resp) => {
                let elements_arr = elements_resp.get("elements").and_then(|v| v.as_array());
                let mut clash_elements = Vec::new();

                if let Some(arr) = elements_arr {
                    for el in arr {
                        let id = el.get("id").and_then(|v| v.as_i64()).unwrap_or(0);
                        let cat = el.get("category").and_then(|v| v.as_str()).unwrap_or("Unknown").to_string();
                        let name = el.get("familyName").and_then(|v| v.as_str()).unwrap_or("Element").to_string();

                        if let Some(bbox_val) = el.get("bbox") {
                            let min_x = bbox_val.get("min").and_then(|m| m.get("x")).and_then(|v| v.as_f64()).unwrap_or(0.0);
                            let min_y = bbox_val.get("min").and_then(|m| m.get("y")).and_then(|v| v.as_f64()).unwrap_or(0.0);
                            let min_z = bbox_val.get("min").and_then(|m| m.get("z")).and_then(|v| v.as_f64()).unwrap_or(0.0);
                            let max_x = bbox_val.get("max").and_then(|m| m.get("x")).and_then(|v| v.as_f64()).unwrap_or(0.0);
                            let max_y = bbox_val.get("max").and_then(|m| m.get("y")).and_then(|v| v.as_f64()).unwrap_or(0.0);
                            let max_z = bbox_val.get("max").and_then(|m| m.get("z")).and_then(|v| v.as_f64()).unwrap_or(0.0);

                            clash_elements.push(ClashElement {
                                id,
                                category: cat,
                                name,
                                bbox: geometry::BBox3D { min_x, min_y, min_z, max_x, max_y, max_z },
                            });
                        }
                    }
                }

                let start_time = std::time::Instant::now();
                let clashes = geometry::run_parallel_clash_detection(&clash_elements, clearance_mm);
                let duration_ms = start_time.elapsed().as_secs_f64() * 1000.0;

                let result = json!({
                    "success": true,
                    "engine": "Rust Parallel Rayon",
                    "analyzedElements": clash_elements.len(),
                    "clashesCount": clashes.len(),
                    "executionTimeMs": (duration_ms * 100.0).round() / 100.0,
                    "clearanceMm": clearance_mm,
                    "clashes": clashes
                });

                let text = serde_json::to_string_pretty(&result).unwrap_or_default();
                return (json!({ "content": [{ "type": "text", "text": text }] }), false);
            }
            Err(e) => {
                let err_msg = format!("Failed to retrieve elements for clash analysis: {}", e);
                return (json!({ "content": [{ "type": "text", "text": err_msg }], "isError": true }), true);
            }
        }
    }

    if tool_name == "revit_analyze_room_metrics" {
        let raw_vertices = post_body.get("vertices").and_then(|v| v.as_array());
        let raw_glazed = post_body.get("glazedAreasM2").and_then(|v| v.as_array());
        let raw_orientations = post_body.get("windowOrientations").and_then(|v| v.as_array());

        let mut vertices = Vec::new();
        if let Some(arr) = raw_vertices {
            for pt in arr {
                if let Some(coords) = pt.as_array() {
                    if coords.len() >= 2 {
                        let x = coords[0].as_f64().unwrap_or(0.0);
                        let y = coords[1].as_f64().unwrap_or(0.0);
                        vertices.push([x, y]);
                    }
                }
            }
        }

        let mut glazed = Vec::new();
        if let Some(arr) = raw_glazed {
            for g in arr {
                glazed.push(g.as_f64().unwrap_or(0.0));
            }
        }

        let mut orientations = Vec::new();
        if let Some(arr) = raw_orientations {
            for o in arr {
                orientations.push(o.as_str().unwrap_or("Sud").to_string());
            }
        }

        let poly = Polygon2D { vertices };
        let metrics = geometry::analyze_spatial_metrics(&poly, &glazed, &orientations);
        let text = serde_json::to_string_pretty(&metrics).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_solve_parametric_layout" {
        let solution = solver::solve_parametric_layout(&post_body);

        // Si l'utilisateur demande d'appliquer automatiquement la prévisualisation fantôme dans Revit
        if post_body.get("autoGhostPreview").and_then(|v| v.as_bool()).unwrap_or(true) {
            if let Some(ghost_items) = solution.get("ghost_preview_items") {
                let _ = try_pipe_call("overlay/ghost-preview", json!({
                    "clearExisting": true,
                    "items": ghost_items
                })).await;
            }
        }

        let text = serde_json::to_string_pretty(&solution).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    // ── HEALTH CHECK ─────────────────────────────────────────────────────────
    if tool.is_health_check {
        // Try pipe health first
        if let Ok(pipe_health) = try_pipe_call("healthz", json!({})).await {
            let payload = json!({
                "transport": "Windows Named Pipe (Zero-Latency)",
                "pipe": r"\\.\pipe\revit_mcp_ipc",
                "health": pipe_health
            });
            let text = serde_json::to_string_pretty(&payload).unwrap_or_default();
            return (json!({ "content": [{ "type": "text", "text": text }] }), false);
        }

        let health_url = format!("{}/healthz", state.config.host);
        match state.client.get(&health_url).send().await {
            Ok(resp) => {
                let status = resp.status();
                let body: Value = resp.json().await.unwrap_or_else(|_| json!({ "status": status.as_u16() }));
                let payload = json!({
                    "transport": "HTTP",
                    "host": state.config.host,
                    "timeoutMs": state.config.timeout.as_millis(),
                    "health": body
                });
                let text = serde_json::to_string_pretty(&payload).unwrap_or_default();
                (json!({ "content": [{ "type": "text", "text": text }] }), false)
            }
            Err(e) => {
                let err_msg = format!(
                    "Cannot reach Revit addin via Named Pipe or HTTP at {}. Make sure Autodesk Revit is open with a project loaded and RevitMCP addin is running. (Error: {})",
                    state.config.host, e
                );
                (json!({ "content": [{ "type": "text", "text": err_msg }], "isError": true }), true)
            }
        }
    } else {
        // ── REGULAR REVIT MCP ACTIONS ─────────────────────────────────────────
        match dispatch_bridge_request(state, &tool.action, post_body).await {
            Ok(body) => {
                let is_mcp_err = body.get("success").and_then(|v| v.as_bool()).map(|s| !s).unwrap_or(false);
                let mut content_items = Vec::new();

                // Support multimodal natif MCP : transmission directe de l'image en Base64
                if let Some(img_b64) = body.get("imageBase64").and_then(|v| v.as_str()) {
                    let mime = body.get("mimeType")
                        .and_then(|v| v.as_str())
                        .unwrap_or("image/png");

                    content_items.push(json!({
                        "type": "image",
                        "data": img_b64,
                        "mimeType": mime
                    }));
                }

                let text = serde_json::to_string_pretty(&body).unwrap_or_default();
                content_items.push(json!({
                    "type": "text",
                    "text": text
                }));

                (
                    json!({
                        "content": content_items,
                        "isError": is_mcp_err
                    }),
                    is_mcp_err,
                )
            }
            Err(e) => {
                let err_msg = format!("Revit bridge execution failed for action '{}': {}", tool.action, e);
                (json!({ "content": [{ "type": "text", "text": err_msg }], "isError": true }), true)
            }
        }
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    eprintln!("[INFO] Revit MCP Server in Rust (v2.5.0) starting with Named Pipe IPC & Multithreaded Geometry Engine...");

    let config = resolve_config();
    eprintln!("[INFO] Target bridge: {} (Timeout: {} ms)", config.host, config.timeout.as_millis());

    let client = Client::builder()
        .timeout(config.timeout)
        .build()
        .expect("Failed to build HTTP client");

    let manifest: ManifestRoot = serde_json::from_str(TOOLS_MANIFEST_RAW)
        .expect("Failed to parse embedded tools manifest");

    let mut tools_map = HashMap::new();
    let mut tools_list_vec = Vec::new();

    for t in manifest.tools {
        let public_tool = json!({
            "name": t.name,
            "description": t.description,
            "inputSchema": t.input_schema
        });
        tools_list_vec.push(public_tool);
        tools_map.insert(t.name.clone(), t);
    }

    eprintln!("[INFO] Loaded {} tools into registry.", tools_map.len());

    let state = Arc::new(AppState {
        config,
        client,
        tools_map,
        tools_list_json: json!(tools_list_vec),
    });

    let stdin = tokio::io::stdin();
    let mut stdout = tokio::io::stdout();
    let reader = BufReader::new(stdin);
    let mut lines = reader.lines();

    while let Ok(Some(line)) = lines.next_line().await {
        let trimmed = line.trim();
        if trimmed.is_empty() {
            continue;
        }

        let req: JsonRpcRequest = match serde_json::from_str(trimmed) {
            Ok(r) => r,
            Err(e) => {
                eprintln!("[WARN] Malformed JSON-RPC request: {}", e);
                continue;
            }
        };

        match req.method.as_str() {
            "initialize" => {
                let resp = json!({
                    "jsonrpc": "2.0",
                    "id": req.id,
                    "result": {
                        "protocolVersion": "2024-11-05",
                        "capabilities": {
                            "tools": {
                                "listChanged": false
                            }
                        },
                        "serverInfo": {
                            "name": "revit-claude-mcp",
                            "version": "2.5.0"
                        }
                    }
                });
                let mut out = serde_json::to_string(&resp)?;
                out.push('\n');
                stdout.write_all(out.as_bytes()).await?;
                stdout.flush().await?;
            }

            "notifications/initialized" => {
                eprintln!("[INFO] Client completed initialization.");
            }

            "ping" => {
                if let Some(id) = req.id {
                    let resp = json!({
                        "jsonrpc": "2.0",
                        "id": id,
                        "result": {}
                    });
                    let mut out = serde_json::to_string(&resp)?;
                    out.push('\n');
                    stdout.write_all(out.as_bytes()).await?;
                    stdout.flush().await?;
                }
            }

            "tools/list" => {
                let resp = json!({
                    "jsonrpc": "2.0",
                    "id": req.id,
                    "result": {
                        "tools": &state.tools_list_json
                    }
                });
                let mut out = serde_json::to_string(&resp)?;
                out.push('\n');
                stdout.write_all(out.as_bytes()).await?;
                stdout.flush().await?;
            }

            "tools/call" => {
                let params = req.params.unwrap_or_else(|| json!({}));
                let tool_name = params.get("name").and_then(|v| v.as_str()).unwrap_or("");
                let args = params.get("arguments").cloned();

                let (call_result, _) = handle_tools_call(&state, tool_name, args).await;

                let resp = json!({
                    "jsonrpc": "2.0",
                    "id": req.id,
                    "result": call_result
                });
                let mut out = serde_json::to_string(&resp)?;
                out.push('\n');
                stdout.write_all(out.as_bytes()).await?;
                stdout.flush().await?;
            }

            unknown => {
                eprintln!("[DEBUG] Unhandled method: {}", unknown);
                if let Some(id) = req.id {
                    let resp = json!({
                        "jsonrpc": "2.0",
                        "id": id,
                        "error": {
                            "code": -32601,
                            "message": format!("Method '{}' not found", unknown)
                        }
                    });
                    let mut out = serde_json::to_string(&resp)?;
                    out.push('\n');
                    stdout.write_all(out.as_bytes()).await?;
                    stdout.flush().await?;
                }
            }
        }
    }

    eprintln!("[INFO] Revit MCP Server shutting down.");
    Ok(())
}
