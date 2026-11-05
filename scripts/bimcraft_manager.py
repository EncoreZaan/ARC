#!/usr/bin/env python3
"""
ARC — BIMcraft Universal Automation & Bridge Manager (v2.7.0)
Multi-plateforme natif : macOS (Apple Silicon M1/M2/M3/M4 & Intel), Linux & Windows.
Gère le cycle de vie de BIMcraft, le pont Agent MCP, la conversion des fichiers RVT/IFC,
et la synchronisation des modèles architecturaux.
"""

import sys
import os
import json
import time
import subprocess
from pathlib import Path

# Résolution multi-plateforme du répertoire Home
HOME = Path(os.environ.get("HOME") or os.environ.get("USERPROFILE") or ".")

def find_cli_path() -> Path:
    candidates = [
        HOME / ".cargo" / "bin" / ("bimcraft-cli.exe" if sys.platform == "win32" else "bimcraft-cli"),
        HOME / ".local" / "bin" / "bimcraft-cli",
        Path("/opt/homebrew/bin/bimcraft-cli"),
        Path("/usr/local/bin/bimcraft-cli"),
        Path("/usr/bin/bimcraft-cli")
    ]
    for c in candidates:
        if c.exists():
            return c
    return Path("bimcraft-cli.exe" if sys.platform == "win32" else "bimcraft-cli")

def find_gui_path() -> Path:
    candidates = [
        HOME / ".cargo" / "bin" / ("bimcraft.exe" if sys.platform == "win32" else "bimcraft"),
        HOME / ".local" / "bin" / "bimcraft",
        Path("/Applications/BIMCraft.app/Contents/MacOS/bimcraft"),
        HOME / "Applications" / "BIMCraft.app" / "Contents" / "MacOS" / "bimcraft",
        Path("/opt/homebrew/bin/bimcraft"),
        Path("/usr/local/bin/bimcraft"),
        Path("/usr/bin/bimcraft")
    ]
    for c in candidates:
        if c.exists():
            return c
    return Path("bimcraft.exe" if sys.platform == "win32" else "bimcraft")

def find_bridge_json_path() -> Path:
    if sys.platform == "win32":
        appdata = os.environ.get("APPDATA")
        if appdata:
            return Path(appdata) / "BIMCraft" / "bridge.json"
    elif sys.platform == "darwin":
        return HOME / "Library" / "Application Support" / "BIMCraft" / "bridge.json"
    else:
        xdg = os.environ.get("XDG_CONFIG_HOME")
        if xdg:
            return Path(xdg) / "BIMCraft" / "bridge.json"
        return HOME / ".config" / "BIMCraft" / "bridge.json"
    return HOME / ".config" / "BIMCraft" / "bridge.json"

BIMCRAFT_CLI = find_cli_path()
BIMCRAFT_GUI = find_gui_path()
BRIDGE_JSON = find_bridge_json_path()

LONDON_PROJECT_DIR = Path(r"C:\Users\teoba\Documents\Projet Appart Londonien\06_Revit")


def get_status():
    """Vérifie l'état de BIMcraft CLI et GUI sur la plateforme courante."""
    remote_release = fetch_latest_github_release()
    latest_tag = remote_release.get("tag_name") if remote_release else None
    cli_ok = BIMCRAFT_CLI.exists()
    gui_ok = BIMCRAFT_GUI.exists()
    
    bridge_info = None
    gui_active = False
    
    if BRIDGE_JSON.exists():
        try:
            with open(BRIDGE_JSON, "r", encoding="utf-8") as f:
                bridge_info = json.load(f)
                gui_active = True
        except Exception:
            pass
            
    version = "N/A"
    if cli_ok:
        try:
            out = subprocess.run([str(BIMCRAFT_CLI), "--version"], capture_output=True, text=True, timeout=5)
            if out.returncode == 0:
                version = out.stdout.strip()
        except Exception:
            pass
            
    status = {
        "status": "OPERATIONAL",
        "platform": sys.platform,
        "bimcraftVersion": version,
        "cliInstalled": cli_ok,
        "guiInstalled": gui_ok,
        "guiActive": gui_active,
        "cliPath": str(BIMCRAFT_CLI),
        "guiPath": str(BIMCRAFT_GUI),
        "bridgeConfig": bridge_info,
        "bridgePath": str(BRIDGE_JSON) if BRIDGE_JSON.exists() else None,
        "crossPlatformSupport": {
            "macOS": "100% Natif Metal / Apple Silicon M1-M4 & Intel",
            "linux": "100% Natif Vulkan / Wayland / X11",
            "windows": "100% Natif DirectX 12 / Vulkan"
        }
    }
    return status


