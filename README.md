# REVIT AI BRIDGE — MCP POUR AUTODESK REVIT (v2.1.1)

Pont natif et haute performance entre **Claude (Claude Desktop / Claude Code)** et **Autodesk Revit 2027** (compatible 2026, 2025 et 2024) via le **Model Context Protocol (MCP)**.

Revit AI Bridge permet à Claude de lire, concevoir, modifier, annoter, coter, auditer et coordonner des maquettes BIM Revit à travers **191 outils locaux dédiés**.

---

## 1. Architecture & Résilience Zéro-Crash

```
Claude (Claude Desktop ou Claude CLI)
       │
       │ Protocole MCP (stdio / JSON-RPC)
       ▼
Serveur MCP Local (Node.js / TypeScript)
       │
       │ HTTP REST JSON (http://localhost:6543)
       ▼
Add-in Revit C# .NET 10 / .NET 8 (RevitMCP.dll)
       │
       │ Revit ExternalEvent (Thread UI Principal)
       ▼
Autodesk Revit 2027 API (Document BIM ouvert)
```

### Principes de robustesse et sécurité absolue
1. **Thread-Safety Revit API** : Toutes les opérations modifiant ou interrogeant le modèle BIM transitent par un `ExternalEvent` marshalisé sur le thread UI principal de Revit. Aucun appel multi-thread non sécurisé n'est exécuté.
2. **SafeFailurePreprocessor & Zéro Boîte de Dialogue Bloquante** : Toutes les transactions Revit intègrent un préprocesseur d'échecs (`SafeFailurePreprocessor`) qui absorbe et valide automatiquement les alertes mineures de Revit sans freeze modal, tout en renvoyant systématiquement les messages dans `bimWarnings: string[]`.
3. **Rollback Automatique & Mode Simulation (`dryRun`)** : En cas d'erreur ou d'exception lors d'une transaction, celle-ci est immédiatement annulée (`tx.RollBack()`). De plus, tous les outils d'écriture supportent le paramètre `dryRun: boolean` pour tester les modifications sans impacter le document.
4. **Transactions nommées & Annulation** : Chaque modification porte le nom `"MCP: <nom_outil>"`. Les outils `revit_undo`, `revit_redo` et `revit_get_transaction_history` permettent de suivre et d'annuler les opérations pas à pas.
5. **Concurrence & Sérialisation** : Un sémaphore (`SemaphoreSlim`) sérialise les requêtes vers Revit pour empêcher tout télescopage de transactions.
6. **Support Bilingue Universel (FR / EN / BuiltInCategory)** : Grâce au résolveur automatique `CategoryResolver`, les catégories peuvent être spécifiées en français (`Mobilier`, `Murs`, `Portes`, `Toits`), en anglais (`Furniture`, `Walls`, `Doors`, `Roofs`) ou en identifiant interne `BuiltInCategory` (`OST_Furniture`).
7. **Identifiants 64 bits (`long`)** : Compatibilité native avec l'architecture `ElementId` 64 bits de Revit 2024+.
8. **Unités standardisées** : Toutes les longueurs en **millimètres (mm)**, les angles en **degrés (°)**, les surfaces en **m²** et les volumes en **m³** (conversions automatiques via `UnitUtils`).
9. **Multi-Documents** : Chaque outil accepte le paramètre optionnel `documentTitle` ou `documentPath`. Si omis, l'opération cible automatiquement le document actif.
10. **Format d'erreur normalisé** : Toutes les anomalies renvoient une structure `{ "success": false, "error": { "code": "...", "message": "...", "details": {...} } }`.
11. **Démarrage Silencieux & Signature** : Configuration automatique du commutateur officiel `DisableAddInSecurity=1` dans `Revit.ini` et enregistrement canonique dans le registre Windows, garantissant le chargement direct sans popup de confirmation d'éditeur.

---

## 2. Inventaire des 191 outils MCP

Consultez le document détaillé [`docs/mcp_tools_reference.md`](docs/mcp_tools_reference.md) pour la documentation exhaustive des paramètres et schémas JSON.

### A. Diagnostic, Connexion & Santé (6 outils)
`revit_health_check`, `revit_get_model_info`, `revit_get_ui_state`, `revit_bring_to_front`, `revit_dismiss_dialogs`, `revit_get_log`.

