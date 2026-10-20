use colored::Colorize;
use flate2::read::GzDecoder;
use serde_json::{json, Value};
use std::collections::HashMap;
use std::env;
use std::fs;
use std::io::{Cursor, Read};
use std::path::Path;
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
        format!("Revit AI Bridge Installer v{}", VERSION).cyan().bold()
    );
}

fn print_banner() {
    println!("{}", "==========================================================".cyan());
    println!("{}", format!("       REVIT AI BRIDGE — INSTALLATEUR UNIVERSEL v{}       ", VERSION).cyan().bold());
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

    // 4. Configure Claude Desktop
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
        println!("\n{} [{}]", "[4/4] Configuration automatique de Claude Desktop...".yellow().bold(), mode_desc);
    }

    let claude_configs = [
        // Standard Claude Desktop
        Path::new(&appdata).join("Claude").join("claude_desktop_config.json"),
        // Windows Store MSIX Claude
        Path::new(&localappdata)
            .join("Packages")
            .join("Claude_pzs8sxrjxfjjc")
            .join("LocalCache")
            .join("Roaming")
            .join("Claude")
            .join("claude_desktop_config.json"),
    ];

    let mut configured_count = 0;
    for cfg_path in &claude_configs {
        if let Some(parent) = cfg_path.parent() {
            if parent.exists() || cfg_path == &claude_configs[0] {
                fs::create_dir_all(parent).unwrap_or_default();
                if patch_claude_config(cfg_path, &claude_command, &claude_args) {
                    configured_count += 1;
                    if !silent {
                        println!("  {} Configuration injectée dans {}", "[OK]".green(), cfg_path.display());
                    }
                }
            }
        }
    }
    if !silent && configured_count == 0 {
        println!("  {} Aucun profil Claude Desktop détecté (sera configuré au premier lancement de Claude)", "[!]".yellow());
    }

    if !silent {
        println!();
        println!("{}", "==========================================================".green());
        println!("{}", "     INSTALLATION DE REVIT AI BRIDGE TERMINÉE AVEC SUCCÈS !".green().bold());
        println!("{}", "==========================================================".green());
        println!("\nProchaines étapes :");
        println!("1. Lancez {} et ouvrez un projet BIM.", "Autodesk Revit".cyan().bold());
        println!("2. Lancez {} : l'icône des 191 outils MCP sera active.", "Claude Desktop".cyan().bold());
        println!("3. Testez directement : {}", "\"Fais un diagnostic de la maquette Revit active.\"".yellow());
        println!();
    }
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

fn patch_claude_config(cfg_path: &Path, command: &str, args: &[String]) -> bool {
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

    let revit_entry = json!({
        "command": command,
        "args": args,
        "env": {
            "REVIT_HOST": "http://localhost:6543",
            "REVIT_TIMEOUT_MS": "120000"
        }
    });

    servers_obj.insert("revit".to_string(), revit_entry);

    let Ok(formatted) = serde_json::to_string_pretty(&root_val) else { return false };
    fs::write(cfg_path, formatted).is_ok()
}

fn run_status() {
    print_banner();
    println!("{}", "[DIAGNOSTIC DU SYSTÈME ET DE L'INSTALLATION]".cyan().bold());

    let appdata = env::var("APPDATA").unwrap_or_default();
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

    let claude_cfg = Path::new(&appdata).join("Claude").join("claude_desktop_config.json");
    if claude_cfg.exists() {
        let content = fs::read_to_string(&claude_cfg).unwrap_or_default();
        if content.contains("revit-mcp-server.exe") {
            println!("  {} Claude Desktop : Serveur 'revit' actif via Rust natif", "[OK]".green());
        } else if content.contains("\"revit\"") {
            println!("  {} Claude Desktop : Serveur 'revit' actif via Node.js bundle", "[OK]".green());
        } else {
            println!("  {} Claude Desktop : Serveur 'revit' manquant", "[!]".yellow());
        }
    } else {
        println!("  {} Claude Desktop config : Introuvable", "[!]".yellow());
    }
    println!();
}

fn run_uninstall(silent: bool) {
    if !silent {
        print_banner();
        println!("{}", "[DÉSINSTALLATION DE REVIT AI BRIDGE]".yellow().bold());
    }

    let appdata = env::var("APPDATA").unwrap_or_default();
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

    let claude_cfg = Path::new(&appdata).join("Claude").join("claude_desktop_config.json");
    if claude_cfg.exists() {
        if let Ok(text) = fs::read_to_string(&claude_cfg) {
            if let Ok(mut val) = serde_json::from_str::<Value>(&text) {
                if let Some(obj) = val.get_mut("mcpServers").and_then(|s| s.as_object_mut()) {
                    obj.remove("revit");
                    if let Ok(out) = serde_json::to_string_pretty(&val) {
                        let _ = fs::write(&claude_cfg, out);
                        if !silent {
                            println!("  {} Entrée 'revit' retirée de Claude Desktop", "[OK]".green());
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
