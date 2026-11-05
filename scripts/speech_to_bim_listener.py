#!/usr/bin/env python3
"""
🎙️ Speech-to-BIM : Contrôle Vocal Temps Réel pour Autodesk Revit via Named Pipe IPC
Exécute des commandes architecturales orales en < 300 ms sans toucher au clavier.

Exemples de commandes vocales :
- "Isole les gaines de ventilation du R+1 et masque le mobilier"
- "Masque les portes et les fenêtres"
- "Réinitialise la vue"
- "Active la vue 3D"
- "Efface les hologrammes"
- "Fais une capture d'écran"
"""

import sys
import time
import json
import os

PIPE_NAME = r"\\.\pipe\revit_mcp_ipc"
HTTP_FALLBACK_URL = "http://localhost:6543"

def send_pipe_command(action: str, payload: dict) -> dict:
    """Envoie une commande directement au Named Pipe Windows RevitMCP (< 0.5 ms)."""
    try:
        import win32file
        import win32pipe
        
        handle = win32file.CreateFile(
            PIPE_NAME,
            win32file.GENERIC_READ | win32file.GENERIC_WRITE,
            0,
            None,
            win32file.OPEN_EXISTING,
            0,
            None
        )
        
        request_obj = {"action": action, "payload": payload}
        request_bytes = (json.dumps(request_obj) + "\n").encode("utf-8")
        
        t0 = time.perf_counter()
        win32file.WriteFile(handle, request_bytes)
        
        # Lecture de la réponse
        _, resp_bytes = win32file.ReadFile(handle, 65536)
        win32file.CloseHandle(handle)
        elapsed_ms = (time.perf_counter() - t0) * 1000.0
        
        resp_json = json.loads(resp_bytes.decode("utf-8").strip())
        resp_json["ipcLatencyMs"] = round(elapsed_ms, 2)
        return resp_json
        
    except ImportError:
        # Fallback si pywin32 n'est pas installé : utilisation du protocole natif file
        try:
            with open(PIPE_NAME, "r+b", buffering=0) as pipe:
                request_obj = {"action": action, "payload": payload}
                request_bytes = (json.dumps(request_obj) + "\n").encode("utf-8")
                t0 = time.perf_counter()
                pipe.write(request_bytes)
                pipe.flush()
                resp_line = pipe.readline()
                elapsed_ms = (time.perf_counter() - t0) * 1000.0
                resp_json = json.loads(resp_line.decode("utf-8").strip())
                resp_json["ipcLatencyMs"] = round(elapsed_ms, 2)
                return resp_json
        except Exception as e:
            return send_http_fallback(action, payload, str(e))
    except Exception as e:
        return send_http_fallback(action, payload, str(e))

def send_http_fallback(action: str, payload: dict, pipe_error: str) -> dict:
    """Bascule automatiquement sur le pont HTTP si le pipe est fermé."""
    import urllib.request
    try:
        url = f"{HTTP_FALLBACK_URL}/{action}"
        data = json.dumps(payload).encode("utf-8")
        req = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"})
        t0 = time.perf_counter()
        with urllib.request.urlopen(req, timeout=5) as response:
            res_data = response.read()
            elapsed_ms = (time.perf_counter() - t0) * 1000.0
            resp_json = json.loads(res_data.decode("utf-8"))
            resp_json["ipcLatencyMs"] = round(elapsed_ms, 2)
            resp_json["transport"] = "HTTP Fallback"
            return resp_json
    except Exception as err:
        return {
            "success": False,
            "error": f"Revit inaccessible via Pipe ({pipe_error}) et HTTP ({err})"
        }

def execute_spoken_command(transcript: str):
    """Transmet la commande orale au moteur d'exécution RevitMCP."""
    print(f"\n🎙️ Commande vocale entendue : \"{transcript}\"")
    t0 = time.perf_counter()
    
    result = send_pipe_command("voice/execute-command", {"command": transcript})
    total_time_ms = (time.perf_counter() - t0) * 1000.0
    
    if result.get("success"):
        print(f"✅ Exécuté en {total_time_ms:.1f} ms (Latence IPC : {result.get('ipcLatencyMs', 0)} ms)")
        print(f"👉 Action : {result.get('message')}")
        if result.get("executedActions"):
            for a in result["executedActions"]:
                print(f"   • {a}")
    else:
        print(f"⚠️ {result.get('message', result.get('error', 'Erreur inconnue'))}")

def interactive_loop():
    print("=" * 70)
    print("🏛️  RevitMCP Speech-to-BIM Listener (Mode Temps Réel)")
    print("=" * 70)
    print("Tapez vos phrases vocales (ou intégrez un microphone avec Whisper / Gemini Live).")
    print("Exemples :")
    print("  1. 'Isole les gaines de ventilation du R+1 et masque le mobilier'")
    print("  2. 'Masque les portes et les fenêtres'")
    print("  3. 'Réinitialise la vue'")
    print("  4. 'Active la vue 3D'")
    print("  5. 'Efface les hologrammes'")
    print("  6. 'Fais une capture d'écran de la vue'")
    print("Tapez 'exit' ou Ctrl+C pour quitter.\n")
    
    while True:
        try:
            line = input("🎙️ [Parlez ou tapez] > ").strip()
            if not line:
                continue
            if line.lower() in ("exit", "quit", "q"):
                break
            execute_spoken_command(line)
        except (KeyboardInterrupt, EOFError):
            print("\nArrêt du listener vocal.")
            break

if __name__ == "__main__":
    if len(sys.argv) > 1:
        cmd = " ".join(sys.argv[1:])
        execute_spoken_command(cmd)
    else:
        interactive_loop()