### B. Gestion des Documents, Projet & Variantes (13 outils)
`revit_list_open_documents`, `revit_activate_document`, `revit_open_document`, `revit_close_document`, `revit_save_document`, `revit_save_as`, `revit_purge_unused`, `revit_set_project_info`, `revit_copy_elements_between_documents`, `revit_get_design_options`, `revit_create_design_option`, `revit_move_elements_to_design_option`, `revit_set_view_design_option`.

### C. Exécution de Code, Transactions & Automatisation (8 outils)
`revit_execute_csharp`, `revit_execute_python`, `revit_batch`, `revit_undo`, `revit_redo`, `revit_get_transaction_history`, `revit_list_dynamo_scripts`, `revit_run_dynamo_inline`.

### D. Géométrie, Espace & Requêtes Spatiales (9 outils)
`revit_get_element_location`, `revit_get_element_geometry`, `revit_get_elements_in_region`, `revit_measure_distance`, `revit_get_height_at_point`, `revit_check_clearance`, `revit_get_wall_openings`, `revit_get_room_at_point`, `revit_detect_clashes`.

### E. Exploration du Modèle BIM (8 outils)
`revit_get_elements`, `revit_get_element_parameters`, `revit_count_elements`, `revit_list_categories`, `revit_get_levels`, `revit_get_families`, `revit_find_elements_by_parameter`, `revit_get_materials`.

### F. Modification d'Éléments & Positionnement (14 outils)
`revit_set_element_location`, `revit_move_element`, `revit_rotate_element`, `revit_change_element_type`, `revit_copy_elements`, `revit_mirror_elements`, `revit_align_elements`, `revit_flip_element`, `revit_hosted_set_position`, `revit_set_element_pinned`, `revit_join_geometry`, `revit_group_elements`, `revit_ungroup_elements`, `revit_delete_element`.

### G. Création & Modification d'Architecture & Structure (18 outils)
`revit_create_wall`, `revit_modify_wall`, `revit_split_wall`, `revit_attach_wall`, `revit_create_floor`, `revit_modify_floor_boundary`, `revit_create_roof_footprint`, `revit_create_roof_extrusion`, `revit_modify_roof`, `revit_create_ceiling`, `revit_create_structural_column`, `revit_create_structural_beam`, `revit_create_beam_system`, `revit_create_railing`, `revit_create_stair`, `revit_place_family_instance`, `revit_create_simple_family`, `revit_edit_family_parameters`.

### H. Niveaux, Pièces, Cloisonnement & Finitions (15 outils)
`revit_create_level`, `revit_modify_level`, `revit_create_room`, `revit_create_room_separation_lines`, `revit_place_room_at_point`, `revit_delete_unplaced_rooms`, `revit_tag_rooms`, `revit_get_rooms`, `revit_get_room_finishes`, `revit_set_room_finishes_bulk`, `revit_set_room_color_fill`, `revit_create_color_fill_scheme`, `revit_apply_color_fill`, `revit_create_area_plan`, `revit_create_area`.

### I. Types, Familles & Bibliothèque (6 outils)
`revit_duplicate_type`, `revit_set_type_parameters`, `revit_get_type_parameters`, `revit_create_window_or_door_type`, `revit_load_family`, `revit_list_family_library`.

### J. Vues, Caméras, Filtres & Gabarits (19 outils)
`revit_get_views`, `revit_activate_view`, `revit_create_plan_views_for_levels`, `revit_create_section_view`, `revit_create_3d_view`, `revit_create_elevation_views`, `revit_create_drafting_view`, `revit_create_legend_view`, `revit_place_legend_component`, `revit_duplicate_view`, `revit_set_view_properties`, `revit_set_category_visibility`, `revit_hide_elements_in_view`, `revit_isolate_elements_in_view`, `revit_crop_view_to_elements`, `revit_create_section_box`, `revit_apply_view_template_bulk`, `revit_create_view_template_from_view`, `revit_create_view_filter`, `revit_apply_view_filter`, `revit_delete_views`, `revit_close_inactive_views`.

