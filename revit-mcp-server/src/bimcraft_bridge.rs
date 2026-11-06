use serde_json::{json, Value};
use std::env;
use std::fs;
use std::path::PathBuf;
use std::process::Command;

/// Détecte le chemin de l'exécutable bimcraft-cli de manière multi-plateforme (macOS, Linux, Windows)
fn get_bimcraft_cli_path() -> PathBuf {
    let mut candidates = Vec::new();

    // 1. Profil utilisateur / Home directory
    if let Ok(home) = env::var("HOME").or_else(|_| env::var("USERPROFILE")) {
        let home_p = PathBuf::from(home);
        // Windows .exe
        candidates.push(home_p.join(".cargo").join("bin").join("bimcraft-cli.exe"));
        // macOS / Linux binaire unix
        candidates.push(home_p.join(".cargo").join("bin").join("bimcraft-cli"));
        candidates.push(home_p.join(".local").join("bin").join("bimcraft-cli"));
    }

    // 2. Chemins standards macOS (Homebrew Apple Silicon & Intel)
    candidates.push(PathBuf::from("/opt/homebrew/bin/bimcraft-cli"));
    candidates.push(PathBuf::from("/usr/local/bin/bimcraft-cli"));

    // 3. Chemins standards Linux
    candidates.push(PathBuf::from("/usr/bin/bimcraft-cli"));

    for c in candidates {
        if c.exists() {
            return c;
        }
    }

    if cfg!(windows) {
        PathBuf::from("bimcraft-cli.exe")
    } else {
        PathBuf::from("bimcraft-cli")
    }
}

/// Détecte le chemin de l'exécutable bimcraft GUI de manière multi-plateforme (macOS, Linux, Windows)
fn get_bimcraft_gui_path() -> PathBuf {
    let mut candidates = Vec::new();

    // 1. Profil utilisateur / Home directory
    if let Ok(home) = env::var("HOME").or_else(|_| env::var("USERPROFILE")) {
        let home_p = PathBuf::from(home);
        candidates.push(home_p.join(".cargo").join("bin").join("bimcraft.exe"));
        candidates.push(home_p.join(".cargo").join("bin").join("bimcraft"));
        candidates.push(home_p.join(".local").join("bin").join("bimcraft"));
    }

    // 2. Applications macOS (.app bundle & Homebrew)
    candidates.push(PathBuf::from("/Applications/BIMCraft.app/Contents/MacOS/bimcraft"));
    if let Ok(home) = env::var("HOME") {
        candidates.push(PathBuf::from(home).join("Applications/BIMCraft.app/Contents/MacOS/bimcraft"));
    }
    candidates.push(PathBuf::from("/opt/homebrew/bin/bimcraft"));
    candidates.push(PathBuf::from("/usr/local/bin/bimcraft"));

    // 3. Linux
    candidates.push(PathBuf::from("/usr/bin/bimcraft"));

    for c in candidates {
        if c.exists() {
            return c;
        }
    }

    if cfg!(windows) {
        PathBuf::from("bimcraft.exe")
    } else {
        PathBuf::from("bimcraft")
    }
}

/// Détecte le fichier bridge.json de manière multi-plateforme
fn get_bridge_json_path() -> PathBuf {
    // 1. Windows: %APPDATA%\BIMCraft\bridge.json
    if let Ok(app_data) = env::var("APPDATA") {
        let p = PathBuf::from(app_data).join("BIMCraft").join("bridge.json");
        if p.exists() || cfg!(windows) {
            return p;
        }
    }

    // 2. macOS: ~/Library/Application Support/BIMCraft/bridge.json
    if let Ok(home) = env::var("HOME") {
        let mac_p = PathBuf::from(&home).join("Library").join("Application Support").join("BIMCraft").join("bridge.json");
        if mac_p.exists() || cfg!(target_os = "macos") {
            return mac_p;
        }

        // 3. Linux / XDG: ~/.config/BIMCraft/bridge.json
        if let Ok(xdg) = env::var("XDG_CONFIG_HOME") {
            let xdg_p = PathBuf::from(xdg).join("BIMCraft").join("bridge.json");
            if xdg_p.exists() {
                return xdg_p;
            }
        }
        let linux_p = PathBuf::from(home).join(".config").join("BIMCraft").join("bridge.json");
        return linux_p;
    }

    PathBuf::from("bridge.json")
}

