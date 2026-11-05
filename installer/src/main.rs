use colored::Colorize;
use flate2::read::GzDecoder;
use serde_json::{json, Value};
use std::collections::HashMap;
use std::env;
use std::fs;
use std::io::{Cursor, Read};
use std::path::{Path, PathBuf};
use tar::Archive;
use winreg::enums::{HKEY_CURRENT_USER, KEY_ALL_ACCESS, KEY_READ};
use winreg::RegKey;

static PAYLOAD_GZ: &[u8] = include_bytes!(concat!(env!("OUT_DIR"), "/payload.tar.gz"));
const CLIENT_ID_GUID: &str = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
const VERSION: &str = env!("CARGO_PKG_VERSION");

#[derive(Default)]
struct InstallOptions {
    silent: bool,
    uninstall: bool,
    status_only: bool,
    force_node: bool,
}

fn main() {
    let args: Vec<String> = env::args().collect();
    let mut opts = InstallOptions::default();

    for arg in args.iter().skip(1) {
        match arg.as_str() {
            "--silent" | "-s" => opts.silent = true,
            "--uninstall" | "-u" => opts.uninstall = true,
            "--status" => opts.status_only = true,
            "--use-node" => opts.force_node = true,
            "--help" | "-h" => {
                print_help();
                return;
            }
            _ => {
                eprintln!("Argument inconnu : {}", arg);
                print_help();
                std::process::exit(1);
            }
        }
    }

    if opts.status_only {
        run_status();
        return;
    }

    if opts.uninstall {
        run_uninstall(opts.silent);
        return;
    }

    run_install(opts.silent, opts.force_node);
}

fn print_help() {
    println!(
        "{}\n\
        Usage: revit-mcp-installer.exe [OPTIONS]\n\n\
        Options:\n\
          -s, --silent       Installation silencieuse sans confirmation utilisateur\n\
          -u, --uninstall    Désinstalle l'add-in Revit et retire la configuration Claude\n\
              --status       Affiche le diagnostic d'installation et l'état du pont\n\
              --use-node     Force le serveur MCP Node.js au lieu du binaire Rust natif\n\
          -h, --help         Affiche cette aide\n",
        format!("ARC Installer v{}", VERSION).cyan().bold()
    );
}

fn print_banner() {
    println!("{}", "==========================================================".cyan());
    println!("{}", format!("       ARC (ARCHITECTURE & REVIT COPILOT) v{}       ", VERSION).cyan().bold());
    println!("{}", "   Pont natif Autodesk Revit <-> Claude AI via MCP        ".white());
    println!("{}", "==========================================================".cyan());
    println!();
}

fn extract_payload() -> HashMap<String, Vec<u8>> {
    let mut files = HashMap::new();
    let tar = GzDecoder::new(Cursor::new(PAYLOAD_GZ));
    let mut archive = Archive::new(tar);

    for entry_res in archive.entries().expect("Failed to read tar archive") {
        let mut entry = entry_res.expect("Failed to read tar entry");
        let path = entry.path().expect("Failed to get entry path").to_string_lossy().replace('\\', "/");
        let mut content = Vec::new();
        entry.read_to_end(&mut content).expect("Failed to extract file from archive");
        files.insert(path, content);
    }
    files
}

