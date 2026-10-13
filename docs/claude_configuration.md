# GUIDE DE CONFIGURATION CLAUDE — REVIT AI BRIDGE

Ce document explique comment configurer vos environnements Claude (Claude Desktop et Claude Code CLI) pour interagir avec Autodesk Revit.

---

## 1. Claude Desktop (Interface Graphique)

Claude Desktop charge automatiquement les serveurs MCP déclarés dans son fichier de configuration standard sous Windows :
```
%APPDATA%\Claude\claude_desktop_config.json
```
(Chemin complet : `C:\Users\<votre_nom>\AppData\Roaming\Claude\claude_desktop_config.json`)

### Configuration installée automatiquement

Le script d'installation a configuré le serveur suivant :
```json
{
  "mcpServers": {
    "revit": {
      "command": "node",
      "args": [
        "C:/Users/teoba/Documents/Devs/MCP Revit/mcp-server/dist/index.js"
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
- `REVIT_TIMEOUT_MS` : Délai maximal en millisecondes avant timeout pour les opérations Revit (défaut : `120000`, soit 2 minutes). Pour des modèles massifs nécessitant des calculs longs, cette valeur peut être passée à `300000` (5 minutes).
- `REVIT_ALLOW_REMOTE` : Par sécurité, seules les requêtes sur l'interface loopback locale (`localhost`, `127.0.0.1`) sont autorisées.

---

## 2. Claude Code (CLI Terminal)

Si vous utilisez l'agent en ligne de commande Claude Code, ajoutez le serveur avec la commande suivante :

```powershell
claude mcp add revit -- node "C:/Users/teoba/Documents/Devs/MCP Revit/mcp-server/dist/index.js"
```

Pour vérifier que le serveur est bien enregistré :
```powershell
claude mcp list
```

---

## 3. Exemple de prompt pour démarrer une session avec Claude

Une fois Revit 2027 ouvert et Claude connecté :

> *"Bonjour Claude, vérifie la connexion avec Revit, donne-moi le nom et les informations du projet actif, et liste les niveaux du bâtiment."*

Claude utilisera alors :
1. `revit_health_check` pour s'assurer que le pont répond.
2. `revit_get_model_info` pour récupérer le nom du fichier, l'auteur, les phases et la vue active.
3. `revit_get_levels` pour extraire tous les niveaux et leurs élévations.
