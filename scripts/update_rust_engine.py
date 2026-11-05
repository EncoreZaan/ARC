import re

# 1. Update bimcraft_bridge.rs
with open('revit-mcp-server/src/bimcraft_bridge.rs', 'r', encoding='utf-8') as f:
    bc_code = f.read()

# Add detect_running_bimcraft_pids before get_bimcraft_status
pid_detection_code = '''/// Détecte les processus BIMcraft actifs en cours d'exécution de manière multi-plateforme
pub fn detect_running_bimcraft_pids() -> Vec<u32> {
    let mut pids = Vec::new();
    #[cfg(windows)]
    {
        if let Ok(output) = Command::new("tasklist")
            .args(["/FI", "IMAGENAME eq bimcraft.exe", "/FO", "CSV", "/NH"])
            .output()
        {
            let text = String::from_utf8_lossy(&output.stdout);
            for line in text.lines() {
                let parts: Vec<&str> = line.split(',').collect();
                if parts.len() >= 2 {
                    let pid_str = parts[1].trim().trim_matches('"');
                    if let Ok(pid) = pid_str.parse::<u32>() {
                        pids.push(pid);
                    }
                }
            }
        }
    }
    #[cfg(unix)]
    {
        if let Ok(output) = Command::new("pgrep").arg("bimcraft").output() {
            let text = String::from_utf8_lossy(&output.stdout);
            for line in text.lines() {
                if let Ok(pid) = line.trim().parse::<u32>() {
                    pids.push(pid);
                }
            }
        }
    }
    pids
}

/// Vérifie l'état de BIMcraft (processus GUI, port de contrôle bridge.json et CLI)'''

bc_code = bc_code.replace("/// Vérifie l'état de BIMcraft (processus GUI, port de contrôle bridge.json et CLI)", pid_detection_code)

# In get_bimcraft_status, update gui_running and return object
old_status_calc = '''    let mut bridge_info = None;
    let mut gui_running = false;
    if bridge_path.exists() {
        if let Ok(content) = fs::read_to_string(&bridge_path) {
            if let Ok(val) = serde_json::from_str::<Value>(&content) {
                bridge_info = Some(val);
                gui_running = true;
            }
        }
    }'''

new_status_calc = '''    let mut bridge_info = None;
    if bridge_path.exists() {
        if let Ok(content) = fs::read_to_string(&bridge_path) {
            if let Ok(val) = serde_json::from_str::<Value>(&content) {
                bridge_info = Some(val);
            }
        }
    }

    let running_pids = detect_running_bimcraft_pids();
    let instance_count = running_pids.len();
    let gui_running = instance_count > 0 || bridge_info.is_some();'''

bc_code = bc_code.replace(old_status_calc, new_status_calc)

old_status_json = '''        "guiRunning": gui_running,
        "cliPath": cli_path.to_string_lossy(),'''

new_status_json = '''        "guiRunning": gui_running,
        "activeInstances": instance_count,
        "runningPids": running_pids,
        "multiInstanceReady": true,
        "cliPath": cli_path.to_string_lossy(),'''

bc_code = bc_code.replace(old_status_json, new_status_json)

with open('revit-mcp-server/src/bimcraft_bridge.rs', 'w', encoding='utf-8') as f:
    f.write(bc_code)

print("bimcraft_bridge.rs updated successfully.")


# 2. Update main.rs
with open('revit-mcp-server/src/main.rs', 'r', encoding='utf-8') as f:
    main_code = f.read()

# Add new modules at top
old_modules = '''mod agent_swarm;
mod auto_healing;
mod bimcraft_bridge;
mod cost_estimator;
mod floorplan_generator;
mod geometry;
mod ifc_engine;
mod live_delta_stream;
mod shared_memory;
mod solar_raytracer;
mod solver;
mod spatial_index;'''

