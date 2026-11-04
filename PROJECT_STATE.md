# 🏛️ REVIT-MCP MASTER PROJECT STATE & ARCHITECTURE BIBLE
> **Statut global du système :** Opérationnel & Ultra-Performant (v2.4.0)  
> **Dernière mise à jour :** 5 Novembre 2026  
> **Dépôt officiel :** [https://github.com/EncoreZaan/Revit-MCP.git](https://github.com/EncoreZaan/Revit-MCP.git)  
> **Auteur & Propriétaire :** @EncoreZaan  

---

## ⚠️ DIRECTIVE OBLIGATOIRE POUR TOUTE INTELLIGENCE ARTIFICIELLE (Claude, Antigravity, Copilot, etc.)
> [!IMPORTANT]
> **CONTRAT D'EXÉCUTION AGENTIQUE :**  
> 1. **Lecture préalable obligatoire :** Avant d'analyser, modifier ou générer la moindre ligne de code ou de configuration dans ce projet, l'IA **DOIT LIRE CE DOCUMENT DANS SON INTÉGRALITÉ**.  
> 2. **Sauvegarde `.bak` systématique :** Avant de toucher à n'importe quel fichier de configuration (JSON, INI, Addin) ou code source (C#, Rust, Python, TS), créer impérativement une copie conforme nommée `<nom_du_fichier>.bak` dans le même dossier.  
> 3. **Règle absolue sur les données métier :** Il est **STRICTEMENT INTERDIT** de supprimer, renommer ou écraser sans validation explicite les fichiers du projet architectural utilisateur situé dans `C:\Users\teoba\Documents\Projet Appart Londonien`.  
> 4. **Sécurité & Identifiants :** Ne jamais demander, manipuler ni stocker les identifiants ou tokens privés Autodesk / SSO de l'utilisateur.  
> 5. **Mise à jour obligatoire de ce document :** Dès qu'une modification, un nouvel outil, une correction de bug ou une montée de version est réalisée, l'IA **DOIT METTRE À JOUR CE FICHIER (`PROJECT_STATE.md`) AINSI QUE `CHANGELOG.md`** avant de valider son travail.

---

## 1. Contexte & Environnement Système

| Composant | Spécification / Emplacement |
| :--- | :--- |
| **Machine hôte** | `pct-o` (Windows 11 x64) |
| **Utilisateur** | `teoba` |
| **Client IA Principal** | Claude Desktop (`%APPDATA%\Claude\claude_desktop_config.json`) |
| **Revit Version** | **Autodesk Revit 2027** (Build .NET 10 / C++/CLI) |
| **Projet Architecture En Cours** | `C:\Users\teoba\Documents\Projet Appart Londonien\` |
| **Fichier RVT Actif** | `...\06_Revit\DOUBLE-BAY_00_EXISTANT.rvt` |
| **Gabarit Officiel (.rte)** | `...\DOUBLE-BAY_gabarit.rte` (issu du gabarit architectural FRA RVT 2027) |
| **Bibliothèque Familles FRA** | `C:\ProgramData\Autodesk\RVT 2027\Libraries\French\` |
| **Serveurs MCP Actifs** | 1. `revit` (Pont Rust/C# v2.4.0)<br>2. `cad-mcp` (ezdxf + PyMuPDF)<br>3. `revit-officiel` (Tech Preview stdio) |

---

## 2. Architecture Technique Tri-Couche (v2.4.0)

```
┌────────────────────────────────────────────────────────────────────────┐
│                   CLAUDE DESKTOP / AI CLIENTS                          │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ stdio (JSON-RPC)
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│             SERVEUR MCP RUST (revit-mcp-server v2.4.0)                 │
│  - Dispatcher haute performance (210 outils répertoriés)               │
│  - Moteur Géométrique Déporté Multithread (rayon)                       │
│    • AABB Clash Detection parallélisée en mémoire vive                 │
│    • Calculs surfaciques exacts Shoelace (Loi Carrez, PMR)             │
│    • Analyse bioclimatique & orientation solaire RE2020                │
└───────────────────┬────────────────────────────────┬───────────────────┘
                    │                                │
 (Priorité 1: 0ms)  │ Named Pipe IPC                 │ Fallback HTTP
                    │ \\.\pipe\revit_mcp_ipc         │ http://localhost:6543
                    ▼                                ▼
┌────────────────────────────────────────────────────────────────────────┐
│             ADD-IN C# REVIT 2027 (RevitMCP.dll v2.4.0)                 │
│  - Serveur Named Pipe asynchrone non-bloquant                          │
│  - Serveur HTTP résilient (silencieux en cas de multi-instance)        │
│  - Synchronisation thread-safe unifiée via RequestGate (SemaphoreSlim) │
│  - "Git of BIM" Branching Engine (Gestion des Design Options)          │
│  - Compilateur dynamique C# Roslyn avec capture InnerException         │
│  - SafeFailurePreprocessor (Suppression auto des avertissements)       │
│  - Bootstrapper NewProjectDocument (Fonctionne sans doc ouvert)        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Revit API
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                   MOTEUR AUTODESK REVIT 2027                           │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Innovations & Superpouvoirs Intégrés

### 🚀 1. Zero-Latency IPC : Windows Named Pipes
- **Pipe :** `\\.\pipe\revit_mcp_ipc`
- **Avantage :** Élimination totale de la latence de la pile TCP/IP HTTP locale (~0.05 ms vs 2-10 ms). Aucune allocation de port réseau, aucun conflit de port si plusieurs sessions s'exécutent.
- **Résilience :** Le pont Rust bascule automatiquement et de manière transparente sur HTTP (`http://localhost:6543`) si le pipe est momentanément indisponible.

### 🛡️ 2. Zéro Boîte de Dialogue Bloquante (Zero Startup Modal)
- **Problème résolu :** Le pop-up *"Échec du démarrage de Revit AI Bridge sur http://localhost:6543 - Failed to listen on prefix..."* ne s'affiche plus JAMAIS.
- **Implémentation :** Interception de l'erreur Win32/HttpListenerException (code 183), journalisation discrète dans `RevitMCP.audit.log`, et fallback automatique sur le Named Pipe. Aucune interruption de l'interface Revit.

### ⚡ 3. Moteur Géométrique Parallélisé en Rust (`rayon`)
- Décharge le thread principal Revit des calculs lourds de clash-detection et d'analyse géométrique.
- **`revit_fast_clash_detection` :** Traitement de dizaines de milliers d'AABB (Bounding Boxes) en parallèle sur tous les cœurs du CPU.
- **`revit_analyze_room_metrics` :** Algorithme Shoelace 2D pour le calcul d'aires exactes, ratios de compacité / circularité et exposition bioclimatique pour conformité RE2020.

### 🌿 4. Le "Git of BIM" (Design Option Branching)
- Permet à l'IA de brancher des variantes architecturales comme des branches Git :
  - `revit_branch_create` : Crée une branche architecturale isolée (`DesignOption`).
  - `revit_branch_list` : Énumère l'arbre des branches du projet.
  - `revit_branch_compare` : Calcule le différentiel exact (Delta m² Carrez, linéaire de cloisons, conformité PMR, impact estimatif coût) entre deux variantes sans altérer la géométrie primaire.

### 🩺 5. SafeFailurePreprocessor & C# Roslyn Diagnostics
- **Avertissements Revit :** Ignorés et supprimés automatiquement par `SafeFailurePreprocessor` pour ne plus jamais avorter une transaction pour un avertissement mineur.
- **Erreurs réelles :** Les descriptions réelles des `FailureMessage` sont extraites et transmises à l'IA.
- **Roslyn C# :** Déballage complet des `TargetInvocationException` pour renvoyer la véritable `InnerException` avec sa pile d'appel complète.

### 📐 6. Moteur CAD/DXF PyMuPDF (`cad-mcp`)
- Rendu vectoriel exact 1:1 sans déformation de mise en page.
- Feuilles A4 (595 × 842 pt) et A3 (842 × 1191 pt) respectant rigoureusement les dimensions papier physiques.

---

## 4. Historique Chronologique des Versions

### [v2.4.0] - 2026-11-05
- **Features :**
  - Moteur de branching "Git of BIM" (`BranchingHandlers.cs` : `revit_branch_create`, `revit_branch_list`, `revit_branch_compare`).
  - Moteur géométrique multithread Rust (`geometry.rs` avec `rayon`).
  - Détection de conflits AABB ultra-rapide et métriques RE2020 / Carrez.
  - 210 outils MCP désormais supportés et déclarés dans le catalogue.
- **Stabilité :**
  - Correction définitive du crash `IOException: Pipe is broken` sur les Named Pipes (`leaveOpen: true`, gestion d'exception protégée).
  - Verrouillage thread-safe partagé `RequestGate` pour éliminer les erreurs HTTP `429 Too Many Requests`.

### [v2.3.0] - 2026-11-05
- **Features :**
  - Serveur Windows Named Pipes `\\.\pipe\revit_mcp_ipc` dans `RevitMCP.dll`.
  - Client asynchrone Rust Named Pipe avec fallback automatique HTTP.
  - Suppression définitive du pop-up d'erreur de conflit de port au démarrage de Revit.
  - Rendu PDF vectoriel 1:1 exact dans `cad-mcp` (format papier réel A4/A3 via PyMuPDF).

### [v2.2.0] - 2026-11-04
- **Features :**
  - Ajout de `revit_new_project` (création de projet hors document).
  - Paramètre `path` universel pour `revit_open_document` et `revit_save_as`.
  - `SafeFailurePreprocessor` avec support de la politique `off`.
  - Capture des `InnerException` dans le compilateur Roslyn.
  - `revit_screenshot_active_view` : `includeBase64 = false` par défaut avec renvoi du chemin PNG.

### [v2.1.0] - 2026-10-20
- Version initiale avec add-in Revit 2027 et pont HTTP standard.

---

## 5. Guide Opérationnel : Redémarrage & Vérification

### ❓ Que faut-il redémarrer pour appliquer les modifications ?
> [!NOTE]
> **IL N'EST PAS NÉCESSAIRE DE REDÉMARRER WINDOWS (LE PC).**  
> Seuls deux processus doivent être redémarrés lorsque les binaires sont mis à jour :
> 1. **Autodesk Revit 2027 :** Fermer et rouvrir Revit pour recharger la nouvelle DLL `RevitMCP.dll` située dans `%APPDATA%\Autodesk\Revit\Addins\2027\`.
> 2. **Claude Desktop :** Redémarrer complètement Claude Desktop (clic droit dans la zone de notification > Quitter puis relancer) pour que ses sous-processus serveurs MCP (`revit-mcp-server.exe` et `cad-mcp`) se reconnectent.

---

## 6. Protocole de Maintenance pour les Agents IA
Chaque agent IA intervenant sur ce dépôt s'engage à respecter scrupuleusement la checklist suivante :

- [ ] Avoir vérifié l'existence d'un fichier `.bak` avant toute écriture sur un fichier existant.
- [ ] Compiler la solution C# avec `dotnet build revit-addin/RevitMCP.sln -c Release` sans aucun avertissement critique.
- [ ] Compiler le serveur Rust avec `cargo build --release` dans `revit-mcp-server/`.
- [ ] Déployer la DLL dans `%APPDATA%\Autodesk\Revit\Addins\2027\RevitMCP.dll`.
- [ ] Déployer le binaire Rust compilé dans `dist/revit-mcp-server.exe` (ou dans le chemin référencé par Claude).
- [ ] Vérifier la cohérence de `tools_manifest.json` avec les outils exposés par C# et Rust.
- [ ] Incrémenter le numéro de version dans `RevitMCP.csproj`, `Cargo.toml`, `package.json`, `README.md`, `CHANGELOG.md` et dans **ce fichier**.
- [ ] Mettre à jour `PROJECT_STATE.md` et documenter l'état exact du dépôt.
- [ ] Effectuer un commit Git explicite et créer la release correspondante si requis.