fn detect_revit_years() -> Vec<String> {
    let candidates = ["2027", "2026", "2025", "2024"];
    let mut detected = Vec::new();

    let appdata = env::var("APPDATA").unwrap_or_default();
    let program_files = env::var("ProgramFiles").unwrap_or_else(|_| r"C:\Program Files".to_string());

    let hkcu = RegKey::predef(HKEY_CURRENT_USER);

    for &year in &candidates {
        let mut found = false;

        // 1. Program Files directory
        let pf_dir = Path::new(&program_files).join("Autodesk").join(format!("Revit {}", year));
        if pf_dir.exists() {
            found = true;
        }

        // 2. Existing add-in folder in APPDATA
        if !appdata.is_empty() {
            let addin_dir = Path::new(&appdata).join("Autodesk").join("Revit").join("Addins").join(year);
            if addin_dir.exists() {
                found = true;
            }
        }

        // 3. Registry check in HKCU
        let reg_sub = format!(r"SOFTWARE\Autodesk\Revit\Autodesk Revit {}", year);
        if hkcu.open_subkey_with_flags(&reg_sub, KEY_READ).is_ok() {
            found = true;
        }

        if found && !detected.contains(&year.to_string()) {
            detected.push(year.to_string());
        }
    }

    if detected.is_empty() {
        // Fallback default
        detected.push("2027".to_string());
        detected.push("2026".to_string());
    }

    detected
}

