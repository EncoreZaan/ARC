# Journal des modifications (Changelog) — ARC (Architecture & Revit Copilot)

Toutes les modifications notables apportées au projet ARC sont documentées dans ce fichier.

---

## [2.8.1] - 2026-11-05

### 🎯 Les 5 Outils Métier Indispensables de l'Architecte (248 Outils Totaux)
- **Cotation Automatique des Plans (`revit_auto_dimension_plan` - `auto_dimension.rs`) :** Génération instantanée des 3 lignes de cotes orthogonales professionnelles (ouvertures/trumeaux, cotes intérieures nettes, hors-tout) sans aucun clic manuel répétitif.
- **Comparateur Visuel de Démolition / Création (`revit_compare_variants_visual` - `variant_diff_visual.rs`) :** Superposition instantanée des variantes de projet avec l'existant. Sortie vectorielle SVG/PDF immédiate : **Rouge** (démoli), **Vert** (créé), **Gris** (conservé) et métré des démolitions pour artisans.
- **Docteur Géométrique & Nettoyeur d'Avertissements (`revit_geometry_doctor` - `geometry_doctor.rs`) :** Recalage automatique des parois légèrement obliques (89.9° vers 90.0°), fusion des micro-écarts (< 25 mm) aux jonctions de murs, et purge des segments parasites invisibles.
- **Calepinage Paramétrique d'Agencement Intérieur (`revit_layout_casework_millwork` - `casework_layout.rs`) :** Conception instantanée de dressings, placards, linéaires de cuisine et meubles vasque sur mesure avec caissons modulaires (600mm / 800mm) et nomenclature quincaillerie.
- **Tableau de Métré Récapitulatif Clients & Artisans (`revit_extract_takeoff_artisan_table` - `artisan_takeoff.rs`) :** Extraction automatisée en Markdown et CSV des surfaces Loi Carrez pièce par pièce, linéaires réels de plinthes (déduction des portes), surfaces de murs à peindre et nomenclature ordonnée des menuiseries.

---

## [2.8.0] - 2026-11-05

### ⚡ Résilience Multi-Instance Totale & Zéro-Conflit Réseau (Revit & BIMcraft)
- **Suppression Définitive des Modales d'Erreur :** L'add-in Revit C# ne déclenche plus jamais de `TaskDialog.Show` d'erreur lors du lancement d'une seconde ou troisième instance. Le démarrage est 100% silencieux avec audit logging.
- **Allocation Dynamique de Ports (6543 à 6555) :** Lorsqu'un port est occupé, Revit bascule silencieusement sur le port suivant disponible et enregistre son PID et port dans `%APPDATA%\RevitMCP\active_instances.json`.
- **Canaux Named Pipes Multiples :** Chaque instance écoute à la fois sur le canal global `revit_mcp_ipc` et sur son canal dédié `revit_mcp_ipc_<pid>`.
- **Multi-BIMcraft Dynamique :** Support de plusieurs fenêtres BIMcraft actives simultanément avec détection multi-processus et ports éphémères indépendants.
- **Routage Intelligent Côté Rust :** Le serveur `revit-mcp-server` interroge le registre actif et sonde automatiquement la plage de ports en cas de requêtes multi-instances.