### K. Annotations, Cotations, Symboles & Dessin 2D (14 outils)
`revit_create_linear_dimension`, `revit_auto_dimension_walls`, `revit_dimension_openings`, `revit_create_spot_elevation`, `revit_create_tag`, `revit_tag_all`, `revit_create_text_note`, `revit_create_detail_curve`, `revit_create_filled_region`, `revit_create_grid`, `revit_create_detail_line`, `revit_create_line_style`, `revit_place_annotation_symbol`, `revit_place_detail_component`.

### L. Feuilles, Cartouches & Mises en Page (12 outils)
`revit_get_sheets`, `revit_create_sheet`, `revit_duplicate_sheet`, `revit_change_title_block`, `revit_set_sheet_parameters`, `revit_place_view_on_sheet`, `revit_get_viewports`, `revit_move_viewport`, `revit_set_viewport_type`, `revit_align_viewports`, `revit_place_schedule_on_sheet`, `revit_auto_populate_sheets`.

### M. Réservations, Trémies, Escaliers & Formes Libres (4 outils)
`revit_create_opening`, `revit_create_ladder`, `revit_create_direct_shape`, `revit_paint_face`.

### N. Matériaux, Nomenclatures & Quantitatifs (6 outils)
`revit_create_material`, `revit_assign_material`, `revit_create_schedule`, `revit_get_schedule_data`, `revit_get_material_takeoff`, `revit_export_material_takeoff_csv`.

### O. Paramètres & Données BIM (5 outils)
`revit_set_parameter`, `revit_batch_set_parameters`, `revit_import_csv_parameters`, `revit_copy_parameter_values`, `revit_set_element_phase`.

### P. Imports, Exports & Formats Vectoriels (11 outils)
`revit_export_pdf`, `revit_export_dwg`, `revit_export_dxf`, `revit_export_ifc`, `revit_export_view_image`, `revit_render_view`, `revit_import_cad`, `revit_get_cad_geometry`, `revit_create_walls_from_cad`, `revit_import_image`, `revit_export_clash_report`, `revit_export_schedule`.

### Q. Graphismes, Révisions & Variantes (13 outils)
`revit_color_elements`, `revit_reset_color_overrides`, `revit_zoom_to_elements`, `revit_get_current_selection`, `revit_set_current_selection`, `revit_isolate_elements`, `revit_reset_temporary_override`, `revit_get_view_templates`, `revit_bulk_rename_views`, `revit_duplicate_views_bulk`, `revit_get_revision_history`, `revit_get_revisions`, `revit_add_revision_cloud`, `revit_get_worksets`, `revit_issue_drawings`.

---

## 3. Serveur Compagnon CAD-MCP (Python)

Le dossier `cad-mcp/` contient un serveur MCP autonome dédié à l'inspection et à la coordination DXF/DWG :