def launch_gui():
    """Démarre l'interface graphique BIMcraft avec le pont Agent actif (--control 0) de manière multi-plateforme."""
    gui = find_gui_path()
    if not gui.exists() and not Path("/Applications/BIMCraft.app").exists():
        print(f"[ERREUR] Exécutable BIMcraft introuvable à : {gui}")
        return False
        
    print(f"[INFO] Démarrage du GUI BIMcraft sur {sys.platform} avec pont Agent actif (--control 0)...")
    
    try:
        if sys.platform == "win32":
            # Lancement détaché WMI sous Windows
            ps_cmd = f"Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{{CommandLine = '\"{str(gui)}\" --control 0'}}"
            subprocess.run(["powershell", "-NoProfile", "-Command", ps_cmd], capture_output=True, text=True, timeout=10)
        elif sys.platform == "darwin":
            # macOS : Application bundle ou binaire Cargo détaché
            if Path("/Applications/BIMCraft.app").exists():
                subprocess.Popen(["open", "-a", "BIMCraft", "--args", "--control", "0"])
            else:
                subprocess.Popen([str(gui), "--control", "0"], start_new_session=True)
        else:
            # Linux : Processus détaché
            subprocess.Popen([str(gui), "--control", "0"], start_new_session=True)
    except Exception as e:
        print(f"[ERREUR] Lancement échoué: {e}")
        return False

    # Attente de bridge.json
    bridge_p = find_bridge_json_path()
    for _ in range(15):
        time.sleep(0.3)
        if bridge_p.exists():
            try:
                with open(bridge_p, "r", encoding="utf-8") as f:
                    cfg = json.load(f)
                    print(f"[SUCCÈS] BIMcraft GUI actif ! Port de contrôle : {cfg.get('port')}, Token : {cfg.get('token')}")
                    return True
            except Exception:
                pass
                
    print("[WARN] BIMcraft lancé mais bridge.json n'a pas encore répondu.")
    return True


def convert_file(input_path: str, output_path: str):
    """Convertit un fichier (.rvt, .bimcraft, .ifc) vers le format cible."""
    cli = find_cli_path()
    if not cli.exists():
        print(f"[ERREUR] CLI introuvable : {cli}")
        return False
        
    p_in = Path(input_path)
    p_out = Path(output_path)
    p_out.parent.mkdir(parents=True, exist_ok=True)
    
    cmd = [str(cli), "convert", str(p_in), str(p_out)]
    print(f"[CONVERT] {p_in.name} -> {p_out.name}...")
    res = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
    if res.returncode == 0:
        print(f"[OK] Fichier généré avec succès : {p_out}")
        return True
    else:
        print(f"[ERREUR] Échec de conversion : {res.stderr or res.stdout}")
        return False


def inspect_file(file_path: str):
    """Inspecte un modèle et affiche les métadonnées."""
    cli = find_cli_path()
    if not cli.exists():
        print(f"[ERREUR] CLI introuvable : {cli}")
        return
        
    cmd = [str(cli), "info", str(file_path)]
    res = subprocess.run(cmd, capture_output=True, text=True, timeout=15)
    if res.returncode == 0:
        try:
            data = json.loads(res.stdout)
            print(json.dumps(data, indent=2, ensure_ascii=False))
        except Exception:
            print(res.stdout)
    else:
        print(f"[ERREUR] {res.stderr}")


def convert_all_project_variants():
    """Convertit l'ensemble des fichiers RVT du projet en .bimcraft et génère des plans SVG."""
    if not LONDON_PROJECT_DIR.exists():
        print(f"[WARN] Dossier projet introuvable : {LONDON_PROJECT_DIR}")
        return
        
    rvt_files = list(LONDON_PROJECT_DIR.glob("*.rvt"))
    print(f"[INFO] {len(rvt_files)} variantes RVT trouvées dans le projet.")
    
    for rvt in rvt_files:
        bimcraft_file = rvt.with_suffix(".bimcraft")
        svg_file = rvt.with_suffix(".svg")
        
        convert_file(str(rvt), str(bimcraft_file))
        convert_file(str(bimcraft_file), str(svg_file))



