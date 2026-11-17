#![cfg_attr(windows, windows_subsystem = "windows")]
//! `arc-bimcraft-ext` — Extension Rust Zéro-Latence pour BIMcraft
//! - Zéro fenêtre console, zéro dépendance (ni Antigravity, ni PowerShell, ni Node/Python)
//! - Lance instantanément `bimcraft.exe` (`BIMCRAFT_NO_UPDATE_CHECK=1 --control 0`)
//! - Démarre en parallèle `arc-studio.exe serve 7878` en processus détaché autonome s'il n'est pas déjà actif.

use std::env;
use std::net::{SocketAddr, TcpStream};
use std::path::PathBuf;
use std::process::{Command, Stdio};
use std::time::Duration;

fn resolve_cargo_bin_exe(name: &str) -> PathBuf {
    if let Ok(exe) = env::current_exe() {
        if let Some(dir) = exe.parent() {
            let sibling = dir.join(name);
            if sibling.exists() {
                return sibling;
            }
        }
    }
    env::var("USERPROFILE")
        .or_else(|_| env::var("HOME"))
        .map(|h| PathBuf::from(h).join(".cargo").join("bin").join(name))
        .unwrap_or_else(|_| PathBuf::from(name))
}

fn main() {
    let args: Vec<String> = env::args().skip(1).collect();

    // 1. Démarrage instantané de BIMcraft (0 ms de blocage réseau)
    let bimcraft_exe = resolve_cargo_bin_exe("bimcraft.exe");
    let mut bc_cmd = Command::new(&bimcraft_exe);
    bc_cmd.env("BIMCRAFT_NO_UPDATE_CHECK", "1");
    let mut has_control = false;
    for a in &args {
        if a == "--control" {
            has_control = true;
        }
        bc_cmd.arg(a);
    }
    if !has_control {
        bc_cmd.arg("--control").arg("0");
    }
    let _ = bc_cmd
        .stdin(Stdio::null())
        .stdout(Stdio::null())
        .stderr(Stdio::null())
        .spawn();

    // 2. Démarrage autonome et détaché d'ARC Autonomous Studio si inactif (aucun lien avec Antigravity)
    if let Ok(addr) = "127.0.0.1:7878".parse::<SocketAddr>() {
        let already_up = TcpStream::connect_timeout(&addr, Duration::from_millis(20)).is_ok();
        if !already_up {
            let studio_exe = resolve_cargo_bin_exe("arc-studio.exe");
            if studio_exe.exists() {
                let mut srv_cmd = Command::new(&studio_exe);
                srv_cmd
                    .arg("serve")
                    .arg("7878")
                    .stdin(Stdio::null())
                    .stdout(Stdio::null())
                    .stderr(Stdio::null());
                #[cfg(windows)]
                {
                    use std::os::windows::process::CommandExt;
                    const DETACHED_PROCESS: u32 = 0x00000008;
                    const CREATE_NO_WINDOW: u32 = 0x08000000;
                    srv_cmd.creation_flags(DETACHED_PROCESS | CREATE_NO_WINDOW);
                }
                let _ = srv_cmd.spawn();
            }
        }
    }
}
