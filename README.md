# REVIT AI BRIDGE — MCP POUR AUTODESK REVIT

Pont natif et haute performance entre **Claude (Claude Desktop / Claude Code)** et **Autodesk Revit 2027** (compatible 2026 et 2025) via le **Model Context Protocol (MCP)**.

Revit Claude MCP lets Claude read, modify, export, audit, and coordinate Revit models through 70 local tools.

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
Add-in Revit C# .NET 10 (RevitMCP.dll)
       │
       │ Revit ExternalEvent (Thread UI Principal)
       ▼
Autodesk Revit 2027 API (Document BIM ouvert)
```

### Principes de robustesse et sécurité absolue
1. **Thread-Safety Revit API** : Toutes les opérations modifiant ou interrogeant le modèle BIM transitent par un `ExternalEvent` marshalisé sur le thread UI principal de Revit. Aucun appel multi-thread non sécurisé n'est exécuté.
2. **WarningSwallower & Zéro Popup Bloquant** : Toutes les transactions Revit intègrent un préprocesseur d'échecs (`IFailuresPreprocessor`) qui absorbe et valide automatiquement les alertes mineures de Revit, évitant tout blocage ou freeze de la boîte de dialogue modale.
3. **Rollback Automatique Garanti** : En cas d'erreur ou d'exception lors d'une transaction, celle-ci est immédiatement annulée (`tx.RollBack()`), protégeant l'intégrité de la maquette BIM de l'utilisateur.
4. **Concurrence & Sérialisation** : Un sémaphore (`SemaphoreSlim`) sérialise les requêtes vers Revit pour empêcher tout télescopage de transactions.
5. **Gestion Hors-Ligne & Document Fermé** : Si Revit n'est pas lancé ou si aucun document n'est ouvert (écran d'accueil), le pont renvoie un statut propre et descriptif sans jamais crasher.
6. **Sécurité réseau stricte** : Le pont HTTP écoute exclusivement sur l'adresse loopback `localhost:6543`. Toute tentative de connexion distante est bloquée.

---

## 2. Inventaire des 70 Outils MCP

| Catégorie | Outils exposés à Claude | Description |
|-----------|-------------------------|-------------|
| **Diagnostic & Connexion** | `revit_health_check` | Vérifie l'état du pont local, version et disponibilité de Revit |
| **Document, Sauvegarde & Synthèse** | `revit_get_model_info`<br>`revit_get_model_warnings`<br>`revit_validate_model_changes`<br>`revit_get_summary_report`<br>`revit_save_document`<br>`revit_get_unused_types` | Métadonnées du projet, avertissements natifs, rapport consolidé, sauvegarde/save as, audit des types inutilisés |
| **Sélection & Visualisation** | `revit_get_current_selection`<br>`revit_set_current_selection`<br>`revit_isolate_elements`<br>`revit_reset_temporary_override`<br>`revit_zoom_to_elements`<br>`revit_color_elements`<br>`revit_reset_color_overrides` | Sélection courante, isolation, zoom automatique sur éléments, colorisation graphique temporaire par couleur RGB |
| **Exploration BIM & Recherche** | `revit_get_elements`<br>`revit_get_element_parameters`<br>`revit_count_elements`<br>`revit_list_categories`<br>`revit_get_levels`<br>`revit_get_families`<br>`revit_find_elements_by_parameter`<br>`revit_get_materials` | Filtrage multicritère, paramètres typés, comptage, niveaux, familles, recherche textuelle de paramètres, matériaux du projet |
| **Création d'éléments & Dessin** | `revit_create_wall`<br>`revit_create_floor`<br>`revit_place_family_instance`<br>`revit_create_room`<br>`revit_create_grid`<br>`revit_create_detail_line` | Murs, dalles, familles/portes/fenêtres/mobilier, pièces avec calcul de surface, files/quadrillages, lignes de détail 2D |
| **Modification BIM & Paramètres** | `revit_set_parameter`<br>`revit_batch_set_parameters`<br>`revit_import_csv_parameters`<br>`revit_copy_parameter_values`<br>`revit_move_element`<br>`revit_rotate_element`<br>`revit_delete_element` | Paramétrage unitaire/lot, import CSV, copie de valeurs entre paramètres, translation, rotation axiale, suppression sécurisée |
| **Pièces & Finitions** | `revit_get_rooms`<br>`revit_get_room_finishes`<br>`revit_set_room_finishes_bulk` | Extraction des pièces, finitions des sols/murs/plafonds, mise à jour en masse |
| **Vues, Boîtes de coupe & Gabarits** | `revit_get_views`<br>`revit_activate_view`<br>`revit_create_plan_views_for_levels`<br>`revit_duplicate_views_bulk`<br>`revit_bulk_rename_views`<br>`revit_apply_view_template_bulk`<br>`revit_get_view_templates`<br>`revit_create_section_box` | Plans, coupes, duplication avec détails, renommage par lot, gabarits de vue, boîte de coupe 3D cadrée sur sélection |
| **Feuilles & Cartouches** | `revit_get_sheets`<br>`revit_create_sheet`<br>`revit_duplicate_sheet`<br>`revit_place_view_on_sheet`<br>`revit_auto_populate_sheets` | Création de feuilles, duplication de feuille avec cartouche, placement de vues, génération automatique de carnet |
| **Coordination, Sous-projets & Liens** | `revit_detect_clashes`<br>`revit_export_clash_report`<br>`revit_get_phases`<br>`revit_get_links`<br>`revit_get_worksets`<br>`revit_issue_drawings` | Détection d'interférences géométriques (BBox), rapport HTML/CSV, phases du projet, liens RVT/CAD/IFC, sous-projets, émission de plans |
| **Révisions & Métrés** | `revit_get_revision_history`<br>`revit_get_revisions`<br>`revit_get_material_takeoff`<br>`revit_export_material_takeoff_csv` | Historique et liste des révisions/indices, quantitatifs surfaciques et volumiques de matériaux, export CSV |
| **Nomenclatures & Exports** | `revit_create_material_schedule`<br>`revit_export_schedule`<br>`revit_export_pdf`<br>`revit_export_dwg`<br>`revit_export_ifc`<br>`revit_export_view_image` | Nomenclatures natives, export CSV de table, PDF vectoriel, DWG/DXF, IFC, rendu/export d'image haute résolution |
| **Audit & Automatisation** | `revit_audit_families`<br>`revit_run_command`<br>`revit_run_dynamo` | Audit du poids des familles, commandes postables Revit, lancement Dynamo |

---

## 3. Configuration pour Claude Desktop & Claude Code

### Configuration Claude Desktop
Fichiers synchronisés :
- `%APPDATA%\Claude\claude_desktop_config.json`
- `%LOCALAPPDATA%\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude\claude_desktop_config.json` (MSIX Store)

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
    }
  }
}
```

---

## 4. Démarrage et Utilisation

1. **Compiler et installer l'Add-in Revit** :
   ```powershell
   .\scripts\build.ps1
   .\scripts\install.ps1
   ```
2. **Lancer Autodesk Revit 2027** et ouvrir un projet `.rvt`.
3. L'add-in démarre automatiquement le serveur d'écoute local sur le port 6543.
4. Ouvrir **Claude Desktop** : l'icône marteau affiche les 70 outils Revit disponibles.
5. Dialoguer avec Claude en langage naturel (ex: *"Fais un résumé complet du projet, vérifie les sous-projets et crée une boîte de coupe 3D sur le mur sélectionné"*).