/// Lance l'interface graphique de BIMcraft avec le pont agent actif (--control 0)
/// Compatible Windows, macOS (Apple Silicon / Intel) et Linux
pub fn launch_bimcraft_gui() -> Result<Value, String> {
    let bridge_path = get_bridge_json_path();
    let gui_path = get_bimcraft_gui_path();

    if !gui_path.exists() {
        return Err(format!("Exécutable BIMcraft introuvable à l'emplacement : {:?}", gui_path));
    }

    #[cfg(windows)]
    {
        // Lancement détaché via WMI sous Windows pour persistance hors subshell
        let ps_cmd = format!(
            "Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{{CommandLine = '\"{}\" --control 0'}}",
            gui_path.to_string_lossy().replace('\\', "\\\\")
        );

        let output = Command::new("powershell")
            .args(["-NoProfile", "-Command", &ps_cmd])
            .output()
            .map_err(|e| format!("Échec du lancement PowerShell: {}", e))?;

        if !output.status.success() {
            let err_str = String::from_utf8_lossy(&output.stderr);
            return Err(format!("Erreur lors du démarrage de BIMcraft: {}", err_str));
        }
    }

    #[cfg(target_os = "macos")]
    {
        // Sous macOS, privilégie l'application bundle si disponible, sinon exécution binaire détachée
        let app_bundle = PathBuf::from("/Applications/BIMCraft.app");
        if app_bundle.exists() {
            let _ = Command::new("open")
                .args(["-a", "BIMCraft", "--args", "--control", "0"])
                .spawn()
                .map_err(|e| format!("Échec du lancement open macOS: {}", e))?;
        } else {
            let _ = Command::new(&gui_path)
                .args(["--control", "0"])
                .spawn()
                .map_err(|e| format!("Échec du lancement BIMcraft binaire macOS: {}", e))?;
        }
    }

    #[cfg(all(unix, not(target_os = "macos")))]
    {
        // Sous Linux, démarrage détaché
        let _ = Command::new(&gui_path)
            .args(["--control", "0"])
            .spawn()
            .map_err(|e| format!("Échec du lancement BIMcraft sous Linux: {}", e))?;
    }

    // Attente brève de l'écriture du bridge.json (max 2.5s)
    let start = std::time::Instant::now();
    let mut bridge_data: Option<Value> = None;
    while start.elapsed().as_millis() < 2500 {
        if bridge_path.exists() {
            if let Ok(content) = fs::read_to_string(&bridge_path) {
                if let Ok(val) = serde_json::from_str::<Value>(&content) {
                    bridge_data = Some(val);
                    break;
                }
            }
        }
        std::thread::sleep(std::time::Duration::from_millis(200));
    }

    Ok(json!({
        "success": true,
        "platform": env::consts::OS,
        "arch": env::consts::ARCH,
        "message": format!("BIMcraft GUI démarré avec succès sur {} avec le pont Agent actif.", env::consts::OS),
        "guiPath": gui_path.to_string_lossy(),
        "bridge": bridge_data.unwrap_or(json!({ "status": "initializing" })),
        "mcpConnectCommand": "bimcraft-cli mcp --connect"
    }))
}

/// Détecte les processus BIMcraft actifs en cours d'exécution de manière multi-plateforme
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

/// Vérifie l'état de BIMcraft (processus GUI, port de contrôle bridge.json et CLI)
pub fn get_bimcraft_status() -> Result<Value, String> {
    let cli_path = get_bimcraft_cli_path();
    let gui_path = get_bimcraft_gui_path();
    let bridge_path = get_bridge_json_path();

    let cli_installed = cli_path.exists();
    let gui_installed = gui_path.exists();

    let mut version = "unknown".to_string();
    if cli_installed {
        if let Ok(out) = Command::new(&cli_path).arg("--version").output() {
            if out.status.success() {
                version = String::from_utf8_lossy(&out.stdout).trim().to_string();
            }
        }
    }

    let mut bridge_info = None;
    if bridge_path.exists() {
        if let Ok(content) = fs::read_to_string(&bridge_path) {
            if let Ok(val) = serde_json::from_str::<Value>(&content) {
                bridge_info = Some(val);
            }
        }
    }

    let running_pids = detect_running_bimcraft_pids();
    let instance_count = running_pids.len();
    let gui_running = instance_count > 0 || bridge_info.is_some();

    Ok(json!({
        "success": true,
        "engine": "BIMcraft Native Rust Modeler",
        "platform": env::consts::OS,
        "arch": env::consts::ARCH,
        "version": version,
        "cliInstalled": cli_installed,
        "guiInstalled": gui_installed,
        "guiRunning": gui_running,
        "activeInstances": instance_count,
        "runningPids": running_pids,
        "multiInstanceReady": true,
        "cliPath": cli_path.to_string_lossy(),
        "guiPath": gui_path.to_string_lossy(),
        "bridgePath": bridge_path.to_string_lossy(),
        "bridgeConfig": bridge_info,
        "crossPlatform": {
            "macOS": "Prêt (Apple Silicon Metal & Intel)",
            "linux": "Prêt (Vulkan / X11 / Wayland)",
            "windows": "Prêt (DirectX 12 / Vulkan)"
        },
        "capabilities": [
            "rvt_native_import_crossplatform",
            "ifc4_import_export",
            "fast_vector_render_svg_png",
            "bimcraft_interactive_gui",
            "agent_mcp_bridge"
        ]
    }))
}