def fetch_latest_github_release():
    """Interroge l'API GitHub pour obtenir la dernière release officielle de IPedrax/BIMCraft."""
    import urllib.request
    url = "https://api.github.com/repos/IPedrax/BIMCraft/releases/latest"
    req = urllib.request.Request(url, headers={"User-Agent": "ARC-BIMcraft-Manager"})
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except Exception as e:
        print(f"[WARN] Impossible d'interroger l'API GitHub pour BIMcraft : {e}")
        return None


def get_recommended_asset_for_platform(release_data):
    """Détecte l'asset approprié selon l'OS courant."""
    if not release_data or "assets" not in release_data:
        return None
        
    assets = release_data.get("assets", [])
    
    if sys.platform == "darwin":
        # macOS Universal DMG (M1-M4 & Intel)
        for a in assets:
            if a["name"].endswith(".dmg"):
                return a
    elif sys.platform == "win32":
        # Windows MSI ou portable ZIP
        for a in assets:
            if a["name"].endswith(".msi"):
                return a
        for a in assets:
            if "windows" in a["name"].lower() and a["name"].endswith(".zip"):
                return a
    else:
        # Linux AppImage ou tar.gz
        for a in assets:
            if a["name"].endswith(".AppImage") and ("x86_64" in a["name"] or "amd64" in a["name"]):
                return a
        for a in assets:
            if a["name"].endswith(".deb") and ("x86_64" in a["name"] or "amd64" in a["name"]):
                return a
                
    return None


def download_and_update_bimcraft():
    """Télécharge et installe automatiquement la dernière release de BIMcraft."""
    import urllib.request
    print("[INFO] Vérification des mises à jour sur https://github.com/IPedrax/BIMCraft...")
    release = fetch_latest_github_release()
    if not release:
        print("[ERREUR] Impossible de récupérer les informations de release.")
        return False
        
    tag = release.get("tag_name", "Inconnu")
    print(f"[INFO] Dernière version officielle trouvée : {tag}")
    
    asset = get_recommended_asset_for_platform(release)
    if not asset:
        print(f"[ERREUR] Aucun paquet binaire trouvé pour la plateforme {sys.platform}.")
        return False
        
    url = asset.get("browser_download_url")
    name = asset.get("name")
    size_mb = asset.get("size", 0) / (1024 * 1024)
    print(f"[INFO] Fichier cible : {name} ({size_mb:.1f} Mo)")
    print(f"[INFO] Téléchargement depuis : {url}...")
    
    download_dir = HOME / "Downloads"
    download_dir.mkdir(parents=True, exist_ok=True)
    target_path = download_dir / name
    
    try:
        def progress(block_num, block_size, total_size):
            downloaded = block_num * block_size
            if total_size > 0:
                percent = min(100.0, (downloaded / total_size) * 100.0)
                sys.stdout.write(f"\rTéléchargement : {percent:.1f}% ({downloaded / (1024*1024):.1f}/{total_size / (1024*1024):.1f} Mo)")
                sys.stdout.flush()
                
        urllib.request.urlretrieve(url, target_path, reporthook=progress)
        print(f"\n[OK] Fichier téléchargé avec succès : {target_path}")
        
        # Guide d'exécution selon l'OS
        if sys.platform == "darwin":
            print(f"[INFO] Pour installer sur macOS : double-cliquez sur {target_path} et glissez BIMCraft dans Applications.")
        elif sys.platform == "win32":
            if target_path.suffix == ".msi":
                print(f"[INFO] Lancement de l'installateur Windows...")
                os.startfile(target_path)
            else:
                print(f"[INFO] Archive portable disponible : {target_path}")
        else:
            if target_path.suffix == ".AppImage":
                os.chmod(target_path, 0o755)
                print(f"[INFO] AppImage rendue exécutable : {target_path}")
                
        return True
    except Exception as e:
        print(f"\n[ERREUR] Échec du téléchargement : {e}")
        return False

def main():
    if len(sys.argv) < 2:
        print("Usage: bimcraft_manager.py [status|launch|update|install|convert|info|convert-all]")
        return
        
    action = sys.argv[1].lower()
    if action == "status":
        print(json.dumps(get_status(), indent=2))
    elif action == "launch":
        launch_gui()
    elif action in ("update", "install"):
        download_and_update_bimcraft()
    elif action == "convert" and len(sys.argv) >= 4:
        convert_file(sys.argv[2], sys.argv[3])
    elif action == "info" and len(sys.argv) >= 3:
        inspect_file(sys.argv[2])
    elif action == "convert-all":
        convert_all_project_variants()
    else:
        print("Commande non reconnue.")


if __name__ == "__main__":
    main()
