# Guide pour Claude Desktop & Claude Code

Avant toute intervention dans ce projet :

1. **Lire impérativement [`PROJECT_STATE.md`](./PROJECT_STATE.md)** : C'est la référence absolue de l'architecture (Rust, C# Revit 2027, Named Pipes `\\.\pipe\revit_mcp_ipc`, 210 outils, "Git of BIM").
2. **Sauvegardes `.bak`** : Toujours créer un `.bak` avant de toucher à un fichier source ou de configuration.
3. **Mise à jour obligatoire** : À chaque modification, correction de bug ou ajout d'outil, mettre à jour **`PROJECT_STATE.md`** et **`CHANGELOG.md`**.
4. **Données architecturales** : Ne rien modifier ou effacer dans `C:\Users\teoba\Documents\Projet Appart Londonien`.
5. **Compilation & Déploiement** :
   - C# : `dotnet build revit-addin/RevitMCP.sln -c Release`
   - Rust : `cargo build --release --manifest-path revit-mcp-server/Cargo.toml`
   - Copie de l'add-in : `%APPDATA%\Autodesk\Revit\Addins\2027\`
   - Binaire Rust Claude : `C:\Users\teoba\Documents\Devs\MCP Revit\revit-mcp-server\target\release\revit-mcp-server.exe`