/// Convertit n'importe quel fichier (.rvt, .rfa, .ifc, .bimcraft) vers .ifc, .svg, .png ou .bimcraft
pub fn convert_file(input_path: &str, output_path: &str) -> Result<Value, String> {
    let cli_path = get_bimcraft_cli_path();
    if !cli_path.exists() {
        return Err(format!("bimcraft-cli introuvable à : {:?}", cli_path));
    }

    let output = Command::new(&cli_path)
        .args(["convert", input_path, output_path])
        .output()
        .map_err(|e| format!("Échec d'exécution de bimcraft-cli: {}", e))?;

    if !output.status.success() {
        let err_msg = String::from_utf8_lossy(&output.stderr);
        return Err(format!("Échec de conversion BIMcraft: {}", err_msg.trim()));
    }

    let stdout_str = String::from_utf8_lossy(&output.stdout);
    let parsed: Value = serde_json::from_str(stdout_str.trim()).unwrap_or_else(|_| {
        json!({
            "format": "converted",
            "path": output_path
        })
    });

    Ok(json!({
        "success": true,
        "platform": env::consts::OS,
        "input": input_path,
        "output": output_path,
        "result": parsed
    }))
}

/// Analyse et extrait les niveaux, éléments, catégories et paramètres d'un fichier .rvt, .rfa, .ifc ou .bimcraft
pub fn inspect_file(file_path: &str) -> Result<Value, String> {
    let cli_path = get_bimcraft_cli_path();
    if !cli_path.exists() {
        return Err(format!("bimcraft-cli introuvable à : {:?}", cli_path));
    }

    let output = Command::new(&cli_path)
        .args(["info", file_path])
        .output()
        .map_err(|e| format!("Échec d'exécution de bimcraft-cli: {}", e))?;

    if !output.status.success() {
        let err_msg = String::from_utf8_lossy(&output.stderr);
        return Err(format!("Erreur d'analyse BIMcraft info: {}", err_msg.trim()));
    }

    let stdout_str = String::from_utf8_lossy(&output.stdout);
    let parsed: Value = serde_json::from_str(stdout_str.trim())
        .map_err(|e| format!("Échec de désérialisation JSON info: {}", e))?;

    Ok(json!({
        "success": true,
        "platform": env::consts::OS,
        "filePath": file_path,
        "modelInfo": parsed
    }))
}

/// Exécute un script de modélisation BIMcraft (lignes `commande {params}`)
pub fn execute_script(params: &Value) -> Result<Value, String> {
    let cli_path = get_bimcraft_cli_path();
    if !cli_path.exists() {
        return Err(format!("bimcraft-cli introuvable à : {:?}", cli_path));
    }

    let mut args = vec!["run".to_string()];

    // Fichier projet existant ou échantillon
    if let Some(file) = params.get("projectPath").and_then(|v| v.as_str()) {
        args.push(file.to_string());
    } else if params.get("sample").and_then(|v| v.as_bool()).unwrap_or(false) {
        args.push("--sample".to_string());
    }

    // Commandes inline
    if let Some(cmds) = params.get("commands").and_then(|v| v.as_array()) {
        for c in cmds {
            if let Some(s) = c.as_str() {
                args.push("--cmd".to_string());
                args.push(s.to_string());
            }
        }
    }

    // Fichier script
    if let Some(script_file) = params.get("scriptFile").and_then(|v| v.as_str()) {
        args.push("--script".to_string());
        args.push(script_file.to_string());
    }

    // Sauvegardes par extension (.bimcraft, .ifc, .svg, .png)
    if let Some(save_path) = params.get("savePath").and_then(|v| v.as_str()) {
        args.push("--save".to_string());
        args.push(save_path.to_string());
    }

    let output = Command::new(&cli_path)
        .args(&args)
        .output()
        .map_err(|e| format!("Échec d'exécution de bimcraft-cli run: {}", e))?;

    let stdout_str = String::from_utf8_lossy(&output.stdout);
    let stderr_str = String::from_utf8_lossy(&output.stderr);

    Ok(json!({
        "success": output.status.success(),
        "platform": env::consts::OS,
        "exitCode": output.status.code().unwrap_or(-1),
        "stdout": stdout_str.trim(),
        "stderr": stderr_str.trim()
    }))
}

/// Effectue un rendu 2D ou 3D d'un projet BIMcraft
pub fn render_view(params: &Value) -> Result<Value, String> {
    let input_file = params.get("projectPath").and_then(|v| v.as_str())
        .ok_or_else(|| "Paramètre 'projectPath' manquant".to_string())?;
    let output_image = params.get("outputPath").and_then(|v| v.as_str())
        .ok_or_else(|| "Paramètre 'outputPath' manquant".to_string())?;

    convert_file(input_file, output_image)
}


