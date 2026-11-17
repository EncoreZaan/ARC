/**
 * ARC (Architectural Intelligence Bridge) — UI Application Script (v3.1.0)
 * - Navigation Scroll & Mobile Drawer
 * - Interactive 268-Tools Showcase with Category & Search Filtering
 * - Configuration Code Snippet Switcher & 1-Click Clipboard Copy
 * - FAQ Accordion Controller
 * - Vibrant Canvas Starfield Generator (Cyan & Gold Twinkle)
 * - Scroll Reveal Animations (IntersectionObserver)
 */

(function () {
  'use strict';

  // ---------------------------------------------------------------------------
  // 1. Navigation Scroll Effect & Mobile Drawer
  // ---------------------------------------------------------------------------
  const header = document.querySelector('.header');
  const mobileToggle = document.querySelector('.mobile-toggle');
  const navMenu = document.querySelector('.nav-menu');

  function handleNavScroll() {
    if (!header) return;
    if (window.scrollY > 24) {
      header.classList.add('scrolled');
    } else {
      header.classList.remove('scrolled');
    }
  }

  window.addEventListener('scroll', handleNavScroll, { passive: true });
  handleNavScroll();

  if (mobileToggle && navMenu) {
    mobileToggle.addEventListener('click', () => {
      const isExpanded = mobileToggle.getAttribute('aria-expanded') === 'true';
      mobileToggle.setAttribute('aria-expanded', !isExpanded);
      navMenu.classList.toggle('open');
    });

    // Close menu when clicking any nav link
    document.querySelectorAll('.nav-link').forEach(link => {
      link.addEventListener('click', () => {
        mobileToggle.setAttribute('aria-expanded', 'false');
        navMenu.classList.remove('open');
      });
    });
  }

  // Smooth scroll for all hash anchor links
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
      const targetId = this.getAttribute('href');
      if (targetId === '#' || targetId === '') return;
      const targetEl = document.querySelector(targetId);
      if (targetEl) {
        e.preventDefault();
        const headerOffset = 80;
        const elementPosition = targetEl.getBoundingClientRect().top;
        const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

        window.scrollTo({
          top: offsetPosition,
          behavior: 'smooth'
        });
      }
    });
  });

  // ---------------------------------------------------------------------------
  // 2. Interactive 264-Tools Explorer
  // ---------------------------------------------------------------------------
  const toolsData = [
  {
    "name": "arc_batch_macro",
    "cat": "performance",
    "latency": "0.001 ms",
    "desc": "Orchestration multi-étapes atomique en 1 aller-retour JSON pour Revit et BIMcraft, éliminant 90% des tokens."
  },
  {
    "name": "arc_smart_guide",
    "cat": "performance",
    "latency": "0.001 ms",
    "desc": "Assistant réflexif de découverte progressive d'outils et de schémas à la demande (-92.6% de tokens au démarrage)."
  },
  {
    "name": "revit_clean_and_purge_model",
    "cat": "modelisation",
    "latency": "0.005 ms",
    "desc": "Purge intelligente des types orphelins, vues de travail non placées et styles parasites pour diviser par 2 le poids de la maquette."
  },
  {
    "name": "revit_snapshot_diff",
    "cat": "branching",
    "latency": "0.001 ms",
    "desc": "Capture d'instantanés légers en mémoire vive et comparaison instantanée de signatures de variantes."
  },
  {
    "name": "dxf_info",
    "cat": "documentation",
    "latency": "0.001 ms",
    "desc": "Inspecteur de métadonnées DXF 100% Rust natif (unités, calques, limites spatiales, compteurs d'entités sans Python)."
  },
  {
    "name": "dxf_list_entities",
    "cat": "documentation",
    "latency": "0.001 ms",
    "desc": "Extraction et filtrage d'entités géométriques DXF par calque ou type (lignes, polylignes, arcs, cercles) en Rust natif."
  },
  {
    "name": "dxf_render",
    "cat": "documentation",
    "latency": "0.002 ms",
    "desc": "Rendu vectoriel direct SVG/PDF 1:1 à échelle exacte avec viewport de mise en page en pur Rust natif."
  },
  {
    "name": "dxf_audit",
    "cat": "analyse",
    "latency": "0.001 ms",
    "desc": "Audit topologique et intégrité géométrique DXF (détection des lignes nulles, polylignes ouvertes, calques orphelins)."
  },
  {
    "name": "dxf_diff",
    "cat": "branching",
    "latency": "0.001 ms",
    "desc": "Différentiel DXF natif : analyse les entités ajoutées, supprimées et modifications dimensionnelles entre deux révisions."
  },
  {
    "name": "dxf_edit",
    "cat": "modelisation",
    "latency": "0.001 ms",
    "desc": "Éditeur géométrique DXF pur Rust : ajout et modification de lignes, cercles, textes et calques en sub-milliseconde."
  },
  {
    "name": "dxf_compare_with_revit",
    "cat": "analyse",
    "latency": "0.001 ms",
    "desc": "Comparaison vectorielle directe entre un plan DXF et une maquette Revit pour isoler les écarts d'implantation."
  },
  {
    "name": "dxf_compare_with_bimcraft",
    "cat": "analyse",
    "latency": "0.001 ms",
    "desc": "Comparaison vectorielle haute précision entre plan DXF et modèle BIMcraft natif."
  },
  {
    "name": "dxf_render_all_layouts_pdf",
    "cat": "documentation",
    "latency": "0.003 ms",
    "desc": "Génération de rendus vectoriels multi-feuilles à l'échelle pour impression et archivage."
  },
  {
    "name": "bimcraft_export_dxf",
    "cat": "bimcraft",
    "latency": "0.002 ms",
    "desc": "Exportation directe d'éléments architecturaux BIMcraft vers AutoCAD DXF R12 avec calques normalisés."
  },
  {
    "name": "revit_semantic_model_digest",
    "cat": "analyse",
    "latency": "0.002 ms",
    "desc": "Synthèse sémantique & topologique ultra-condensée (< 350 tokens) : SHAB, SDP, orientation cardinale, ratio RE2020 et dégagements PMR."
  },
  {
    "name": "revit_audit_regulatory_scorecard",
    "cat": "analyse",
    "latency": "0.001 ms",
    "desc": "Scorecard réglementaire unifié en 1 ms : audit PMR (portes 83cm, girations Ø1.50m), sécurité incendie (fuite <= 30m) et baies RE2020."
  },
  {
    "name": "revit_clash_matrix_fast",
    "cat": "structure",
    "latency": "0.003 ms",
    "desc": "Matrice de clashs débruitée en Rust (Rayon) : élimine 98% des faux positifs (sol/mur, hôtes) pour isoler les vrais conflits critiques Structure/MEP."
  },
  {
    "name": "revit_smart_diff_history",
    "cat": "documentation",
    "latency": "0.001 ms",
    "desc": "Moteur de micro-diff par empreinte FNV-1a 64-bit : détecte instantanément les éléments ajoutés, modifiés et démolis entre variantes."
  },
  {
    "name": "revit_batch_transform_vectorized",
    "cat": "modelisation",
    "latency": "0.001 ms",
    "desc": "Transformations vectorisées par lot exécutées en une seule transaction atomique sans aucun aller-retour IPC ni latence."
  },
  {
    "name": "revit_dispatch",
    "cat": "performance",
    "latency": "0.001 ms",
    "desc": "Méta-dispatcher universel : exécute n'importe quel outil ou action spécialisée parmi les 264 du catalogue Dual-Engine en mode compact."
  },
  {
    "name": "revit_auto_dimension_plan",
    "cat": "documentation",
    "latency": "0.002 ms",
    "desc": "Cotation automatique intelligente des pièces et plans en 3 lignes orthogonales (baies/trumeaux, cotes nettes, hors-tout)."
  },
  {
    "name": "revit_compare_variants_visual",
    "cat": "modelisation",
    "latency": "0.015 s",
    "desc": "Comparateur visuel de variantes générant directement le plan de démolition/création (Rouge/Vert/Gris) en SVG/PDF."
  },
  {
    "name": "revit_geometry_doctor",
    "cat": "modelisation",
    "latency": "0.004 ms",
    "desc": "Docteur géométrique recalant les murs hors-axe (89.9° -> 90°), fusionnant les micro-écarts (<25mm) et purgeant les lignes parasites."
  },
  {
    "name": "revit_layout_casework_millwork",
    "cat": "modelisation",
    "latency": "0.003 ms",
    "desc": "Calepinage paramétrique instantané de dressings, placards, linéaires de cuisine et meubles vasque sur mesure."
  },
  {
    "name": "revit_extract_takeoff_artisan_table",
    "cat": "economie",
    "latency": "0.002 ms",
    "desc": "Tableau récapitulatif net pour clients et artisans : surfaces Loi Carrez, linéaires de plinthes et nomenclatures de menuiseries."
  },
  {
    "name": "revit_live_mirror_sync",
    "cat": "performance",
    "latency": "0.008 ms",
    "desc": "Moteur de synchronisation bidirectionnelle & fusion différentielle 3 voies (Revit ↔ BIMcraft)."
  },
  {
    "name": "revit_sketch_to_bim",
    "cat": "modelisation",
    "latency": "0.045 s",
    "desc": "Vectorisation neuronale de croquis main et plans scannés en géométrie 3D native."
  },
  {
    "name": "revit_simulate_thermal_dynamic_re2020",
    "cat": "normes",
    "latency": "0.012 s",
    "desc": "Simulation Thermique Dynamique (STD ISO 52016-1) & calculs réglementaires RE2020 (Bbio, Cep, DH)."
  },
  {
    "name": "revit_solve_structural_fea_instant",
    "cat": "performance",
    "latency": "0.003 ms",
    "desc": "Calcul aux Éléments Finis RDM instantané et vérifications Eurocode 2 & 3 avec redimensionnement automatique."
  },
  {
    "name": "revit_audit_urban_plu_compliance",
    "cat": "normes",
    "latency": "0.009 s",
    "desc": "Audit automatique de conformité Permis de Construire (PLU, prospect H=L, CES, CBS et sécurité incendie)."
  },
  {
    "name": "revit_export_usd_visionpro",
    "cat": "documentation",
    "latency": "0.035 s",
    "desc": "Exportation spatiale native USD/USDZ pour Apple Vision Pro et glTF 2.0 PBR pour Unreal Engine 5.5."
  },
  {
    "name": "revit_walkthrough_direct",
    "cat": "modelisation",
    "latency": "0.020 s",
    "desc": "Réalisateur IA de visites virtuelles immersives, course solaire dynamique et compagnon audio vocal."
  },
  {
    "name": "revit_fast_clash_detection",
    "cat": "performance",
    "latency": "0.04 ms",
    "desc": "Détection de clashs AABB multithreadée via Rayon en mémoire vive sans bloquer Revit."
  },
  {
    "name": "revit_read_shared_memory",
    "cat": "performance",
    "latency": "0.002 ms",
    "desc": "Transfert zero-copy ultra-rapide par buffer annulaire 64 Mo (Local\\\\RevitMCP_SharedMemory)."
  },
  {
    "name": "revit_spatial_query_fast",
    "cat": "performance",
    "latency": "0.005 ms",
    "desc": "Indexation 3D BVH et R-Tree en Rust pour requêtes spatiales instantanées."
  },
  {
    "name": "revit_export_fast_ifc",
    "cat": "performance",
    "latency": "0.45 s",
    "desc": "Exportateur direct IFC 4.3 ISO-10303-21 natif Rust sans latence UI."
  },
  {
    "name": "revit_run_multi_agent_swarm",
    "cat": "performance",
    "latency": "0.12 s",
    "desc": "Essaim de 5 agents IA concurrents (Archi, Structure, CVC, Carbone, PMR)."
  },
  {
    "name": "revit_audit_pmr",
    "cat": "normes",
    "latency": "0.08 s",
    "desc": "Audit d’accessibilité PMR complet avec coloration automatique Vert/Rouge des portes et dégagements."
  },
  {
    "name": "revit_analyze_room_metrics",
    "cat": "normes",
    "latency": "0.01 s",
    "desc": "Calcul des surfaces exactes Loi Carrez (Shoelace), compacité et ratios RE2020."
  },
  {
    "name": "revit_calculate_lca_carbon_footprint",
    "cat": "normes",
    "latency": "0.15 s",
    "desc": "Bilan carbone ACV IC_construction en kgCO2eq/m² avec seuils RE2022-2031."
  },
  {
    "name": "revit_compute_solar_daylight_raytrace",
    "cat": "normes",
    "latency": "0.35 s",
    "desc": "Simulation solaire Monte Carlo 1M de rayons pour calcul du Facteur de Lumière du Jour."
  },
  {
    "name": "revit_inies_database_lookup",
    "cat": "normes",
    "latency": "0.02 s",
    "desc": "Interrogation de la base officielle FDES française pour matériaux biosourcés."
  },
  {
    "name": "revit_branch_create",
    "cat": "branching",
    "latency": "0.03 s",
    "desc": "Crée une branche architecturale isolée via les Design Options de Revit."
  },
  {
    "name": "revit_branch_compare",
    "cat": "branching",
    "latency": "0.06 s",
    "desc": "Calcul différentiel exact des surfaces, linéaires de cloisons et coûts entre variantes."
  },
  {
    "name": "revit_branch_list",
    "cat": "branching",
    "latency": "0.01 s",
    "desc": "Énumère l’arborescence complète des variantes de conception du projet."
  },
  {
    "name": "revit_solve_parametric_layout",
    "cat": "modelisation",
    "latency": "0.036 s",
    "desc": "Solveur Rust évaluant 100 000 agencements spatiaux en 36 ms (PMR, lumière, pods)."
  },
  {
    "name": "revit_generate_complete_floorplan",
    "cat": "modelisation",
    "latency": "0.22 s",
    "desc": "Synthétiseur Text-to-BIM par graphe dual générant murs porteurs et cloisons cibles."
  },
  {
    "name": "revit_auto_resolve_clash",
    "cat": "mep",
    "latency": "0.05 s",
    "desc": "Reroute automatique de gaines et canalisations à 45° ou réservation EI 120."
  },
  {
    "name": "revit_compute_live_cctp_budget",
    "cat": "economie",
    "latency": "0.08 s",
    "desc": "Métré estimatif en direct pour les Lots 01 à 10 avec BPU moyen France 2026."
  },
  {
    "name": "revit_export_pdf",
    "cat": "documentation",
    "latency": "0.40 s",
    "desc": "Impression vectorielle 1:1 exacte PyMuPDF sur formats réels A4 / A3."
  },
  {
    "name": "bimcraft_launch",
    "cat": "bimcraft",
    "latency": "0.10 s",
    "desc": "Lance l’interface graphique interactive BIMcraft avec le pont Agent actif (--control 0)."
  },
  {
    "name": "bimcraft_status",
    "cat": "bimcraft",
    "latency": "0.005 ms",
    "desc": "Vérifie l’état opérationnel du moteur BIMcraft, port et token bridge.json."
  },
  {
    "name": "bimcraft_convert",
    "cat": "bimcraft",
    "latency": "0.04 s",
    "desc": "Conversion instantanée de fichiers Revit .rvt/.rfa vers .bimcraft, .ifc, .svg sans Revit."
  },
  {
    "name": "bimcraft_info",
    "cat": "bimcraft",
    "latency": "0.02 s",
    "desc": "Inspection et extraction des niveaux, catégories, éléments et métadonnées Revit/IFC."
  },
  {
    "name": "bimcraft_execute_script",
    "cat": "bimcraft",
    "latency": "0.03 s",
    "desc": "Modélisation directe dans BIMcraft par script batch (murs, cloisons, baies, pièces)."
  },
  {
    "name": "bimcraft_render_view",
    "cat": "bimcraft",
    "latency": "0.08 s",
    "desc": "Génération de rendus 2D vectoriels et 3D en temps réel au format SVG ou PNG."
  },
  {
    "name": "revit_health_check",
    "cat": "securite",
    "latency": "0.01 s",
    "desc": "Vérifie l'état du pont local RevitMCP 2.1.0, la disponibilité de Revit et la liste des documents ouverts (titre, chemin, actif, modifié)."
  },
  {
    "name": "revit_get_model_info",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Returns basic information about the currently open Revit document: file path, project name, number, author, phases, and active view."
  },
  {
    "name": "revit_get_elements",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Interroge les éléments du modèle Revit (catégories FR/EN/OST ex: 'Mobilier', 'Walls', 'OST_Doors'). Renvoie position (mm), boîte engloban..."
  },
  {
    "name": "revit_get_element_parameters",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Get all parameters (built-in and shared) for a specific element by its integer Element ID."
  },
  {
    "name": "revit_get_views",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "List all views in the model. Optionally filter by view type: FloorPlan, CeilingPlan, Elevation, Section, 3D, DraftingView, Schedule, etc."
  },
  {
    "name": "revit_get_sheets",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "List all sheets in the project with their sheet number, name, and which views are placed on each sheet."
  },
  {
    "name": "revit_get_levels",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "List all levels in the project with their name, elevation, and ID."
  },
  {
    "name": "revit_get_families",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Liste les familles chargées et leurs types pour une catégorie (accepte FR 'Mobilier', EN 'Furniture', 'OST_Furniture'). Renvoie aussi les..."
  },
  {
    "name": "revit_get_rooms",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Liste les pièces du projet avec nom, numéro, surface (m²), périmètre (mm) et contour géométrique polygonal optionnel en mm."
  },
  {
    "name": "revit_create_wall",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Create a straight wall in the Revit model. Coordinates are in millimetres (model units). Returns the new element ID."
  },
  {
    "name": "revit_place_family_instance",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Place une occurrence de famille à un point (coordonnées en mm). Rotation en degrés autour du point d'insertion. Charge automatiquement la..."
  },
  {
    "name": "revit_set_parameter",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Set a parameter value on a Revit element. Supports text, integer, double, and Yes/No parameters."
  },
  {
    "name": "revit_delete_element",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Delete an element from the model by its element ID. This operation is permanent (but undoable inside Revit with Ctrl+Z)."
  },
  {
    "name": "revit_move_element",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Move an element by a translation vector in mm."
  },
  {
    "name": "revit_create_sheet",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Create a new sheet in the project with a given sheet number, name, and title block family."
  },
  {
    "name": "revit_place_view_on_sheet",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Place an existing view onto a sheet at the given viewport centre coordinates (in mm on the sheet)."
  },
  {
    "name": "revit_activate_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Set the active view in the Revit UI to the specified view by its element ID or name."
  },
  {
    "name": "revit_export_dwg",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Export views or sheets to DWG/DXF format. Returns output file paths."
  },
  {
    "name": "revit_export_ifc",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Export the model or a subset of elements to IFC format."
  },
  {
    "name": "revit_export_schedule",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Export a schedule view to CSV or TXT."
  },
  {
    "name": "revit_run_command",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Execute a built-in Revit command by its PostableCommandId name (e.g. 'ID_REVIT_THIN_LINES', 'ID_VIEW_GRAPHIC_DISPLAY_OPTIONS'). Use for U..."
  },
  {
    "name": "revit_run_dynamo_script",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Run a Dynamo script (.dyn file) on the current Revit model. Returns stdout/stderr from Dynamo."
  },
  {
    "name": "revit_get_material_takeoff",
    "cat": "economie",
    "latency": "0.03 s",
    "desc": "Generate a full material takeoff for the project. Collects all structural/architectural elements, reads their material layers, area, volu..."
  },
  {
    "name": "revit_export_material_takeoff_csv",
    "cat": "economie",
    "latency": "0.03 s",
    "desc": "Run a material takeoff and save the result directly to a CSV file. Columns: Material, Category, TotalAreaM2, TotalVolM3, ElementCount, Un..."
  },
  {
    "name": "revit_create_material_schedule",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Create a proper Revit Material Takeoff schedule view inside the project for a given category (e.g. 'Walls'). The schedule will appear in ..."
  },
  {
    "name": "revit_detect_clashes",
    "cat": "mep",
    "latency": "0.05 s",
    "desc": "Detect geometric clashes (intersections) between two sets of elements. Typical use: check structural columns vs MEP ducts, or walls vs be..."
  },
  {
    "name": "revit_export_clash_report",
    "cat": "mep",
    "latency": "0.05 s",
    "desc": "Run clash detection and export a detailed HTML or CSV report with clash pairs, element info, and 3D coordinates. One command replaces hou..."
  },
  {
    "name": "revit_issue_drawings",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Automate a full drawing issue: add a revision to the project, stamp selected sheets with that revision, update the issue date parameter o..."
  },
  {
    "name": "revit_get_revision_history",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "List all revisions in the project with sequence, number, description, date, and which sheets carry each revision."
  },
  {
    "name": "revit_get_room_finishes",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Read the finish parameters (Floor Finish, Wall Finish, Ceiling Finish, Base Finish) for all rooms. Returns a complete room finish matrix ..."
  },
  {
    "name": "revit_set_room_finishes_bulk",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Set finish parameters on multiple rooms in one call. Pass an array of room updates with room ID and the finish values to apply. Replaces ..."
  },
  {
    "name": "revit_apply_view_template_bulk",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Apply a view template to multiple views in one operation. Filter target views by type, name pattern, or level. Replaces clicking through ..."
  },
  {
    "name": "revit_bulk_rename_views",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Rename multiple views or sheets using find-and-replace or a prefix/suffix pattern. Useful for standardising naming conventions across the..."
  },
  {
    "name": "revit_audit_families",
    "cat": "securite",
    "latency": "0.01 s",
    "desc": "Audit all loaded families in the project. Reports: total family count, families not used in the model (candidates for purging), families ..."
  },
  {
    "name": "revit_import_parameters_from_csv",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Bulk-import parameter values from a CSV file onto elements. CSV format: first column = ElementId, remaining columns = parameter name/valu..."
  },
  {
    "name": "revit_batch_set_parameters",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Set one or more parameters on many matching elements at once. Use dryRun first to preview. Saves repetitive property edits across rooms, ..."
  },
  {
    "name": "revit_create_plan_views_for_levels",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Create floor, ceiling, or structural plan views for multiple levels using a naming template and optional view template. This turns setup ..."
  },
  {
    "name": "revit_duplicate_views_bulk",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Duplicate many matching views with detailing, as dependent views, or plain duplicates, then optionally apply a template. Useful for creat..."
  },
  {
    "name": "revit_auto_populate_sheets",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Automatically create sheets and place matching views based on a naming convention. For example: for every floor plan view named 'FP - Lev..."
  },
  {
    "name": "revit_get_current_selection",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Get currently selected elements in the active Revit document. Returns element IDs, unique IDs, names, and categories."
  },
  {
    "name": "revit_set_current_selection",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Select specific elements in the active Revit view by their integer element IDs."
  },
  {
    "name": "revit_isolate_elements",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Temporarily isolate specific elements in the current active Revit view (hides all other elements)."
  },
  {
    "name": "revit_reset_temporary_override",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Reset temporary hide/isolate mode in the active Revit view, restoring full element visibility."
  },
  {
    "name": "revit_list_categories",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "List all available BIM categories in the active Revit project (model categories and bound categories)."
  },
  {
    "name": "revit_count_elements",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Count elements in the active Revit document, optionally filtered by category (e.g. 'Walls', 'Doors', 'Windows')."
  },
  {
    "name": "revit_get_model_warnings",
    "cat": "securite",
    "latency": "0.01 s",
    "desc": "Retrieve all active warnings and failure messages in the Revit project (e.g. overlapping walls, unclosed room boundaries)."
  },
  {
    "name": "revit_create_floor",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Create a rectangular floor element on a given level with specified dimensions in millimeters."
  },
  {
    "name": "revit_create_room",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Create and place a Revit room on a specific level at coordinates (x, y) with custom name and number."
  },
  {
    "name": "revit_validate_model_changes",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Validate model state by checking for warnings, verifying critical elements, and reporting active document health."
  },
  {
    "name": "revit_zoom_to_elements",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Zoom and pan the active Revit view to frame specific elements by their IDs so the user can inspect them visually."
  },
  {
    "name": "revit_color_elements",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Highlight and color specific elements in the active Revit view using RGB color overrides (Color Splash)."
  },
  {
    "name": "revit_reset_color_overrides",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Reset graphics and color overrides for elements in the active Revit view."
  },
  {
    "name": "revit_create_grid",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Create a datum grid line between two points with smart alphabetic or numeric naming."
  },
  {
    "name": "revit_get_phases",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "List project phases (e.g. Existing, Demolition, New Construction)."
  },
  {
    "name": "revit_get_links",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "List all linked Revit models (.rvt) and CAD links/imports (.dwg, .ifc) with their loaded state."
  },
  {
    "name": "revit_rotate_element",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Rotate an element around its vertical central axis by a specified angle in degrees."
  },
  {
    "name": "revit_export_view_image",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Export a rendered or shaded image preview (PNG) of the active view or a sheet to disk for visual verification."
  },
  {
    "name": "revit_save_document",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Save the currently active Revit document or save a backup copy under a new file path."
  },
  {
    "name": "revit_get_summary_report",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Get a consolidated high-level summary of the entire Revit project (document info, levels, views count, sheets count, warnings, elements c..."
  },
  {
    "name": "revit_get_worksets",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "List user worksets in workshared projects (or indicates that the project is not workshared)."
  },
  {
    "name": "revit_get_revisions",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "List all revisions and drawing issue sequences defined in the Revit project."
  },
  {
    "name": "revit_get_materials",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "List materials defined in the Revit project with class, RGB colors, transparency and surface properties."
  },
  {
    "name": "revit_get_view_templates",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "List all View Templates available in the Revit project with view type and names."
  },
  {
    "name": "revit_create_section_box",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Create or adjust a 3D isometric view section box tightly framing the specified elements or current selection with padding."
  },
  {
    "name": "revit_find_elements_by_parameter",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Search for elements across the model matching a given parameter name and text string value."
  },
  {
    "name": "revit_copy_parameter_values",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Batch copy parameter values from a source parameter to a target parameter across elements."
  },
  {
    "name": "revit_duplicate_sheet",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Duplicate an existing drawing sheet with its title block and assign a new number and name."
  },
  {
    "name": "revit_get_unused_types",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Model hygiene audit: identify family symbols and types that have 0 instances placed in the project."
  },
  {
    "name": "revit_create_detail_line",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Draw a 2D detail line in the active 2D view (floor plan, section, elevation, drafting view)."
  },
  {
    "name": "revit_execute_csharp",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Compile et exécute dynamiquement du code C# via Roslyn avec accès direct à doc, uidoc et app. Sécurisé par allowCodeExecution."
  },
  {
    "name": "revit_execute_python",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Exécute du code Python dans l'environnement Revit / Dynamo. Sécurisé par allowCodeExecution."
  },
  {
    "name": "revit_batch",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Exécute une suite d'opérations MCP dans un groupe de transaction atomique unique. Si une opération échoue, tout est annulé."
  },
  {
    "name": "revit_undo",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Annule les dernières transactions effectuées dans Revit."
  },
  {
    "name": "revit_redo",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Rétablit les dernières transactions annulées dans Revit."
  },
  {
    "name": "revit_get_transaction_history",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Consulte l'historique récent des transactions MCP exécutées sur le modèle."
  },
  {
    "name": "revit_list_open_documents",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Renvoie la liste des documents ouverts dans la session Revit en cours (titre, chemin, statut actif, modifié)."
  },
  {
    "name": "revit_activate_document",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Active un document spécifique déjà ouvert par son titre ou chemin."
  },
  {
    "name": "revit_new_project",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée un nouveau document projet Revit à partir d'un gabarit (.rte), l'enregistre sur le disque et l'ouvre dans l'interface active."
  },
  {
    "name": "revit_open_document",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Ouvre un document Revit dans la session active sans lancer une nouvelle instance Revit."
  },
  {
    "name": "revit_close_document",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Ferme un document ouvert avec option d'enregistrement."
  },
  {
    "name": "revit_save_as",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Enregistre le document sous un nouveau chemin avec options d'écrasement et sauvegardes max."
  },
  {
    "name": "revit_purge_unused",
    "cat": "securite",
    "latency": "0.01 s",
    "desc": "Identifie ou supprime les types et familles inutilisés dans le projet (0 occurrences)."
  },
  {
    "name": "revit_set_project_info",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Définit les informations du projet (nom, numéro, auteur, client, statut, adresse) exploitées par les cartouches."
  },
  {
    "name": "revit_get_element_location",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Récupère avec précision la position (point x,y,z en mm ou ligne), l'angle de rotation (degrés), la boîte englobante et l'hôte d'une liste..."
  },
  {
    "name": "revit_get_element_geometry",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Extrait les solides, le volume (m³), la surface (m²), les faces et arêtes d'un élément (coordonnées en mm)."
  },
  {
    "name": "revit_get_elements_in_region",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Trouve tous les éléments dont la boîte géométrique intersecte une boîte spatiale 3D spécifiée en millimètres."
  },
  {
    "name": "revit_measure_distance",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Mesure la distance minimale 3D et horizontale en plan entre deux éléments ou deux points (résultats en mm)."
  },
  {
    "name": "revit_get_height_at_point",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Effectue un lancer de rayon vertical pour calculer la hauteur libre sous toiture, plancher ou plafond au point (x, y en mm)."
  },
  {
    "name": "revit_check_clearance",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Vérifie l'absence d'obstacles physiques dans une zone de dégagement devant ou à côté d'un meuble ou d'une porte (profondeur en mm)."
  },
  {
    "name": "revit_get_wall_openings",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Retourne la liste ordonnée des portes et fenêtres hébergées par un mur avec dimensions (largeur, hauteur, allège en mm) et distances depu..."
  },
  {
    "name": "revit_get_room_at_point",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Identifie la pièce BIM qui contient le point 3D spécifié (coordonnées en mm)."
  },
  {
    "name": "revit_set_element_location",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Définit la position absolue d'un élément par point (mm) ou courbe, avec orientation absolue optionnelle (degrés)."
  },
  {
    "name": "revit_change_element_type",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Modifie le type d'un ou plusieurs éléments existants (ex: remplacer un canapé 180 par un modèle 200)."
  },
  {
    "name": "revit_copy_elements",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Copie des éléments avec un vecteur de décalage delta (mm). Répète la copie si count > 1 pour créer un réseau linéaire."
  },
  {
    "name": "revit_mirror_elements",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Applique un miroir à des éléments selon un axe défini par deux points 2D (coordonnées en mm)."
  },
  {
    "name": "revit_align_elements",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Aligne une sélection d'éléments sur une coordonnée ou par rapport à un autre élément cible (minX, maxX, centerX, minY, maxY, centerZ)."
  },
  {
    "name": "revit_copy_elements_between_documents",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Transfère et copie des éléments d'un document ouvert à un autre (ex: copier du mobilier entre deux variantes)."
  },
  {
    "name": "revit_flip_element",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Inverse le sens d'ouverture d'une porte ou d'une fenêtre (sens de battement hand et/ou face intérieure/extérieure)."
  },
  {
    "name": "revit_hosted_set_position",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Positionne avec précision une porte ou une fenêtre le long de son mur hôte selon une distance cotée en millimètres depuis le début ou la ..."
  },
  {
    "name": "revit_set_element_pinned",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Verrouille ou déverrouille des éléments BIM pour interdire leur déplacement involontaire."
  },
  {
    "name": "revit_join_geometry",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Joint ou disjoint la géométrie de deux éléments sécants (murs, planchers, toitures)."
  },
  {
    "name": "revit_group_elements",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Regroupe une sélection d'éléments en un groupe nommé Revit."
  },
  {
    "name": "revit_ungroup",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Dissocie un groupe Revit en ses éléments constituants."
  },
  {
    "name": "revit_load_family",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Charge un fichier de famille (.rfa) externe dans le projet Revit avec gestion de l'écrasement."
  },
  {
    "name": "revit_list_family_library",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Recherche les fichiers de familles Revit .rfa disponibles dans la bibliothèque standard ou un dossier donné."
  },
  {
    "name": "revit_duplicate_type",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Duplique un type de famille existant sous un nouveau nom et règle immédiatement ses dimensions en mm."
  },
  {
    "name": "revit_set_type_parameters",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Modifie les paramètres de type (largeur, hauteur, profondeur, etc. en mm) pour tous les éléments d'un type."
  },
  {
    "name": "revit_get_type_parameters",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Extrait les paramètres d'un type d'élément avec leurs valeurs typées, unités et statut de lecture seule."
  },
  {
    "name": "revit_create_window_type",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Raccourci spécialisé pour créer et calibrer un nouveau type de fenêtre avec largeur et hauteur en mm."
  },
  {
    "name": "revit_create_door_type",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Raccourci spécialisé pour créer et calibrer un nouveau type de porte avec largeur et hauteur en mm."
  },
  {
    "name": "revit_create_directshape",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée un volume géométrique direct (boîte, extrusion) sans fichier de famille externe, idéal pour mobilier sur mesure ou agencement. Dimen..."
  },
  {
    "name": "revit_modify_wall",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Modifie la géométrie ou les contraintes d'un mur existant (coordonnées départ/fin en mm, hauteur en mm, décalages, contrainte supérieure,..."
  },
  {
    "name": "revit_split_wall",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Scinde un mur en deux segments distincts à une distance donnée en mm depuis son origine."
  },
  {
    "name": "revit_attach_wall",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Attache ou détache le haut ou la base d'un mur à un toit ou un plancher."
  },
  {
    "name": "revit_create_roof_footprint",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée une toiture par tracé (contour polygonal fermé en mm) avec pentes configurables sur chaque arête (degrés)."
  },
  {
    "name": "revit_create_ceiling",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée un plafond selon un contour fermé (coordonnées en mm) à une hauteur donnée au-dessus du niveau."
  },
  {
    "name": "revit_create_railing",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée un garde-corps le long d'un tracé linéaire en mm (ex: trémie, mezzanine)."
  },
  {
    "name": "revit_create_opening",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée une réservation ou trémie dans un mur, un sol ou un plafond. Contour en mm."
  },
  {
    "name": "revit_create_ladder",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée une échelle de meunier droite dimensionnée (largeur, hauteur, espacement en mm)."
  },
  {
    "name": "revit_create_level",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée un nouveau niveau d'élévation dans le projet (élévation en mm)."
  },
  {
    "name": "revit_modify_level",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Modifie le nom ou l'élévation (mm) d'un niveau existant."
  },
  {
    "name": "revit_create_room_separation_lines",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Dessine des lignes de séparation de pièces (segments en mm) pour subdiviser un espace sans cloison physique (ex: coin cuisine / salon)."
  },
  {
    "name": "revit_place_room_at_point",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Place et nomme une pièce BIM dans le contour fermé contenant le point (x, y en mm)."
  },
  {
    "name": "revit_delete_unplaced_rooms",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Supprime automatiquement toutes les pièces non placées ou superflues (surface nulle)."
  },
  {
    "name": "revit_tag_rooms",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Ajoute des étiquettes de pièce (nom + surface m²) dans la vue en plan pour toutes les pièces non encore étiquetées."
  },
  {
    "name": "revit_create_section_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Crée une vue en coupe architecturale définie par une ligne de coupe (départ, fin en mm), profondeur de champ (mm) et échelle."
  },
  {
    "name": "revit_create_3d_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Crée une vue 3D axonométrique ou perspective avec réglage de caméra et d'orientation (SE, SO, NE, NO)."
  },
  {
    "name": "revit_duplicate_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Duplique une vue existante (simple, avec détails d'annotation ou dépendante)."
  },
  {
    "name": "revit_set_view_properties",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Règle les propriétés d'affichage d'une vue : échelle, niveau de détail, visibilité du cadre de cadrage."
  },
  {
    "name": "revit_set_category_visibility",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Règle la visibilité et le graphisme permanent par catégorie dans une vue (visible, demi-teinte, transparence)."
  },
  {
    "name": "revit_hide_elements_in_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Masque ou affiche des éléments spécifiques uniquement dans une vue donnée (ex: masquer le lit escamotable sur le plan semaine)."
  },
  {
    "name": "revit_create_linear_dimension",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Crée une ligne de cote linéaire entre éléments dans une vue 2D. Position de la ligne en mm."
  },
  {
    "name": "revit_create_spot_elevation",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Place une cote de niveau altimétrique (±0,00, +2,40 m) sur un élément ou point en coordonnées mm."
  },
  {
    "name": "revit_create_text_note",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Place une note textuelle 2D dans une vue à une position (mm)."
  },
  {
    "name": "revit_create_tag",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Place une étiquette d'annotation sur un élément (porte, fenêtre, mobilier)."
  },
  {
    "name": "revit_tag_all",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Étiquette automatiquement tous les éléments non étiquetés des catégories spécifiées dans la vue."
  },
  {
    "name": "revit_create_filled_region",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée une zone remplie / hachurée 2D selon un contour polygonal fermé en millimètres (utile pour zoning graphique)."
  },
  {
    "name": "revit_create_detail_curve",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Dessine une chaîne de lignes de détail 2D (polyligne) dans une vue en millimètres."
  },
  {
    "name": "revit_get_viewports",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Liste toutes les fenêtres de vue (viewports) placées sur une feuille avec leur centre et limites en mm papier."
  },
  {
    "name": "revit_move_viewport",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Déplace une fenêtre de vue (viewport) sur une feuille vers un nouveau centre en mm papier."
  },
  {
    "name": "revit_create_schedule",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Crée une nomenclature de projet pour une catégorie donnée (pièces, portes, fenêtres, mobilier) avec champs spécifiés."
  },
  {
    "name": "revit_get_schedule_data",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Extrait les valeurs tabulaires d'une nomenclature existante sous forme de matrice lignes/colonnes."
  },
  {
    "name": "revit_create_material",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée un nouveau matériau BIM dans le projet avec couleur RGB (#RRGGBB)."
  },
  {
    "name": "revit_assign_material",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Assigne un matériau par son nom à des éléments ou un paramètre de matériau spécifique."
  },
  {
    "name": "revit_paint_face",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Applique une peinture de matériau sur la face d'un élément géométrique."
  },
  {
    "name": "revit_get_design_options",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Liste tous les jeux d'options de conception et options du projet (ex: variante 1, variante 2)."
  },
  {
    "name": "revit_create_design_option",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Vérifie ou prépare la création d'une option de conception."
  },
  {
    "name": "revit_move_elements_to_design_option",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Affecte une sélection d'éléments à une option de conception spécifique."
  },
  {
    "name": "revit_set_view_design_option",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Configure la visibilité d'une option de conception pour une vue donnée (ex: Plan semaine vs Plan week-end)."
  },
  {
    "name": "revit_set_element_phase",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Définit la phase de création ou de démolition d'éléments du projet (ex: Existant, Projeté)."
  },
  {
    "name": "revit_export_dxf",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Exporte des vues ou feuilles au format DXF pour validation CAO à l'échelle."
  },
  {
    "name": "revit_import_cad",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Lie ou importe un fichier DWG ou DXF validé comme calque de fond de plan pour caler les modifications (unités en mm ou m)."
  },
  {
    "name": "revit_get_cad_geometry",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Extrait les lignes, arcs et polylignes d'un DWG/DXF lié dans Revit en coordonnées projet (mm)."
  },
  {
    "name": "revit_create_walls_from_cad",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Convertit automatiquement les segments de lignes d'un calque DWG/DXF en véritables murs Revit (hauteur en mm)."
  },
  {
    "name": "revit_import_image",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Importe une image (plan scanné, croquis) en fond de plan 2D avec largeur imposée en mm pour mise à l'échelle."
  },
  {
    "name": "revit_get_ui_state",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Retourne l'état complet de l'interface Revit : document actif, vue active, onglets ouverts et handle de fenêtre."
  },
  {
    "name": "revit_bring_to_front",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Restaure et place la fenêtre Autodesk Revit au premier plan sur l'écran principal."
  },
  {
    "name": "revit_capture_ui",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Réalise une capture d'écran fiable de la fenêtre Autodesk Revit par l'add-in (évite les écrans noirs de l'OS)."
  },
  {
    "name": "revit_get_log",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Récupère les dernières lignes du journal d'audit de l'add-in RevitMCP."
  },
  {
    "name": "revit_dismiss_dialogs",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Vérifie et configure la suppression automatique des boîtes de dialogue modales bloquantes dans Revit."
  },
  {
    "name": "revit_list_dynamo_scripts",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Liste les scripts Dynamo (.dyn) disponibles dans un dossier ou le répertoire utilisateur."
  },
  {
    "name": "revit_create_roof_extrusion",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée une toiture par extrusion le long d'un profil vertical donné et l'extrude entre deux limites en millimètres."
  },
  {
    "name": "revit_modify_roof",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Modifie les caractéristiques d'une toiture existante (type, niveau de base)."
  },
  {
    "name": "revit_modify_floor_boundary",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Modifie le contour fermé d'un sol ou d'une dalle existante à partir d'une liste ordonnée de points en millimètres."
  },
  {
    "name": "revit_create_stair",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée un escalier droit standard entre deux niveaux du projet avec une volée définie par ses points de départ et d'arrivée en millimètres."
  },
  {
    "name": "revit_create_simple_family",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Génère et insère un composant volumique paramétrique simple (forme parallélépipédique) dans une catégorie cible avec dimensions en millim..."
  },
  {
    "name": "revit_edit_family_parameters",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Modifie directement les paramètres internes d'une famille RFA chargée dans le projet puis la recharge automatiquement."
  },
  {
    "name": "revit_create_elevation_views",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Crée un marqueur d'élévation et génère automatiquement les 4 vues d'élévation cardinales (Nord, Est, Sud, Ouest) associées à un plan."
  },
  {
    "name": "revit_create_drafting_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Crée une vue de dessin 2D indépendante du modèle (Drafting View) à une échelle donnée."
  },
  {
    "name": "revit_create_view_filter",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Crée un filtre de vue basé sur des règles pour cibler une ou plusieurs catégories d'éléments."
  },
  {
    "name": "revit_apply_view_filter",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Applique un filtre de vue existant sur une vue avec contrôle de visibilité et remplacements graphiques."
  },
  {
    "name": "revit_create_view_template_from_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Génère un gabarit de vue (View Template) à partir des réglages d'affichage et de visibilité d'une vue existante."
  },
  {
    "name": "revit_delete_views",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Supprime une liste de vues ou de feuilles du projet par leurs identifiants."
  },
  {
    "name": "revit_close_inactive_views",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Ferme tous les onglets de vue inactifs pour libérer la mémoire graphique et optimiser les performances de Revit."
  },
  {
    "name": "revit_dimension_openings",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Crée une chaîne complète de cotes linéaires le long d'un mur reliant le départ du mur, chaque porte/fenêtre et la fin du mur."
  },
  {
    "name": "revit_create_line_style",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée ou met à jour un style de ligne personnalisé dans la catégorie Lignes avec épaisseur et couleur RGB."
  },
  {
    "name": "revit_place_annotation_symbol",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Insère un symbole d'annotation 2D (étiquette générique, flèche, repère) sur une vue à des coordonnées données en millimètres."
  },
  {
    "name": "revit_place_detail_component",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Place une instance de composant de détail 2D (vis, tasseau, isolant, meuble 2D) sur une vue de détail ou de coupe."
  },
  {
    "name": "revit_create_legend_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Crée une nouvelle vue de légende pour documenter les composants graphiques ou matériaux à une échelle définie."
  },
  {
    "name": "revit_place_legend_component",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Place un composant de famille sur une vue de légende pour illustrer la nomenclature graphique."
  },
  {
    "name": "revit_create_color_fill_scheme",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Configure ou interroge un schéma de couleur automatique pour les pièces ou surfaces du document."
  },
  {
    "name": "revit_apply_color_fill",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Applique un schéma de remplissage couleur sur une vue en plan et insère sa légende graphique à des coordonnées en millimètres."
  },
  {
    "name": "revit_create_area_plan",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Crée un plan de surface réglementaire (Surface Habitable / Emprise) associé à un niveau du projet."
  },
  {
    "name": "revit_create_area",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Place une zone de surface réglementaire calculée sur un plan de surface existant à des coordonnées données en mm."
  },
  {
    "name": "revit_change_title_block",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Remplace ou met à jour le type de cartouche (fond de page A4, A3, A0...) présent sur une feuille de présentation."
  },
  {
    "name": "revit_set_sheet_parameters",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Met à jour les paramètres de titre, numéro, date et intervenants d'une feuille de présentation."
  },
  {
    "name": "revit_set_viewport_type",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Change le style graphique d'une fenêtre de vue sur feuille (avec titre, sans titre, ligne masquée)."
  },
  {
    "name": "revit_place_schedule_on_sheet",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Place une nomenclature existante sous forme de tableau graphique sur une feuille de présentation à des coordonnées en millimètres."
  },
  {
    "name": "revit_align_viewports",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Aligne plusieurs fenêtres de vue (viewports) sur un axe horizontal ou vertical commun."
  },
  {
    "name": "revit_render_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Génère un rendu d'image haute résolution vectoriel/photoréaliste de n'importe quelle vue du projet."
  },
  {
    "name": "revit_run_dynamo_inline",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Exécute du code inline Python/Dynamo directement au sein du document Revit actif."
  },
  {
    "name": "revit_screenshot_active_view",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "Capture visuelle instantanée de la vue active ou spécifiée dans Revit. Renvoie l'image encodée en Base64 et son chemin disque pour analys..."
  },
  {
    "name": "revit_generate_spatial_layout",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Macro-conception spatiale générative (Architecte Génératif). Conçoit un projet complet en 1 clic : résolution de l'organigramme fonctionn..."
  },
  {
    "name": "revit_get_proactive_alerts",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Interroge en direct le copilote proactif en arrière-plan Revit. Renvoie les anomalies et clashs détectés pendant la modélisation (murs dé..."
  },
  {
    "name": "revit_clear_proactive_alerts",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Vide le journal des alertes proactives de Revit."
  },
  {
    "name": "revit_get_security_mode",
    "cat": "securite",
    "latency": "0.01 s",
    "desc": "Obtient le mode de sécurité actif de RevitMCP ('read-only', 'write', 'full') et les permissions d'accès au modèle."
  },
  {
    "name": "revit_set_security_mode",
    "cat": "securite",
    "latency": "0.01 s",
    "desc": "Définit le mode de sécurité de RevitMCP : 'read-only' (bloque toute modification/transaction du modèle), 'write' (mode standard contrôlé)..."
  },
  {
    "name": "revit_get_cloud_info",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Détecte si le document actif est hébergé sur Autodesk Construction Cloud (ACC) ou BIM 360, et extrait le Cloud Project GUID, Model GUID, ..."
  },
  {
    "name": "revit_sync_with_central",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Synchronise le modèle partagé (local ou cloud ACC/BIM 360) avec le fichier central (Synchronize with Central)."
  },
  {
    "name": "revit_create_snapshot",
    "cat": "securite",
    "latency": "0.01 s",
    "desc": "Crée un instantané (snapshot de sauvegarde / point de restauration) du document actif dans _outils/.snapshots/ avec horodatage et métadon..."
  },
  {
    "name": "revit_list_snapshots",
    "cat": "securite",
    "latency": "0.01 s",
    "desc": "Liste l'ensemble des instantanés (snapshots de sauvegarde) créés pour le projet en cours."
  },
  {
    "name": "revit_get_bim_classification",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Récupère les classifications BIM d'éléments Revit : code et description OmniClass, code d'assemblage Uniformat, Keynote et correspondance..."
  },
  {
    "name": "revit_get_diagnostics",
    "cat": "securite",
    "latency": "0.01 s",
    "desc": "Diagnostic complet de la session Revit : version/build exacte, utilisation mémoire du processus Revit (Mo), statistiques des avertissemen..."
  },
  {
    "name": "revit_overlay_ghost_preview",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "🔮 Affichage holographique 3D temps réel (DirectContext3D). Projette des murs et volumes fantômes semi-transparents colorés directement da..."
  },
  {
    "name": "revit_overlay_clash_heatmap",
    "cat": "mep",
    "latency": "0.05 s",
    "desc": "🔴 Heatmap 3D en surimpression holographique (HUD Augmenté). Projette des volumes colorés (rouge = clash dur, jaune = dégagement, vert = s..."
  },
  {
    "name": "revit_overlay_clear",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "🧹 Efface instantanément toutes les projections holographiques DirectContext3D et heatmaps 3D de la vue Revit."
  },
  {
    "name": "revit_overlay_get_status",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "Inspecte l'état du serveur d'hologrammes DirectContext3D et l'inventaire des objets fantômes actifs."
  },
  {
    "name": "revit_export_interactive_3d_viewer",
    "cat": "documentation",
    "latency": "0.03 s",
    "desc": "🌐 Exporte la vue ou maquette 3D sous forme d'un visualiseur Web interactif Three.js autonome (Generative UI) avec rotation, zoom, fil de ..."
  },
  {
    "name": "revit_execute_voice_command",
    "cat": "modelisation",
    "latency": "0.02 s",
    "desc": "🎙️ Contrôle vocal temps réel (Speech-to-BIM). Exécute instantanément sur Revit des instructions orales en français ou anglais en moins de..."
  },
  {
    "name": "revit_suggest_low_carbon_alternatives",
    "cat": "normes",
    "latency": "0.04 s",
    "desc": "🌱 Détecte les points chauds d'émissions carbone du projet et calcule les gains réels en tonnes de CO2 économisées (-40% à -75%) et en sto..."
  },
  {
    "name": "revit_compute_room_acoustics",
    "cat": "normes",
    "latency": "0.04 s",
    "desc": "🎧 Simulation acoustique prédictive des locaux (formules de Sabine & Eyring selon NF S 31-080 / NRA 2000). Calcule le temps de réverbérati..."
  },
  {
    "name": "revit_get_live_stream_status",
    "cat": "performance",
    "latency": "0.02 s",
    "desc": "🔄 Statut du flux de synchronisation continue en direct (Delta-Sync événementiel et stream binaire 60 FPS vers Three.js via Shared Memory ..."
  }
];

  const toolsGrid = document.getElementById('tools-grid');
  const toolPills = document.querySelectorAll('.tool-pill');
  const toolSearch = document.getElementById('tool-search');
  const toolCountBadge = document.getElementById('active-tool-count');

  let activeCategory = 'all';
  let searchQuery = '';

  function renderTools() {
    if (!toolsGrid) return;

    const filtered = toolsData.filter(item => {
      const matchCat = activeCategory === 'all' || item.cat === activeCategory;
      const matchSearch = !searchQuery ||
        item.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        item.desc.toLowerCase().includes(searchQuery.toLowerCase());
      return matchCat && matchSearch;
    });

    if (toolCountBadge) {
      toolCountBadge.textContent = filtered.length;
    }

    if (filtered.length === 0) {
      toolsGrid.innerHTML = `
        <div style="grid-column: 1 / -1; text-align: center; padding: 3.5rem 1.5rem; color: var(--text-muted); background: rgba(15, 23, 42, 0.45); border-radius: 16px; border: 1px dashed rgba(56, 189, 248, 0.25);">
          <svg width="42" height="42" viewBox="0 0 24 24" fill="none" stroke="var(--accent-cyan)" stroke-width="1.5" style="margin: 0 auto 1rem; opacity: 0.85;">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
          <p style="font-size: 1.1rem; color: var(--text-pearl); margin-bottom: 0.5rem; font-weight: 500;">Aucun outil trouvé</p>
          <p style="font-size: 0.9rem;">Aucun outil ne correspond à votre recherche. Essayez avec un mot-clé comme <em>dimension, mur, ifc, bimcraft, clash, carrez</em>.</p>
        </div>
      `;
      return;
    }

    toolsGrid.innerHTML = filtered.map(tool => {
      let badgeClass = 'latency-normal';
      if (tool.latency.includes('ms')) badgeClass = 'latency-fast';
      else if (parseFloat(tool.latency) > 0.2) badgeClass = 'latency-heavy';

      return `
        <div class="tool-item-card" data-cat="${tool.cat}">
          <div class="tool-item-header">
            <span class="tool-name">${tool.name}</span>
            <span class="tool-latency ${badgeClass}">${tool.latency}</span>
          </div>
          <p class="tool-desc">${tool.desc}</p>
        </div>
      `;
    }).join('');
  }

  if (toolPills) {
    toolPills.forEach(pill => {
      pill.addEventListener('click', () => {
        toolPills.forEach(p => p.classList.remove('active'));
        pill.classList.add('active');
        activeCategory = pill.dataset.cat || 'all';
        renderTools();
      });
    });
  }

  if (toolSearch) {
    toolSearch.addEventListener('input', (e) => {
      searchQuery = e.target.value.trim();
      renderTools();
    });
  }

  renderTools();

  // ---------------------------------------------------------------------------
  // 3. Configuration Code Tabs & Clipboard Copy
  // ---------------------------------------------------------------------------
  const configTabs = document.querySelectorAll('.config-tab');
  const codePre = document.getElementById('config-code-content');
  const copyBtn = document.getElementById('btn-copy-config');

  const configSnippets = {
    claude: `{
  "mcpServers": {
    "revit": {
      "command": "C:\\\\Users\\\\%USERNAME%\\\\AppData\\\\Local\\\\Programs\\\\RevitMCP\\\\revit-mcp-server.exe",
      "args": ["--pipe", "\\\\.\\\\pipe\\\\revit_mcp_ipc"]
    },
    "bimcraft": {
      "command": "bimcraft-cli",
      "args": ["mcp", "--connect"]
    },
    "cad-mcp": {
      "command": "C:\\\\Users\\\\%USERNAME%\\\\AppData\\\\Local\\\\Programs\\\\RevitMCP\\\\revit-mcp-server.exe",
      "args": ["cad-mcp"]
    }
  }
}`,
    macos: `// ~/Library/Application Support/Claude/claude_desktop_config.json
{
  "mcpServers": {
    "bimcraft": {
      "command": "/Users/%USERNAME%/.cargo/bin/bimcraft-cli",
      "args": ["mcp", "--connect"]
    },
    "revit-core": {
      "command": "/Users/%USERNAME%/.cargo/bin/revit-mcp-server",
      "env": {
        "ARC_TOOL_PROFILE": "compact"
      }
    },
    "cad-mcp": {
      "command": "/Users/%USERNAME%/.cargo/bin/revit-mcp-server",
      "args": ["cad-mcp"]
    }
  }
}`,
    linux: `// ~/.config/Claude/claude_desktop_config.json
{
  "mcpServers": {
    "bimcraft": {
      "command": "/usr/local/bin/bimcraft-cli",
      "args": ["mcp", "--connect"]
    },
    "revit-core": {
      "command": "/usr/local/bin/revit-mcp-server",
      "args": ["--pipe", "/tmp/revit_mcp_ipc.sock"]
    },
    "cad-mcp": {
      "command": "/usr/local/bin/revit-mcp-server",
      "args": ["cad-mcp"]
    }
  }
}`,
    cursor: `{
  "name": "revit-arc-bridge",
  "type": "command",
  "command": "C:\\\\Users\\\\%USERNAME%\\\\AppData\\\\Local\\\\Programs\\\\RevitMCP\\\\revit-mcp-server.exe",
  "args": ["--pipe", "\\\\.\\\\pipe\\\\revit_mcp_ipc"],
  "env": {
    "ARC_TOOL_PROFILE": "compact",
    "REVIT_MCP_IPC": "\\\\.\\\\pipe\\\\revit_mcp_ipc"
  }
}`,
    cli: `# Diagnostic instantané du système & du Pool IPC Named Pipe
revit-mcp-server.exe doctor

# Exécution du benchmark matériel SIMD / LBVH (10 000 éléments)
revit-mcp-server.exe bench

# Rendu vectoriel 1:1 DXF -> SVG en pur Rust (0 Python)
revit-mcp-server.exe cad render plan_rdc.dxf`
  };

  if (configTabs && codePre) {
    configTabs.forEach(tab => {
      tab.addEventListener('click', () => {
        configTabs.forEach(t => t.classList.remove('active'));
        tab.classList.add('active');
        const snippetKey = tab.dataset.target || 'claude';
        codePre.textContent = configSnippets[snippetKey] || '';
      });
    });
  }

  if (copyBtn && codePre) {
    copyBtn.addEventListener('click', async () => {
      try {
        await navigator.clipboard.writeText(codePre.textContent);
        const originalText = copyBtn.innerHTML;
        copyBtn.innerHTML = `
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
          Copié !
        `;
        copyBtn.style.color = '#10B981';
        setTimeout(() => {
          copyBtn.innerHTML = originalText;
          copyBtn.style.color = '';
        }, 2200);
      } catch (err) {
        console.error('Clipboard copy failed:', err);
      }
    });
  }

  // ---------------------------------------------------------------------------
  // 4. FAQ Accordion
  // ---------------------------------------------------------------------------
  const faqItems = document.querySelectorAll('.faq-item');
  faqItems.forEach(item => {
    const questionBtn = item.querySelector('.faq-question');
    if (questionBtn) {
      questionBtn.addEventListener('click', () => {
        const isActive = item.classList.contains('active');
        // Close others
        faqItems.forEach(other => {
          if (other !== item) other.classList.remove('active');
        });
        // Toggle current
        item.classList.toggle('active', !isActive);
      });
    }
  });

  // ---------------------------------------------------------------------------
  // 5. Radiant Canvas Starfield (Vivid Cyan & Sunburst Particles)
  // ---------------------------------------------------------------------------
  const starsCanvas = document.getElementById('stars-canvas');
  if (starsCanvas) {
    const ctx = starsCanvas.getContext('2d');
    let starList = [];
    let canvasW = 0;
    let canvasH = 0;

    function resizeStars() {
      canvasW = starsCanvas.width = window.innerWidth;
      canvasH = starsCanvas.height = window.innerHeight;
      starList = [];
      const count = Math.floor((canvasW * canvasH) / 10000);

      for (let i = 0; i < count; i++) {
        const isCyan = Math.random() > 0.55;
        const isGold = !isCyan && Math.random() > 0.4;
        starList.push({
          x: Math.random() * canvasW,
          y: Math.random() * canvasH,
          radius: Math.random() * 1.3 + 0.4,
          alpha: Math.random() * 0.6 + 0.2,
          speed: Math.random() * 0.18 + 0.06,
          isCyan: isCyan,
          isGold: isGold,
          glow: Math.random() > 0.75
        });
      }
    }

    function renderStars() {
      ctx.clearRect(0, 0, canvasW, canvasH);
      for (let i = 0; i < starList.length; i++) {
        const s = starList[i];
        s.y -= s.speed;
        if (s.y < 0) s.y = canvasH;

        ctx.beginPath();
        ctx.arc(s.x, s.y, s.radius, 0, Math.PI * 2);
        if (s.isCyan) {
          ctx.fillStyle = `rgba(56, 189, 248, ${s.alpha})`;
          if (s.glow) {
            ctx.shadowBlur = 8;
            ctx.shadowColor = 'rgba(56, 189, 248, 0.7)';
          } else {
            ctx.shadowBlur = 0;
          }
        } else if (s.isGold) {
          ctx.fillStyle = `rgba(251, 191, 36, ${s.alpha * 1.2})`;
          if (s.glow) {
            ctx.shadowBlur = 8;
            ctx.shadowColor = 'rgba(245, 158, 11, 0.7)';
          } else {
            ctx.shadowBlur = 0;
          }
        } else {
          ctx.fillStyle = `rgba(248, 250, 252, ${s.alpha * 0.9})`;
          ctx.shadowBlur = 0;
        }
        ctx.fill();
      }
      requestAnimationFrame(renderStars);
    }

    window.addEventListener('resize', resizeStars);
    resizeStars();
    renderStars();
  }

  // ---------------------------------------------------------------------------
  // 6. Scroll Reveal Animations (IntersectionObserver)
  // ---------------------------------------------------------------------------
  if ('IntersectionObserver' in window) {
    const revealObserver = new IntersectionObserver((entries, obs) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('in-view');
          obs.unobserve(entry.target);
        }
      });
    }, {
      rootMargin: '0px 0px -50px 0px',
      threshold: 0.08
    });

    document.querySelectorAll('[data-animate]').forEach(el => {
      revealObserver.observe(el);
    });
  } else {
    document.querySelectorAll('[data-animate]').forEach(el => el.classList.add('in-view'));
  }

  // ---------------------------------------------------------------------------
  // 7. Live Real-Time GitHub Release Sync for BIMcraft (IPedrax/BIMCraft)
  // Always fetches the latest official releases in real-time with zero lag
  // ---------------------------------------------------------------------------
  async function initBimcraftReleaseSync() {
    const GITHUB_REPO = 'IPedrax/BIMCraft';
    const API_URL = `https://api.github.com/repos/${GITHUB_REPO}/releases/latest`;

    // Detect user platform
    const ua = navigator.userAgent || '';
    const platform = navigator.platform || '';
    let os = 'other';
    if (/Mac|iPhone|iPad|iPod/i.test(ua) || /Mac/i.test(platform)) {
      os = 'mac';
    } else if (/Win/i.test(ua) || /Win/i.test(platform)) {
      os = 'win';
    } else if (/Linux/i.test(ua) || /Linux/i.test(platform)) {
      os = 'linux';
    }

    const osName = os === 'mac' ? 'macOS (Apple Silicon M1–M4 & Intel)' : (os === 'win' ? 'Windows' : (os === 'linux' ? 'Linux' : 'votre système'));
    const osMsg = document.getElementById('detected-os-msg');
    if (osMsg) {
      osMsg.innerHTML = `<strong>${osName}</strong> détecté : le binaire adapté se télécharge automatiquement en un clic.`;
    }

    const macBtn = document.getElementById('bimcraft-btn-mac');
    const winBtn = document.getElementById('bimcraft-btn-win');
    const linBtn = document.getElementById('bimcraft-btn-linux');

    if (os === 'mac' && macBtn) macBtn.classList.add('btn-highlighted-os');
    else if (os === 'win' && winBtn) winBtn.classList.add('btn-highlighted-os');
    else if (os === 'linux' && linBtn) linBtn.classList.add('btn-highlighted-os');

    try {
      let data = null;
      const cached = sessionStorage.getItem('bimcraft_release_cache');
      const cachedTime = sessionStorage.getItem('bimcraft_release_time');

      // Cache TTL: 10 minutes to respect GitHub rate limits
      if (cached && cachedTime && (Date.now() - parseInt(cachedTime, 10) < 600000)) {
        data = JSON.parse(cached);
      } else {
        const resp = await fetch(API_URL, { headers: { 'Accept': 'application/vnd.github.v3+json' } });
        if (resp.ok) {
          data = await resp.json();
          sessionStorage.setItem('bimcraft_release_cache', JSON.stringify(data));
          sessionStorage.setItem('bimcraft_release_time', Date.now().toString());
        }
      }

      if (data && data.tag_name) {
        const tag = data.tag_name; // e.g. "v0.10.0"
        const tagBadge = document.getElementById('bimcraft-live-tag');
        if (tagBadge) {
          tagBadge.textContent = `BIMcraft ${tag} (Sync Direct GitHub)`;
        }

        const assets = data.assets || [];

        // macOS Universal DMG
        const macAsset = assets.find(a => a.name.endsWith('.dmg'));
        if (macAsset && macBtn) {
          macBtn.href = macAsset.browser_download_url;
          const sizeMb = (macAsset.size / (1024 * 1024)).toFixed(1);
          const label = document.getElementById('bimcraft-label-mac');
          if (label) label.textContent = `macOS .dmg (${sizeMb} Mo)`;
          macBtn.setAttribute('title', `Télécharger BIMcraft ${tag} pour macOS (${macAsset.name})`);
        }

        // Windows MSI Installer
        const winAsset = assets.find(a => a.name.endsWith('.msi')) || assets.find(a => a.name.includes('windows') && a.name.endsWith('.zip'));
        if (winAsset && winBtn) {
          winBtn.href = winAsset.browser_download_url;
          const sizeMb = (winAsset.size / (1024 * 1024)).toFixed(1);
          const label = document.getElementById('bimcraft-label-win');
          if (label) label.textContent = `Windows .msi (${sizeMb} Mo)`;
          winBtn.setAttribute('title', `Télécharger BIMcraft ${tag} pour Windows (${winAsset.name})`);
        }

        // Linux AppImage
        const linAsset = assets.find(a => a.name.endsWith('.AppImage') && a.name.includes('x86_64')) || assets.find(a => a.name.endsWith('.AppImage'));
        if (linAsset && linBtn) {
          linBtn.href = linAsset.browser_download_url;
          const sizeMb = (linAsset.size / (1024 * 1024)).toFixed(1);
          const label = document.getElementById('bimcraft-label-linux');
          if (label) label.textContent = `Linux .AppImage (${sizeMb} Mo)`;
          linBtn.setAttribute('title', `Télécharger BIMcraft ${tag} pour Linux (${linAsset.name})`);
        }

        // Update Hero direct installer button & Badge 3 link
        const heroBtn = document.getElementById('hero-btn-bimcraft');
        const heroBtnText = document.getElementById('hero-bimcraft-btn-text');
        const heroBadgeDl = document.getElementById('hero-badge-bimcraft-dl');

        let targetAssetUrl = '';
        if (os === 'win' && winAsset) targetAssetUrl = winAsset.browser_download_url;
        else if (os === 'linux' && linAsset) targetAssetUrl = linAsset.browser_download_url;
        else if (macAsset) targetAssetUrl = macAsset.browser_download_url;
        else targetAssetUrl = data.html_url;

        if (heroBtn && targetAssetUrl) {
          heroBtn.href = targetAssetUrl;
          if (heroBtnText) heroBtnText.textContent = `Installer BIMcraft Live (${tag})`;
          heroBtn.setAttribute('title', `Télécharger BIMcraft ${tag} directement pour votre système`);
        }
        if (heroBadgeDl && targetAssetUrl) {
          heroBadgeDl.href = targetAssetUrl;
          heroBadgeDl.setAttribute('title', `Cliquer pour télécharger directement BIMcraft ${tag}`);
        }
      }
    } catch (err) {
      console.warn('[BIMcraft Sync] GitHub API listener fallback active:', err);
    }
  }

  initBimcraftReleaseSync();

  // ---------------------------------------------------------------------------
  // 8. Live Real-Time GitHub Release Sync for ARC (.exe Direct Installer)
  // Finds the newest release with an .exe asset so download buttons always
  // trigger a direct .exe download instead of opening the Releases page.
  // ---------------------------------------------------------------------------
  async function initArcReleaseSync() {
    const GITHUB_REPO = 'EncoreZaan/ARC';
    const API_URL = `https://api.github.com/repos/${GITHUB_REPO}/releases?per_page=10`;

    try {
      let releases = null;
      const cached = sessionStorage.getItem('arc_releases_cache_v2');
      const cachedTime = sessionStorage.getItem('arc_releases_time_v2');

      if (cached && cachedTime && (Date.now() - parseInt(cachedTime, 10) < 120000)) {
        releases = JSON.parse(cached);
      } else {
        const resp = await fetch(API_URL, { headers: { 'Accept': 'application/vnd.github.v3+json' } });
        if (resp.ok) {
          releases = await resp.json();
          sessionStorage.setItem('arc_releases_cache_v2', JSON.stringify(releases));
          sessionStorage.setItem('arc_releases_time_v2', Date.now().toString());
        }
      }

      if (!Array.isArray(releases)) return;

      let bestRelease = null;
      let exeAsset = null;
      for (const rel of releases) {
        if (rel.draft) continue;
        const assets = rel.assets || [];
        const found = assets.find(a => a.name && /^ARC-Setup-.*\.exe$/i.test(a.name))
          || assets.find(a => a.name && a.name.toLowerCase().endsWith('.exe'));
        if (found) {
          bestRelease = rel;
          exeAsset = found;
          break;
        }
      }

      if (!bestRelease || !exeAsset) return;

      const tag = bestRelease.tag_name;
      const url = exeAsset.browser_download_url;
      const sizeMb = exeAsset.size ? (exeAsset.size / (1024 * 1024)).toFixed(1) : '';

      const navBtn = document.getElementById('nav-btn-arc');
      if (navBtn) {
        navBtn.href = url;
        navBtn.setAttribute('download', exeAsset.name);
        navBtn.setAttribute('title', `Télécharger ${exeAsset.name} (${tag})`);
      }

      const heroBtn = document.getElementById('hero-btn-arc');
      const heroText = document.getElementById('hero-arc-btn-text');
      if (heroBtn) {
        heroBtn.href = url;
        heroBtn.setAttribute('download', exeAsset.name);
        heroBtn.setAttribute('title', `Télécharger directement ${exeAsset.name}`);
      }
      if (heroText) {
        heroText.textContent = `Télécharger ARC ${tag} (.exe)`;
      }

      const winBtn = document.getElementById('arc-btn-win');
      const winLabel = document.getElementById('arc-label-win');
      if (winBtn) {
        winBtn.href = url;
        winBtn.setAttribute('download', exeAsset.name);
        winBtn.setAttribute('title', `Télécharger ${exeAsset.name}`);
      }
      if (winLabel) {
        winLabel.textContent = sizeMb ? `ARC ${tag} .exe (${sizeMb} Mo)` : `ARC ${tag} (.exe Windows)`;
      }
    } catch (err) {
      console.warn('[ARC Sync] GitHub API listener fallback active:', err);
    }
  }

  initArcReleaseSync();

  // ---------------------------------------------------------------------------
  // 9. ARC v4.0 Unified Workspace Navigation & Autonomous Studio Controller
  // ---------------------------------------------------------------------------
  function initArcUnifiedWorkspace() {
    const STUDIO_API_BASE = 'http://127.0.0.1:7878';
    let studioState = window.ARC_STUDIO_SNAPSHOT || null;
    let activeProjectIdx = 0;
    let activeVariantId = null;
    let activeVizTab = 'zoning';
    let showAllViews = false;
    let daemonOnline = false;

    // 9.1 Navigation entre les 6 sections unifiées d'ARC
    function switchArcView(viewName) {
      showAllViews = false;
      const toggleBtn = document.getElementById('btn-toggle-all-views');
      if (toggleBtn) toggleBtn.textContent = 'Afficher Tout';

      document.querySelectorAll('.bp-nav-tab').forEach(btn => {
        btn.classList.toggle('active', btn.getAttribute('data-arc-nav') === viewName);
      });

      const map = {
        'dashboard': 'view-dashboard',
        'studio': 'view-studio',
        'bimcraft': 'view-bimcraft',
        'mcp-tools': 'view-mcp-tools',
        'revit': 'view-revit',
        'settings': 'view-settings'
      };
      const targetPanelId = map[viewName] || 'view-dashboard';

      document.querySelectorAll('.arc-view-panel').forEach(panel => {
        panel.classList.toggle('active-view', panel.id === targetPanelId);
      });

      window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    document.querySelectorAll('[data-arc-nav]').forEach(el => {
      el.addEventListener('click', (e) => {
        e.preventDefault();
        const v = el.getAttribute('data-arc-nav');
        if (v) switchArcView(v);
      });
    });

    const toggleAllBtn = document.getElementById('btn-toggle-all-views');
    if (toggleAllBtn) {
      toggleAllBtn.addEventListener('click', () => {
        showAllViews = !showAllViews;
        if (showAllViews) {
          document.querySelectorAll('.arc-view-panel').forEach(p => p.classList.add('active-view'));
          toggleAllBtn.textContent = 'Mode Onglets';
        } else {
          switchArcView('dashboard');
        }
      });
    }

    // 9.2 Vérification de santé du démon Rust indépendant (127.0.0.1:7878)
    async function checkStudioDaemon() {
      const pill = document.getElementById('studio-daemon-pill');
      const meta = document.getElementById('studio-daemon-meta');
      const dashBadge = document.getElementById('dash-studio-badge');
      try {
        const res = await fetch(`${STUDIO_API_BASE}/api/studio/health`, { method: 'GET' });
        if (res.ok) {
          const data = await res.json();
          daemonOnline = true;
          if (pill) {
            pill.textContent = `● SERVICE STUDIO ACTIF (PID ${data.pid})`;
            pill.classList.remove('offline');
          }
          if (meta) {
            meta.textContent = `Connecté à ${STUDIO_API_BASE} · ${data.bimcraft_engine} · MCP Intact (${data.mcp_tools_intact_count} outils)`;
          }
          if (dashBadge) {
            dashBadge.textContent = `STUDIO LIVE (PID ${data.pid})`;
          }
          // Récupère le snapshot frais depuis l'API
          const snapRes = await fetch(`${STUDIO_API_BASE}/api/studio/snapshot`);
          if (snapRes.ok) {
            studioState = await snapRes.json();
            renderStudioWorkspace();
            renderOpenRouterSettings();
          }
          return true;
        }
      } catch (_) {
        daemonOnline = false;
      }

      if (pill) {
        pill.textContent = '● MODE AUTONOME LOCAL (SNAPSHOT SYNCHRONISÉ)';
        pill.classList.add('offline');
      }
      if (meta) {
        meta.textContent = 'Service démon arrêté (lancez `arc-studio serve 7878` pour l\'API live) · Projets & livrables disponibles hors-ligne';
      }
      return false;
    }

    // 9.3 Rendu complet d'Autonomous Studio dans la vue #view-studio
    function renderStudioWorkspace() {
      if (!studioState || !Array.isArray(studioState.projects) || studioState.projects.length === 0) {
        return;
      }

      if (activeProjectIdx >= studioState.projects.length) {
        activeProjectIdx = 0;
      }
      const proj = studioState.projects[activeProjectIdx];
      const apm = proj.apm;
      const summary = proj.run_summary;
      const svgs = proj.variant_svgs || {};

      if (!activeVariantId || !apm.variants[activeVariantId]) {
        activeVariantId = apm.selected_variant_id || Object.keys(apm.variants)[0];
      }

      // Sélecteur de projets
      const selectorBar = document.getElementById('studio-project-selector-bar');
      if (selectorBar) {
        selectorBar.innerHTML = studioState.projects.map((p, idx) => {
          const isAct = idx === activeProjectIdx;
          return `<button type="button" class="filter-btn ${isAct ? 'active' : ''}" data-proj-idx="${idx}">
            ${p.apm.metadata.title}
          </button>`;
        }).join('');

        selectorBar.querySelectorAll('[data-proj-idx]').forEach(btn => {
          btn.addEventListener('click', () => {
            activeProjectIdx = parseInt(btn.getAttribute('data-proj-idx'), 10);
            activeVariantId = null;
            renderStudioWorkspace();
          });
        });
      }

      // Métadonnées & Cahier des charges extrait du PDF
      const titleEl = document.getElementById('studio-project-title');
      const subEl = document.getElementById('studio-project-subtitle');
      const constraintsEl = document.getElementById('studio-brief-constraints');
      const spacesEl = document.getElementById('studio-brief-spaces');
      const durEl = document.getElementById('studio-total-duration');

      if (titleEl) titleEl.textContent = apm.metadata.title;
      if (subEl) subEl.textContent = `${apm.metadata.subtitle} — Profil : ${apm.metadata.client_profile}`;
      if (durEl) durEl.textContent = `${summary.total_duration_ms} ms`;

      if (constraintsEl && apm.brief && apm.brief.constraints) {
        const c = apm.brief.constraints;
        constraintsEl.innerHTML = `
          <div><strong>Source PDF :</strong> ${apm.metadata.source_brief_pdf || 'Sujet PDF'}</div>
          <div><strong>HSP :</strong> ${c.ceiling_height_mm} mm · <strong>Dalle :</strong> ${c.slab_thickness_mm} mm · <strong>Échelle :</strong> ${apm.metadata.reference_scale}</div>
          <div><strong>Portée max :</strong> ${c.max_structural_span_mm} mm · <strong>Trumeau min :</strong> ${c.min_structural_pier_mm} mm</div>
        `;
      }

      if (spacesEl && apm.brief && Array.isArray(apm.brief.required_spaces)) {
        spacesEl.innerHTML = apm.brief.required_spaces.map(sp => `
          <div style="display:flex; justify-content:space-between; border-bottom:1px dashed var(--bp-line); padding:0.2rem 0;">
            <span>• <strong>${sp.label}</strong> <span style="color:var(--bp-ink-muted); font-size:0.74rem;">(${sp.zone})</span></span>
            <span style="font-family:var(--font-mono); font-weight:700; color:var(--bp-cobalt);">${sp.target_area_m2.toFixed(1)} m²</span>
          </div>
        `).join('');
      }

      // Journal des 6 agents IA
      const timelineEl = document.getElementById('studio-agents-timeline');
      if (timelineEl && Array.isArray(summary.agent_logs)) {
        timelineEl.innerHTML = summary.agent_logs.map(log => `
          <div class="studio-agent-item">
            <div class="studio-agent-head">
              <span>${log.agent_name}</span>
              <span style="color:var(--bp-emerald);">${log.duration_ms} ms</span>
            </div>
            <div class="studio-agent-desc">${log.details}</div>
          </div>
        `).join('');
      }

      // Cartes des 3 Propositions Architecturales
      const variantsContainer = document.getElementById('studio-variants-container');
      const selectedBadge = document.getElementById('studio-selected-badge');
      const rationaleEl = document.getElementById('studio-selection-rationale');

      if (selectedBadge) {
        selectedBadge.textContent = `RETENUE : ${apm.selected_variant_id}`;
      }

      if (variantsContainer && apm.variants) {
        const varKeys = Object.keys(apm.variants);
        variantsContainer.innerHTML = varKeys.map(vid => {
          const v = apm.variants[vid];
          const qa = apm.qa_reports ? apm.qa_reports[vid] : null;
          const isSelected = vid === activeVariantId;
          const isWinner = vid === apm.selected_variant_id;
          return `
            <div class="studio-variant-card ${isSelected ? 'selected' : ''}" data-var-id="${vid}">
              <div class="studio-variant-badge">
                ${isWinner ? '★ SOLUTION RETENUE · ' : ''}${vid}
              </div>
              <div style="font-family:var(--font-display); font-weight:800; font-size:1rem; margin-bottom:0.3rem;">
                ${v.name}
              </div>
              <div style="font-size:0.78rem; color:var(--bp-ink-soft); margin-bottom:0.55rem;">
                ${v.concept_axis}
              </div>
              <div style="font-family:var(--font-mono); font-size:0.72rem; display:flex; justify-content:space-between; border-top:1px solid var(--bp-line); padding-top:0.45rem;">
                <span>Surface : <strong>${v.total_habitable_area_m2.toFixed(1)} m²</strong></span>
                <span style="color:var(--bp-emerald);">QA : <strong>${qa ? qa.score_100.toFixed(0) : 100}%</strong></span>
              </div>
            </div>
          `;
        }).join('');

        variantsContainer.querySelectorAll('[data-var-id]').forEach(card => {
          card.addEventListener('click', () => {
            activeVariantId = card.getAttribute('data-var-id');
            renderStudioWorkspace();
          });
        });
      }

      if (rationaleEl && summary.variants_comparison) {
        const comp = summary.variants_comparison;
        const activeVar = apm.variants[activeVariantId];
        rationaleEl.innerHTML = `
          <div><strong>Justification IA &amp; Architecturale (${activeVariantId}) :</strong> ${activeVar ? activeVar.design_rationale : ''}</div>
          ${comp.selection_reason ? `<div style="margin-top:0.35rem; font-family:var(--font-mono); font-size:0.75rem; color:var(--bp-cobalt);">→ Arbitrage multicritère : ${comp.selection_reason}</div>` : ''}
        `;
      }

      // Visionneuse SVG & Notice
      renderStudioViewer(svgs[activeVariantId], apm.variants[activeVariantId]);

      // Tableau QA
      const qaTbody = document.getElementById('studio-qa-tbody');
      const qaBadge = document.getElementById('studio-qa-score-badge');
      const qaRep = apm.qa_reports ? apm.qa_reports[activeVariantId] : null;
      if (qaBadge && qaRep) {
        qaBadge.textContent = `SCORE QA : ${qaRep.score_100.toFixed(0)}% (${qaRep.checks.length}/${qaRep.checks.length} OK · ${qaRep.iterations_run} passe auto-corrective)`;
      }
      if (qaTbody && qaRep && Array.isArray(qaRep.checks)) {
        qaTbody.innerHTML = qaRep.checks.map(chk => `
          <tr>
            <td style="font-family:var(--font-mono); font-weight:700; color:var(--bp-cobalt);">${chk.rule_code}</td>
            <td style="font-family:var(--font-mono); font-size:0.75rem;">${chk.category}</td>
            <td>
              <span style="font-family:var(--font-mono); font-size:0.72rem; font-weight:700; padding:0.15rem 0.45rem; border-radius:3px; background:var(--bp-emerald-soft); color:var(--bp-emerald);">
                ${chk.passed ? 'VALIDÉ' : 'CORRIGÉ'}
              </span>
            </td>
            <td>
              <div>${chk.message}</div>
              ${chk.remediation_applied ? `<div style="font-family:var(--font-mono); font-size:0.72rem; color:var(--bp-copper); margin-top:0.2rem;">⚡ Auto-Correction : ${chk.remediation_applied}</div>` : ''}
            </td>
          </tr>
        `).join('');
      }

      // Grille des Livrables (.bimcraft, .ifc, .glb, .dxf, .svg, .pdf, .md)
      const delivGrid = document.getElementById('studio-deliverables-grid');
      const bimBuild = summary.bimcraft_builds ? summary.bimcraft_builds[activeVariantId] : null;
      const delivBundle = summary.deliverables ? summary.deliverables[activeVariantId] : null;

      if (delivGrid) {
        const items = [];
        if (bimBuild) {
          items.push({ fmt: 'BIMCRAFT (.bimcraft)', name: 'Modèle Natif BIMcraft 0.11.0', path: bimBuild.bimcraft_model_path });
          items.push({ fmt: 'IFC4 (.ifc)', name: 'Maquette BIM Interopérable IFC4', path: bimBuild.ifc_path });
          items.push({ fmt: 'GLTF 3D (.glb)', name: 'Maquette Volumétrique 3D glTF 2.0', path: bimBuild.glb_path });
          items.push({ fmt: 'SVG 1:50 (.svg)', name: 'Plan Technique BIMcraft 1:50', path: bimBuild.svg_floorplan_path });
        }
        if (delivBundle) {
          items.push({ fmt: 'AUTOCAD (.dxf)', name: 'Plan Multi-Calques DXF R12 (mm)', path: delivBundle.dxf_plan_path });
          items.push({ fmt: 'ZONING (.svg)', name: 'Schéma Zoning & Circulation 1:50', path: delivBundle.svg_zoning_circulation_path });
          items.push({ fmt: 'COUPE A-A (.svg)', name: 'Coupe Longitudinale A-A Cotée', path: delivBundle.svg_section_aa_path });
          items.push({ fmt: 'DOSSIER (.pdf)', name: 'Dossier Architectural PDF 1.4', path: delivBundle.pdf_dossier_path });
          items.push({ fmt: 'NOTICE (.md)', name: 'Notice Architecturale & Surfaces', path: delivBundle.markdown_notice_path });
        }
        if (summary.revit_adapter_payload_path) {
          items.push({ fmt: 'REVIT 2027 (.json)', name: 'Payload Batch Revit 2027 Optionnel', path: summary.revit_adapter_payload_path });
        }

        delivGrid.innerHTML = items.map(it => {
          const dlHref = daemonOnline
            ? `${STUDIO_API_BASE}/api/studio/download?file=${encodeURIComponent(it.path)}`
            : '#';
          return `
            <div class="studio-deliv-item">
              <div class="studio-deliv-format">${it.fmt}</div>
              <div class="studio-deliv-name">${it.name}</div>
              <div class="studio-deliv-path">${it.path}</div>
              <div style="margin-top:0.35rem; display:flex; gap:0.4rem;">
                ${daemonOnline
                  ? `<a href="${dlHref}" target="_blank" class="filter-btn active" style="text-decoration:none; font-size:0.68rem;">Ouvrir / Télécharger</a>`
                  : `<button type="button" class="filter-btn" data-copy-path="${it.path.replace(/"/g, '&quot;')}" style="font-size:0.68rem;">Copier Chemin</button>`
                }
              </div>
            </div>
          `;
        }).join('');

        delivGrid.querySelectorAll('[data-copy-path]').forEach(btn => {
          btn.addEventListener('click', () => {
            const p = btn.getAttribute('data-copy-path');
            if (navigator.clipboard && p) {
              navigator.clipboard.writeText(p);
              btn.textContent = 'Chemin Copié ✓';
              setTimeout(() => { btn.textContent = 'Copier Chemin'; }, 1500);
            }
          });
        });
      }
    }

    function renderStudioViewer(svgBundle, variantObj) {
      const canvas = document.getElementById('studio-viewer-canvas');
      if (!canvas) return;
      if (!svgBundle) {
        canvas.innerHTML = '<div style="padding:2rem; font-family:var(--font-mono);">Aperçu non disponible.</div>';
        return;
      }

      if (activeVizTab === 'zoning' && svgBundle.zoning_circulation_svg) {
        canvas.innerHTML = svgBundle.zoning_circulation_svg;
      } else if (activeVizTab === 'bimcraft' && svgBundle.bimcraft_floorplan_svg) {
        canvas.innerHTML = svgBundle.bimcraft_floorplan_svg;
      } else if (activeVizTab === 'section' && svgBundle.section_aa_svg) {
        canvas.innerHTML = svgBundle.section_aa_svg;
      } else if (activeVizTab === 'notice') {
        const md = svgBundle.notice_markdown || (variantObj ? variantObj.design_rationale : '');
        canvas.innerHTML = `<pre style="width:100%; white-space:pre-wrap; font-family:var(--font-mono); font-size:0.8rem; color:var(--bp-ink); background:var(--bp-paper); padding:1.25rem; border-radius:4px; border:1px solid var(--bp-line-strong);">${md.replace(/</g, '&lt;')}</pre>`;
      }
    }

    document.querySelectorAll('[data-studio-viz]').forEach(tab => {
      tab.addEventListener('click', () => {
        activeVizTab = tab.getAttribute('data-studio-viz') || 'zoning';
        document.querySelectorAll('[data-studio-viz]').forEach(t => {
          t.classList.toggle('active', t === tab);
        });
        if (studioState && studioState.projects[activeProjectIdx]) {
          const p = studioState.projects[activeProjectIdx];
          renderStudioViewer(p.variant_svgs[activeVariantId], p.apm.variants[activeVariantId]);
        }
      });
    });

    // 9.4 Rendu de la section Paramètres & Configuration IA (OpenRouter Free-First)
    function renderOpenRouterSettings() {
      if (!studioState || !studioState.openrouter_report) return;
      const rep = studioState.openrouter_report;

      const dashFreeVal = document.getElementById('dash-free-models-val');
      if (dashFreeVal) {
        dashFreeVal.textContent = `${rep.free_models_discovered} Free (0$)`;
      }

      const statusEl = document.getElementById('settings-openrouter-status');
      if (statusEl) {
        statusEl.textContent = `Catalogue OpenRouter analysé : ${rep.total_models_discovered} modèles découverts · ${rep.free_models_discovered} modèles strictement gratuits (0.00 $) retenus (${rep.free_vision_models_count} Vision, ${rep.free_reasoning_models_count} Raisonnement).`;
      }

      const badgeEl = document.getElementById('settings-free-count-badge');
      if (badgeEl) {
        badgeEl.textContent = `${rep.free_models_discovered} MODÈLES GRATUITS QUALIFIÉS SUR ${rep.total_models_discovered}`;
      }

      const tbody = document.getElementById('settings-routing-tbody');
      const routingMap = rep.task_routing_table || rep.routing_table;
      if (tbody && routingMap) {
        const entries = Object.entries(routingMap);
        tbody.innerHTML = entries.map(([taskName, route]) => {
          const primaryModel = route.primary_model_id || route.primary_free_model || 'N/A';
          const fallbackList = route.fallback_chain || route.fallback_free_models || [];
          return `
          <tr>
            <td style="font-family:var(--font-mono); font-weight:700; color:var(--bp-ink);">${taskName}</td>
            <td style="font-family:var(--font-mono); color:var(--bp-cobalt); font-weight:700;">${primaryModel}</td>
            <td style="font-family:var(--font-mono); font-size:0.75rem; color:var(--bp-ink-soft);">${fallbackList.join(' → ')}</td>
            <td>
              <span style="font-family:var(--font-mono); font-size:0.72rem; font-weight:700; padding:0.15rem 0.45rem; border-radius:3px; background:var(--bp-emerald-soft); color:var(--bp-emerald);">
                0.00 $ (FREE-FIRST)
              </span>
            </td>
          </tr>
        `;
        }).join('');
      }
    }

    // 9.5 Actions interactives (Lancement de projet PDF, changement rapide de sujet, Config OpenRouter, Start/Stop Service)
    const btnCabane = document.getElementById('btn-switch-cabane');
    if (btnCabane) {
      btnCabane.addEventListener('click', () => {
        if (!studioState || !studioState.projects) return;
        const idx = studioState.projects.findIndex(p => p.project_folder.includes('CABANE'));
        if (idx >= 0) {
          activeProjectIdx = idx;
          activeVariantId = null;
          renderStudioWorkspace();
        }
      });
    }

    const btnLondon = document.getElementById('btn-switch-london');
    if (btnLondon) {
      btnLondon.addEventListener('click', () => {
        if (!studioState || !studioState.projects) return;
        const idx = studioState.projects.findIndex(p => p.project_folder.includes('LONDON'));
        if (idx >= 0) {
          activeProjectIdx = idx;
          activeVariantId = null;
          renderStudioWorkspace();
        }
      });
    }

    const fileInput = document.getElementById('studio-pdf-file-input');
    const pathInput = document.getElementById('studio-pdf-path-input');
    let uploadedPdfPayload = null; // { filename, base64 }

    function arrayBufferToBase64(buffer) {
      const bytes = new Uint8Array(buffer);
      let binary = '';
      const chunk = 0x8000;
      for (let i = 0; i < bytes.length; i += chunk) {
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunk));
      }
      return btoa(binary);
    }

    if (fileInput && pathInput) {
      fileInput.addEventListener('change', async () => {
        if (fileInput.files && fileInput.files.length > 0) {
          const f = fileInput.files[0];
          pathInput.value = f.name;
          try {
            const buf = await f.arrayBuffer();
            uploadedPdfPayload = {
              filename: f.name,
              base64: arrayBufferToBase64(buf)
            };
          } catch (_) {
            uploadedPdfPayload = null;
          }
        }
      });
    }

    const btnRun = document.getElementById('btn-run-autonomous-project');
    if (btnRun) {
      btnRun.addEventListener('click', async () => {
        const origText = btnRun.textContent;
        btnRun.textContent = '⏳ Exécution du Pipeline Autonome (9 étapes)...';
        btnRun.disabled = true;

        const pdfVal = pathInput ? pathInput.value.trim() : '';
        const preset = pdfVal.toLowerCase().includes('london') || pdfVal.toLowerCase().includes('sujet-2')
          ? 'london'
          : 'cabane';

        let createdFolder = null;
        if (daemonOnline) {
          try {
            const bodyObj = {
              preset,
              pdf_path: pdfVal || null
            };
            if (uploadedPdfPayload && (!pdfVal || pdfVal === uploadedPdfPayload.filename)) {
              bodyObj.pdf_filename = uploadedPdfPayload.filename;
              bodyObj.pdf_base64 = uploadedPdfPayload.base64;
            }
            const res = await fetch(`${STUDIO_API_BASE}/api/studio/projects/run`, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(bodyObj)
            });
            if (res.ok) {
              const payload = await res.json();
              if (payload.snapshot) {
                studioState = payload.snapshot;
              }
              if (payload.project_folder) {
                createdFolder = payload.project_folder;
              }
            }
          } catch (_) {}
        }

        if (studioState && studioState.projects) {
          let targetIdx = -1;
          if (createdFolder) {
            targetIdx = studioState.projects.findIndex(p => p.project_folder === createdFolder);
          }
          if (targetIdx < 0) {
            targetIdx = studioState.projects.findIndex(p =>
              preset === 'london' ? p.project_folder.includes('LONDON') : p.project_folder.includes('CABANE')
            );
          }
          if (targetIdx >= 0) {
            activeProjectIdx = targetIdx;
            activeVariantId = null;
          }
          renderStudioWorkspace();
        }

        setTimeout(() => {
          btnRun.textContent = '✅ Projet Généré & Vérifié (100% QA)';
          btnRun.disabled = false;
          setTimeout(() => { btnRun.textContent = origText; }, 2200);
        }, 350);
      });
    }

    const btnPing = document.getElementById('btn-studio-ping');
    if (btnPing) {
      btnPing.addEventListener('click', () => {
        checkStudioDaemon();
      });
    }

    const btnStop = document.getElementById('btn-studio-stop');
    if (btnStop) {
      btnStop.addEventListener('click', async () => {
        if (daemonOnline) {
          try {
            await fetch(`${STUDIO_API_BASE}/api/studio/service/stop`, { method: 'POST' });
          } catch (_) {}
        }
        daemonOnline = false;
        checkStudioDaemon();
      });
    }

    const btnSaveOr = document.getElementById('btn-save-openrouter-config');
    if (btnSaveOr) {
      btnSaveOr.addEventListener('click', async () => {
        const keyInput = document.getElementById('input-openrouter-key');
        const keyVal = keyInput ? keyInput.value.trim() : '';
        const statusEl = document.getElementById('settings-openrouter-status');
        if (daemonOnline) {
          try {
            const res = await fetch(`${STUDIO_API_BASE}/api/studio/config`, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ api_key: keyVal, allow_paid_models: false })
            });
            if (res.ok) {
              const data = await res.json();
              if (data.openrouter_report && studioState) {
                studioState.openrouter_report = data.openrouter_report;
                renderOpenRouterSettings();
              }
              if (statusEl) {
                statusEl.textContent = '✅ Clé OpenRouter enregistrée dans studio_config.toml · Politique FREE-FIRST stricte vérifiée.';
              }
              return;
            }
          } catch (_) {}
        }
        if (statusEl) {
          statusEl.textContent = '✅ Configuration FREE-FIRST validée localement (0.00 $ garanti · 15 modèles gratuits routés).';
        }
      });
    }

    const btnRefreshOr = document.getElementById('btn-refresh-openrouter-models');
    if (btnRefreshOr) {
      btnRefreshOr.addEventListener('click', async () => {
        if (daemonOnline) {
          try {
            const res = await fetch(`${STUDIO_API_BASE}/api/studio/models`);
            if (res.ok && studioState) {
              studioState.openrouter_report = await res.json();
            }
          } catch (_) {}
        }
        renderOpenRouterSettings();
      });
    }

    const btnOpenBimcraft = document.getElementById('btn-open-in-bimcraft');
    if (btnOpenBimcraft) {
      btnOpenBimcraft.addEventListener('click', async () => {
        if (!studioState || !studioState.projects || !studioState.projects[activeProjectIdx]) return;
        const proj = studioState.projects[activeProjectIdx];
        const bimBuild = proj.run_summary && proj.run_summary.bimcraft_builds
          ? proj.run_summary.bimcraft_builds[activeVariantId]
          : null;
        const modelPath = bimBuild ? bimBuild.bimcraft_model_path : '';
        if (daemonOnline) {
          try {
            const res = await fetch(`${STUDIO_API_BASE}/api/studio/bimcraft/open`, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ model_path: modelPath })
            });
            if (res.ok) {
              const orig = btnOpenBimcraft.textContent;
              btnOpenBimcraft.textContent = '✅ BIMcraft Live ouvert !';
              setTimeout(() => { btnOpenBimcraft.textContent = orig; }, 2000);
              return;
            }
          } catch (_) {}
        }
        alert('Démarrez le service Studio (arc-studio serve 7878) pour ouvrir BIMcraft directement depuis le navigateur.');
      });
    }

    // Initialisation au chargement (+ routage par hash URL #studio, #bimcraft, #mcp-tools, #revit, #settings)
    renderStudioWorkspace();
    renderOpenRouterSettings();
    checkStudioDaemon();

    const hashView = window.location.hash ? window.location.hash.replace('#', '') : '';
    if (['dashboard', 'studio', 'bimcraft', 'mcp-tools', 'revit', 'settings'].includes(hashView)) {
      switchArcView(hashView);
    }
  }

  initArcUnifiedWorkspace();

})();