- `dxf_info` : Métadonnées complètes d'un fichier DXF (version, calques, limites en mm, compteurs).
- `dxf_list_entities` : Lecture détaillée de la géométrie CAO 2D/3D avec coordonnées en millimètres.
- `dxf_render` : Rendu vectoriel haute résolution en image PNG ou PDF à l'échelle avec gestion des tirets réels.
- `dxf_render_all_layouts_pdf` : Rendu de toutes les présentations (Model et Paper Space) dans un PDF multi-pages.
- `dxf_audit` : Détection des anomalies du plan (lignes dupliquées, polylignes ouvertes, tolérances).
- `dxf_edit` : Édition vectorielle sûre (ajout/déplacement/suppression d'entités) avec détection de verrou.
- `dxf_diff` : Comparaison géométrique et structurelle entre deux versions d'un fichier DXF.
- `dxf_compare_with_revit` : Comparaison spatiale croisée directe entre le fond de plan DXF et la maquette 3D Revit active.

---

## 4. Configuration & Sécurité (`RevitMCP.config.json`)

```json
{
  "port": 6543,
  "queueTimeoutSeconds": 10,
  "revitApiTimeoutSeconds": 120,
  "maxRequestBytes": 2097152,
  "allowCodeExecution": false,
  "logLevel": "Information"
}
```

> [!CAUTION]
> L'exécution de code dynamique (`revit_execute_csharp`, `revit_execute_python`, `revit_run_dynamo_inline`) est **désactivée par défaut** pour la sécurité de la distribution. Elle ne peut être autorisée qu'en basculant `"allowCodeExecution": true` dans le fichier déployé dans `%APPDATA%\Autodesk\Revit\Addins\[Version]\RevitMCP.config.json`. Toute tentative sans cette autorisation renvoie une erreur `PERMISSION_DENIED`.

---

## 5. Configuration pour Claude Desktop & Claude Code

Dans votre fichier `claude_desktop_config.json` :

```json
{
  "mcpServers": {
    "revit": {
      "command": "node",
      "args": ["c:/Users/teoba/Documents/Devs/MCP Revit/mcp-server/dist/index.js"],
      "env": {
        "REVIT_HOST": "http://localhost:6543",
        "REVIT_TIMEOUT_MS": "120000"
      }
    },
    "cad-mcp": {
      "command": "python",
      "args": ["c:/Users/teoba/Documents/Devs/MCP Revit/cad-mcp/server.py"]
    }
  }
}
```

---

## 6. Installation & Déploiement

### Option A : Installateur Universel 1-Clic en Rust (Recommandé)

Téléchargez la dernière version de l'installateur dans les [Releases GitHub](https://github.com/EncoreZaan/Revit-MCP/releases) : **`RevitMCP-Setup-v2.1.1.exe`** (~4 Mo).

Développé en **Rust natif** ultra-léger et autonome, cet installateur embarque l'intégralité du pont (Add-in C#, compilateurs Roslyn, serveur MCP autonome bundled sans dépendances `node_modules`).

```powershell
# Installation interactive standard (serveur Rust natif par défaut, zéro dépendance Node.js)
.\RevitMCP-Setup-v2.1.1.exe

# Diagnostic du système et de l'installation
.\RevitMCP-Setup-v2.1.1.exe --status

# Forcer le mode Node.js (secours / rétrocompatibilité)
.\RevitMCP-Setup-v2.1.1.exe --use-node

# Installation silencieuse (déploiement automatisé)
.\RevitMCP-Setup-v2.1.1.exe --silent

# Désinstallation propre
.\RevitMCP-Setup-v2.1.1.exe --uninstall
```

L'installateur effectue automatiquement :
1. **Détection multi-versions** de Revit (2024, 2025, 2026, 2027) via Program Files, APPDATA et le Registre Windows.
2. **Déploiement de l'add-in** `.NET` dans `%APPDATA%\Autodesk\Revit\Addins\<Année>\` sans jamais écraser vos réglages personnalisés de sécurité (`allowCodeExecution`).
3. **Démarrage silencieux garanti** : injection automatique de `[Misc] DisableAddInSecurity=1` dans `Revit.ini` (compatible UTF-16 LE) et enregistrement du GUID canonique dans `HKCU:\SOFTWARE\Autodesk\Revit\Autodesk Revit <Année>\CodeSigning`. Zéro popup au lancement de Revit.
4. **Déploiement du serveur MCP double-moteur** :
   - Moteur principal : **Rust natif autonome** (`revit-mcp-server.exe`, 1,28 Mo, temps de réponse < 2 ms, 0 dépendance Node.js).
   - Moteur de secours : **Node.js bundle** (`index.bundle.mjs`) activable instantanément via `--use-node`.
5. **Configuration automatique de Claude Desktop** (supporte les profils standard APPDATA et Windows Store MSIX).

---

### Option B : Déploiement Développeur depuis les sources

```powershell
# Build complet de la release (C# + Bundle TS + Rust Setup)
.\scripts\build-release.ps1

# Déploiement rapide en développement local
.\scripts\install.ps1
```

### Build et tests manuels
```powershell
# Add-in C# .NET 10 / 8
dotnet build revit-addin\RevitMCP\RevitMCP.csproj -c Release

# Serveur MCP TypeScript
npm run build --prefix mcp-server
npm test --prefix mcp-server

# Serveur compagnon CAD-MCP Python
python -m unittest discover -s cad-mcp
```

### Validation et tests d'acceptation
Consultez le document [`docs/test_report.md`](docs/test_report.md) pour les preuves d'exécution réelles sur Autodesk Revit 2027 et le protocole des scénarios validés.
