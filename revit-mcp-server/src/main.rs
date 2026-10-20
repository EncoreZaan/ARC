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

    if tool.is_health_check {
        let health_url = format!("{}/healthz", state.config.host);
        match state.client.get(&health_url).send().await {
            Ok(resp) => {
                let status = resp.status();
                let body: Value = resp.json().await.unwrap_or_else(|_| json!({ "status": status.as_u16() }));
                let payload = json!({
                    "host": state.config.host,
                    "timeoutMs": state.config.timeout.as_millis(),
                    "health": body
                });
                let text = serde_json::to_string_pretty(&payload).unwrap_or_default();
                (json!({ "content": [{ "type": "text", "text": text }] }), false)
            }
            Err(e) => {
                let err_msg = format!(
                    "Cannot reach Revit addin at {}. Make sure Autodesk Revit is open with a project loaded and RevitMCP addin is running. (Error: {})",
                    state.config.host, e
                );
                (json!({ "content": [{ "type": "text", "text": err_msg }], "isError": true }), true)
            }
        }
    } else {
        let api_url = format!("{}/api/{}", state.config.host, tool.action);
        let post_body = args.unwrap_or_else(|| json!({}));

        match state.client.post(&api_url).json(&post_body).send().await {
            Ok(resp) => {
                let is_http_err = !resp.status().is_success();
                let body: Value = match resp.json().await {
                    Ok(val) => val,
                    Err(_) => json!({ "error": "Non-JSON response from Revit bridge" }),
                };

                let is_mcp_err = is_http_err
                    || body.get("success").and_then(|v| v.as_bool()).map(|s| !s).unwrap_or(false);

                let text = serde_json::to_string_pretty(&body).unwrap_or_default();
                (
                    json!({
                        "content": [{ "type": "text", "text": text }],
                        "isError": is_mcp_err
                    }),
                    is_mcp_err,
                )
            }
            Err(e) => {
                let err_msg = if e.is_timeout() {
                    format!("Timed out after {} ms waiting for Revit action '{}'.", state.config.timeout.as_millis(), tool.action)
                } else {
                    format!(
                        "Cannot reach Revit addin at {}. Make sure Autodesk Revit is open with a project loaded and RevitMCP addin is running. (Error: {})",
                        state.config.host, e
                    )
                };
                (json!({ "content": [{ "type": "text", "text": err_msg }], "isError": true }), true)
            }
        }
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    eprintln!("[INFO] Revit MCP Server in Rust (v2.1.1) starting...");

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
                            "version": "2.1.1"
                        }
                    }
                });
                let mut out = serde_json::to_string(&resp)?;
                out.push('\n');
                stdout.write_all(out.as_bytes()).await?;
                stdout.flush().await?;
            }

            "notifications/initialized" => {
                // MCP specification: notification sent by client once ready, no reply needed.
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