fn run_install(silent: bool, force_node: bool) {
    if !silent {
        print_banner();
    }

    let payload = extract_payload();
    let years = detect_revit_years();
    let appdata = env::var("APPDATA").expect("APPDATA environment variable not defined");
    let localappdata = env::var("LOCALAPPDATA").unwrap_or_default();

    if !silent {
        println!("{}", format!("[1/4] Versions de Revit détectées : {}", years.join(", ")).yellow().bold());
    }

    // 1. Deploy Add-in for each detected year
    for year in &years {
        let addin_dir = Path::new(&appdata).join("Autodesk").join("Revit").join("Addins").join(year);
        fs::create_dir_all(&addin_dir).unwrap_or_default();

        let files_to_deploy = [
            ("addin/RevitMCP.dll", "RevitMCP.dll"),
            ("addin/RevitMCP.addin", "RevitMCP.addin"),
            ("addin/Newtonsoft.Json.dll", "Newtonsoft.Json.dll"),
            ("addin/Microsoft.CodeAnalysis.dll", "Microsoft.CodeAnalysis.dll"),
            ("addin/Microsoft.CodeAnalysis.CSharp.dll", "Microsoft.CodeAnalysis.CSharp.dll"),
        ];

        for (arch_name, dest_name) in files_to_deploy {
            if let Some(data) = payload.get(arch_name) {
                let dest_path = addin_dir.join(dest_name);
                let _ = fs::write(&dest_path, data);
            }
        }

        // RevitMCP.config.json: only deploy if not already present
        let config_dest = addin_dir.join("RevitMCP.config.json");
        if !config_dest.exists() {
            if let Some(data) = payload.get("addin/RevitMCP.config.json") {
                let _ = fs::write(&config_dest, data);
            }
        }

        if !silent {
            println!("  {} Add-in déployé pour Revit {}", "[OK]".green(), year);
        }
    }

    // 2. Configure Revit.ini & Windows Registry for silent startup
    if !silent {
        println!("\n{}", "[2/4] Configuration du démarrage silencieux Revit (zéro popup)...".yellow().bold());
    }

    let hkcu = RegKey::predef(HKEY_CURRENT_USER);

    for year in &years {
        // Registry: HKCU\SOFTWARE\Autodesk\Revit\Autodesk Revit <Year>\CodeSigning
        let reg_path = format!(r"SOFTWARE\Autodesk\Revit\Autodesk Revit {}\CodeSigning", year);
        if let Ok((key, _)) = hkcu.create_subkey_with_flags(&reg_path, KEY_ALL_ACCESS) {
            // Clean bad non-guid names
            let names: Vec<String> = key.enum_values().filter_map(|x| x.ok().map(|(n, _)| n)).collect();
            for n in names {
                if n.starts_with("RevitMCP") || n.starts_with('{') {
                    let _ = key.delete_value(&n);
                }
            }
            // Set canonical GUID = 1
            let _ = key.set_value(CLIENT_ID_GUID, &1u32);
        }

        // Revit.ini: APPDATA\Autodesk\Revit\Autodesk Revit <Year>\Revit.ini
        let ini_path = Path::new(&appdata).join("Autodesk").join("Revit").join(format!("Autodesk Revit {}", year)).join("Revit.ini");
        patch_revit_ini(&ini_path);

        // Also check template in ProgramData
        let progdata = env::var("ProgramData").unwrap_or_else(|_| r"C:\ProgramData".to_string());
        let template_ini = Path::new(&progdata).join("Autodesk").join(format!("RVT {}", year)).join("UserDataCache").join("Revit.ini");
        if template_ini.exists() {
            patch_revit_ini(&template_ini);
        }

        if !silent {
            println!("  {} Paramètres de sécurité configurés pour Revit {}", "[OK]".green(), year);
        }
    }

    // 3. Deploy MCP server backends (Rust native binary and/or Node bundle)
    if !silent {
        println!("\n{}", "[3/4] Déploiement du serveur MCP autonome...".yellow().bold());
    }

    let mcp_install_dir = Path::new(&appdata).join("RevitMCP").join("mcp-server");
    fs::create_dir_all(&mcp_install_dir).unwrap_or_default();

    let mut has_rust_server = false;
    let rust_exe_dest = mcp_install_dir.join("revit-mcp-server.exe");
    if let Some(exe_data) = payload.get("server/revit-mcp-server.exe") {
        let _ = fs::write(&rust_exe_dest, exe_data);
        has_rust_server = true;
        if !silent {
            println!("  {} Serveur MCP Rust natif déployé ({})", "[OK]".green(), rust_exe_dest.display());
        }
    }

    let server_bundle_dest = mcp_install_dir.join("index.bundle.mjs");
    if let Some(bundle_data) = payload.get("server/index.bundle.mjs") {
        let _ = fs::write(&server_bundle_dest, bundle_data);
        if !silent {
            println!("  {} Serveur MCP Node bundle déployé ({})", "[OK]".green(), server_bundle_dest.display());
        }
    }

    // 4. Configure all AI clients (Universal Zero-Config)
    let use_rust = has_rust_server && !force_node;
    let claude_command = if use_rust {
        rust_exe_dest.to_string_lossy().replace('\\', "/")
    } else {
        "node".to_string()
    };
    let claude_args: Vec<String> = if use_rust {
        vec![]
    } else {
        vec![server_bundle_dest.to_string_lossy().replace('\\', "/")]
    };

    if !silent {
        let mode_desc = if use_rust {
            "Rust natif autonome (Zéro dépendance Node.js)".cyan().bold()
        } else {
            "Node.js bundle".yellow()
        };
        println!("\n{} [{}]", "[4/4] Configuration automatique de toutes vos IA (Zéro-Config)...".yellow().bold(), mode_desc);
    }

    let home = env::var("USERPROFILE").unwrap_or_else(|_| env::var("HOME").unwrap_or_default());
    let home_path = Path::new(&home);

    let ai_targets = get_ai_client_targets(&appdata, &localappdata, home_path);
    let mut configured_count = 0;

    for target in &ai_targets {
        let should_configure = target.path.exists()
            || target.condition_dir.as_ref().map(|d| d.exists()).unwrap_or(false)
            || target.always_create;

        if should_configure {
            if let Some(parent) = target.path.parent() {
                let _ = fs::create_dir_all(parent);
            }
            if patch_mcp_config(&target.path, &claude_command, &claude_args, target.is_cline_style) {
                configured_count += 1;
                if !silent {
                    println!("  {} {} configuré ({})", "[OK]".green(), target.name, target.path.display());
                }
            }
        }
    }

    if !silent && configured_count == 0 {
        println!("  {} Aucune IA détectée pour l'instant (les fichiers seront créés dès le premier lancement)", "[!]".yellow());
    }

    // 5. Live connection verification with Revit
    if !silent {
        println!("\n{}", "[VÉRIFICATION DU PONT EN DIRECT]".cyan().bold());
        match check_live_revit_bridge() {
            Some(info) => {
                println!("  {} Autodesk Revit est OUVERT et CONNECTÉ !", "[CONNECTÉ]".green().bold());
                println!("     Maquette(s) active(s) : {}", info.cyan());
            }
            None => {
                println!("  {} Autodesk Revit n'est pas encore ouvert.", "[EN ATTENTE]".yellow().bold());
                println!("     Dès que vous lancerez Revit, toutes vos IA s'y connecteront automatiquement avec zéro configuration.");
            }
        }

        println!();
        println!("{}", "==========================================================".green());
        println!("{}", "     INSTALLATION DE REVIT AI BRIDGE TERMINÉE AVEC SUCCÈS !".green().bold());
        println!("{}", "==========================================================".green());
        println!("\nPrêt à l'emploi :");
        println!("* Lancez n'importe quelle IA (Claude Desktop, Claude Code, Cursor, Windsurf, Cline, Roo Code).");
        println!("* Les 191 outils Revit sont immédiatement reconnus et disponibles !");
        println!();
    }
}

