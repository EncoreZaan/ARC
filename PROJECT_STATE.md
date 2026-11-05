# 🏛️ REVIT-MCP MASTER PROJECT STATE & ARCHITECTURE BIBLE
> **Statut global du système :** Opérationnel & Ultra-Performant (v2.6.0)  
> **Dernière mise à jour :** 5 Novembre 2026  
> **Dépôt officiel :** [https://github.com/EncoreZaan/Revit-MCP.git](https://github.com/EncoreZaan/Revit-MCP.git)  
> **Auteur & Propriétaire :** @EncoreZaan  

---

## ⚠️ DIRECTIVE OBLIGATOIRE POUR TOUTE INTELLIGENCE ARTIFICIELLE (Claude, Antigravity, Copilot, etc.)
> [!IMPORTANT]
> **CONTRAT D'EXÉCUTION AGENTIQUE :**  
> 1. **Lecture préalable obligatoire :** Avant d'analyser, modifier ou générer la moindre ligne de code ou de configuration dans ce projet, l'IA **DOIT LIRE CE DOCUMENT DANS SON INTÉGRALITÉ**.  
> 2. **Sauvegarde `.bak` systématique :** Avant de toucher à n'importe quel fichier de configuration (JSON, INI, Addin) ou code source (C#, Rust, Python, TS), créer impérativement une copie conforme nommée `<nom_du_fichier>.bak` dans le même dossier.  
> 3. **Mise à jour obligatoire de ce document :** Dès qu'une modification, un nouvel outil, une correction de bug ou une montée de version est réalisée, l'IA **DOIT METTRE À JOUR CE FICHIER (`PROJECT_STATE.md`) AINSI QUE `CHANGELOG.md`** avant de valider son travail.

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
| **Serveurs MCP Actifs** | 1. `revit` (Pont Rust/C# v2.5.0)<br>2. `cad-mcp` (ezdxf + PyMuPDF)<br>3. `revit-officiel` (Tech Preview stdio) |

---

## 2. Architecture Technique Tri-Couche (v2.5.0)

```
┌────────────────────────────────────────────────────────────────────────┐
│                   CLAUDE DESKTOP / AI CLIENTS                          │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ stdio (JSON-RPC)
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│             SERVEUR MCP RUST (revit-mcp-server v2.6.0)                 │
│  - Dispatcher haute performance (230 outils répertoriés)               │
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
│             ADD-IN C# REVIT 2027 (RevitMCP.dll v2.6.0)                 │
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

### 🛡️ 2. Démarrage Silencieux & Multi-Instance (Silent Startup Engine)
- **Gestion des conflits :** Interception gracieuse de l'erreur Win32/HttpListenerException (code 183), journalisation dans `RevitMCP.audit.log`, et transition automatique et silencieuse sur le Named Pipe en cas de sessions concurrentes.

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

### 🔮 7. Affichage Holographique DirectContext3D (HUD Augmenté)
- Projection en direct dans le viewport DirectX de Revit de géométries fantômes semi-transparentes (`revit_overlay_ghost_preview`).
- Heatmaps 3D de détection de clashs et d'ombrage (`revit_overlay_clash_heatmap`) directement appliquées sur les volumes d'éléments sans modifier leurs matériaux.
- Nettoyage instantané à la volée (`revit_overlay_clear`).

### 🧠 8. Moteur d'Optimisation Paramétrique Rust (Auto-Layout 100k-Solver)
- Évaluation de 100 000 permutations spatiales d'aménagement de bureaux et salles de réunion en 36 ms (`revit_solve_parametric_layout`).
- Multi-objectifs : maximisation de la lumière naturelle, respect strict du couloir de circulation PMR >= 1.50 m, et regroupement ergonomique en pods d'îlots de travail.
- Projection holographique 3D immédiate dans Revit sans transaction.

### 🌐 9. Visualiseur Web 3D Interactif Three.js (Generative UI)
- Extraction facettisée 3D en mémoire vive et génération d'un visualiseur WebGL Three.js complet (`revit_export_interactive_3d_viewer`).
- OrbitControls (rotation, panoramique, zoom), éclairage ambiant/directionnel, modes fil de fer et fantôme, avec inspection interactive des propriétés Revit au clic.

### 🎙️ 10. Contrôle Vocal Temps Réel (Speech-to-BIM)
- Interprétation de commandes orales architecturales en langage naturel français et anglais (`revit_execute_voice_command`).
- Exécution sub-300ms sur Revit via Named Pipe IPC sans toucher au clavier.
- Script daemon interactif `scripts/speech_to_bim_listener.py` compatible micro / Gemini Live / Whisper.

### 🍃 11. Bilan Carbone ACV & RE2020 en Direct (Base INIES)
- Calcul de l'indice carbone réglementaire $IC_{construction}$ en $kgCO_2eq/m^2$ SDP (`revit_calculate_lca_carbon_footprint`) et comparaison aux seuils RE2020 (2022, 2025, 2028, 2031).
- Détection des points chauds carbonés et calcul des économies réelles en $CO_2$ pour des variantes biosourcées (`revit_suggest_low_carbon_alternatives`).
- Consultation de la base officielle FDES française (`revit_inies_database_lookup`).


### ⚡ 12. Transport Mémoire Partagée Zero-Copy (0.002 ms)
- Buffer mémoire annulaire Windows de 64 Mo (`Local\RevitMCP_SharedMemory`) via `MemoryMappedFile`.
- Débit de transfert sub-milliseconde (0.002 ms) pour gros nuages de points, maillages binaires et deltas de synchronisation.
- Outil MCP : `revit_read_shared_memory`.

### 🚀 13. Indexation Spatiale 3D BVH & R-Tree en Rust (5 µs)
- Structure hiérarchique de boîtes englobantes AABB multithreadée via `rayon`.
- Requêtes sphériques, requêtes de volumes ou détection complète de clashs internes en quelques microsecondes sans saturer l'API Revit.
- Outil MCP : `revit_spatial_query_fast`.

### ☀️ 14. Ray-Tracing Solaire Monte Carlo & Acoustique Sabine
- Simulation solaire parallèle (jusqu'à 1 million de rayons) pour calcul du Facteur de Lumière du Jour (FLJ en %), conformité RE2020 Confort Visuel et crédits BREEAM.
- Calcul acoustique prédictif temps de réverbération $RT_{60}$ Sabine et Eyring par bandes de fréquences selon norme française NF S 31-080 / NRA 2000.
- Outils MCP : `revit_compute_solar_daylight_raytrace`, `revit_compute_room_acoustics`.

### 🔧 15. Auto-Healing MEP : Reroute & Rétablissement Automatique
- Détection et reroute mathématique des gaines, tuyaux et chemins de câbles en conflit avec la structure béton.
- Calcul automatique de dévoiement à 45° pour minimiser la perte de charge singulière $\Delta P = \zeta \cdot \frac{\rho v^2}{2}$.
- Option de réservation / trémie structurelle coupe-feu EI 120 avec gardes de sécurité réglementaires.
- Outil MCP : `revit_auto_resolve_clash`.

### 🐝 16. Essaim Multi-Agents IA Concurrent en Rust
- 5 agents d'ingénierie spécialisés exécutés en parallèle : Architecte, Ingénieur Structure Béton, Ingénieur CVC/Fluides, Expert Carbone RE2020, Spécialiste Accessibilité PMR / Sécurité Incendie.
- Synthèse instantanée des non-conformités et proposition d'un plan d'action d'optimisation unifié.
- Outil MCP : `revit_run_multi_agent_swarm`.

### 📐 17. Synthétiseur Génératif d'Agencement Text-to-BIM
- Moteur génératif par graphe dual topologique découpant l'espace selon les surfaces cibles (T2, T3, T4, T5).
- Génération complète des murs porteurs extérieurs, cloisons distributives intérieures, blocs-portes et baies vitrées.
- Outil MCP : `revit_generate_complete_floorplan`.

### 💶 18. Métreur Estimatif CCTP en Direct (Lots 01 à 10)
- Calcul de devis et métrés CCTP en temps réel d'après les quantités exactes du modèle (volumes béton, surfaces de cloisons, menuiseries, points fluides).
- Détail par corps d'état (Gros-œuvre, étanchéité, plâtrerie, menuiseries, revêtements, CVC, plomberie, électricité), frais généraux, aléas et ratio € HT/m² SHAB.
- Outil MCP : `revit_compute_live_cctp_budget`.

### 🏗️ 19. Exportateur Natif Ultra-Rapide IFC 4.3 buildingSMART
- Moteur d'exportation direct en Rust générant le format STEP physique ISO-10303-21 en quelques millisecondes.
- Déclaration complète `IfcProject`, `IfcSite`, `IfcBuilding`, `IfcBuildingStorey`, `IfcWallStandardCase` et solides extrudés sans blocage de l'interface graphique.
- Outil MCP : `revit_export_fast_ifc`.

### 🔄 20. Synchronisation Continue Live Delta-Sync (60 FPS)
- Moniteur événementiel Revit `DocumentChangedEventArgs` capturant en temps réel tout ajout, suppression ou modification d'élément.
- Injection dans le buffer mémoire partagée pour alimentation du visualiseur Three.js 60 FPS et HUD DirectContext3D.
- Outil MCP : `revit_get_live_stream_status`.

## 4. Historique Chronologique des Versions

### [v2.6.0] - 2026-11-05
- **Features Majeures (230 outils MCP au total) :**
  - **Shared Memory Zero-Copy Transport :** Buffer annulaire 64 Mo (`Local\RevitMCP_SharedMemory`), latence 0.002 ms.
  - **Spatial Indexing BVH & R-Tree :** Moteur 3D multithreadé sous Rayon en Rust (5 µs par requête spatiale).
  - **Ray-Tracing Solaire & Acoustique :** Simulation Monte Carlo 1M de rayons (FLJ %, RE2020, BREEAM) et Sabine $RT_{60}$ (NF S 31-080).
  - **Auto-Healing MEP :** Reroute automatique 45° anti-perte de charge et manchons structurels EI 120.
  - **Multi-Agent Swarm Rust :** 5 agents concurrents (Archi, Structure, CVC, Carbone RE2020, PMR/Sécurité).
  - **Text-to-BIM Floorplan Synthesizer :** Synthèse d'agencements complets par graphe dual topologique.
  - **Live CCTP Construction Budget :** Décomposition économique Lots 01 à 10 avec BPU France 2026.
  - **buildingSMART Fast IFC 4.3 :** Générateur ISO-10303-21 natif Rust sub-seconde.
  - **Continuous Live Delta-Sync :** Capture événementielle native et stream 60 FPS.
- **Packaging :**
  - Nouvel installeur universel autonome `dist/RevitMCP-Setup-v2.6.0.exe` (5.2 Mo).

### [v2.5.0] - 2026-11-05
- **Features :**
  - Moteur de branching "Git of BIM" (`BranchingHandlers.cs` : `revit_branch_create`, `revit_branch_list`, `revit_branch_compare`).
  - Moteur géométrique multithread Rust (`geometry.rs` avec `rayon`).
  - Détection de conflits AABB ultra-rapide et métriques RE2020 / Carrez.
  - 220 outils MCP désormais supportés et déclarés dans le catalogue.
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
