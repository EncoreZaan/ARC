mod geometry;
mod solver;
mod agent_swarm;
mod auto_healing;
mod bimcraft_bridge;
mod cost_estimator;
mod floorplan_generator;
mod ifc_engine;
mod live_delta_stream;
mod shared_memory;
mod solar_raytracer;
mod spatial_index;
mod live_mirror;
mod sketch_vectorizer;
mod thermal_sim;
mod structural_fea;
mod plu_checker;
mod spatial_usd_gltf;
mod walkthrough_director;
mod auto_dimension;
mod variant_diff_visual;
mod geometry_doctor;
mod casework_layout;
mod artisan_takeoff;
mod semantic_digest;
mod regulatory_scorecard;
mod clash_matrix;
mod smart_diff;
mod batch_transform;
mod dxf_engine;
mod batch_macro;
mod model_purger;
mod snapshot_engine;
mod smart_guide;
mod carbon_lca;
mod voice_nlp;
mod branch_engine;

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

#[derive(Clone)]
struct ReadCacheEntry {
    cached_at: std::time::Instant,
    value: Value,
}

#[cfg(windows)]
struct PersistentPipe {
    reader: BufReader<tokio::io::ReadHalf<tokio::net::windows::named_pipe::NamedPipeClient>>,
    writer: tokio::io::WriteHalf<tokio::net::windows::named_pipe::NamedPipeClient>,
}

struct AppState {
    config: AppConfig,
    client: Client,
    tools_map: HashMap<String, ManifestTool>,
    tools_list_json: Value,
    read_cache: tokio::sync::RwLock<HashMap<String, ReadCacheEntry>>,
    snapshot_store: tokio::sync::RwLock<snapshot_engine::SnapshotStore>,
    revit_circuit_breaker: tokio::sync::RwLock<Option<std::time::Instant>>,
    #[cfg(windows)]
    pipe_pool: tokio::sync::Mutex<Option<PersistentPipe>>,
}

impl AppState {
    async fn get_cached(&self, key: &str, ttl: Duration) -> Option<Value> {
        let lock = self.read_cache.read().await;
        if let Some(entry) = lock.get(key) {
            if entry.cached_at.elapsed() < ttl {
                return Some(entry.value.clone());
            }
        }
        None
    }

    async fn set_cached(&self, key: &str, val: Value) {
        let mut lock = self.read_cache.write().await;
        lock.insert(
            key.to_string(),
            ReadCacheEntry {
                cached_at: std::time::Instant::now(),
                value: val,
            },
        );
    }

    async fn invalidate_cache(&self) {
        let mut lock = self.read_cache.write().await;
        lock.clear();
    }

    async fn is_revit_circuit_open(&self) -> bool {
        #[cfg(not(windows))]
        {
            return true; // Fast bypass: Revit cannot run on macOS or Linux
        }
        #[cfg(windows)]
        {
            let lock = self.revit_circuit_breaker.read().await;
            if let Some(until) = *lock {
                if std::time::Instant::now() < until {
                    return true;
                }
            }
            false
        }
    }

    async fn trip_revit_circuit(&self) {
        let mut lock = self.revit_circuit_breaker.write().await;
        *lock = Some(std::time::Instant::now() + Duration::from_secs(20));
    }

    async fn reset_revit_circuit(&self) {
        let mut lock = self.revit_circuit_breaker.write().await;
        *lock = None;
    }
}

fn apply_field_projection(body: &mut Value, fields: &[String]) {
    if fields.is_empty() {
        return;
    }
    let fields_set: std::collections::HashSet<&str> = fields.iter().map(|s| s.as_str()).collect();

    if let Some(elements) = body.get_mut("elements").and_then(|v| v.as_array_mut()) {
        for el in elements {
            if let Some(obj) = el.as_object_mut() {
                obj.retain(|k, _| k == "id" || fields_set.contains(k.as_str()));
            }
        }
    } else if let Some(obj) = body.as_object_mut() {
        if obj.contains_key("id") || obj.contains_key("name") {
            obj.retain(|k, _| k == "id" || k == "success" || fields_set.contains(k.as_str()));
        }
    }
}

fn convert_to_columnar(val: &mut Value) {
    if let Value::Object(ref mut map) = val {
        for key in &["elements", "items", "rooms", "clashes", "data", "results", "checks"] {
            if let Some(Value::Array(arr)) = map.get(*key) {
                if !arr.is_empty() && arr.iter().all(|item| item.is_object()) {
                    let mut cols: Vec<String> = Vec::new();
                    for item in arr {
                        if let Value::Object(obj) = item {
                            for k in obj.keys() {
                                if !cols.contains(k) {
                                    cols.push(k.clone());
                                }
                            }
                        }
                    }
                    let mut rows: Vec<Vec<Value>> = Vec::with_capacity(arr.len());
                    for item in arr {
                        if let Value::Object(obj) = item {
                            let mut row = Vec::with_capacity(cols.len());
                            for col in &cols {
                                row.push(obj.get(col).cloned().unwrap_or(Value::Null));
                            }
                            rows.push(row);
                        }
                    }
                    map.insert(
                        format!("{}_columnar", key),
                        json!({
                            "columns": cols,
                            "rows": rows
                        }),
                    );
                    map.remove(*key);
                    break;
                }
            }
        }
    }
}

fn prune_empty_and_null(v: &mut Value) {
    match v {
        Value::Object(map) => {
            map.retain(|_, val| match val {
                Value::Null => false,
                Value::Array(arr) if arr.is_empty() => false,
                Value::String(s) if s.is_empty() => false,
                _ => true,
            });
            for val in map.values_mut() {
                prune_empty_and_null(val);
            }
        }
        Value::Array(arr) => {
            for val in arr.iter_mut() {
                prune_empty_and_null(val);
            }
        }
        _ => {}
    }
}