new_modules = '''mod agent_swarm;
mod auto_healing;
mod bimcraft_bridge;
mod cost_estimator;
mod floorplan_generator;
mod geometry;
mod ifc_engine;
mod live_delta_stream;
mod live_mirror;
mod plu_checker;
mod shared_memory;
mod sketch_vectorizer;
mod solar_raytracer;
mod solver;
mod spatial_index;
mod spatial_usd_gltf;
mod structural_fea;
mod thermal_sim;
mod walkthrough_director;'''

main_code = main_code.replace(old_modules, new_modules)

# Enhance dispatch_bridge_request with multi-instance dynamic port fallback
old_dispatch = '''    // 2. Fallback to HTTP Bridge
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
    }'''

new_dispatch = '''    // 2. Primary HTTP Bridge attempt
    let api_url = format!("{}/api/{}", state.config.host, action);
    if let Ok(resp) = state.client.post(&api_url).json(&payload).send().await {
        if let Ok(val) = resp.json::<Value>().await {
            return Ok(val);
        }
    }

    // 3. Multi-instance discovery from active_instances.json
    if let Ok(app_data) = env::var("APPDATA") {
        let reg_path = std::path::PathBuf::from(app_data).join("RevitMCP").join("active_instances.json");
        if reg_path.exists() {
            if let Ok(content) = std::fs::read_to_string(&reg_path) {
                if let Ok(parsed) = serde_json::from_str::<Value>(&content) {
                    if let Some(arr) = parsed.get("instances").and_then(|v| v.as_array()) {
                        for inst in arr {
                            if let Some(port) = inst.get("port").and_then(|v| v.as_u64()) {
                                let alt_url = format!("http://localhost:{}/api/{}", port, action);
                                if let Ok(resp) = state.client.post(&alt_url).json(&payload).send().await {
                                    if let Ok(val) = resp.json::<Value>().await {
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

    // 4. Sequential fallback across ports 6543..6552
    for alt_port in 6543..=6552 {
        let alt_url = format!("http://localhost:{}/api/{}", alt_port, action);
        if let Ok(resp) = state.client.post(&alt_url).json(&payload).send().await {
            if let Ok(val) = resp.json::<Value>().await {
                return Ok(val);
            }
        }
    }

    Err("Could not reach any active Revit instance via Named Pipe or HTTP ports 6543-6552".to_string())'''

main_code = main_code.replace(old_dispatch, new_dispatch)

# Add 7 cutting-edge tools dispatch before health check
new_tools_dispatch = '''    // ── ADVANCED CUTTING-EDGE NATIVE RUST TOOLS ─────────────────────────────
    if tool_name == "revit_live_mirror_sync" {
        let src_elements = post_body.get("sourceElements").and_then(|v| v.as_array()).cloned().unwrap_or_default();
        let tgt_elements = post_body.get("targetElements").and_then(|v| v.as_array()).cloned().unwrap_or_default();
        let strategy = post_body.get("strategy").and_then(|v| v.as_str()).unwrap_or("SmartGeometricSnap");
        let res = live_mirror::compute_live_mirror_sync(&src_elements, &tgt_elements, strategy);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_sketch_to_bim" {
        let res = sketch_vectorizer::vectorize_sketch(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_simulate_thermal_dynamic_re2020" {
        let res = thermal_sim::simulate_thermal_dynamic_re2020(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_solve_structural_fea_instant" {
        let res = structural_fea::solve_structural_fea(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_audit_urban_plu_compliance" {
        let res = plu_checker::audit_urban_plu_compliance(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_export_usd_visionpro" {
        let res = spatial_usd_gltf::export_spatial_usd_gltf(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_walkthrough_direct" {
        let res = walkthrough_director::generate_walkthrough_direct(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }
'''

target_anchor = '    if tool.is_health_check {'
if target_anchor in main_code:
    main_code = main_code.replace(target_anchor, new_tools_dispatch + '\n' + target_anchor)

with open('revit-mcp-server/src/main.rs', 'w', encoding='utf-8') as f:
    f.write(main_code)

print("main.rs updated successfully.")
