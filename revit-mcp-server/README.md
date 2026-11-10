# revit-mcp-rust (`revit-mcp-server`) — Serveur MCP Unifié Revit + CAD en Rust Natif

> **Empreinte mémoire au repos :** `~6 Mo` de RAM (binaire natif `x86_64` de `2,41 Mo`, démarrage en `< 5 ms`).  
> **Économie réalisée :** Remplace **le serveur Python (`cad-mcp/server.py`, `160 Mo` par instance)** et **le serveur Node.js (`mcp-server/dist/index.js`, `72 Mo` par instance)** dans Claude Desktop et Antigravity.

---

## 🎯 Objectif

Lors de l'audit de performance du PC, nous avons constaté que **Claude Desktop** (version Microsoft Store `Claude-Data\claude_pzs8sxrjxfjjc`) et **Antigravity** (`.gemini\config\mcp_config.json`) lançaient chacun :
- Une instance Python 3.13 (`cad-mcp/server.py`) consommant **~160 Mo de RAM** (soit **480 Mo** pour 3 instances ouvertes).
- Une instance Node.js (`mcp-server/dist/index.js`) consommant **~72 Mo de RAM** (soit **144 Mo** pour 2 instances ouvertes).

Ce binaire Rust unique (`revit-mcp-server.exe`) gère **les deux protocoles MCP** :
1. **Mode Revit (`revit`)** : lancé sans argument (`revit-mcp-server.exe`), expose l'intégralité des outils Autodesk Revit + BIMcraft via JSON-RPC stdio ultra-rapide (`tokio` + `reqwest` + `memmap2` + `rayon`).
2. **Mode AutoCAD / CAD (`cad-mcp`)** : lancé avec la sous-commande `cad-mcp` (`revit-mcp-server.exe cad-mcp`), expose le serveur MCP CAD sans interpréteur Python.

---

## ⚙️ Configuration Active sur le PC

### 1. Antigravity (`C:\Users\teoba\.gemini\config\mcp_config.json`)
```json
{
  "mcpServers": {
    "revit": {
      "command": "C:/Users/teoba/Documents/Devs/MCP Revit/revit-mcp-server/target/release/revit-mcp-server.exe",
      "args": [],
      "env": {
        "REVIT_HOST": "http://localhost:6543",
        "REVIT_TIMEOUT_MS": "120000"
      }
    }
  }
}
```

### 2. Claude Desktop Store (`C:\Users\teoba\AppData\Local\Claude-Data\claude_pzs8sxrjxfjjc\claude_desktop_config.json`) & Roaming (`C:\Users\teoba\AppData\Roaming\Claude\claude_desktop_config.json`)
```json
{
  "mcpServers": {
    "cad-mcp": {
      "command": "C:/Users/teoba/Documents/Devs/MCP Revit/revit-mcp-server/target/release/revit-mcp-server.exe",
      "args": ["cad-mcp"]
    },
    "revit": {
      "command": "C:/Users/teoba/Documents/Devs/MCP Revit/revit-mcp-server/target/release/revit-mcp-server.exe",
      "args": [],
      "env": {
        "REVIT_HOST": "http://localhost:6543",
        "REVIT_TIMEOUT_MS": "120000"
      }
    }
  }
}
```

---

## 🚀 Compilation & Mise à jour

```powershell
cd "C:\Users\teoba\Documents\Devs\MCP Revit\revit-mcp-server"
cargo build --release
```
Le binaire compilé (`LTO = "fat"`, `opt-level = 3`, `strip = true`) est généré dans :
`C:\Users\teoba\Documents\Devs\MCP Revit\revit-mcp-server\target\release\revit-mcp-server.exe`.
