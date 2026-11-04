# 🤖 INSTRUCTIONS CRITIQUES POUR LES AGENTS IA

Bienvenue dans le dépôt **Revit-MCP**.

Avant de réaliser la moindre action, vous devez vous conformer aux instructions suivantes. Ce dépôt est un projet de haute précision interfaçant des modèles IA, des serveurs MCP haute performance (Rust & Python), et l'API native d'Autodesk Revit 2027.

---

## 📜 1. La Règle d'Or : `PROJECT_STATE.md`
Le fichier [`PROJECT_STATE.md`](./PROJECT_STATE.md) est la **Bible d'Architecture et d'État** de ce projet.
- **AVANT toute modification :** Vous **DEVEZ** lire [`PROJECT_STATE.md`](./PROJECT_STATE.md) pour comprendre l'architecture tri-couche (Rust $\leftrightarrow$ Named Pipe IPC $\leftrightarrow$ C# Addin), la liste des 210 outils, l'historique et les contraintes.
- **APRÈS toute modification :** Vous **DEVEZ METTRE À JOUR** [`PROJECT_STATE.md`](./PROJECT_STATE.md) ainsi que [`CHANGELOG.md`](./CHANGELOG.md) avec les détails de vos changements, le nouveau statut, et la version si applicable.

---

## 🔒 2. Règles de Sécurité & Intégrité
1. **Sauvegarde `.bak` obligatoire :** Créez systématiquement un fichier `<nom>.bak` à côté de tout fichier de code ou de configuration avant de le modifier.
2. **Sanctuaire Projet Utilisateur :** Ne supprimez, ne renommez et n'altérez **JAMAIS** les fichiers situés dans `C:\Users\teoba\Documents\Projet Appart Londonien` sans ordre explicite de l'utilisateur.
3. **Identifiants & Secrets :** Ne sollicitez ni ne stockez aucun mot de passe ou jeton Autodesk SSO privé.
4. **Préservation des serveurs existants :** Veillez à ne jamais casser les configurations des trois serveurs MCP actifs (`revit`, `cad-mcp`, `revit-officiel`).

---

## 🛠️ 3. Architecture & Piles Techniques
- **C# / .NET 10 (Revit 2027 Addin) :** Situé dans `revit-addin/`. Add-in déployé dans `%APPDATA%\Autodesk\Revit\Addins\2027\`.
- **Rust (revit-mcp-server) :** Situé dans `revit-mcp-server/`. Gère le transport stdio pour Claude Desktop, l'IPC haute performance via Named Pipe (`\\.\pipe\revit_mcp_ipc`), et le moteur géométrique multithreadé (`rayon`).
- **Python (cad-mcp) :** Microservice basé sur `ezdxf` et `PyMuPDF` pour les rendus vectoriels 1:1 à échelle exacte.
- **TypeScript / Node.js (mcp-server) :** Serveur pont alternatif.

Pour plus de détails techniques, consultez immédiatement [`PROJECT_STATE.md`](./PROJECT_STATE.md).