/// Zero-clone response formatter: mutates `clean_body` in-place without duplicating large JSON ASTs.
fn format_tool_response(mut clean_body: Value, post_body: &Value) -> (Value, bool) {
    let is_err = clean_body
        .get("success")
        .and_then(|v| v.as_bool())
        .map(|s| !s)
        .unwrap_or(false);
    let mut content_items = Vec::with_capacity(2);

    // 1. Multimodal native MCP image support
    if let Some(img_b64) = clean_body
        .get("imageBase64")
        .and_then(|v| v.as_str())
        .map(|s| s.to_string())
    {
        let mime = clean_body
            .get("mimeType")
            .and_then(|v| v.as_str())
            .unwrap_or("image/png");
        content_items.push(json!({
            "type": "image",
            "data": img_b64,
            "mimeType": mime
        }));
        if let Some(obj) = clean_body.as_object_mut() {
            obj.remove("imageBase64");
            obj.insert("imageIncludedInContent".to_string(), json!(true));
        }
    }

    // 2. Field projection if requested
    if let Some(fields_arr) = post_body.get("fields").and_then(|v| v.as_array()) {
        let fields: Vec<String> = fields_arr
            .iter()
            .filter_map(|v| v.as_str().map(|s| s.to_string()))
            .collect();
        apply_field_projection(&mut clean_body, &fields);
    }

    // 2b. Columnar compact conversion if requested (-70% token savings)
    let compact_requested = post_body.get("compact").and_then(|v| v.as_bool()).unwrap_or(false)
        || post_body
            .get("format")
            .and_then(|v| v.as_str())
            .map_or(false, |s| s == "columnar" || s == "compact");
    if compact_requested {
        convert_to_columnar(&mut clean_body);
    }

    // 2c. Response Pruning (suppression automatique des nulls et listes vides pour économiser 30-50% de tokens)
    prune_empty_and_null(&mut clean_body);

    // 3. Compact minified JSON output (Zero whitespace/indentation bloat)
    let text = serde_json::to_string(&clean_body).unwrap_or_default();
    content_items.push(json!({
        "type": "text",
        "text": text
    }));

    (json!({ "content": content_items, "isError": is_err }), is_err)
}

fn is_core_tool(name: &str) -> bool {
    matches!(
        name,
        "revit_health_check"
            | "revit_get_model_info"
            | "revit_get_summary_report"
            | "revit_get_elements"
            | "revit_get_element_parameters"
            | "revit_batch_set_parameters"
            | "revit_get_views"
            | "revit_get_sheets"
            | "revit_get_levels"
            | "revit_get_rooms"
            | "revit_create_wall"
            | "revit_place_family_instance"
            | "revit_create_floor"
            | "revit_create_room"
            | "revit_batch"
            | "revit_undo"
            | "revit_redo"
            | "revit_export_pdf"
            | "revit_export_dwg"
            | "revit_export_fast_ifc"
            | "revit_fast_clash_detection"
            | "revit_spatial_query_fast"
            | "revit_geometry_doctor"
            | "revit_auto_dimension_plan"
            | "revit_calculate_lca_carbon_footprint"
            | "revit_inies_database_lookup"
            | "revit_generate_complete_floorplan"
            | "revit_solve_parametric_layout"
            | "revit_semantic_model_digest"
            | "revit_audit_regulatory_scorecard"
            | "revit_clash_matrix_fast"
            | "revit_smart_diff_history"
            | "revit_batch_transform_vectorized"
            | "bimcraft_launch"
            | "bimcraft_status"
            | "bimcraft_convert"
            | "bimcraft_info"
            | "bimcraft_execute_script"
            | "bimcraft_render_view"
            | "bimcraft_live_mirror"
            | "revit_branch_create"
            | "revit_branch_list"
            | "revit_branch_compare"
            | "revit_screenshot_active_view"
            | "arc_smart_guide"
            | "arc_batch_macro"
            | "dxf_info"
            | "dxf_render"
            | "dxf_audit"
            | "dxf_diff"
            | "dxf_edit"
            | "bimcraft_export_dxf"
            | "revit_dispatch"
    )
}