struct AiClientTarget {
    name: &'static str,
    path: PathBuf,
    is_cline_style: bool,
    condition_dir: Option<PathBuf>,
    always_create: bool,
}

fn get_ai_client_targets(appdata: &str, localappdata: &str, home: &Path) -> Vec<AiClientTarget> {
    vec![
        // 1. Claude Desktop (APPDATA standard)
        AiClientTarget {
            name: "Claude Desktop",
            path: Path::new(appdata).join("Claude").join("claude_desktop_config.json"),
            is_cline_style: false,
            condition_dir: Some(Path::new(appdata).join("Claude")),
            always_create: true,
        },
        // 2. Claude Desktop (Windows Store MSIX)
        AiClientTarget {
            name: "Claude Desktop (Windows Store)",
            path: Path::new(localappdata)
                .join("Packages")
                .join("Claude_pzs8sxrjxfjjc")
                .join("LocalCache")
                .join("Roaming")
                .join("Claude")
                .join("claude_desktop_config.json"),
            is_cline_style: false,
            condition_dir: Some(Path::new(localappdata).join("Packages").join("Claude_pzs8sxrjxfjjc")),
            always_create: false,
        },
        // 3. Claude Code CLI
        AiClientTarget {
            name: "Claude Code CLI",
            path: home.join(".claude.json"),
            is_cline_style: false,
            condition_dir: None,
            always_create: home.join(".claude.json").exists(),
        },
        // 4. Cline (VS Code)
        AiClientTarget {
            name: "Cline (VS Code)",
            path: Path::new(appdata)
                .join("Code")
                .join("User")
                .join("globalStorage")
                .join("saoudrizwan.claude-dev")
                .join("settings")
                .join("cline_mcp_settings.json"),
            is_cline_style: true,
            condition_dir: Some(Path::new(appdata).join("Code")),
            always_create: false,
        },
        // 5. Cline (VS Code Insiders)
        AiClientTarget {
            name: "Cline (VS Code Insiders)",
            path: Path::new(appdata)
                .join("Code - Insiders")
                .join("User")
                .join("globalStorage")
                .join("saoudrizwan.claude-dev")
                .join("settings")
                .join("cline_mcp_settings.json"),
            is_cline_style: true,
            condition_dir: Some(Path::new(appdata).join("Code - Insiders")),
            always_create: false,
        },
        // 6. Roo Code (VS Code)
        AiClientTarget {
            name: "Roo Code (VS Code)",
            path: Path::new(appdata)
                .join("Code")
                .join("User")
                .join("globalStorage")
                .join("rooveterinaryinc.roo-cline")
                .join("settings")
                .join("cline_mcp_settings.json"),
            is_cline_style: true,
            condition_dir: Some(Path::new(appdata).join("Code")),
            always_create: false,
        },
        // 7. Roo Code (VS Code Insiders)
        AiClientTarget {
            name: "Roo Code (VS Code Insiders)",
            path: Path::new(appdata)
                .join("Code - Insiders")
                .join("User")
                .join("globalStorage")
                .join("rooveterinaryinc.roo-cline")
                .join("settings")
                .join("cline_mcp_settings.json"),
            is_cline_style: true,
            condition_dir: Some(Path::new(appdata).join("Code - Insiders")),
            always_create: false,
        },
        // 8. Cursor IDE (Global)
        AiClientTarget {
            name: "Cursor IDE (Global MCP)",
            path: home.join(".cursor").join("mcp.json"),
            is_cline_style: false,
            condition_dir: Some(home.join(".cursor")),
            always_create: true,
        },
        // 9. Cursor IDE (Extension Storage)
        AiClientTarget {
            name: "Cursor IDE (Storage MCP)",
            path: Path::new(appdata)
                .join("Cursor")
                .join("User")
                .join("globalStorage")
                .join("rooveterinaryinc.cursor-mcp")
                .join("mcp.json"),
            is_cline_style: false,
            condition_dir: Some(Path::new(appdata).join("Cursor")),
            always_create: false,
        },
        // 10. Windsurf / Codeium IDE
        AiClientTarget {
            name: "Windsurf / Codeium IDE",
            path: home.join(".codeium").join("windsurf").join("mcp_config.json"),
            is_cline_style: false,
            condition_dir: Some(home.join(".codeium")),
            always_create: true,
        },
    ]
}