/// Exécute une commande directe de modélisation dans BIMcraft CLI
pub fn execute_command(cmd_str: &str, project_path: Option<&str>) -> Result<Value, String> {
    let cli_path = get_bimcraft_cli_path();
    if !cli_path.exists() {
        return Err(format!("bimcraft-cli introuvable à : {:?}", cli_path));
    }

    let mut cmd = Command::new(&cli_path);
    cmd.arg("run");
    if let Some(p) = project_path {
        cmd.arg(p);
    }
    cmd.args(["--cmd", cmd_str]);

    let output = cmd.output().map_err(|e| format!("Échec d'exécution de la commande BIMcraft '{}': {}", cmd_str, e))?;
    let stdout_str = String::from_utf8_lossy(&output.stdout);
    let stderr_str = String::from_utf8_lossy(&output.stderr);

    Ok(json!({
        "success": output.status.success(),
        "engine": "BIMcraft Native CLI (Cross-Platform / Zero-Revit)",
        "platform": env::consts::OS,
        "command": cmd_str,
        "stdout": stdout_str.trim(),
        "stderr": stderr_str.trim()
    }))
}

/// Crée un mur dans BIMcraft
pub fn create_wall_bimcraft(params: &Value) -> Result<Value, String> {
    let start = params.get("startPoint")
        .or_else(|| params.get("start"))
        .and_then(|v| v.as_array())
        .map(|arr| [
            arr.first().and_then(|v| v.as_f64()).unwrap_or(0.0),
            arr.get(1).and_then(|v| v.as_f64()).unwrap_or(0.0),
        ])
        .unwrap_or([0.0, 0.0]);

    let end = params.get("endPoint")
        .or_else(|| params.get("end"))
        .and_then(|v| v.as_array())
        .map(|arr| [
            arr.first().and_then(|v| v.as_f64()).unwrap_or(5000.0),
            arr.get(1).and_then(|v| v.as_f64()).unwrap_or(0.0),
        ])
        .unwrap_or([5000.0, 0.0]);

    let height = params.get("height").and_then(|v| v.as_f64()).unwrap_or(2800.0);
    let thickness = params.get("thickness").or_else(|| params.get("wallWidth")).and_then(|v| v.as_f64()).unwrap_or(200.0);

    let cmd = format!("wall {{\"start\": [{:.1}, {:.1}], \"end\": [{:.1}, {:.1}], \"height\": {:.1}, \"thickness\": {:.1}}}",
        start[0], start[1], end[0], end[1], height, thickness);

    let proj_p = params.get("projectPath").and_then(|v| v.as_str());
    execute_command(&cmd, proj_p)
}

/// Crée une pièce dans BIMcraft
pub fn create_room_bimcraft(params: &Value) -> Result<Value, String> {
    let name = params.get("name").or_else(|| params.get("roomName")).and_then(|v| v.as_str()).unwrap_or("Pièce");
    let at = params.get("at")
        .or_else(|| params.get("point"))
        .and_then(|v| v.as_array())
        .map(|arr| [
            arr.first().and_then(|v| v.as_f64()).unwrap_or(1500.0),
            arr.get(1).and_then(|v| v.as_f64()).unwrap_or(1500.0),
        ])
        .unwrap_or([1500.0, 1500.0]);

    let cmd = format!("room {{\"name\": \"{}\", \"at\": [{:.1}, {:.1}]}}", name, at[0], at[1]);
    let proj_p = params.get("projectPath").and_then(|v| v.as_str());
    execute_command(&cmd, proj_p)
}

/// Crée une dalle / sol dans BIMcraft
pub fn create_floor_bimcraft(params: &Value) -> Result<Value, String> {
    let cmd = "floor {\"boundary\": [[0,0], [6000,0], [6000,5000], [0,5000]]}".to_string();
    let proj_p = params.get("projectPath").and_then(|v| v.as_str());
    execute_command(&cmd, proj_p)
}

/// Crée un niveau dans BIMcraft
pub fn create_level_bimcraft(params: &Value) -> Result<Value, String> {
    let elevation = params.get("elevation").or_else(|| params.get("elevationMm")).and_then(|v| v.as_f64()).unwrap_or(3000.0);
    let name = params.get("name").and_then(|v| v.as_str()).unwrap_or("Nouveau Niveau");

    let cmd = format!("level {{\"elevation\": {:.1}, \"name\": \"{}\"}}", elevation, name);
    let proj_p = params.get("projectPath").and_then(|v| v.as_str());
    execute_command(&cmd, proj_p)
}
