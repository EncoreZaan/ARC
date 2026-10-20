# GUIDE DE CONFIGURATION CLAUDE & CURSOR — REVIT AI BRIDGE (v2.2.0)

Ce document explique comment configurer vos environnements **Claude Desktop**, **Cursor** et **Claude Code CLI** pour interagir avec Autodesk Revit via le serveur natif haute performance en **Rust**.

---

## 1. Claude Desktop (Interface Graphique)

Claude Desktop charge automatiquement les serveurs MCP déclarés dans son fichier de configuration standard sous Windows :
```
%APPDATA%\Claude\claude_desktop_config.json
```
*(Chemin complet : `C:\Users\<votre_nom>\AppData\Roaming\Claude\claude_desktop_config.json`)*

### Configuration recommandée (Serveur Natif Standalone en Rust) ⚡

L'installateur autonome (`RevitMCP-Setup-v2.2.0.exe`) configure automatiquement le serveur en Rust qui offre des temps de réponse sous les 5 ms et supporte nativement la vision multimodale (images PNG encodées en Base64) :

```json
{
  "mcpServers": {
    "revit": {
      "command": "C:/Users/<votre_nom>/Documents/Devs/MCP Revit/revit-mcp-server/target/release/revit-mcp-server.exe",
      "args": [],
      "env": {
        "REVIT_HOST": "http://localhost:6543",
        "REVIT_TIMEOUT_MS": "120000"
      }
    },
    "cad-mcp": {
      "command": "python",
      "args": [
        "C:/Users/<votre_nom>/Documents/Devs/MCP Revit/cad-mcp/server.py"
      ]
    }
  }
}
```

### Configuration alternative (Serveur Node.js / TypeScript)

Si vous développez ou personnalisez les outils TypeScript :
```json
{
  "mcpServers": {
    "revit": {
      "command": "node",
      "args": [
        "C:/Users/<votre_nom>/Documents/Devs/MCP Revit/mcp-server/dist/index.js"
      ],
      "env": {
        "REVIT_HOST": "http://localhost:6543",
        "REVIT_TIMEOUT_MS": "120000"
      }
    }
  }
}
```

### Options d'environnement configurables
- `REVIT_HOST` : Adresse du pont HTTP local (défaut : `http://localhost:6543`).
- `REVIT_TIMEOUT_MS` : Délai maximal en millisecondes avant timeout pour les opérations Revit (défaut : `120000`, soit 2 minutes). Pour des maquettes volumineuses ou des audits complexes, cette valeur peut être passée à `300000` (5 minutes).
- `REVIT_ALLOW_REMOTE` : Par défaut à `0`. Par sécurité, seules les requêtes sur l'interface loopback locale (`localhost`, `127.0.0.1`) sont autorisées. Passer à `1` pour autoriser une connexion distante contrôlée.

---

## 2. Configuration Cursor IDE

Dans **Cursor** (Paramètres > Features > MCP Servers > Add New MCP Server) :
- **Name :** `revit`
- **Type :** `command`
- **Command :** `C:/Users/<votre_nom>/Documents/Devs/MCP Revit/revit-mcp-server/target/release/revit-mcp-server.exe`

---

## 3. Claude Code (CLI Terminal)

Si vous utilisez l'agent en ligne de commande Claude Code, ajoutez le serveur avec la commande suivante :

```powershell
claude mcp add revit -- "C:/Users/<votre_nom>/Documents/Devs/MCP Revit/revit-mcp-server/target/release/revit-mcp-server.exe"
```

Pour vérifier que le serveur est bien enregistré :
```powershell
claude mcp list
```

---

## 4. Exemples de prompts pour démarrer une session avec Claude

Une fois Revit ouvert avec votre maquette et Claude connecté :

> *"Bonjour Claude, vérifie la connexion avec Revit, prends une capture d'écran de la vue 3D avec `revit_screenshot_active_view`, puis lance un audit réglementaire PMR avec `revit_audit_pmr`."*

Claude utilisera alors :
1. `revit_health_check` pour s'assurer que le pont répond.
2. `revit_screenshot_active_view` pour analyser visuellement le bâtiment.
3. `revit_audit_pmr` pour surligner en vert et rouge les conformités et non-conformités d'accessibilité directement dans Revit.