fn patch_mcp_config(cfg_path: &Path, command: &str, args: &[String], is_cline_style: bool) -> bool {
    let mut root_val: Value = if cfg_path.exists() {
        let Ok(text) = fs::read_to_string(cfg_path) else { return false };
        serde_json::from_str(&text).unwrap_or_else(|_| json!({}))
    } else {
        json!({})
    };

    if !root_val.is_object() {
        root_val = json!({});
    }

    let root_obj = root_val.as_object_mut().unwrap();
    if !root_obj.contains_key("mcpServers") || !root_obj["mcpServers"].is_object() {
        root_obj.insert("mcpServers".to_string(), json!({}));
    }

    let servers_obj = root_obj.get_mut("mcpServers").unwrap().as_object_mut().unwrap();

    let mut revit_entry = json!({
        "command": command,
        "args": args,
        "env": {
            "REVIT_HOST": "http://localhost:6543",
            "REVIT_TIMEOUT_MS": "120000"
        }
    });

    if is_cline_style {
        if let Some(obj) = revit_entry.as_object_mut() {
            obj.insert("disabled".to_string(), json!(false));
            obj.insert("autoApprove".to_string(), json!([]));
        }
    }

    servers_obj.insert("revit".to_string(), revit_entry);

    let Ok(formatted) = serde_json::to_string_pretty(&root_val) else { return false };
    fs::write(cfg_path, formatted).is_ok()
}

fn check_live_revit_bridge() -> Option<String> {
    use std::io::{Read, Write};
    use std::net::{SocketAddr, TcpStream};
    use std::time::Duration;

    let addr: SocketAddr = "127.0.0.1:6543".parse().ok()?;
    let mut stream = TcpStream::connect_timeout(&addr, Duration::from_millis(800)).ok()?;
    stream.set_read_timeout(Some(Duration::from_millis(1500))).ok()?;
    stream.set_write_timeout(Some(Duration::from_millis(800))).ok()?;

    let req = "GET /healthz HTTP/1.1\r\nHost: localhost:6543\r\nConnection: close\r\n\r\n";
    stream.write_all(req.as_bytes()).ok()?;

    let mut resp = String::new();
    stream.read_to_string(&mut resp).ok()?;

    if let Some(body_start) = resp.find("\r\n\r\n") {
        let body = &resp[body_start + 4..];
        if let Ok(val) = serde_json::from_str::<Value>(body) {
            if val.get("ok").and_then(|v| v.as_bool()) == Some(true) {
                let open_docs = val.get("openDocuments").and_then(|d| d.as_array());
                if let Some(docs) = open_docs {
                    let titles: Vec<String> = docs
                        .iter()
                        .filter_map(|d| d.get("title").and_then(|t| t.as_str()).map(|s| s.to_string()))
                        .collect();
                    if !titles.is_empty() {
                        return Some(titles.join(", "));
                    }
                }
                return Some("Revit actif (aucun projet ouvert)".to_string());
            }
        }
    }
    None
}

