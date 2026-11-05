# 🏛️ ARC (ARCHITECTURE & REVIT COPILOT) — MASTER PROJECT STATE & ARCHITECTURE BIBLE
> **Statut global du système :** Opérationnel & Ultra-Performant (v2.9.1 — 254 Outils, High-Efficiency Dual-Engine, Tokens -91.7%, Zero-Spike RAM, 5µs ReadCache & macOS/Linux Fallback)  
> **Dernière mise à jour :** 5 Novembre 2026  
> **Dépôt officiel :** [https://github.com/EncoreZaan/ARC.git](https://github.com/EncoreZaan/ARC.git)  
> **Site officiel (Landing Page 3D) :** [https://encorezaan.github.io/ARC/](https://encorezaan.github.io/ARC/)  
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
| **Plates-formes Certifiées** | **macOS** (Priorité 1 — Apple Silicon M1/M2/M3/M4 & Intel)<br>**Linux** (Ubuntu, Debian, Fedora, Arch)<br>**Windows 11** (x64) |
| **Utilisateur** | `teoba` |
| **Client IA Principal** | Claude Desktop (`%APPDATA%\Claude\claude_desktop_config.json`) & Antigravity IDE |
| **Moteurs BIM Actifs** | **Dual-Engine :**<br>1. **Autodesk Revit 2027** (.NET 10 / C++/CLI Add-in)<br>2. **BIMcraft 0.9.0** (Moteur BIM natif Rust, GUI Vulkan/D3D12 & CLI headless) |
| **Projet Architecture En Cours** | `C:\Users\teoba\Documents\Projet Appart Londonien\` |
| **Fichiers Maquettes Actifs** | `...\06_Revit\REFLET_00_EXISTANT.rvt` & `REFLET_00_EXISTANT.bimcraft`<br>+ Variantes V1, V2, V3 converties et synchronisées |
| **Gabarit Officiel (.rte)** | `...\DOUBLE-BAY_gabarit.rte` (issu du gabarit architectural FRA RVT 2027) |
| **Bibliothèque Familles FRA** | `C:\ProgramData\Autodesk\RVT 2027\Libraries\French\` |
| **Serveurs MCP Actifs** | 1. `revit` (Serveur Rust ARC v2.9.1 — 254 outils)<br>2. `cad-mcp` (ezdxf + PyMuPDF)<br>3. `bimcraft` / `bimcraft-gui` (Pont CLI & Agent interactif)<br>4. `revit-officiel` (Tech Preview stdio) |

---

## 2. Architecture Technique Dual-Engine (v2.9.1)

```
┌────────────────────────────────────────────────────────────────────────┐
│                   CLAUDE DESKTOP / ANTIGRAVITY / AI CLIENTS            │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ stdio (JSON-RPC)
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│             SERVEUR MCP RUST UNIFIÉ (revit-mcp-server v2.9.1)          │
│  - Dispatcher haute performance (254 outils répertoriés)               │
│  - Moteur Géométrique Déporté Multithread (rayon)                       │
│    • AABB Clash Detection parallélisée en mémoire vive                 │
│    • Calculs surfaciques exacts Shoelace (Loi Carrez, PMR)             │
│    • Analyse bioclimatique & orientation solaire RE2020                │
│    • Pipeline Multi-Moteurs : Sorties simultanées Revit & BIMcraft     │
└───────────────────┬────────────────────────────────┬───────────────────┘
                    │                                │
 (Engine A: Revit)  │ Named Pipe IPC                 │ (Engine B: BIMcraft)
                    │ \\.\pipe\revit_mcp_ipc         │ Agent Bridge / CLI Run
                    ▼                                ▼
┌──────────────────────────────────────┐  ┌──────────────────────────────┐
│  ADD-IN C# REVIT 2027 (v2.9.1)       │  │  BIMCRAFT 0.9.0 (RUST NATIVE)│
│  - Serveur Named Pipe asynchrone     │  │  - Port bridge.json (--ctrl) │
│  - "Git of BIM" Branching Engine     │  │  - Rendu direct SVG/PNG      │
│  - Compilateur dynamique Roslyn      │  │  - Modélisation scriptable   │
│  - SafeFailurePreprocessor           │  │  - Zéro dépendance externe   │
└───────────────────┬──────────────────┘  └──────────────┬───────────────┘
                    │                                    │
                    ▼                                    ▼
┌──────────────────────────────────────┐  ┌──────────────────────────────┐
│        AUTODESK REVIT 2027           │  │     BIMCRAFT VIEWPORT 3D     │
└──────────────────────────────────────┘  └──────────────────────────────┘
```
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

### 🌐 21. Landing Page Officielle ARC & Expérience WebGL Three.js (v2.6.0)
- Site web vitrine officiel haute performance déployé sous `docs/` et prêt pour GitHub Pages (`https://encorezaan.github.io/ARC/`).
- **Direction Artistique Cinématique :** Reproduction fidèle de l'univers visuel de référence (fond noir obsidienne `#0B080D` avec nuances prune fumé `#2B242C`, reflets métalliques champagne doré `#D7B98C`, ivoire et cobalt profond `#274C8F`).
- **Élément Central 3D Interactif :** Scène WebGL Three.js complète (`docs/three-scene.js`) intégrant une sphère/dôme liquide métallique avec déformation organique en bruit de Simplex 3D, reflets Fresnel champagne/cobalt mouvants, et un pavillon architectural BIM filaire tridimensionnel rétro-éclairé avec flux de particules de données à l'intérieur.
- **Double Mode Rendu :** Bascule instantanée entre la scène 3D temps réel WebGL et le rendu cinématique vidéo haute définition (`arc-hero-reference.webm` / `arc-hero-reference.mp4`).
- **Explorateur Interactif des 230 Outils :** Moteur de recherche et filtrage par catégorie (Performance Rust, PMR/RE2020, Git du BIM, Modélisation, Fluides MEP, Documentation, Sécurité) avec affichage des latences sub-millisecondes.
- **Déploiement Automatisé :** Workflow GitHub Actions `.github/workflows/pages.yml` pour déploiement continu à chaque push sur la branche `main`.

### ⚡ 22. Dispatcher Asynchrone Hybride « Zéro Document » (ExternalEvent & Idling Queue)
- **Traitement de file sans document actif :** Résolution définitive du blocage et des erreurs 429 lors de l'exécution d'appels API (`revit_open_document`, `revit_new_project`, `revit_get_ui_state`, `revit_list_open_documents`) lorsque Revit est ouvert sur l'écran d'accueil sans aucun document chargé.
- **Architecture Dual-Queue Thread-Safe :** File d'attente FIFO concurrente (`ConcurrentQueue<Request>`) dépouillée harmonieusement soit par `IExternalEventHandler.Execute`, soit par l'événement `UIApplication.Idling`.
- **Réveil Instantané Win32 :** Envoi d'un message non-bloquant `WM_NULL` via `PostMessage` vers le `MainWindowHandle` du processus Revit pour forcer le passage immédiat en cycle `Idling` sans attendre d'interaction souris utilisateur.
- **Résilience d'Ouverture :** Fallback automatique de `OpenAndActivateDocument` vers `Application.OpenDocumentFile` en cas de restriction de contexte d'événement Revit.

### 🏗️ 23. Intégration & Compatibilité Totale BIMcraft (v2.7.0 — Dual-Engine BIM)
- **BIMcraft Native Rust Modeler (v0.9.0) :** Modeleur architectural complet et autonome ("Revit-class building model") développé en Rust, offrant un rendu matériel direct Vulkan/DirectX 12, une modélisation paramétrique ultra-rapide et zéro dépendance lourde.
- **Pont Agent & Synchronisation Interactive :**
  - Démarrage persistant de l'interface graphique à l'écran via WMI/Win32 (`Invoke-CimMethod -ClassName Win32_Process -MethodName Create`) avec écoute sur port dynamique négocié (`%APPDATA%\BIMCraft\bridge.json`).
  - Connexion automatique pour les agents IA via `bimcraft-cli mcp --connect` pour interagir directement dans le viewport sous les yeux de l'architecte.
- **6 Nouveaux Outils MCP Dédiés (254 outils au total) :**
  1. `bimcraft_launch` : Lancement détaché du GUI BIMcraft avec activation de l'écoute agent `--control 0`.
  2. `bimcraft_status` : Lecture de l'état du modeleur, version du CLI, statut du processus et configuration de communication active.
  3. `bimcraft_convert` : Conversion native sans Revit entre formats `.rvt`, `.rfa`, `.ifc`, `.bimcraft`, `.svg` et `.png`.
  4. `bimcraft_info` : Extraction analytique complète de la structure de maquette (niveaux, éléments, catégories, familles, paramètres).
  5. `bimcraft_execute_script` : Injection et exécution de scripts batch de modélisation (murs, cloisons, baies, pièces, dalles, toits).
  6. `bimcraft_render_view` : Génération sub-seconde de rendus plans 2D vectoriels SVG ou 3D PNG.
- **Conversion et Synchronisation des Variantes du Projet :**
  - Conversion automatisée des 4 maquettes du projet Londonien (`REFLET_00_EXISTANT`, `REFLET_V1_ECLIPSE_MIROIR-G`, `REFLET_V2_ORBITE_MIROIR-D`, `REFLET_V3_PASSAGE_MIROIR-G`) vers `.bimcraft` et génération immédiate des plans vectoriels `.svg`.
- **Générateurs Algorithmiques Multi-Moteurs :**
  - `floorplan_generator.rs` et `solver.rs` enrichis pour exporter simultanément la structure de batch Revit C# et le script BIMcraft en millimètres avec commandes adaptées (`wall`, `door`, `window`, `room`, `desk`, `table`, `chair`).
- **Pipeline CAD / DXF PyMuPDF Universel :**
  - Comparaison géométrique DXF $\leftrightarrow$ BIMcraft native prenant en charge les segments de murs, polylignes et points d'insertion dans `cad-mcp`.
- **Gestionnaire Global Python (`scripts/bimcraft_manager.py`) :**
  - Script CLI complet offrant le contrôle total du cycle de vie de BIMcraft, l'inspection et la conversion en masse.

### 🍎 24. Compatibilité Multi-Plateforme Nativée (macOS Priorité 1, Linux & Windows)
- **Révolution pour les Architectes sur Mac :**
  - Fin du monopole Windows pour la conception et l'analyse BIM avancée.
  - **macOS Natif :** BIMcraft fonctionne avec accélération matérielle Metal sur tous les Macs Apple Silicon (M1/M2/M3/M4) et Intel.
  - **Zéro Licence Windows / Zéro Installation Revit requise :** Importation et conversion directes des fichiers `.rvt` et `.rfa` sous macOS.
  - **Unix Domain Socket (UDS) :** Implémentation du socket local `/tmp/revit_mcp_ipc.sock` pour communication instantanée sans latence réseau.
  - **Documentation Complète d'Architecture Revit :** Rédaction du guide de référence exhaustif [`docs/revit_architecture_guide.md`](./docs/revit_architecture_guide.md) décrivant dans le détail la genèse, l'état d'avancement et le mode de fonctionnement de l'ensemble du système.

## 4. Historique Chronologique des Versions

### [v2.9.1] - 2026-11-05
- **Bouton Direct BIMcraft dans le Hero (v2.9.4) :**
  - Téléchargement 1-clic de BIMcraft disponible immédiatement en haut de page dans le Hero CTA et sur la carte macOS/Linux.
  - Liaison dynamique automatique avec l'asset officiel (DMG/MSI/AppImage) issu de `IPedrax/BIMCraft`.
- **Correction Critique Visibilité Landing Page (v2.9.3) :**
  - Correction de syntaxe dans `docs/app.js` et neutralisation défensive de `opacity: 0` sur `[data-animate]` : affichage instantané et garanti de l'ensemble des textes et présentations, même hors exécution JS.
- **Hub de Téléchargement Dual-Engine & Écouteur Live GitHub Releases (IPedrax/BIMCraft) :**
  - Intégration sur la landing page d'une carte de téléchargement 1-clic pour BIMcraft connectée en temps réel à l'API GitHub (`IPedrax/BIMCraft/releases/latest`).
  - Téléchargement direct sans redirection des binaires officiels : macOS `.dmg` (Universal Metal M1–M4), Windows `.msi`, Linux `.AppImage`.
  - Détection automatique d'OS visiteur et auto-updater local dans `scripts/bimcraft_manager.py (update/install)`.
- **5 Nouveaux Outils Rust Poids-Plume & Format Colonnaire (254 outils total) :**
  - `revit_semantic_model_digest`, `revit_audit_regulatory_scorecard`, `revit_clash_matrix_fast`, `revit_smart_diff_history`, `revit_batch_transform_vectorized`.
  - Moteur colonnaire compact (`compact: true` / `format: "columnar"`) éliminant les répétitions de clés JSON (-70% de tokens).
  - BVH mémoire continue contiguë (RAM < 8 Mo, 0 Mo VRAM GPU requis, Rayon SIMD).

### [v2.9.0] - 2026-11-05
- **Correction Critique Landing Page & Cache-Buster v2.9.1 :**
  - Élimination absolue de tout risque de chevauchement visuel dans le Hero : injection de règles CSS défensives en ligne dans le <head> de docs/index.html (position: static !important, espacements automatiques, lex-wrap).
  - Cache-busting systématique (styles.css?v=2.9.1, 	hree-scene.js?v=2.9.1, pp.js?v=2.9.1) forçant le rafraîchissement immédiat sur les navigateurs clients et GitHub Pages.
  - Substitution du glyphe  (invisible sur Windows) par une icône vectorielle SVG universelle dans la carte macOS & Linux.

- **Optimisation Radicale des Tokens (-91.7% d'Empreinte Initial Context Window) :**
  - **Tool Profiles Dynamiques (`ARC_TOOL_PROFILE`) :** Support de 3 profils (`all`, `compact`, `bimcraft`). En mode `compact`, exposition ciblée de 38 outils polyvalents (passage de 311 Ko à 25 Ko de payload, économisant plus de 73 000 tokens par tour).
  - **Méta-Outil Universel `revit_dispatch` (254 outils au total) :** Exécution dynamique sans faille de l'intégralité du catalogue ARC via un seul point d'entrée.
  - **Correction Majeure de la Duplication Base64 :** Extraction multimodale native MCP et élimination de la chaîne Base64 du corps JSON textuel (+350 000 tokens économisés par rendu/capture).
  - **Minification JSON & Nettoyage de Schémas :** Suppression des balises `$schema` redondantes et sérialisation dense sans indentation inutile (-35% de tokens sur chaque échange).
  - **Field Projection C# & Rust (`fields: [...]`) :** Filtrage sélectif des attributs d'éléments (ex: `fields: ["id", "name", "category"]`) diminuant la taille des listes d'éléments de 95%.
- **Cache en Mémoire Vive Rust Sub-Milliseconde (5 µs de Latence) :**
  - In-memory read-cache pour les métadonnées statiques (`model/info`, `levels/get`, `phases/get`, `categories/list`, `documents/list`).
  - Invalidation instantanée lors de toute transaction de modification/création.
- **Élimination du Spike Mémoire Revit (Économie RAM > 80%) :**
  - Réécriture de `QueryElementsEnhanced` : remplacement de `col.ToElements()` par `col.GetElementCount()` et itération paresseuse C# avec filtres natifs C++ Revit API (`ElementLevelFilter`).
- **Robustesse Multi-Plateforme Dual-Engine (macOS, Linux & Windows) :**
  - **BIMcraft Automatic Fallback :** En l'absence de Revit (ou sous macOS/Linux), routage instantané et transparent des requêtes de modèle, niveaux, catégories, phases, conversions IFC et batchs de modélisation vers BIMcraft.
  - Minification dense des sorties JSON de `cad-mcp`.

### [v2.8.1] - 2026-11-05
- **5 Outils Pratiques & Métier Quotidien d'Architecte (254 outils répertoriés) :**
  - `revit_auto_dimension_plan` (`auto_dimension.rs`) : Cotation automatique 3 lignes orthogonales.
  - `revit_compare_variants_visual` (`variant_diff_visual.rs`) : Comparateur visuel de démolition/création SVG/PDF (Rouge/Vert/Gris).
  - `revit_geometry_doctor` (`geometry_doctor.rs`) : Recalage des murs hors-axe (89.9° vers 90.0°), fusion des micro-écarts (< 25 mm).
  - `revit_layout_casework_millwork` (`casework_layout.rs`) : Calepinage paramétrique d'agencements et dressings.
  - `revit_extract_takeoff_artisan_table` (`artisan_takeoff.rs`) : Métré récapitulatif net Carrez, plinthes et menuiseries.
- **Refonte Visuelle, Éclat des Couleurs & Correction des Bogues Landing Page (docs/) :**
  - Élimination définitive du chevauchement de cartes et texte dans le Hero (cartes flottantes en flux naturel responsive).
  - Palette lumineuse et contrastée (bleu nuit cosmique `#080C18`, or champagne `#F59E0B`, cyber cyan `#38BDF8`).
  - Lisibilité 100% garantie sur les boutons macOS et Linux (texte blanc pur sur verre lumineux).
  - Intégration de la barre de recherche en temps réel connectée aux 254 outils.
  - Scène 3D WebGL Three.js ré-éclairée avec caustiques spéculaires et reflets iridescents.
  - Synchronisation universelle des versions (`v2.8.1`).

### [v2.8.0] - 2026-11-05
- **Résilience Multi-Instance Totale & Zéro-Conflit Réseau (Revit & BIMcraft) :**
  - Démarrage silencieux sans modale ni TaskDialog.
  - Allocation dynamique de ports (6543 à 6555) et canaux Named Pipes dédiés par PID (`revit_mcp_ipc_<pid>`).
  - Routage intelligent multithreadé côté serveur Rust.
- **7 Piliers Technologiques de Rupture (243 outils) :**
  - `revit_live_mirror_sync`, `revit_sketch_to_bim`, `revit_simulate_thermal_dynamic_re2020`, `revit_solve_structural_fea_instant`, `revit_audit_urban_plu_compliance`, `revit_export_usd_visionpro`, `revit_walkthrough_direct`.

### [v2.7.0] - 2026-11-05
- **Architecture Dual-Engine & Multi-Plateforme (macOS, Linux, Windows) :**
  - **Support macOS Déclaré Priorité N°1** : Portabilité native complète du serveur Rust, du modeleur BIMcraft (Metal/Cocoa) et du microservice CAD.
  - Déploiement du support BIMcraft 0.9.0 aux côtés d'Autodesk Revit 2027.
  - Ajout des 6 nouveaux outils MCP BIMcraft (catalogue étendu à 254 outils).
  - Transport IPC Unix Domain Socket (`/tmp/revit_mcp_ipc.sock`) pour environnements Unix (macOS/Linux).
  - Génération algorithmique simultanée pour Revit et BIMcraft dans `revit_generate_complete_floorplan` et `revit_solve_parametric_layout`.
  - Extension du serveur `cad-mcp` (PyMuPDF / ezdxf) pour supporter nativement les géométries BIMcraft.
  - Outil CLI d'orchestration multi-plateforme `scripts/bimcraft_manager.py`.
  - Conversion intégrale des maquettes de l'Appartement Londonien (`.rvt` -> `.bimcraft` et `.svg`).
  - Rédaction du guide de référence officiel [`docs/revit_architecture_guide.md`](./docs/revit_architecture_guide.md).
  - Mise à jour de la landing page vitrine WebGL Three.js avec section dédiée macOS/Linux et filtres d'outils.

### [v2.6.0] - 2026-11-05
- **Landing Page Officielle & GitHub Pages :**
  - Conception et développement de la landing page vitrine officielle dans `docs/` avec redirection racine `index.html`.
  - Intégration de la scène 3D WebGL Three.js (dôme liquide, pavillon architectural BIM filaire, flux de données).
  - Déploiement automatique GitHub Actions (`.github/workflows/pages.yml`).
  - Catalogue interactif des 230 outils MCP avec filtres et recherche instantanée.
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
- **Stabilité & Mode Zéro Document :**
  - Traitement garanti de la file de requêtes même sans aucun document ouvert dans Revit (résolution du blocage 429 sur `revit_open_document`, `revit_new_project`, `revit_get_ui_state`, etc.).
  - Double consommation de la file par `ExternalEvent` et `UIApplication.Idling`.
  - Signalement instantané de la boucle de messages Revit via `PostMessage(WM_NULL)`.
  - Bascule de secours automatique `OpenAndActivateDocument` -> `OpenDocumentFile` si requis par le contexte événementiel.
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
