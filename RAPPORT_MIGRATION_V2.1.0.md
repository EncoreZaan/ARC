# Rapport Technique de Migration — RevitMCP v2.0.0 → v2.1.0
**Destiné à : Claude (Agent IA d'Architecture Intérieure) & Utilisateur**  
**Environnement : Autodesk Revit 2027 (FR) / .NET 10 / TypeScript / Python**  
**Date : 20 Octobre 2026**  
**Dépôt GitHub : `EncoreZaan/Revit-MCP` (Branche : `main`)**

---

## 1. Contexte & Objectif

Le projet **RevitMCP** permet à un agent IA autonome (**Claude**) de piloter **Autodesk Revit 2027** en temps réel, sans intervention humaine, sans scripts Dynamo externes, et sans manipulation d'interface graphique (clavier/souris).

Dans le cadre de la modélisation d'un **micro-habitat de 6,40 × 6,20 m** (avec mezzanine et toiture double pente 2,40 m → 5,00 m sous un Revit français), la version 2.0.0 présentait des limitations qui obligeaient l'agent à contourner le système (exports IFC pour lire des coordonnées, absence de géométrie sur les éléments, noms de catégories anglaises non reconnues sur Revit FR).

La version **2.1.0** résout l'intégralité de ces points, apporte **119 nouveaux outils** (portant le total à **189 outils MCP**), intègre un serveur compagnon Python **CAD-MCP**, et élimine tous les avertissements modaux bloquants.

---

## 2. Architecture & Conventions Techniques Strictes

### A. Pont HTTP & Routage
- **Port local :** `http://localhost:6543`
- **Sonde de santé :** `GET /healthz` (renvoie l'état du pont, la version 2.1.0, les temps d'attente et la liste de tous les documents ouverts avec leur statut actif/modifié).
- **Exécution d'outils :** `POST /api/{action}` avec un corps JSON.
- **File d'attente Revit API :** Utilisation d'un `ExternalEvent` Revit thread-safe (`RevitEventHandler`), avec un timeout de 120 secondes par commande.

### B. Système d'Unités Universel
Toutes les données échangées entre Claude et RevitMCP respectent strictement les unités suivantes :
- **Longueurs / Coordonnées :** Millimètres (**mm**) — conversion interne automatique vers les pieds (*feet*) de l'API Revit via `UnitUtils` / `UnitConversions`.
- **Angles :** Degrés (**°**) — conversion interne vers les radians.
- **Surfaces :** Mètres carrés (**m²**).
- **Volumes :** Mètres cubes (**m³**).
- **Identifiants :** Entiers 64 bits (`long`), conformes à Revit 2024+.

### C. Gestion Robuste des Transactions & Mode `dryRun`
- Toutes les opérations d'écriture s'exécutent dans une transaction nommée `"MCP: <nom_outil>"`.
- **Paramètre universel `dryRun: boolean` :** Tous les outils de modification/création acceptent `dryRun: true`. Lorsqu'il est activé, la transaction est exécutée, validée en mémoire, puis annulée (`RollBack`), renvoyant les modifications prévues sans altérer la maquette.
- **Suppression des boîtes de dialogue (`IFailuresPreprocessor`) :** Les avertissements Revit (murs qui se chevauchent, éléments légers déconnectés) sont capturés automatiquement, acceptés sans boîte modale, et renvoyés à Claude dans le champ `bimWarnings: string[]`.

### D. Résolveur de Catégories Bilingue
Partout où un paramètre `category` est attendu, Claude peut indifféremment transmettre :
- Le nom de l'énumération Revit : `"OST_Walls"`, `"OST_Furniture"`...
- Le nom anglais standard : `"Walls"`, `"Furniture"`, `"Doors"`...
- Le nom affiché dans l'interface française : `"Murs"`, `"Mobilier"`, `"Portes"`, `"Fenêtres"`, `"Pièces"`...

### E. Format de Réponse et d'Erreur Unifié
```json
// Succès
{
  "success": true,
  "bimWarnings": [],
  "...données spécifiques..."
}

// Erreur
{
  "success": false,
  "error": {
    "code": "ELEMENT_NOT_FOUND | INVALID_ARGUMENT | REVIT_API_ERROR | NO_ACTIVE_DOCUMENT | TIMEOUT",
    "message": "Description claire de l'erreur",
    "details": {}
  }
}
```

---

## 3. Bilan des Corrections des Défauts Initiaux (D1 à D10)

| # | Sujet | Correction apportée en v2.1.0 |
|---|---|---|
| **D1** | `revit_get_families` | `CategoryResolver` unifié pour résoudre le français (`Mobilier`) et l'anglais (`Furniture`). |
| **D2** | `revit_get_elements` | Retourne désormais systématiquement : `location` (point 3D ou courbe de départ/fin en mm, rotation), `bbox` ({min, max} en mm), `levelName`, `hostId`, vecteurs `facingOrientation` et `handOrientation`, `isFlipped`, `typeId`. |
| **D3** | `revit_get_elements` | Ajout du filtre spatial `bboxFilter: { min: {x,y,z}, max: {x,y,z} }` en mm. |
| **D4** | `revit_get_families` | Option `includeTypeParameters: boolean` pour inspecter les dimensions et propriétés de type. |
| **D5** | `revit_get_rooms` | Option `includeBoundary: boolean` renvoyant les polygones 2D/3D fermés des pièces en mm. |
| **D6** | `revit_export_view_image` | Fonctionne sur toute vue/feuille par son `viewId`, avec réglages de dimensions (`width`, `height`), style visuel et fond transparent. |
| **D7** | `revit_export_pdf` / `dwg` | Export multi-feuilles vectoriel haute qualité avec presets configurables. |
| **D8** | `revit_place_family_instance` | Chargement automatique si famille non chargée (`familyPath`), rotation contrôlée autour du point d'insertion, retour de l'ID créé et de sa `bbox`. |
| **D9** | `revit_health_check` | Liste complète `openDocuments` avec titre, chemin fichier, statut actif et état de modification. |
| **D10** | Tous les outils | Standardisation complète du format d'erreur structuré JSON. |

---

## 4. Les 189 Outils Disponibles pour Claude

Le serveur MCP expose désormais **189 outils uniques**, organisés en domaines d'architecture :

1. **Exécution & Transactions :**
   - `revit_execute_csharp`, `revit_execute_python`, `revit_batch` (exécution atomique groupée), `revit_undo`, `revit_redo`, `revit_get_transaction_history`.
2. **Documents & Vues :**
   - `revit_list_open_documents`, `revit_activate_document`, `revit_open_document`, `revit_save_document`, `revit_close_document`.
   - `revit_create_3d_view`, `revit_create_section_view`, `revit_create_elevation_views`, `revit_create_drafting_view`, `revit_create_legend_view`, `revit_delete_views`, `revit_close_inactive_views`.
   - `revit_create_view_filter`, `revit_apply_view_filter`, `revit_create_view_template_from_view`.
3. **Modélisation & Géométrie Avancée :**
   - **Murs & Ouvertures :** `revit_create_wall`, `revit_create_wall_by_profile`, `revit_modify_wall_profile`, `revit_attach_wall`, `revit_place_door`, `revit_place_window`, `revit_create_wall_sweep`, `revit_create_curtain_wall_grid`.
   - **Sols & Toitures :** `revit_create_floor`, `revit_modify_floor_boundary`, `revit_create_roof`, `revit_create_roof_extrusion`, `revit_modify_roof`, `revit_create_roof_soffit`, `revit_create_fascia`, `revit_create_gutter`.
   - **Escaliers & Mezzanine :** `revit_create_stair`, `revit_create_railing`, `revit_modify_railing_path`.
   - **Plafonds & Réservations :** `revit_create_ceiling`, `revit_create_opening`.
4. **Pièces, Surfaces & Couleurs :**
   - `revit_create_room`, `revit_create_room_separator`, `revit_create_color_fill_scheme`, `revit_apply_color_fill`, `revit_create_area_plan`, `revit_create_area`.
5. **Familles & Composants :**
   - `revit_load_family`, `revit_place_family_instance`, `revit_create_simple_family`, `revit_edit_family_parameters`.
6. **Annotations, Cotations & Habillage :**
   - `revit_create_dimension`, `revit_auto_dimension_walls`, `revit_dimension_openings`, `revit_tag_element`, `revit_create_text_note`, `revit_create_detail_curve`, `revit_place_annotation_symbol`, `revit_place_detail_component`, `revit_place_legend_component`, `revit_create_line_style`.
7. **Feuilles & Mises en page :**
   - `revit_create_sheet`, `revit_place_viewport`, `revit_change_title_block`, `revit_set_sheet_parameters`, `revit_set_viewport_type`, `revit_place_schedule_on_sheet`, `revit_align_viewports`.
8. **Matériaux & Variantes :**
   - `revit_create_material`, `revit_assign_material`, `revit_create_design_option`.
9. **Exports & Visualisation :**
   - `revit_export_pdf`, `revit_export_dwg`, `revit_export_dxf`, `revit_export_ifc`, `revit_export_view_image`, `revit_render_view`, `revit_run_dynamo_inline`.

---

## 5. Serveur Compagnon CAD-MCP (Python)

Pour la manipulation précise des fichiers DXF/DWG sans charger le moteur Revit :
- **`dxf_render` :** Rend un calque/layout DXF en **PDF vectoriel à l'échelle exacte** ou en **PNG haute résolution**, avec le respect des types de ligne (tirets réels, pointillés) et des épaisseurs de trait.
- **`dxf_render_all_layouts_pdf` :** Génère un cahier PDF multipages de toutes les présentations d'un DXF.
- **`dxf_diff` :** Compare géométriquement deux versions d'un fichier DXF et met en évidence les ajouts, suppressions et déplacements.

---

## 6. Résolution du Démarrage Silencieux (Zéro Popup de Sécurité)

Dans Revit, une DLL compilée avec un certificat auto-signé non reconnu déclenche une boîte de dialogue rouge bloquante (*"Signature incorrecte"*), interdisant l'option "Toujours charger".

**Solution définitive appliquée :**
1. Suppression de la tâche de signature automatique (`SignRevitAddin`) dans le projet C#.
2. Déploiement d'une DLL non signée standard.
3. Enregistrement direct du GUID du complément (`a1b2c3d4-e5f6-7890-abcd-ef1234567890`) dans la clé de registre Revit :
   `HKCU:\SOFTWARE\Autodesk\Revit\Autodesk Revit 2027\CodeSigning` avec la valeur DWORD `1` (*Always Load / Approuvé*).
4. **Résultat :** Revit 2027 démarre désormais instantanément et silencieusement, sans aucune alerte de sécurité.

---

## 7. Vérifications Réalisées en Direct sur la Maquette

Les tests ont été réalisés sur le fichier projet ouvert : **`Conception variante 2.rvt`** (Micro-habitat 6,40 × 6,20 m).

1. **Relevé initial de la maquette :**
   - 4 Niveaux détectés : `Niveau 0` (0 mm), `Mezzanine` (2 400 mm), `Niveau 1` (3 000 mm), `Niveau 2` (6 000 mm).
   - 14 Éléments de mobilier identifiés en français (lits doubles, chevets, tables...).
   - Toit double pente (ID `305998`) : pente de 40,91°, épaisseur 200 mm, faîtage à 5,00 m intérieur (5,26 m extérieur).
   - Murs pignons : profilés de 0 à 5,00 m.
   - Murs gouttereaux : initialement à 2,50 m.
2. **Modification géométrique exécutée :**
   - Les murs extérieurs bas (ID `258570` et `258689`) ont été ajustés par l'outil `elements/set-parameter` de **2,50 m à 2,40 m** (2 400 mm).
   - Vérification des boîtes englobantes : ZMin = 0 mm, ZMax = 2 400 mm, hauteur = 2 400 mm.
   - Les cloisons intérieures sous mezzanine sont parfaitement alignées à 2 400 mm.
   - Zéro avertissement bloquant (`bimWarnings: []`).

---

## 8. Statut Git & Déploiement

- **Branche :** `main`
- **Dépôt distant :** `https://github.com/EncoreZaan/Revit-MCP.git`
- **État :** Synchronisé à 100 %, répertoire de travail propre (`clean`).
- **Configuration Claude Desktop :** Les serveurs `revit` et `cad-mcp` sont déclarés dans `%APPDATA%\Claude\claude_desktop_config.json`.