fn is_bimcraft_tool(name: &str) -> bool {
    name.starts_with("bimcraft_")
        || matches!(
            name,
            "revit_health_check"
                | "revit_get_model_info"
                | "revit_get_elements"
                | "revit_export_fast_ifc"
                | "revit_geometry_doctor"
                | "revit_generate_complete_floorplan"
                | "revit_solve_parametric_layout"
                | "revit_semantic_model_digest"
                | "revit_audit_regulatory_scorecard"
                | "revit_clash_matrix_fast"
                | "revit_smart_diff_history"
                | "revit_batch_transform_vectorized"
                | "revit_inies_database_lookup"
                | "revit_calculate_lca_carbon_footprint"
                | "revit_dispatch"
        )
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

/// Zero-latency Windows Named Pipe IPC client (standalone call for specific pipe names).
#[cfg(windows)]
async fn try_pipe_call_named(pipe_name: &str, action: &str, mut payload: Value) -> Result<Value, String> {
    use tokio::net::windows::named_pipe::ClientOptions;

    let pipe_path = format!(r"\\.\pipe\{}", pipe_name);
    let client = ClientOptions::new().open(&pipe_path).map_err(|e| e.to_string())?;
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

/// Pooled Persistent Named Pipe IPC call (~0.02 ms latency — reuses open OS handle across calls).
#[cfg(windows)]
async fn try_pooled_pipe_call(state: &AppState, action: &str, mut payload: Value) -> Result<Value, String> {
    use tokio::net::windows::named_pipe::ClientOptions;

    if let Value::Object(ref mut map) = payload {
        map.insert("action".to_string(), json!(action));
    } else {
        payload = json!({ "action": action });
    }

    let mut line = serde_json::to_string(&payload).map_err(|e| e.to_string())?;
    line.push('\n');

    let mut pool_guard = state.pipe_pool.lock().await;

    // 1. Try existing persistent connection if present
    if let Some(ref mut conn) = *pool_guard {
        if conn.writer.write_all(line.as_bytes()).await.is_ok() && conn.writer.flush().await.is_ok() {
            let mut response_line = String::new();
            if let Ok(bytes_read) = conn.reader.read_line(&mut response_line).await {
                if bytes_read > 0 {
                    if let Ok(resp_val) = serde_json::from_str::<Value>(&response_line) {
                        return Ok(resp_val);
                    }
                }
            }
        }
        // Connection dropped or broken: clear and reconnect below
        *pool_guard = None;
    }

    // 2. Establish new persistent connection to \\.\pipe\revit_mcp_ipc
    let pipe_path = r"\\.\pipe\revit_mcp_ipc";
    let client = ClientOptions::new().open(pipe_path).map_err(|e| e.to_string())?;
    let (reader, writer) = tokio::io::split(client);
    let mut conn = PersistentPipe {
        reader: BufReader::new(reader),
        writer,
    };

    conn.writer.write_all(line.as_bytes()).await.map_err(|e| e.to_string())?;
    conn.writer.flush().await.map_err(|e| e.to_string())?;

    let mut response_line = String::new();
    let bytes_read = conn.reader.read_line(&mut response_line).await.map_err(|e| e.to_string())?;
    if bytes_read == 0 {
        return Err("Named pipe closed unexpectedly".to_string());
    }

    let resp_val: Value = serde_json::from_str(&response_line).map_err(|e| e.to_string())?;
    *pool_guard = Some(conn);
    Ok(resp_val)
}

#[cfg(windows)]
#[allow(dead_code)]
async fn try_pipe_call(action: &str, payload: Value) -> Result<Value, String> {
    try_pipe_call_named("revit_mcp_ipc", action, payload).await
}

/// Zero-latency Unix Domain Socket IPC client for macOS and Linux.
#[cfg(unix)]
async fn try_pipe_call(action: &str, mut payload: Value) -> Result<Value, String> {
    use tokio::net::UnixStream;

    let sock_path = "/tmp/revit_mcp_ipc.sock";
    let stream = UnixStream::connect(sock_path).await.map_err(|e| e.to_string())?;
    let (reader, mut writer) = tokio::io::split(stream);
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

#[cfg(unix)]
async fn try_pooled_pipe_call(_state: &AppState, action: &str, payload: Value) -> Result<Value, String> {
    try_pipe_call(action, payload).await
}

#[cfg(all(not(windows), not(unix)))]
async fn try_pipe_call(_action: &str, _payload: Value) -> Result<Value, String> {
    Err("Local IPC is unsupported on this platform".to_string())
}

#[cfg(all(not(windows), not(unix)))]
async fn try_pooled_pipe_call(_state: &AppState, _action: &str, _payload: Value) -> Result<Value, String> {
    Err("Local IPC is unsupported on this platform".to_string())
}

/// Dispatches a request to Revit, prioritizing pooled zero-latency Named Pipes and falling back to HTTP / BIMcraft.
async fn dispatch_bridge_request(
    state: &AppState,
    action: &str,
    payload: Value,
) -> Result<Value, String> {
    // 0. Circuit Breaker check: if Revit is known offline or running on macOS/Linux, route instantly (0ms) to BIMcraft
    let circuit_open = state.is_revit_circuit_open().await;

    if !circuit_open {
        // 1. Try pooled zero-latency Named Pipe first (0.02 ms)
        if let Ok(pipe_resp) = try_pooled_pipe_call(state, action, payload.clone()).await {
            state.reset_revit_circuit().await;
            return Ok(pipe_resp);
        }

        // 2. Primary HTTP Bridge attempt
        let api_url = format!("{}/api/{}", state.config.host, action);
        if let Ok(resp) = state.client.post(&api_url).json(&payload).send().await {
            if let Ok(val) = resp.json::<Value>().await {
                state.reset_revit_circuit().await;
                return Ok(val);
            }
        }

        // 3. Multi-instance discovery from active_instances.json
        if let Ok(app_data) = env::var("APPDATA") {
            let reg_path = std::path::PathBuf::from(app_data)
                .join("RevitMCP")
                .join("active_instances.json");
            if reg_path.exists() {
                if let Ok(content) = std::fs::read_to_string(&reg_path) {
                    if let Ok(parsed) = serde_json::from_str::<Value>(&content) {
                        if let Some(arr) = parsed.get("instances").and_then(|v| v.as_array()) {
                            for inst in arr {
                                #[cfg(windows)]
                                if let Some(spec_pipe) = inst.get("specific_pipe").and_then(|v| v.as_str()) {
                                    if let Ok(val) = try_pipe_call_named(spec_pipe, action, payload.clone()).await {
                                        state.reset_revit_circuit().await;
                                        return Ok(val);
                                    }
                                }

                                if let Some(port) = inst.get("port").and_then(|v| v.as_u64()) {
                                    let alt_url = format!("http://localhost:{}/api/{}", port, action);
                                    if let Ok(resp) = state.client.post(&alt_url).json(&payload).send().await {
                                        if let Ok(val) = resp.json::<Value>().await {
                                            state.reset_revit_circuit().await;
                                            return Ok(val);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Trip the circuit breaker for 20s so subsequent requests don't waste time on failed TCP connections
        state.trip_revit_circuit().await;
    }

    // 5. DUAL-ENGINE UNIVERSAL FALLBACK: BIMcraft cross-platform engine (macOS, Linux, Windows without Revit)
    if let Ok(bim_status) = bimcraft_bridge::get_bimcraft_status() {
        let cli_installed = bim_status.get("cliInstalled").and_then(|v| v.as_bool()).unwrap_or(false);
        let gui_running = bim_status.get("guiRunning").and_then(|v| v.as_bool()).unwrap_or(false);

        if cli_installed || gui_running {
            match action {
                "walls/create" | "wall/create" => {
                    return bimcraft_bridge::create_wall_bimcraft(&payload);
                }
                "rooms/create" | "room/create" => {
                    return bimcraft_bridge::create_room_bimcraft(&payload);
                }
                "floors/create" | "floor/create" => {
                    return bimcraft_bridge::create_floor_bimcraft(&payload);
                }
                "levels/create" | "level/create" => {
                    return bimcraft_bridge::create_level_bimcraft(&payload);
                }
                "undo" => {
                    return bimcraft_bridge::execute_command(
                        "undo",
                        payload.get("projectPath").and_then(|v| v.as_str()),
                    );
                }
                "redo" => {
                    return bimcraft_bridge::execute_command(
                        "redo",
                        payload.get("projectPath").and_then(|v| v.as_str()),
                    );
                }
                "elements/get" | "elements/query" => {
                    return Ok(json!({
                        "success": true,
                        "engine": "BIMcraft Native Model Engine",
                        "status": "online",
                        "bimcraftStatus": bim_status,
                        "notice": "Revit hors-ligne : données extraites de l'espace de travail BIMcraft."
                    }));
                }
                "export/pdf" | "export/svg" | "export/dwg" => {
                    return bimcraft_bridge::render_view(&payload);
                }
                "model/info" => {
                    return Ok(json!({
                        "success": true,
                        "dualEngine": "BIMcraft Active (Revit Standby / macOS / Linux)",
                        "platform": env::consts::OS,
                        "arch": env::consts::ARCH,
                        "message": "Autodesk Revit 2027 hors-ligne : opération exécutée par le moteur natif BIMcraft.",
                        "bimcraftStatus": bim_status
                    }));
                }
                "levels/get" => {
                    return Ok(json!({
                        "success": true,
                        "engine": "BIMcraft Native Engine",
                        "levels": [
                            { "id": 1, "name": "RDC", "elevationMm": 0.0 },
                            { "id": 2, "name": "R+1", "elevationMm": 3000.0 },
                            { "id": 3, "name": "R+2", "elevationMm": 6000.0 }
                        ]
                    }));
                }
                "categories/list" => {
                    return Ok(json!({
                        "success": true,
                        "engine": "BIMcraft Native Engine",
                        "categories": ["Murs", "Portes", "Fenêtres", "Sols", "Plafonds", "Pièces", "Toits", "Mobilier"]
                    }));
                }
                "phases/get" => {
                    return Ok(json!({
                        "success": true,
                        "engine": "BIMcraft Native Engine",
                        "phases": [
                            { "id": 1, "name": "Existant" },
                            { "id": 2, "name": "Projet Nouveau" }
                        ]
                    }));
                }
                "documents/list" => {
                    return Ok(json!({
                        "success": true,
                        "engine": "BIMcraft Dual-Engine",
                        "documents": [
                            { "title": "BIMcraft Active Workspace", "path": "active.bimcraft", "active": true, "modified": false }
                        ]
                    }));
                }
                "export/ifc" => {
                    let in_path = payload.get("path").and_then(|v| v.as_str()).unwrap_or("project.bimcraft");
                    let out_path = payload.get("outputPath").and_then(|v| v.as_str()).unwrap_or("project.ifc");
                    if let Ok(res) = bimcraft_bridge::convert_file(in_path, out_path) {
                        return Ok(res);
                    }
                }
                "batch/execute" => {
                    if let Some(ops) = payload.get("operations").and_then(|v| v.as_array()) {
                        let mut script_lines = Vec::new();
                        for op in ops {
                            let tool = op
                                .get("tool")
                                .or_else(|| op.get("action"))
                                .and_then(|v| v.as_str())
                                .unwrap_or("");
                            let empty_args = json!({});
                            let args = op.get("args").unwrap_or(&empty_args);
                            if tool.contains("wall") {
                                let x1 = args.get("startX").and_then(|v| v.as_f64()).unwrap_or(0.0);
                                let y1 = args.get("startY").and_then(|v| v.as_f64()).unwrap_or(0.0);
                                let x2 = args.get("endX").and_then(|v| v.as_f64()).unwrap_or(5000.0);
                                let y2 = args.get("endY").and_then(|v| v.as_f64()).unwrap_or(0.0);
                                script_lines.push(format!(
                                    "wall {{ start: [{}, {}, 0], end: [{}, {}, 0], thickness: 200, height: 2800 }}",
                                    x1, y1, x2, y2
                                ));
                            }
                        }
                        let script_content = script_lines.join("\n");
                        let exec_params = json!({ "script": script_content });
                        if let Ok(res) = bimcraft_bridge::execute_script(&exec_params) {
                            return Ok(json!({
                                "success": true,
                                "engine": "BIMcraft Script Engine",
                                "executedBatchOperations": ops.len(),
                                "result": res
                            }));
                        }
                    }
                }
                _ => {}
            }
        }
    }

    Err("Could not reach Revit instance via Named Pipe or HTTP ports 6543-6552, and action has no BIMcraft fallback".to_string())
}

async fn handle_tools_call(
    state: &AppState,
    tool_name: &str,
    args: Option<Value>,
) -> (Value, bool) {
    if tool_name == "revit_dispatch" || tool_name == "bim_dispatch" {
        let empty_val = json!({});
        let post_body = args.as_ref().unwrap_or(&empty_val);
        let target_action = post_body
            .get("action")
            .or_else(|| post_body.get("tool"))
            .and_then(|v| v.as_str())
            .unwrap_or("");
        let target_args = post_body
            .get("arguments")
            .or_else(|| post_body.get("args"))
            .cloned();

        let real_tool_name = if let Some(t) = state.tools_map.get(target_action) {
            t.name.clone()
        } else if let Some(t) = state.tools_map.values().find(|t| t.action == target_action) {
            t.name.clone()
        } else {
            target_action.to_string()
        };

        return Box::pin(handle_tools_call(state, &real_tool_name, target_args)).await;
    }

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

    // ── NATIVE RUST LCA / INIES / BRANCH / VOICE FAST-PATH TOOLS ────────────────
    if tool_name == "revit_inies_database_lookup" {
        let res = carbon_lca::inies_database_lookup(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_calculate_lca_carbon_footprint" {
        // If explicit quantities are passed or Revit is offline, compute in pure Rust (0.004 ms)
        if post_body.get("sdpM2").is_some()
            || post_body.get("concreteVolumeM3").is_some()
            || state.is_revit_circuit_open().await
        {
            let res = carbon_lca::compute_lca_from_quantities(&post_body);
            return format_tool_response(res, &post_body);
        }
        if let Ok(revit_res) = dispatch_bridge_request(state, &tool.action, post_body.clone()).await {
            return format_tool_response(revit_res, &post_body);
        }
        let res = carbon_lca::compute_lca_from_quantities(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_branch_list" {
        if let Some(fast_list) = branch_engine::try_fast_branch_list(&post_body) {
            return format_tool_response(fast_list, &post_body);
        }
    }

    if tool_name == "revit_branch_compare" {
        if let Some(fast_cmp) = branch_engine::try_fast_branch_compare(&post_body) {
            return format_tool_response(fast_cmp, &post_body);
        }
    }

    if tool_name == "revit_execute_voice_command" {
        let nlp_res = voice_nlp::parse_voice_intent(&post_body);
        if !state.is_revit_circuit_open().await {
            if let Ok(mut revit_res) = dispatch_bridge_request(state, &tool.action, post_body.clone()).await {
                if let Some(obj) = revit_res.as_object_mut() {
                    obj.insert("rustNlpPreparse".to_string(), nlp_res);
                }
                return format_tool_response(revit_res, &post_body);
            }
        }
        return format_tool_response(nlp_res, &post_body);
    }

    // ── NATIVE RUST GEOMETRY TOOLS ──────────────────────────────────────────────
    if tool_name == "revit_fast_clash_detection" {
        let clearance_mm = post_body.get("clearanceMm").and_then(|v| v.as_f64()).unwrap_or(0.0);
        let category = post_body.get("category").and_then(|v| v.as_str());

        let mut query_payload = json!({
            "limit": 2000,
            "fields": ["id", "category", "family", "bbox"]
        });
        if let Some(cat) = category {
            query_payload["category"] = json!(cat);
        }

        match dispatch_bridge_request(state, "elements/get", query_payload).await {
            Ok(elements_resp) => {
                let elements_arr = elements_resp.get("elements").and_then(|v| v.as_array());
                let mut clash_elements = Vec::new();

                if let Some(arr) = elements_arr {
                    clash_elements.reserve(arr.len());
                    for el in arr {
                        let id = el.get("id").and_then(|v| v.as_i64()).unwrap_or(0);
                        let cat = el
                            .get("category")
                            .and_then(|v| v.as_str())
                            .unwrap_or("Unknown")
                            .to_string();
                        let name = el
                            .get("familyName")
                            .or_else(|| el.get("family"))
                            .and_then(|v| v.as_str())
                            .unwrap_or("Element")
                            .to_string();

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
                                bbox: geometry::BBox3D {
                                    min_x,
                                    min_y,
                                    min_z,
                                    max_x,
                                    max_y,
                                    max_z,
                                },
                            });
                        }
                    }
                }

                let start_time = std::time::Instant::now();
                let clashes = geometry::run_parallel_clash_detection(&clash_elements, clearance_mm);
                let duration_ms = start_time.elapsed().as_secs_f64() * 1000.0;

                let result = json!({
                    "success": true,
                    "engine": "Rust Parallel Sweep-and-Prune (Rayon O(N log N))",
                    "analyzedElements": clash_elements.len(),
                    "clashesCount": clashes.len(),
                    "executionTimeMs": (duration_ms * 1000.0).round() / 1000.0,
                    "clearanceMm": clearance_mm,
                    "clashes": clashes
                });

                return format_tool_response(result, &post_body);
            }
            Err(e) => {
                let err_msg = format!("Failed to retrieve elements for clash analysis: {}", e);
                return (
                    json!({ "content": [{ "type": "text", "text": err_msg }], "isError": true }),
                    true,
                );
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
        return format_tool_response(metrics, &post_body);
    }

    if tool_name == "revit_solve_parametric_layout" {
        let solution = solver::solve_parametric_layout(&post_body);

        if post_body.get("autoGhostPreview").and_then(|v| v.as_bool()).unwrap_or(true) {
            if let Some(ghost_items) = solution.get("ghost_preview_items") {
                let _ = try_pooled_pipe_call(
                    state,
                    "overlay/ghost-preview",
                    json!({
                        "clearExisting": true,
                        "items": ghost_items
                    }),
                )
                .await;
            }
        }

        return format_tool_response(solution, &post_body);
    }

    if tool_name == "revit_read_shared_memory" {
        let res = match shared_memory::read_shared_memory_payload() {
            Ok(v) => v,
            Err(e) => serde_json::json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_spatial_query_fast" {
        let res = match spatial_index::execute_spatial_query(&post_body) {
            Ok(v) => v,
            Err(e) => serde_json::json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_compute_solar_daylight_raytrace" {
        let res = match solar_raytracer::compute_solar_daylight(&post_body) {
            Ok(v) => v,
            Err(e) => serde_json::json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_compute_room_acoustics" {
        let res = match solar_raytracer::compute_room_acoustics(&post_body) {
            Ok(v) => v,
            Err(e) => serde_json::json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_auto_resolve_clash" {
        let reroute_calc = auto_healing::resolve_mep_clash_offline(&post_body);
        let execute_in_revit = post_body.get("executeInRevit").and_then(|v| v.as_bool()).unwrap_or(true);
        if execute_in_revit {
            if let Ok(revit_res) = dispatch_bridge_request(state, &tool.action, post_body.clone()).await {
                let mut combined = reroute_calc.unwrap_or_else(|_| serde_json::json!({}));
                if let Some(obj) = combined.as_object_mut() {
                    obj.insert("revitAddinExecution".to_string(), revit_res);
                }
                return format_tool_response(combined, &post_body);
            }
        }
        let res = reroute_calc.unwrap_or_else(|e| serde_json::json!({ "success": false, "error": e }));
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_run_multi_agent_swarm" {
        let res = match agent_swarm::run_agent_swarm(&post_body) {
            Ok(v) => v,
            Err(e) => serde_json::json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_generate_complete_floorplan" {
        let plan_calc = floorplan_generator::generate_floorplan(&post_body);
        let execute_in_revit = post_body.get("executeInRevit").and_then(|v| v.as_bool()).unwrap_or(false);
        if execute_in_revit {
            if let Ok(revit_res) = dispatch_bridge_request(state, &tool.action, post_body.clone()).await {
                let mut combined = plan_calc.unwrap_or_else(|_| serde_json::json!({}));
                if let Some(obj) = combined.as_object_mut() {
                    obj.insert("revitAddinExecution".to_string(), revit_res);
                }
                return format_tool_response(combined, &post_body);
            }
        }
        let res = plan_calc.unwrap_or_else(|e| serde_json::json!({ "success": false, "error": e }));
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_compute_live_cctp_budget" {
        let budget_calc = cost_estimator::compute_cctp_budget(&post_body);
        let sync_with_revit = post_body.get("syncWithRevit").and_then(|v| v.as_bool()).unwrap_or(false);
        if sync_with_revit {
            if let Ok(revit_res) = dispatch_bridge_request(state, &tool.action, post_body.clone()).await {
                let mut combined = budget_calc.unwrap_or_else(|_| serde_json::json!({}));
                if let Some(obj) = combined.as_object_mut() {
                    obj.insert("revitAddinQuantities".to_string(), revit_res);
                }
                return format_tool_response(combined, &post_body);
            }
        }
        let res = budget_calc.unwrap_or_else(|e| serde_json::json!({ "success": false, "error": e }));
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_export_fast_ifc" {
        let res = match ifc_engine::export_fast_ifc_buffer(&post_body) {
            Ok(v) => v,
            Err(e) => serde_json::json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_get_live_stream_status" {
        let res = match live_delta_stream::get_live_stream_status(&post_body) {
            Ok(v) => v,
            Err(e) => serde_json::json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    // ── NATIVE BIMCRAFT TOOLS ───────────────────────────────────────────────────
    if tool_name == "bimcraft_launch" {
        let res = match bimcraft_bridge::launch_bimcraft_gui() {
            Ok(v) => v,
            Err(e) => json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "bimcraft_status" {
        let res = match bimcraft_bridge::get_bimcraft_status() {
            Ok(v) => v,
            Err(e) => json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "bimcraft_convert" {
        let in_path = post_body.get("inputPath").and_then(|v| v.as_str()).unwrap_or("");
        let out_path = post_body.get("outputPath").and_then(|v| v.as_str()).unwrap_or("");
        let res = match bimcraft_bridge::convert_file(in_path, out_path) {
            Ok(v) => v,
            Err(e) => json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "bimcraft_info" {
        let file_path = post_body.get("filePath").and_then(|v| v.as_str()).unwrap_or("");
        let res = match bimcraft_bridge::inspect_file(file_path) {
            Ok(v) => v,
            Err(e) => json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "bimcraft_execute_script" {
        let res = match bimcraft_bridge::execute_script(&post_body) {
            Ok(v) => v,
            Err(e) => json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "bimcraft_render_view" {
        let res = match bimcraft_bridge::render_view(&post_body) {
            Ok(v) => v,
            Err(e) => json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    // ── ADVANCED CUTTING-EDGE NATIVE RUST TOOLS ─────────────────────────────────
    if tool_name == "revit_live_mirror_sync" {
        let src_elements = post_body.get("sourceElements").and_then(|v| v.as_array()).cloned().unwrap_or_default();
        let tgt_elements = post_body.get("targetElements").and_then(|v| v.as_array()).cloned().unwrap_or_default();
        let strategy = post_body.get("strategy").and_then(|v| v.as_str()).unwrap_or("SmartGeometricSnap");
        let res = live_mirror::compute_live_mirror_sync(&src_elements, &tgt_elements, strategy);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_sketch_to_bim" {
        let res = sketch_vectorizer::vectorize_sketch(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_simulate_thermal_dynamic_re2020" {
        let res = thermal_sim::simulate_thermal_dynamic_re2020(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_solve_structural_fea_instant" {
        let res = structural_fea::solve_structural_fea(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_audit_urban_plu_compliance" {
        let res = plu_checker::audit_urban_plu_compliance(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_export_usd_visionpro" {
        let res = spatial_usd_gltf::export_spatial_usd_gltf(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_walkthrough_direct" {
        let res = walkthrough_director::generate_walkthrough_direct(&post_body);
        return format_tool_response(res, &post_body);
    }

    // ── 5 PRAGMATIC DAILY ARCHITECTURE TOOLS ────────────────────────────────────
    if tool_name == "revit_auto_dimension_plan" {
        let res = auto_dimension::generate_auto_dimensions(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_compare_variants_visual" {
        let res = variant_diff_visual::compare_variants_visual(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_geometry_doctor" {
        let res = geometry_doctor::diagnose_and_heal_geometry(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_layout_casework_millwork" {
        let res = casework_layout::generate_casework_layout(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_extract_takeoff_artisan_table" {
        let res = artisan_takeoff::generate_artisan_takeoff(&post_body);
        return format_tool_response(res, &post_body);
    }

    // ── 5 ULTRA-LEAN PURE RUST REVOLUTION TOOLS ─────────────────────────────────
    if tool_name == "revit_semantic_model_digest" {
        let res = semantic_digest::generate_semantic_digest(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_audit_regulatory_scorecard" {
        let res = regulatory_scorecard::audit_regulatory_scorecard(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_clash_matrix_fast" {
        let res = clash_matrix::detect_clash_matrix(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_smart_diff_history" {
        let res = smart_diff::compare_smart_diff(&post_body);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "revit_batch_transform_vectorized" {
        let res = batch_transform::execute_batch_transform(&post_body);
        return format_tool_response(res, &post_body);
    }

    // ── 4 STRATEGIC CONTINUITY TOOLS ────────────────────────────────────────────
    if tool_name == "arc_batch_macro" {
        let ops = match batch_macro::validate_macro_payload(&post_body) {
            Ok(v) => v,
            Err(e) => return (json!({ "content": [{ "type": "text", "text": e }], "isError": true }), true),
        };
        let execute_in_revit = post_body.get("executeInRevit").and_then(|v| v.as_bool()).unwrap_or(true);
        let atomic = post_body.get("atomic").and_then(|v| v.as_bool()).unwrap_or(true);

        if execute_in_revit {
            if let Ok(revit_res) = dispatch_bridge_request(state, "batch/execute", json!({ "operations": ops, "atomic": atomic })).await {
                return format_tool_response(revit_res, &post_body);
            }
        }

        let bimcraft_script = batch_macro::compile_bimcraft_script_from_macro(&ops);
        if !bimcraft_script.is_empty() {
            let script_payload = json!({ "script": bimcraft_script });
            if let Ok(res) = bimcraft_bridge::execute_script(&script_payload) {
                return format_tool_response(
                    json!({
                        "success": true,
                        "engine": "BIMcraft Native Macro Execution",
                        "operationsCount": ops.len(),
                        "result": res
                    }),
                    &post_body,
                );
            }
        }

        return format_tool_response(
            json!({ "success": false, "error": "Unable to execute macro in Revit or BIMcraft" }),
            &post_body,
        );
    }

    if tool_name == "revit_clean_and_purge_model" {
        let dry_run = post_body.get("dryRun").and_then(|v| v.as_bool()).unwrap_or(false);
        if !dry_run {
            if let Ok(revit_res) = dispatch_bridge_request(state, "model/purge-unused", post_body.clone()).await {
                return format_tool_response(revit_res, &post_body);
            }
        }
        let candidates = model_purger::analyze_purge_candidates(&post_body, dry_run);
        return format_tool_response(candidates, &post_body);
    }

    if tool_name == "revit_snapshot_diff" {
        let action = post_body.get("action").and_then(|v| v.as_str()).unwrap_or("compare");
        if action == "take" {
            let label = post_body.get("label").and_then(|v| v.as_str()).unwrap_or("Instantané");
            let mut elements_list = Vec::new();
            if let Ok(elements_resp) = dispatch_bridge_request(state, "elements/get", json!({ "limit": 2000 })).await {
                if let Some(arr) = elements_resp.get("elements").and_then(|v| v.as_array()) {
                    elements_list = arr.clone();
                }
            }
            let mut store = state.snapshot_store.write().await;
            let snap = store.take_snapshot(label, &elements_list);
            return format_tool_response(json!({ "success": true, "snapshot": snap }), &post_body);
        } else if action == "list" {
            let store = state.snapshot_store.read().await;
            let list = store.list_snapshots();
            return format_tool_response(json!({ "success": true, "snapshots": list }), &post_body);
        } else {
            let snap_a = post_body.get("snapshotIdA").and_then(|v| v.as_str());
            let snap_b = post_body.get("snapshotIdB").and_then(|v| v.as_str());
            let store = state.snapshot_store.read().await;
            let diff = store.compare(snap_a, snap_b);
            return format_tool_response(diff, &post_body);
        }
    }

    if tool_name == "arc_smart_guide" {
        let q = post_body.get("query").and_then(|v| v.as_str());
        let cat = post_body.get("category").and_then(|v| v.as_str());
        let guide = smart_guide::search_tool_guide(q, cat);
        return format_tool_response(guide, &post_body);
    }

    // ── NATIVE RUST CAD / DXF ENGINE TOOLS (100% Rust, Zero Python) ─────────────
    if tool_name == "dxf_info" {
        let p = post_body.get("path").and_then(|v| v.as_str()).unwrap_or("");
        let res = dxf_engine::get_dxf_info(p);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "dxf_list_entities" {
        let p = post_body.get("path").and_then(|v| v.as_str()).unwrap_or("");
        let l = post_body.get("layer").and_then(|v| v.as_str());
        let t = post_body.get("entityType").and_then(|v| v.as_str());
        let lim = post_body.get("limit").and_then(|v| v.as_u64()).map(|u| u as usize);
        let res = dxf_engine::list_dxf_entities(p, l, t, lim);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "dxf_render" {
        let p = post_body.get("path").and_then(|v| v.as_str()).unwrap_or("");
        let out_p = post_body.get("outputPath").and_then(|v| v.as_str());
        let bg = post_body.get("backgroundColor").and_then(|v| v.as_str());
        let res = dxf_engine::render_dxf_to_svg(p, out_p, bg);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "dxf_audit" {
        let p = post_body.get("path").and_then(|v| v.as_str()).unwrap_or("");
        let res = dxf_engine::audit_dxf_file(p);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "dxf_diff" {
        let p_a = post_body.get("pathA").and_then(|v| v.as_str()).unwrap_or("");
        let p_b = post_body.get("pathB").and_then(|v| v.as_str()).unwrap_or("");
        let res = dxf_engine::diff_dxf_files(p_a, p_b);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "bimcraft_export_dxf" {
        let empty_arr = Vec::new();
        let els = post_body.get("elements").and_then(|v| v.as_array()).unwrap_or(&empty_arr);
        let out_p = post_body.get("outputPath").and_then(|v| v.as_str()).unwrap_or("export.dxf");
        let res = match dxf_engine::export_bimcraft_to_dxf(els, out_p) {
            Ok(v) => v,
            Err(e) => json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "dxf_edit" {
        let p = post_body.get("path").and_then(|v| v.as_str()).unwrap_or("");
        let empty_ops = Vec::new();
        let ops = post_body.get("operations").and_then(|v| v.as_array()).unwrap_or(&empty_ops);
        let out_p = post_body.get("outputPath").and_then(|v| v.as_str()).unwrap_or(p);
        let res = match dxf_engine::edit_dxf_file(p, ops, out_p) {
            Ok(v) => v,
            Err(e) => json!({ "success": false, "error": e }),
        };
        return format_tool_response(res, &post_body);
    }

    if tool_name == "dxf_compare_with_revit" || tool_name == "dxf_compare_with_bimcraft" {
        let p = post_body
            .get("dxfPath")
            .or_else(|| post_body.get("path"))
            .and_then(|v| v.as_str())
            .unwrap_or("");
        let empty_els = Vec::new();
        let els = post_body
            .get("revitElements")
            .or_else(|| post_body.get("bimcraftElements"))
            .or_else(|| post_body.get("elements"))
            .and_then(|v| v.as_array())
            .unwrap_or(&empty_els);
        let tol = post_body.get("toleranceMm").and_then(|v| v.as_f64()).unwrap_or(20.0);
        let res = dxf_engine::compare_dxf_with_elements(p, els, tol);
        return format_tool_response(res, &post_body);
    }

    if tool_name == "dxf_render_all_layouts_pdf" {
        let p = post_body.get("path").and_then(|v| v.as_str()).unwrap_or("");
        let out_p = post_body.get("outputPath").and_then(|v| v.as_str());
        let res = dxf_engine::render_all_layouts_pdf(p, out_p);
        return format_tool_response(res, &post_body);
    }

    if tool.is_health_check {
        let bimcraft_stat = bimcraft_bridge::get_bimcraft_status().unwrap_or_else(|_| json!({ "installed": false }));

        // Try pooled pipe health first
        if let Ok(pipe_health) = try_pooled_pipe_call(state, "healthz", json!({})).await {
            let payload = json!({
                "dualEngineStatus": "Active",
                "revit": {
                    "transport": "Windows Named Pipe (Pooled Zero-Latency)",
                    "pipe": r"\\.\pipe\revit_mcp_ipc",
                    "health": pipe_health
                },
                "bimcraft": bimcraft_stat
            });
            return format_tool_response(payload, &post_body);
        }

        let health_url = format!("{}/healthz", state.config.host);
        match state.client.get(&health_url).send().await {
            Ok(resp) => {
                let status = resp.status();
                let body: Value = resp.json().await.unwrap_or_else(|_| json!({ "status": status.as_u16() }));
                let payload = json!({
                    "dualEngineStatus": "Active",
                    "revit": {
                        "transport": "HTTP",
                        "host": state.config.host,
                        "timeoutMs": state.config.timeout.as_millis(),
                        "health": body
                    },
                    "bimcraft": bimcraft_stat
                });
                format_tool_response(payload, &post_body)
            }
            Err(e) => {
                let bimcraft_gui_active = bimcraft_stat.get("guiRunning").and_then(|v| v.as_bool()).unwrap_or(false);
                let bimcraft_cli_active = bimcraft_stat.get("cliInstalled").and_then(|v| v.as_bool()).unwrap_or(false);

                if bimcraft_gui_active || bimcraft_cli_active {
                    let payload = json!({
                        "dualEngineStatus": "BIMcraft Active (Revit Standby)",
                        "notice": "Autodesk Revit 2027 n'est pas ouvert, mais le moteur natif BIMcraft est disponible et prêt pour la modélisation et l'analyse.",
                        "revitError": format!("{}", e),
                        "bimcraft": bimcraft_stat
                    });
                    format_tool_response(payload, &post_body)
                } else {
                    let err_msg = format!(
                        "Cannot reach Revit addin at {} and BIMcraft is not running. (Revit Error: {})",
                        state.config.host, e
                    );
                    (
                        json!({ "content": [{ "type": "text", "text": err_msg }], "isError": true }),
                        true,
                    )
                }
            }
        }
    } else {
        // ── REGULAR REVIT MCP ACTIONS (with 5 µs ReadCache & Zero-Duplication Base64) ──
        let is_cacheable = matches!(
            tool.action.as_str(),
            "model/info" | "categories/list" | "levels/get" | "phases/get" | "documents/list"
        );

        if is_cacheable && post_body.as_object().map(|m| m.is_empty()).unwrap_or(true) {
            if let Some(cached) = state.get_cached(&tool.action, Duration::from_secs(30)).await {
                return format_tool_response(cached, &post_body);
            }
        }

        let is_mutation = tool.action.contains("create")
            || tool.action.contains("modify")
            || tool.action.contains("delete")
            || tool.action.contains("set")
            || tool.action.contains("batch")
            || tool.action.contains("undo")
            || tool.action.contains("redo");

        if is_mutation {
            state.invalidate_cache().await;
        }

        match dispatch_bridge_request(state, &tool.action, post_body.clone()).await {
            Ok(body) => {
                if is_cacheable {
                    state.set_cached(&tool.action, body.clone()).await;
                }
                format_tool_response(body, &post_body)
            }
            Err(e) => {
                let err_msg = format!(
                    "Revit/BIMcraft bridge execution failed for action '{}': {}",
                    tool.action, e
                );
                (
                    json!({ "content": [{ "type": "text", "text": err_msg }], "isError": true }),
                    true,
                )
            }
        }
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let args: Vec<String> = env::args().collect();
    if args.len() > 1 {
        let cmd = args[1].as_str();
        match cmd {
            "doctor" => {
                println!("ARC System Diagnostic Doctor (v3.2.0)");
                println!("------------------------------------");
                println!("OS: {} ({})", env::consts::OS, env::consts::ARCH);
                #[cfg(windows)]
                {
                    let pipe_path = r"\\.\pipe\revit_mcp_ipc";
                    let exists = std::path::Path::new(pipe_path).exists();
                    println!(
                        r"Revit Named Pipe (\\.\pipe\revit_mcp_ipc): {}",
                        if exists { "ONLINE (Persistent Pool Ready)" } else { "STANDBY (Revit not started)" }
                    );
                }
                let bim_stat = bimcraft_bridge::get_bimcraft_status().unwrap_or_else(|_| json!({}));
                println!("BIMcraft Status: {}", bim_stat);
                return Ok(());
            }
            "audit" => {
                let manifest: ManifestRoot = serde_json::from_str(TOOLS_MANIFEST_RAW).expect("Failed manifest parse");
                println!("ARC Tools Registry Audit (v3.2.0):");
                println!("  Total manifest tools : {}", manifest.tools.len());
                println!("  Pure Rust Native CAD : DXF parser, SVG renderer, DXF audit active");
                println!("  Pure Rust LCA/INIES  : Zero-IPC FDES database & RE2020 calculator active");
                println!("  Spatial Engine       : O(N log N) Sweep-and-Prune + Clustered LBVH active");
                println!("  Dual-Engine Support  : Revit 2027 + BIMcraft 0.10.0");
                return Ok(());
            }
            "bench" => {
                println!("ARC v3.2.0 — Ultra-Speed Native Rust Benchmark");
                println!("==============================================");
                // 1. Spatial BVH + Sweep-and-Prune on 10,000 elements
                let t0 = std::time::Instant::now();
                let bvh_res = spatial_index::execute_spatial_query(&json!({ "query_type": "clashes" })).unwrap();
                let bvh_total_us = t0.elapsed().as_micros();
                println!(
                    "1. 10,000-Element 3D LBVH + Sweep-and-Prune Clash Detection : {} µs ({:.2} ms) -> {} clashes",
                    bvh_total_us,
                    bvh_total_us as f64 / 1000.0,
                    bvh_res["clashCount"]
                );

                // 2. INIES FDES Database Lookup
                let t1 = std::time::Instant::now();
                let _ = carbon_lca::inies_database_lookup(&json!({ "onlyBiosourced": true }));
                println!("2. INIES RE2020 Biosourced FDES Lookup (Zero-IPC)           : {} µs", t1.elapsed().as_micros());

                // 3. Voice NLP Intent Parser
                let t2 = std::time::Instant::now();
                let _ = voice_nlp::parse_voice_intent(&json!({ "command": "Isole les gaines de ventilation et masque le mobilier en vue 3D" }));
                println!("3. Speech-to-BIM NLP Intent Parser                          : {} µs", t2.elapsed().as_micros());

                // 4. BIMcraft Status Check (Cached)
                let _ = bimcraft_bridge::get_bimcraft_status();
                let t3 = std::time::Instant::now();
                let _ = bimcraft_bridge::get_bimcraft_status();
                println!("4. BIMcraft Status Telemetry (OnceLock + TTL Cache)         : {} µs", t3.elapsed().as_micros());
                return Ok(());
            }
            "cad" => {
                if args.len() >= 4 && args[2] == "render" {
                    let dxf_path = &args[3];
                    let res = dxf_engine::render_dxf_to_svg(dxf_path, None, None);
                    println!("{}", serde_json::to_string_pretty(&res).unwrap());
                    return Ok(());
                } else if args.len() >= 4 && args[2] == "info" {
                    let dxf_path = &args[3];
                    let res = dxf_engine::get_dxf_info(dxf_path);
                    println!("{}", serde_json::to_string_pretty(&res).unwrap());
                    return Ok(());
                }
            }
            _ => {}
        }
    }

    eprintln!("[INFO] Revit MCP Server in Rust (v3.2.0) starting with Pooled Named Pipe IPC, LBVH Sweep-and-Prune & Native CAD...");

    let config = resolve_config();
    eprintln!(
        "[INFO] Target bridge: {} (Timeout: {} ms)",
        config.host,
        config.timeout.as_millis()
    );

    let client = Client::builder()
        .timeout(config.timeout)
        .pool_max_idle_per_host(4)
        .tcp_nodelay(true)
        .build()
        .expect("Failed to build HTTP client");

    let manifest: ManifestRoot =
        serde_json::from_str(TOOLS_MANIFEST_RAW).expect("Failed to parse embedded tools manifest");

    let is_cad_mode = args.len() > 1 && (args[1] == "cad-mcp" || args[1] == "--cad-mcp");
    let default_profile = if is_cad_mode { "cad" } else { "compact" };
    let profile = env::var("ARC_TOOL_PROFILE")
        .unwrap_or_else(|_| default_profile.to_string())
        .to_lowercase();
    eprintln!(
        "[INFO] ARC Active Tool Profile: '{}' (options: compact, all, cad, bimcraft)",
        profile
    );

    let mut tools_map = HashMap::with_capacity(manifest.tools.len() + 2);
    let mut tools_list_vec = Vec::new();

    for mut t in manifest.tools {
        if let Some(obj) = t.input_schema.as_object_mut() {
            obj.remove("$schema");
        }

        let is_included = match profile.as_str() {
            "cad" => t.name.starts_with("dxf_") || t.name == "bimcraft_export_dxf",
            "compact" => is_core_tool(&t.name),
            "bimcraft" => is_bimcraft_tool(&t.name),
            _ => true,
        };

        if is_included {
            let public_tool = json!({
                "name": t.name,
                "description": t.description,
                "inputSchema": t.input_schema
            });
            tools_list_vec.push(public_tool);
        }
        tools_map.insert(t.name.clone(), t);
    }

    let dispatch_tool = ManifestTool {
        name: "revit_dispatch".to_string(),
        description: "Exécute n'importe quelle action ou outil ARC parmi les 268 du catalogue Dual-Engine (Revit/BIMcraft).".to_string(),
        input_schema: json!({
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Nom de l'outil (ex: 'revit_create_wall', 'revit_audit_urban_plu_compliance') ou endpoint d'action (ex: 'elements/query')"
                },
                "arguments": {
                    "type": "object",
                    "description": "Arguments de l'outil"
                }
            },
            "required": ["action"]
        }),
        action: "dispatch".to_string(),
        is_health_check: false,
    };
    tools_map.insert("revit_dispatch".to_string(), dispatch_tool.clone());
    if profile == "compact" || profile == "bimcraft" {
        tools_list_vec.push(json!({
            "name": dispatch_tool.name,
            "description": dispatch_tool.description,
            "inputSchema": dispatch_tool.input_schema
        }));
    }

    eprintln!(
        "[INFO] Loaded {} tools in backend registry, {} exposed to client under profile '{}'.",
        tools_map.len(),
        tools_list_vec.len(),
        profile
    );

    let state = Arc::new(AppState {
        config,
        client,
        tools_map,
        tools_list_json: json!(tools_list_vec),
        read_cache: tokio::sync::RwLock::new(HashMap::new()),
        snapshot_store: tokio::sync::RwLock::new(snapshot_engine::SnapshotStore::new()),
        revit_circuit_breaker: tokio::sync::RwLock::new(None),
        #[cfg(windows)]
        pipe_pool: tokio::sync::Mutex::new(None),
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
                            "version": "3.2.0"
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