fn read_file_text_utf16_or_utf8(path: &Path) -> Option<(String, bool)> {
    let bytes = fs::read(path).ok()?;
    let is_utf16 = bytes.len() >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE;

    let content = if is_utf16 {
        let u16_chars: Vec<u16> = bytes[2..]
            .chunks_exact(2)
            .map(|chunk| u16::from_le_bytes([chunk[0], chunk[1]]))
            .collect();
        String::from_utf16_lossy(&u16_chars)
    } else {
        String::from_utf8_lossy(&bytes).to_string()
    };

    Some((content, is_utf16))
}

fn patch_revit_ini(ini_path: &Path) {
    let Some((content, is_utf16)) = read_file_text_utf16_or_utf8(ini_path) else {
        return;
    };

    if content.contains("DisableAddInSecurity") {
        return; // Déjà configuré
    }

    let new_content = if content.contains("[Misc]") {
        content.replace("[Misc]", "[Misc]\r\nDisableAddInSecurity=1")
    } else {
        format!("{}\r\n[Misc]\r\nDisableAddInSecurity=1\r\n", content.trim_end())
    };

    if is_utf16 {
        let mut out = vec![0xFF, 0xFE];
        for c in new_content.encode_utf16() {
            out.extend_from_slice(&c.to_le_bytes());
        }
        let _ = fs::write(ini_path, out);
    } else {
        let _ = fs::write(ini_path, new_content.as_bytes());
    }
}

fn run_status() {
    print_banner();
    println!("{}", "[DIAGNOSTIC DU SYSTÈME ET DE L'INSTALLATION]".cyan().bold());

    let appdata = env::var("APPDATA").unwrap_or_default();
    let localappdata = env::var("LOCALAPPDATA").unwrap_or_default();
    let home = env::var("USERPROFILE").unwrap_or_else(|_| env::var("HOME").unwrap_or_default());
    let home_path = Path::new(&home);

    let years = detect_revit_years();
    println!("* Versions Revit détectées : {}", years.join(", "));

    for year in &years {
        let addin_dll = Path::new(&appdata).join("Autodesk").join("Revit").join("Addins").join(year).join("RevitMCP.dll");
        if addin_dll.exists() {
            println!("  {} Add-in Revit {} : Présent ({})", "[OK]".green(), year, addin_dll.display());
        } else {
            println!("  {} Add-in Revit {} : Non installé", "[!]".yellow(), year);
        }

        let ini_path = Path::new(&appdata).join("Autodesk").join("Revit").join(format!("Autodesk Revit {}", year)).join("Revit.ini");
        if ini_path.exists() {
            let content = read_file_text_utf16_or_utf8(&ini_path).map(|(c, _)| c).unwrap_or_default();
            if content.contains("DisableAddInSecurity") {
                println!("  {} Revit.ini {} : Sécurité silencieuse configurée", "[OK]".green(), year);
            } else {
                println!("  {} Revit.ini {} : Non configuré (popup possible)", "[!]".yellow(), year);
            }
        }
    }

    let rust_server = Path::new(&appdata).join("RevitMCP").join("mcp-server").join("revit-mcp-server.exe");
    if rust_server.exists() {
        println!("  {} Serveur MCP Rust natif : Déployé ({})", "[OK]".green(), rust_server.display());
    } else {
        println!("  {} Serveur MCP Rust natif : Non déployé", "[!]".yellow());
    }

    let mcp_bundle = Path::new(&appdata).join("RevitMCP").join("mcp-server").join("index.bundle.mjs");
    if mcp_bundle.exists() {
        println!("  {} Serveur MCP Node bundle : Déployé ({})", "[OK]".green(), mcp_bundle.display());
    } else {
        println!("  {} Serveur MCP Node bundle : Non déployé", "[!]".yellow());
    }

    println!("\n* Clients d'Intelligence Artificielle configurés :");
    let ai_targets = get_ai_client_targets(&appdata, &localappdata, home_path);
    for t in &ai_targets {
        if t.path.exists() {
            let content = fs::read_to_string(&t.path).unwrap_or_default();
            if content.contains("revit-mcp-server.exe") {
                println!("  {} {} : 'revit' actif via Rust natif", "[OK]".green(), t.name);
            } else if content.contains("\"revit\"") {
                println!("  {} {} : 'revit' actif via Node.js bundle", "[OK]".green(), t.name);
            } else {
                println!("  {} {} : Présent mais 'revit' non configuré", "[!]".yellow(), t.name);
            }
        }
    }

    println!("\n* État de connexion avec Revit en direct :");
    match check_live_revit_bridge() {
        Some(info) => {
            println!("  {} Autodesk Revit est OUVERT et le pont est ACTIF !", "[CONNECTÉ]".green().bold());
            println!("     Maquette(s) : {}", info.cyan());
        }
        None => {
            println!("  {} Autodesk Revit n'est pas ouvert en ce moment.", "[EN ATTENTE]".yellow());
        }
    }
    println!();
}

