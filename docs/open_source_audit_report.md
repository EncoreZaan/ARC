# RAPPORT D'AUDIT TECHNIQUE ET COMPARATIF DES PROJETS OPEN SOURCE

Ce document présente l'audit complet des solutions existantes pour relier Claude / MCP à Autodesk Revit, réalisé dans le cadre du projet **REVIT AI BRIDGE**.

---

## 1. Audit des Dépôts Analysés

### A. REA — Reverse Engineer Anything (`morluto/rea`)
- **Nature du projet** : Serveur MCP généraliste pour l'analyse statique et dynamique de binaires, applications mobiles (Android/APK), frameworks hybrides (Electron/ASAR) et traces mémoire (Frida, Ghidra).
- **Pertinence pour Revit** :
  - REA est un excellent outil d'investigation logicielle en cas de reverse-engineering binaire pur.
  - **Limites réelles pour Revit** : REA ne possède aucune connaissance intrinsèque de l'API Revit, du thread model COM/WPF ou du BIM. Les API Autodesk Revit sont documentées et officielles via les DLLs managées `RevitAPI.dll` et `RevitAPIUI.dll`. Utiliser REA pour piloter Revit serait inefficace et inapproprié.
- **Rôle retenu** : Conservé comme ressource d'investigation si une analyse de format propriétaire devenait nécessaire, mais non utilisé pour le runtime de pilotage BIM.

---

### B. NonicaTab PRO (`NonicaTeam/NonicaTab-PRO`)
- **Nature du dépôt** : Dépôt public ne contenant **aucun code source** (uniquement un fichier `README.md` promotionnel pointant vers l'Autodesk App Store et le site commercial nonica.io).
- **Licence & statut** : Logiciel propriétaire payant, sans code open source accessible.
- **Constat** : L'utilisateur a explicitement exprimé le besoin de remplacer sa dépendance à NonicaTab PRO en raison d'une utilisation pénible. Aucun élément de ce dépôt ne peut ou ne doit être réutilisé.

---

### C. AI Connector for Revit (`NonicaTeam/AI-Connector-for-Revit`)
- **Nature du dépôt** : Page de suivi d'issues et vitrine commerciale pour une fonctionnalité intégrée à NonicaTab.
- **Licence** : *"Copyright 2026 © All rights reserved Nonica"*.
- **Constat** : Aucun code source n'est publié. L'add-in dépend entièrement de l'installation propriétaire de NonicaTab. Ne répond pas aux exigences d'indépendance, de transparence et d'open source.

---

### D. mcp-servers-for-revit (`mcp-servers-for-revit/mcp-servers-for-revit`)
- **Auteurs originaux** : lisiting01 et contributeurs open source.
- **Licence** : MIT.
- **Architecture** : Client MCP TypeScript <-> WebSocket bidirectionnel <-> Plugin Revit C# avec interface WPF <-> CommandSet DLL par version.
- **Points forts** : Nombreuses commandes de base (grilles, pièces, dimensions, filtres AI).
- **Points faibles** :
  - Dépendance à une base SQLite locale pour mettre en cache des données, créant des risques de désynchronisation.
  - Architecture morcelée en sous-dossiers et multi-projets séparés par version compilée, difficile à maintenir et automatiser.
  - Gestion WebSocket plus lourde à sécuriser et surveiller que le protocole HTTP local avec `ExternalEvent`.

---

### E. Revit Claude MCP (`UV-Tech/revit-claude-mcp`)
- **Licence** : MIT.
- **Architecture** : Claude <-> Stdio MCP Server (@modelcontextprotocol/sdk) <-> Localhost HTTP JSON (:6543) <-> C# Addin .NET avec `ExternalEvent`.
- **Points forts** :
  - Respect scrupuleux de l'API Revit : toute action est marshalisée sur le thread principal via `ExternalEvent`.
  - Concurrence contrôlée par `SemaphoreSlim(1, 1)`.
  - Filtrage de sécurité strict : le serveur rejette toute IP non loopback et valide les chemins d'action par regex.
  - Couverture fonctionnelle très riche (40+ outils dès le départ : murs, instances, pièces, finitions, gabarits, révisions, nomenclatures, clash detection, exports PDF/DWG/IFC).
  - Implémentation propre et modulaire.
- **Limites initiales constatées** :
  - Ciblait en dur Revit 2026 (`C:\Program Files\Autodesk\Revit 2026`) et `net8.0-windows`.
  - Ne supportait pas encore Revit 2027 (.NET 10).
  - Manquait d'outils interactifs indispensables : lecture de la sélection courante utilisateur (`selection/get`), sélection dynamique, isolation temporaire, création de sol/dalle, création de pièces, diagnostic avancé.

---

## 2. Décision et Stratégie Retenue

Conformément à l'ordre de préférence :
1. **Socle technique retenu** : L'architecture de `UV-Tech/revit-claude-mcp` a été sélectionnée comme la base open source la plus saine, moderne et maintenable.
2. **Modernisation & Migration** :
   - Mise à niveau du projet C# vers **.NET 10 (`net10.0-windows`)** pour s'aligner exactement sur **Autodesk Revit 2027** installé sur la machine de l'utilisateur.
   - Référencement direct des DLLs réelles `C:\Program Files\Autodesk\Revit 2027\RevitAPI.dll` et `RevitAPIUI.dll`.
3. **Extension fonctionnelle majeure** :
   - Ajout des outils de sélection active (`revit_get_current_selection`, `revit_set_current_selection`).
   - Ajout du contrôle de visibilité temporaire (`revit_isolate_elements`, `revit_reset_temporary_override`).
   - Ajout de la création de dalles (`revit_create_floor`) et de pièces (`revit_create_room`).
   - Ajout de l'audit de catégories (`revit_list_categories`) et d'avertissements (`revit_get_model_warnings`).
   - Portant la suite à un total de **50 outils MCP testés et documentés**.
4. **Automatisation Windows** :
   - Pipeline de scripts complet : `build.ps1`, `install.ps1`, `diagnose.ps1`, `uninstall.ps1`.
   - Déploiement automatique dans le dossier d'add-ins Revit 2027 et mise à jour transparente de la configuration Claude Desktop.