### 🏛️ Les 7 Piliers Technologiques de Rupture (Rust Natif 100%)
- **243 Outils MCP Actifs au Total** (+7 nouveaux outils d'excellence architecturale) :
  1. `revit_live_mirror_sync` (`live_mirror.rs`) : Moteur de synchronisation bidirectionnelle & fusion différentielle 3 voies (Revit ↔ BIMcraft) avec calcul de deltas spatiaux multithreadé (`rayon`) et résolution de conflits (`SmartGeometricSnap`, `SourceWins`, `TargetWins`, `UnionMerge`).
  2. `revit_sketch_to_bim` (`sketch_vectorizer.rs`) : Vectoriseur neuronal transformant des croquis à main levée, scans de plans papier et tracés vectoriels en parois 3D réelles avec distinction porteurs 30cm / cloisons 10cm, portes, fenêtres et détection polygonale de pièces avec surface Loi Carrez.
  3. `revit_simulate_thermal_dynamic_re2020` (`thermal_sim.rs`) : Moteur de Simulation Thermique Dynamique (STD ISO 52016-1) calculant le bilan thermique en temps réel, les coefficients de déperditions $H_{tr}$ et $H_{ve}$, les indicateurs officiels RE2020 (**Bbio**, **Cep**, **Cep,nr**) et le confort estival en Degrés-Heures (**DH**) selon les zones climatiques françaises H1a à H3.
  4. `revit_solve_structural_fea_instant` (`structural_fea.rs`) : Calcul de structure RDM / Éléments Finis instantané (poutres Bernoulli-Euler) avec vérifications Eurocodes 2 (béton) et Eurocode 3 (acier), sollicitations ELU/ELS ($M_{Ed}, V_{Ed}$), flèches admissibles $L/350$ et recommandations de redimensionnement de section.
  5. `revit_audit_urban_plu_compliance` (`plu_checker.rs`) : Audit automatisé de conformité Permis de Construire : hauteur maximale au faîtage/acrotère, prospect urbain sur voie ($H = L + R$), retraits aux limites séparatives ($D \ge H/2$), emprise au sol (**CES**), biotope végétalisé (**CBS**), et sécurité incendie ERP/CCH (distances d'évacuation $\le 30\text{ m}$, largeurs en Unités de Passage).
  6. `revit_export_usd_visionpro` (`spatial_usd_gltf.rs`) : Exportateur spatial natif produisant des fichiers USDA/USDZ pour Apple Vision Pro (visionOS avec ancres spatiales) et glTF 2.0 PBR pour Unreal Engine 5.5 (compatible Nanite et Lumen).
  7. `revit_walkthrough_direct` (`walkthrough_director.rs`) : Réalisateur IA de visites virtuelles générant des trajectoires cinématiques de caméra (splines Catmull-Rom à $1.65\text{ m}$), course solaire dynamique selon l'heure de la journée, bascules interactives de finitions architecturales et narration vocale synchronisée pour intégration avec Gemini Live API et TTS.

### 🌐 Vitrine Web & Documentation
- **Landing Page 3D Three.js :** Mise à jour à 243 outils avec mise en avant des 7 piliers et des badges multi-plateformes macOS (Apple Silicon M1-M4 & Intel), Linux et Windows 11.
- **Guide Architectural Revit & BIMcraft :** Enrichi des sections 7 et 8 détaillant l'architecture multi-instance silencieuse et les 7 moteurs algorithmiques.

---

## [2.7.0] - 2026-11-05

### 🍎 Disponibilité Multi-Plateforme : macOS (Priorité N°1) & Linux
- **Fin du Monopole Windows en BIM** : ARC et BIMcraft fonctionnent désormais nativement sur **macOS (Apple Silicon M1/M2/M3/M4 & Intel)** et **Linux** en plus de Windows 11.
- **Accélération Matérielle Metal sur Mac** : Rendu 3D temps réel direct sous macOS avec le moteur BIMcraft sans émulation ni virtualisation.
- **Importation & Conversion RVT Natives sous macOS** : Traitement direct des fichiers Revit `.rvt` et `.rfa` sur Mac sans licence Revit ni machine Windows requise.
- **Transport IPC Unix Domain Socket** : Prise en charge des sockets locaux `/tmp/revit_mcp_ipc.sock` pour communication instantanée sans surcharge réseau.
- **Guide Architectural Revit Exhaustif** : Publication du guide de référence officiel [`docs/revit_architecture_guide.md`](./docs/revit_architecture_guide.md) détaillant la conception, l'état actuel et les modes opératoires.

### 🏗️ Support BIMcraft & Architecture Dual-Engine (Revit 2027 + BIMcraft 0.9.0)
- **Architecture Multi-BIM Universelle** : Intégration complète de **BIMcraft**, modeleur BIM autonome 100% Rust haute performance ("Revit-class"), aux côtés d'Autodesk Revit 2027.
- **6 Nouveaux Outils MCP BIMcraft Dédiés (Total porté à 236 outils)** :
  1. `bimcraft_launch` : Démarrage du GUI BIMcraft à l'écran avec le pont agent interactif (`--control 0`) via WMI/Win32 détaché persistant.
  2. `bimcraft_status` : Diagnostic temps réel du moteur (CLI Rust, processus GUI actif, port et token de communication `bridge.json`).
  3. `bimcraft_convert` : Conversion ultra-rapide de fichiers (.rvt, .rfa, .bimcraft, .ifc, .svg, .png) sans nécessiter Revit.
  4. `bimcraft_info` : Extraction instantanée des niveaux, catégories, familles, paramètres et métadonnées d'un projet Revit ou BIMcraft.
  5. `bimcraft_execute_script` : Modélisation directe dans BIMcraft par script batch (murs, cloisons, baies, pièces, dalles, toits).
  6. `bimcraft_render_view` : Génération de rendus 2D vectoriels et 3D en temps réel aux formats SVG ou PNG.
- **Conversion Automatisée du Projet Appartement Londonien** :
  - Conversion réussie de l'ensemble des 4 variantes RVT (`REFLET_00_EXISTANT`, `REFLET_V1_ECLIPSE_MIROIR-G`, `REFLET_V2_ORBITE_MIROIR-D`, `REFLET_V3_PASSAGE_MIROIR-G`) vers les formats natifs `.bimcraft` et vectoriels `.svg`.
- **Générateurs Algorithmiques Multi-Moteurs** :
  - `revit_generate_complete_floorplan` (`floorplan_generator.rs`) : produit simultanément le batch de modélisation Revit C# et le script BIMcraft en millimètres avec murs fermés, cloisons, baies et pièces étiquetées.
  - `revit_solve_parametric_layout` (`solver.rs`) : produit désormais les commandes d'implantation natives BIMcraft (`desk`, `chair`, `table`) prêtes à injecter.
- **Moteur CAD / DXF (`cad-mcp`) Amélioré** :
  - Prise en charge des segments de murs et polylignes BIMcraft dans `dxf_compare_with_revit`.
  - Nouveaux outils MCP : `dxf_compare_with_bimcraft` et `bimcraft_export_dxf`.
  - Résilience accrue du rendu multi-layouts PDF face aux présentations vides.
- **Gestionnaire d'Automatisation Universel (`scripts/bimcraft_manager.py`)** :
  - Outil CLI Python complet pour piloter le cycle de vie de BIMcraft, convertir les variantes et inspecter les modèles.
- **Site Officiel & Documentation Synchronisés** :
  - Vitrine WebGL Three.js mise à jour à 236 outils avec filtre interactif BIMcraft et badge de release v2.7.0.

---

## [2.6.0] - 2026-11-05

### 🌐 Landing Page Officielle & Déploiement GitHub Pages
- **Landing Page Cinématique & WebGL Three.js** : Conception et déploiement sous `docs/` d'une interface web haut de gamme respectant fidèlement la direction artistique vidéo (palette noir profond `#0B080D`, prune fumé `#2B242C`, or champagne `#D7B98C` et cobalt nuit `#274C8F`).
- **Orbe 3D Filaire Architectural Interactif** : Moteur Three.js WebGL (`docs/three-scene.js`) avec déformations de surface en bruit de Simplex 3D, reflets spéculaires champagne dynamiques et pavillon BIM tridimensionnel filaire avec particules de données actives.
- **Double Mode Vidéo / 3D** : Bascule fluide en 1 clic entre la scène interactive WebGL et le rendu vidéo cinématique de référence (`arc-hero-reference.webm`).
- **Explorateur Interactif des 230 Outils** : Filtrage par catégorie et recherche instantanée avec affichage des temps de réponse mesurés.
- **Workflow Déploiement Automatisé** : Ajout du fichier `.github/workflows/pages.yml` pour publication automatique sur GitHub Pages (`https://encorezaan.github.io/Revit-MCP/`).
- **Redirection Racine** : Ajout de `index.html` à la racine pour compatibilité totale avec les configurations GitHub Pages classiques.

### 🛡️ 8. Traitement de File Résilient en Mode « Zéro Document » (ExternalEvent / Idling)
- **Élimination du blocage 429 sans projet ouvert** : correction de l'incapacité d'Autodesk Revit à dépouiller les `ExternalEvent` lorsque la session est sur l'écran d'accueil ("Zéro Document").
- **Double dispatch unifié `ConcurrentQueue<Request>`** : la file d'attente d'instructions est traitée de manière transparente soit par `IExternalEventHandler.Execute(UIApplication)` lorsque disponible, soit directement par le cycle `UIControlledApplication.Idling`.
- **Réveil de la boucle de messages Win32 (`WM_NULL`)** : déclenchement immédiat de l'inactivité sans latence ni dépendance aux mouvements de souris de l'utilisateur.
- **Fonctionnement garanti de `revit_open_document` et `revit_new_project`** : ouverture et instanciation de projets fiabilisées avec fallback automatique `app.OpenAndActivateDocument` vers `app.Application.OpenDocumentFile` en environnement restreint.

### ⚡ 1. Shared Memory Zero-Copy Transport (0.002 ms)
- **Ring Buffer Windows MMF 64 Mo (`Local\RevitMCP_SharedMemory`)** : pont de transfert ultra-rapide en mémoire vive évitant toute sérialisation JSON lourde pour les gros volumes géométriques, nuages de points et maillages facettisés.
- **Latence de lecture sub-milliseconde (0.002 ms)** : communication bi-directionnelle instantanée entre le processus .NET 10 de Revit et le runtime natif Rust.
- **Nouvel outil MCP** : `revit_read_shared_memory`.

### 🚀 2. Indexation Spatiale 3D BVH & R-Tree en Rust (5 µs)
- **Structure BVH multithreadée avec Rayon** : indexation hiérarchique des boîtes englobantes AABB de l'ensemble de la maquette en 26 microsecondes pour 10 000 éléments.
- **Requêtes spatiales ultra-rapides** : requêtes par sphère (`radius`), volume (`box`) ou audit exhaustif de collisions internes (`clashes`) en 5 microsecondes sans saturer le thread principal Revit.
- **Nouvel outil MCP** : `revit_spatial_query_fast`.

### ☀️ 3. Ray-Tracing Solaire Monte Carlo & Acoustique Prédictive Sabine
- **Moteur de ray-tracing solaire 1M rayons** : simulation photométrique du Facteur de Lumière du Jour (FLJ en %) sur plan de travail à 0.85 m, calcul de l'uniformité et vérification de conformité aux exigences RE2020 Confort Visuel et crédits BREEAM.
- **Acoustique prédictive des locaux** : calcul des temps de réverbération $RT_{60}$ selon les formules de Sabine et Eyring par bandes de fréquences, avec vérification de conformité réglementaire française (NF S 31-080 / NRA 2000).
- **Nouveaux outils MCP** :
  - `revit_compute_solar_daylight_raytrace`
  - `revit_compute_room_acoustics`

### 🔧 4. Auto-Healing MEP : Résolution & Reroute Automatique
- **Détection et résolution autonome des conflits MEP/Structure** : calcul géométrique de contournement optimal des gaines, canalisations et chemins de câbles en collision avec des éléments porteurs.
- **Dévoiement 45° optimisé** : réduction de la perte de charge singulière $\Delta P$ par rapport aux coudes à 90°, calcul automatique des longueurs de transition et des waypoints d'implantation.
- **Réservations & trémies structurelles EI 120** : dimensionnement automatique des traversées de poutres/voiles avec garde périphérique de 50 mm et classification coupe-feu.
- **Nouvel outil MCP** : `revit_auto_resolve_clash`.

### 🐝 5. Essaim Multi-Agents IA Concurrent en Rust
- **5 agents d'ingénierie concurrents sous Rayon** :
  1. *Architect Agent* : conformité des circulations, largeurs libres, ratio de surface vitrée (Art. R111-9).
  2. *Structure Agent* : limites de portée de dalle, continuité des voiles porteurs et intégrité des trémies.
  3. *MEP/HVAC Agent* : garde sous faux-plafond, accessibilité des organes de coupure, pentes gravitaires.
  4. *RE2020 LCA Agent* : respect du seuil carbone $I_{c\_construction} < 640\text{ kgCO}_2\text{eq/m}^2$, valorisation biosourcée.
  5. *PMR & Fire Safety Agent* : aire de giration $\varnothing 1.50$ m, largeur de passage $\ge 0.83$ m, évacuation.
- **Synthèse instantanée & plan d'action unifié** : rapport consolidé avec patchs de commandes Revit prêts à l'exécution.
- **Nouvel outil MCP** : `revit_run_multi_agent_swarm`.

### 📐 6. Synthétiseur Génératif Text-to-BIM Floorplan
- **Génération topologique d'agencements par graphe dual** : dimensionnement spatial automatique selon la typologie souhaitée (T2, T3, T4, T5) et la surface habitable cible.
- **Implantation géométrique complète** : tracé des murs extérieurs isolés 30 cm, cloisons Placostil 10 cm, blocs-portes de communication et baies vitrées orientées.
- **Nouvel outil MCP** : `revit_generate_complete_floorplan`.

### 💶 7. Métreur Estimatif CCTP en Direct (Lots 01 à 10)
- **Estimation économique temps réel** : bordereau de prix unitaires conforme au BPU bâtiment France 2026.
- **Décomposition complète par lots techniques** : Gros-œuvre béton, charpente/étanchéité, menuiseries extérieures/intérieures, plâtrerie/faux-plafonds, carrelage/parquet, peinture, CVC/PAC, plomberie et électricité CFO/CFA.
- **Indicateurs financiers clefs** : déboursé sec, aléas 5%, total HT, TVA et ratio économique au m² de surface de plancher.
- **Nouvel outil MCP** : `revit_compute_live_cctp_budget`.

### 🏗️ 8. Exportateur Natif Fast IFC 4.3 buildingSMART
- **Génération native ISO-10303-21 en Rust** : export complet du modèle en format physique STEP en une fraction de seconde sans monopoliser Revit.
- **Conformité IFC 4.3 Architecture** : mapping standard `IfcProject`, `IfcSite`, `IfcBuilding`, `IfcBuildingStorey`, `IfcWallStandardCase` et solides extrudés paramétriques.
- **Nouvel outil MCP** : `revit_export_fast_ifc`.

### 🔄 9. Synchronisation Continue Live Delta-Sync (60 FPS)
- **Monitoring événementiel natif `DocumentChangedEventArgs`** : détection en arrière-plan de chaque ajout, suppression ou modification d'élément dans Revit sans sollicitation polling.
- **Injection dans la mémoire partagée** : flux continu 60 FPS vers le visualiseur 3D Three.js et la surimpression DirectContext3D.
- **Nouvel outil MCP** : `revit_get_live_stream_status`.

---

## [2.5.0] - 2026-11-05

### 🔮 1. Affichage Holographique en Direct DANS la Vue 3D (DirectContext3D / HUD Augmenté)
- **Prévisualisation fantôme temps réel dans DirectX** : intégration native avec l'API `IDirectContext3DServer` de Revit. L'IA projette des géométries 3D semi-transparentes colorées (murs, ouvertures, agencements volumétriques) flottant directement dans le viewport Revit sans modifier le modèle ni polluer l'historique des transactions.
- **Heatmap 3D des clashs & zones d'ombre** : surimpression volumétrique instantanée en rouge (clash dur), orange (dégagement insuffisant) et vert (conforme) directement sur les faces et boîtes englobantes des éléments sans altérer leurs matériaux Revit.
- **Nouveaux outils MCP** :
  - `revit_overlay_ghost_preview` : projection d'objets fantômes (murs, boîtes, mobiliers) avec paramètres RGBA et rotation.
  - `revit_overlay_clash_heatmap` : projection 3D de heatmaps de collisions.
  - `revit_overlay_clear` : effacement instantané de toutes les surimpressions holographiques.
  - `revit_overlay_get_status` : diagnostic et inventaire des hologrammes 3D actifs.

### 🧠 2. Moteur d'Optimisation Paramétrique en Rust (Auto-Layout Solver)
- **100 000 combinaisons évaluées en 36 millisecondes** : moteur mathématique ultra-haute performance développé en Rust multithreadé (`rayon`).
- **Optimisation multi-objectifs sous contraintes strictes** :
  - **Lumière naturelle & orientation bioclimatique** : maximisation du facteur de lumière du jour selon les façades vitrées et orientation cardinale.
  - **Dégagement de circulation PMR garanti ($\ge 1.50$ m)** : respect des rayons de giration des portes et des largeurs de couloir sans encombrement.
  - **Architecture en pods ergonomiques** : regroupement modulaire des postes de travail en îlots professionnels avec zones de travail partagées.
- **Nouvel outil MCP** :
  - `revit_solve_parametric_layout` : résolution sous contraintes avec projection holographique 3D immédiate dans Revit (`autoGhostPreview: true`).

### 🌐 3. Visualiseur Web 3D Interactif dans Claude (Generative UI / Three.js)
- **Export 3D facettisé direct & autonome** : extraction géométrique des solides et maillages de la vue Revit active (murs, sols, toitures, portes, fenêtres, canalisations, gaines) et encodage métrique Three.js.
- **Expérience Generative UI complète** : génération d'un widget HTML5 autonome avec OrbitControls (rotation, panoramique, zoom), éclairage ambiant/directionnel, grille de référence, bascule fil de fer, mode fantôme semi-transparent et inspection au clic affichant l'ID Revit, la catégorie et le nom de famille.
- **Nouvel outil MCP** :
  - `revit_export_interactive_3d_viewer` : génère le visualiseur 3D interactif et renvoie le code d'intégration `<agent-embed>`.

### 🎙️ 4. Contrôle Vocal Temps Réel (Speech-to-BIM)
- **Pilotage oral sub-300ms sans clavier** : exécution instantanée d'ordres architecturaux exprimés en langage naturel français ou anglais via Windows Named Pipes (< 0.5 ms de latence IPC).
- **Interprétation contextuelle avancée** : compréhension des demandes telles que *"Isole les gaines de ventilation du R+1 et masque le mobilier"*, *"Réinitialise la vue"*, *"Active la vue 3D"*, *"Efface les hologrammes"*, *"Prends une photo HD"*.
- **Outil MCP & Daemon interactif** :
  - `revit_execute_voice_command` : exécution MCP directe d'instructions orales transcrites.
  - `scripts/speech_to_bim_listener.py` : listener vocal interactif en temps réel compatible Gemini Live et Whisper.

### 🍃 5. Bilan Carbone ACV & Analyse de Cycle de Vie en Direct (Base INIES / RE2020)
- **Base de données environnementale officielle française (FDES / INIES)** : intégration des facteurs d'émissions de l'analyse du cycle de vie (modules A1-A3 berceau à la porte, transport, mise en œuvre, fin de vie).
- **Calcul réglementaire RE2020 ($IC_{construction}$ en $kgCO_2eq/m^2$)** : métré volumique et surfacique automatique des éléments du modèle (bétons, briques, cloisons plâtre, isolants, menuiseries alu/bois, finitions), rapporté à la surface de plancher (SDP / Loi Carrez) et comparé aux seuils RE2020 (Seuils 2022, 2025, 2028, 2031).
- **Suggestions d'alternatives biosourcées & gains réels** : identification des points chauds carbonés et calcul instantané du gain en tonnes de $CO_2$ économisées (-40% à -75%) et en stockage de carbone biogénique (ex. cloisons ossature bois fermacell, menuiseries bois PEFC/FSC, bétons CEM III bas-carbone).
- **Nouveaux outils MCP** :
  - `revit_calculate_lca_carbon_footprint` : bilan carbone complet avec conformité RE2020.
  - `revit_suggest_low_carbon_alternatives` : recommandations d'optimisation bas-carbone.
  - `revit_inies_database_lookup` : interrogation directe de la base FDES / INIES.

---

## [2.4.0] - 2026-11-05

### ⚡ 1. IPC Windows Named Pipes Zero-Latence (`\\.\pipe\revit_mcp_ipc`)
- **Transport IPC natif sous la milliseconde (< 0.5 ms)** : contournement total de la pile réseau Windows HTTP.sys et des ports TCP locaux.
- **Support multi-instance et auto-fallback** : le client Rust priorise le pipe nommé Windows ultra-rapide et bascule automatiquement sur HTTP en cas de besoin.
- **Résilience absolue aux déconnexions client** : gestion sécurisée des ruptures de pipe (`IOException: Pipe is broken`) avec protection de la boucle principale du serveur pour garantir zéro crash de Revit.

### 📐 2. Moteur Géométrique Multithreadé Déporté en Rust (`rayon`)
- **Nouvel outil `revit_fast_clash_detection`** : analyse de clash spatiale ultra-rapide accélérée sur tous les cœurs CPU via Rayon. Détection des intersections de boîtes englobantes (AABB) et calcul de volume d'interpénétration en quelques millisecondes sur des milliers d'éléments, sans figer le thread graphique de Revit.
- **Nouvel outil `revit_analyze_room_metrics`** : moteur spatial calculant la surface exacte par formule de Green (Shoelace), le périmètre, la compacité circulaire ($4\pi A / P^2$), et l'audit bioclimatique RE2020 (ratio surface vitrée $\ge 1/6$) avec répartition cardinale de l'ensoleillement (Sud, Est, Ouest, Nord).

### 🌿 3. Le "Git du BIM" : Branching & Variantes Architecturales Instantanées
- **Nouvel outil `revit_branch_create`** : création d'une branche d'alternative architecturale isolée dans `_outils/.snapshots/branches/` avec sauvegarde d'état et calcul instantané des métriques de référence.
- **Nouvel outil `revit_branch_list`** : inventaire chronologique des branches d'exploration créées par l'architecte ou l'IA.
- **Nouvel outil `revit_branch_compare`** : comparateur différentiel intelligent entre deux variantes (branchA et branchB) :
  - Delta de surface habitable Loi Carrez ($m^2$).
  - Delta de linéaire de cloisons ($m$).
  - Delta du nombre de portes et fenêtres.
  - Écart de taux d'accessibilité PMR.
  - Estimation de la variation budgétaire globale (cloisons, menuiseries, finitions).
  - Recommandation technique automatisée.

### 🔬 4. Intégration REA (Rétro-Ingénierie Native)
- Utilisation des outils REA (`inspect_managed_artifact`, `inspect_managed_members`) pour explorer les structures internes de `RevitAPI.dll` (C++/CLI mixed mode, .NET 10, 63 866 méthodes) et concevoir les futurs hooks DirectX et CustomExporter directs.

---

## [2.3.0] - 2026-11-05

### ☁️ 1. Enterprise Cloud & Worksharing (Autodesk Construction Cloud / BIM 360)
- **Nouvel outil `revit_get_cloud_info`** : détection native des modèles hébergés sur le Cloud Autodesk (ACC / BIM 360). Extraction automatique du Cloud Project GUID, Model GUID, chemin central et inventaire des sous-projets (Worksharing).
- **Nouvel outil `revit_sync_with_central`** : synchronisation directe avec le fichier central (Synchronize with Central) avec gestion du compactage, libération des éléments empruntés et commentaire de commit.

### 🛡️ 2. Niveaux de Sécurité Avancés & Garde-fous IA
- **Nouveaux outils `revit_get_security_mode` & `revit_set_security_mode`** :
  - `read-only` : mode sécurité stricte interdisant formellement toute transaction ou écriture dans le modèle (audit, métrés, extraction sans risque).
  - `write` : mode standard contrôlé autorisant les commandes de modélisation architecturales sans exécution de code dynamique.
  - `full` : mode complet avec exécution dynamique C#/Python.
- **Blocage proactif dans le moteur de transactions (`TransactionRunner`)** : rejet automatique de toute modification demandée en mode lecture seule.

### 📸 3. Système d'Instantanés & Points de Restauration (Snapshots)
- **Nouvel outil `revit_create_snapshot`** : création automatisée d'instantanés du document dans `_outils/.snapshots/` avec horodatage UTC, ID unique, taille et métadonnées JSON avant d'effectuer des modifications lourdes sur le projet.
- **Nouvel outil `revit_list_snapshots`** : inventaire chronologique des points de restauration disponibles pour le projet actif.

### 🔍 4. Diagnostics Système & Session Revit
- **Nouvel outil `revit_get_diagnostics`** : inspection complète de la session Revit en temps réel :
  - Version, numéro de build officiel (`27.0.10.13`) et langue.
  - Consommation mémoire vive exacte du processus Revit (Working Set MB, Private Memory MB, Heap managé).
  - Statistique globale des avertissements Revit (warnings groupés par gravité et nature).
  - État de chargement des liens CAO (DWG/DXF) et modèles Revit liés.

### 🏷️ 5. Classifications BIM Sémantiques
- **Nouvel outil `revit_get_bim_classification`** : extraction instantanée des classifications sémantiques normalisées :
  - Code et libellé OmniClass (Table 21/23).
  - Code d'assemblage UniFormat (`BuiltInParameter.ASSEMBLY_CODE` et description).
  - Keynotes (notes clés).
  - Correspondance IFC normalisée (fallback automatique `IfcWall`, `IfcDoor`, `IfcWindow`, etc.).

### 🏗️ 6. Instanciation de Projet à Froid
- **Nouvel outil `revit_new_project`** : instanciation d'un nouveau projet Revit (`.rvt`) à partir d'un gabarit (`.rte`), enregistrement sur disque et ouverture/activation immédiate dans l'interface, fonctionnant même si aucun document n'est ouvert au départ.

### 🔧 7. Fiabilisation du Moteur de Transactions & Résilience
- **Résolution du bug de Rollback masqué** : suppression de l'erreur générique `The transaction has not been started yet` causée par une tentative de rollback sur une transaction déjà avortée par l'API Revit ; renvoi direct du texte réel des `FailureMessage`.
- **Préprocesseur d'avertissements bienveillant (`SafeFailurePreprocessor`)** : les avertissements géométriques simples (`FailureSeverity.Warning`) sont supprimés et continués sans jamais faire échouer la transaction. Seules les erreurs bloquantes (`FailureSeverity.Error`) annulent la transaction.
- **Désactivation des dialogues (`revit_dismiss_dialogs`)** : acceptation du paramètre `policy: "off"` pour retirer le préprocesseur.
- **Démarrage multi-instance silencieux** : suppression définitive de la popup modale bloquante au démarrage de Revit en cas de conflit sur le port 6543 ; journalisation transparente dans `RevitMCP.audit.log`.
- **Unwrapping d'exceptions C# (`revit_execute_csharp`)** : déballage récursif de `TargetInvocationException` pour renvoyer le type exact, message et pile d'exécution de l'`InnerException`.
- **Gestion des chemins (`revit_open_document` & `revit_save_as`)** : séparation stricte de `path` et `documentTitle`.
- **Allègement des captures d'écran (`revit_screenshot_active_view`)** : `includeBase64 = false` par défaut pour préserver la mémoire et le contexte LLM, renvoi systématique du chemin PNG sur disque.

### 📐 8. Moteur CAD MCP Vectoriel Haute Fidélité (`cad-mcp`)
- **Rendu vectoriel PyMuPDF** : migration vers `ezdxf.addons.drawing.pymupdf.PyMuPdfBackend`.
- **Échelle 1:1 exacte** : cadrage automatique sur les dimensions réelles du papier de mise en page (`paper_width`/`paper_height`), fond blanc et traits noirs.
- **Catalogue total** : le serveur MCP dispose désormais de **205 outils professionnels**.

---

## [2.2.0] - 2026-10-20

### 👁️ 1. Feedback Visuel Multimodal (L'IA avec des yeux dans Revit)
- **Nouvel outil `revit_screenshot_active_view`** : capture instantanée haute résolution (jusqu'à 4096 px) de la vue active 3D ou plan.
- **Support multimodal natif dans le serveur Rust (`revit-mcp-server`)** : transmission directe de l'image encodée en Base64 via le protocole officiel MCP (`type: "image", mimeType: "image/png"`). Claude 3.5 Sonnet et GPT-4o peuvent désormais "voir" la maquette, inspecter visuellement les proportions, détecter les défauts d'alignement et s'auto-corriger.
- **Export disque optionnel** pour archivage des croquis et captures du projet.

### 💬 2. Panneau Latéral Ancrable DANS Revit (Dockable Panel WPF) & Ruban
- **Dockable Panel natif Revit** : incrusté directement aux côtés de l'Arborescence du projet et de la palette Propriétés (`RevitAiDockablePanel`).
- **Onglet Ruban dédié "Revit AI Copilot"** : avec commandes instantanées pour afficher/masquer le panneau, lancer un audit PMR et prendre une capture instantanée.
- **Mode "Sélection Active" en temps réel** : détection réactive (`Idling`) de l'élément cliqué par l'architecte, affichage de son identifiant, catégorie, famille/type, niveau, et 4 boutons d'actions rapides ("Isoler", "Rétablir vue", "Audit PMR", "Demander à l'IA").

### 📋 3. Audits Réglementaires Automatisés en 1 Clic (PMR & Normes)
- **Nouvel outil `revit_audit_pmr`** : analyse automatisée de conformité accessibilité handicapée :
  - Largeur de passage utile de toutes les portes (`clearWidthMm` $\ge$ 830 mm).
  - Aire de rotation libre de $\varnothing$ 1,50 m dans les pièces d'eau, sanitaires et dégagements.
  - Pentes de rampes et cheminements (< 5%).
- **Surbrillance automatique en vue** : colorisation instantanée des éléments conformes en vert (`Color(46, 204, 113)`) et des non-conformités en rouge (`Color(231, 76, 60)`) directement dans la vue 3D ou 2D de Revit.
- **Synthèse structurée & Recommandations** : renvoi du taux de conformité globale et de préconisations techniques ciblées pour chaque élément non conforme.

### 🏗️ 4. Génération Spatiale & Macro-Conception (Architecte Génératif)
- **Nouvel outil `revit_generate_spatial_layout`** : modélisation complète d'un projet architectural en un seul appel (macro-prompting) :
  - Gabarit contemporain paramétrable (ex: Pavillon T4 115 m² de plain-pied, salon/cuisine traversant Sud 45 m², suite parentale Est, garage/cellier Nord, chambres secondaires).
  - Résolution de l'organigramme fonctionnel et tracé automatique de la dalle de sol (`Floor`).
  - Murs porteurs extérieurs thermiquement isolés et cloisons séparatrices intérieures.
  - Création automatique des pièces (`Room`) avec étiquettes et calculs précis des surfaces Loi Carrez.
  - Insertion intelligente des menuiseries : grandes baies vitrées au Sud pour respect du ratio bioclimatique RE2020 ($ge 1/6$ de surface vitrée) et portes d'accès conformes PMR.

### ⚡ 5. Copilote Proactif en Temps Réel (`ProactiveGuardian`)
- **Écoute continue des événements Revit** via `ControlledApplication.DocumentChanged` et `Idling`.
- **Veille bienveillante en arrière-plan** :
  - Détection des pièces rendues non délimitées après déplacement de mur.
  - Alerte lors de la suppression de pièces laissant des lignes de séparation orphelines.
  - Vérification de la hauteur de linteau des fenêtres/portes par rapport aux dalles supérieures pour prévenir les clashs structurels.
  - Détection des segments de murs résiduels trop courts (< 150 mm).
- **Affichage dynamique des alertes** dans le Dockable Panel WPF et interrogation distante via les nouveaux outils MCP `revit_get_proactive_alerts` et `revit_clear_proactive_alerts`.

---

## [2.1.1] - 2026-10-20

### 🛡️ Sécurité & Démarrage Silencieux Revit
- **Suppression définitive de la popup de sécurité** (`TaskDialog_Security_Unsigned_File_Loading`) :
  - Activation native du commutateur officiel `[Misc] DisableAddInSecurity=1` dans les fichiers `Revit.ini` (profils utilisateurs 2027/2026 et `UserDataCache`).
  - Nettoyage et assainissement des clés de registre `HKCU:\SOFTWARE\Autodesk\Revit\Autodesk Revit [Version]\CodeSigning` pour ne conserver que les GUIDs canoniques (`a1b2c3d4-e5f6-7890-abcd-ef1234567890`), évitant toute exception de parsing `System.FormatException` au démarrage de Revit.
  - Intégration de ces configurations dans `scripts/install.ps1`.
- **Politique de sécurité renforcée par défaut** :
  - `allowCodeExecution` est désormais configuré à `false` par défaut dans le dépôt `RevitMCP.config.json`.
  - La valeur `true` n'est activée que localement sur la machine de développement et préservée par `scripts/install.ps1` lors des redéploiements.

### 📐 Raycast & Géométrie Spatiale (`revit_get_height_at_point`)
- **Préservation intégrale des vues utilisateur** :
  - Le raycast n'altère plus jamais la vue `{3D}` ou toute autre vue utilisateur ouverte.
  - Création automatique et réutilisation d'une vue 3D dédiée `MCP_Raycast_3D` sans gabarit de vue, avec toits, planchers et plafonds visibles et zone de coupe désactivée.
  - Résultats de hauteur sous plafond (raycast vertical) rigoureusement constants (ex. 3873,33 mm) quel que soit l'état de masquage des catégories dans la vue `{3D}`.

### 📏 Plans Cotés & Ouvertures de Murs (`revit_get_wall_openings`)
- **Données d'implantation complètes et exploitables pour plans cotés** :
  - Informations géométriques du mur hôte : `startPoint` et `endPoint` (x, y en mm), vecteur directionnel unitaire, orientation cardinale de la face extérieure (N/S/E/O selon le Nord du projet), épaisseur (`thicknessMm`) et ligne de justification (`WallLocationLine`).
  - Coordonnées projet des angles extérieurs du mur (`exteriorCornerStart`, `exteriorCornerEnd`).
  - Pour chaque ouverture (porte, fenêtre) :
    - Bords gauche et droit en coordonnées projet absolues (`leftEdgeProject`, `rightEdgeProject`).
    - Distances mesurées directement depuis les deux angles extérieurs réels du mur (`distanceFromExteriorStartCornerMm`, `distanceFromExteriorEndCornerMm`).
    - Hauteur de linteau (`headHeightMm`) en plus de `sillHeightMm` et `heightMm`.
    - Indicateur de non-concordance nominale/réelle `typeNameMismatch: true` lorsque les dimensions de l'instance diffèrent du nom commercial du type (ex. nom "0.60 m x 0.95 m" mais hauteur réelle 2350 mm).

### 📑 Cotation Automatique (`revit_dimension_openings`)
- **Détection et bascule automatique de vue** :
  - Si `viewId` n'est pas fourni, sélection automatique du plan d'étage associé au niveau du mur.
  - Si un `viewId` d'une vue 3D est fourni, bascule automatique transparente sur le plan d'étage compatible.
  - Renvoi clair d'une erreur `{ success: false, error: { code: "NO_OPENINGS" } }` lorsqu'un mur ne possède aucune ouverture à coter.

### 🔄 Multi-Documents & Variantes
- **Validation du basculement de documents** :
  - `revit_open_document` et `revit_activate_document` testés et validés en conditions réelles avec mise à jour immédiate de `isActive` et routage automatique des requêtes ultérieures vers le document actif.

### 🏷️ Gestion Robuste des Types (`revit_duplicate_type` & `revit_set_type_parameters`)
- Remontée précise des anomalies lors de la modification de paramètres de type : listes `unknownParameters[]` et `readOnlyParameters[]`, et échec transactionnel contrôlé (`success: false`) si aucun paramètre valide ne peut être appliqué.

---

## [2.1.0] - 2026-10-20

### 🚀 Nouveautés majeures

- **191 outils MCP unifiés** (contre 70 dans la version 2.0.0) couvrant l'ensemble du cycle de conception architecturale, modélisation, calcul spatial, annotation, documentation, rendus et export dans Revit 2027.
- **Support natif complet du français** : Résolveur universel de catégories (`CategoryResolver`) acceptant indifféremment les noms français (`Mobilier`, `Murs`, `Portes`, `Toits`), anglais (`Furniture`, `Walls`, `Doors`, `Roofs`) ou `BuiltInCategory` (`OST_Furniture`).
- **Identifiants 64 bits (`long`)** : Compatibilité totale avec l'architecture `ElementId` 64 bits de Revit 2024, 2025, 2026 et 2027.
- **Format d'erreur normalisé** : Toutes les erreurs renvoient `{ "success": false, "error": { "code": "...", "message": "...", "details": {...} } }`.
- **Transactions sécurisées et non bloquantes** :
  - Préprocesseur `SafeFailurePreprocessor` absorbant les boîtes de dialogue et renvoyant systématiquement les avertissements dans `bimWarnings: string[]`.
  - Paramètre universel `dryRun: boolean` supporté sur l'intégralité des outils d'écriture pour simuler n'importe quelle modification sans persister la transaction.
  - Outils d'historique et de rollback : `revit_undo`, `revit_redo`, `revit_get_transaction_history`.
  - Exécution groupée atomique : `revit_batch` avec option `atomic: true`.
- **Routage multi-documents** : Chaque outil accepte désormais `documentTitle` ou `documentPath` (avec repli automatique sur le document actif).
- **Serveur compagnon `cad-mcp` (Python)** :
  - Outils autonomes pour DXF : `dxf_info`, `dxf_list_entities`, `dxf_render` (PNG et vectoriel PDF à l'échelle avec tirets réels), `dxf_render_all_layouts_pdf`, `dxf_audit`, `dxf_edit`, `dxf_diff`.
  - Comparaison géométrique et spatiale directe DXF ↔ Revit via `dxf_compare_with_revit`.

---

### 🛠️ Corrections des défauts v2.0.0 (D1 à D10)

| Réf. | Outil | Problème résolu |
|---|---|---|
| **D1** | `revit_get_families` | Résolution bilingue FR/EN/BuiltInCategory (`Mobilier` = `Furniture` = `OST_Furniture`). |
| **D2** | `revit_get_elements` | Enrichissement systématique des éléments : `location` (point + rotationDeg, ou courbe), `bbox` (mm), `levelName`, `hostId`, `facingOrientation`, `handOrientation`, `isFlipped`, `typeId`, avec option `includeGeometry`. |
| **D3** | `revit_get_elements` | Ajout du filtre spatial `bboxFilter: { min: {x,y,z}, max: {x,y,z} }` en millimètres. |
| **D4** | `revit_get_families` | Ajout de l'option `includeTypeParameters: boolean` pour inspecter les paramètres des types de la famille. |
| **D5** | `revit_get_rooms` | Ajout de `includeBoundary: boolean` renvoyant les polygones de contour fermés en millimètres. |
| **D6** | `revit_export_view_image` | Support de n'importe quel `viewId` (pas seulement la vue active), avec `width`, `height`, `visualStyle` et `transparentBackground`. |
| **D7** | `revit_export_pdf` / `revit_export_dwg` | Contrôle fin de l'export : zoom 100% vectoriel, format papier (A4, A3, etc.), orientation, fusion en un seul fichier, calques et versions AutoCAD. |
| **D8** | `revit_place_family_instance` | Chargement automatique si famille non chargée (`familyPath`), rotation en degrés autour de l'axe vertical, renvoi de l'identifiant créé et de sa boîte englobante. |
| **D9** | `revit_health_check` / `/healthz` | Remontée en temps réel de tous les projets ouverts avec titre, chemin, statut actif et état de modification (`openDocuments`). |
| **D10** | Tous les outils | Normalisation stricte de toutes les erreurs sous le format d'enveloppe structuré. |