fn run_uninstall(silent: bool) {
    if !silent {
        print_banner();
        println!("{}", "[DÉSINSTALLATION DE REVIT AI BRIDGE]".yellow().bold());
    }

    let appdata = env::var("APPDATA").unwrap_or_default();
    let localappdata = env::var("LOCALAPPDATA").unwrap_or_default();
    let home = env::var("USERPROFILE").unwrap_or_else(|_| env::var("HOME").unwrap_or_default());
    let home_path = Path::new(&home);

    let years = detect_revit_years();

    for year in &years {
        let addin_dir = Path::new(&appdata).join("Autodesk").join("Revit").join("Addins").join(year);
        let files = ["RevitMCP.dll", "RevitMCP.addin", "RevitMCP.pdb", "RevitMCP.deps.json"];
        for f in files {
            let p = addin_dir.join(f);
            if p.exists() {
                let _ = fs::remove_file(p);
            }
        }
        if !silent {
            println!("  {} Fichiers add-in supprimés pour Revit {}", "[OK]".green(), year);
        }
    }

    let mcp_dir = Path::new(&appdata).join("RevitMCP");
    if mcp_dir.exists() {
        let _ = fs::remove_dir_all(mcp_dir);
        if !silent {
            println!("  {} Répertoire serveur MCP supprimé", "[OK]".green());
        }
    }

    let ai_targets = get_ai_client_targets(&appdata, &localappdata, home_path);
    for t in &ai_targets {
        if t.path.exists() {
            if let Ok(text) = fs::read_to_string(&t.path) {
                if let Ok(mut val) = serde_json::from_str::<Value>(&text) {
                    if let Some(obj) = val.get_mut("mcpServers").and_then(|s| s.as_object_mut()) {
                        if obj.remove("revit").is_some() {
                            if let Ok(out) = serde_json::to_string_pretty(&val) {
                                let _ = fs::write(&t.path, out);
                                if !silent {
                                    println!("  {} Entrée 'revit' retirée de {}", "[OK]".green(), t.name);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    if !silent {
        println!("\n{}", "Désinstallation terminée avec succès.".green().bold());
    }
}
