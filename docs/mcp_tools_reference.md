# RÉFÉRENCE COMPLÈTE DES 70 OUTILS MCP — REVIT AI BRIDGE

Ce document liste l'ensemble des 70 outils exposés par le serveur MCP à Claude, avec leurs paramètres, leurs préconditions, leur comportement et leur niveau de sécurité.

---

## 1. Santé, Diagnostic & Sauvegarde

### `revit_health_check`
- **Description** : Vérifie la connectivité avec l'add-in Revit via le pont loopback HTTP (`http://localhost:6543/healthz`).
- **Paramètres** : Aucun.
- **Lecture seule** : Oui (`readOnlyHint: true`).
- **Retour** : Statut du service, version (2.0.0), uptime en secondes, timeouts configurés.

### `revit_save_document`
- **Description** : Enregistre le document actif ou effectue un "Enregistrer sous" vers un nouveau chemin.
- **Paramètres** : `filePath` (chaîne optionnelle pour "Enregistrer sous").
- **Lecture seule** : Non.

### `revit_get_summary_report`
- **Description** : Synthétise en une seule requête les informations clés du modèle : chemin, niveaux, nombre de vues, nombre de feuilles, nombre d'éléments et avertissements actifs.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

---

## 2. Modèle, Niveaux & Diagnostic BIM

### `revit_get_model_info`
- **Description** : Renvoie les métadonnées générales : titre du projet, chemin de fichier, version exacte de Revit (2027), statut lecture seule, auteur, nom et numéro de projet.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_model_warnings`
- **Description** : Extrait la liste des avertissements natifs Revit actifs dans le fichier (ex: géométries jointes avec conflit, pièces non closes).
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_validate_model_changes`
- **Description** : Valide l'état d'intégrité du modèle après une modification en recoupant métadonnées et nouveaux avertissements.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_levels`
- **Description** : Liste tous les niveaux définis dans le projet avec leur élévation en millimètres et leur nom.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_phases`
- **Description** : Liste les phases temporelles du projet (ex: Existant, Projet, Démolition).
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_links`
- **Description** : Liste tous les liens externes associés au projet (liens Revit `.rvt`, fichiers CAO DWG/DXF, IFC).
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_worksets`
- **Description** : Liste les sous-projets (Worksets) d'un projet partagé (nom, ID, éditable, ouvert). Si le fichier n'est pas partagé, indique `isWorkshared: false`.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_unused_types`
- **Description** : Audit de santé et nettoyage (Purge) : identifie les types et symboles de familles chargés dans le projet ayant 0 instances placées.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

---

## 3. Sélection, Vue Active & Visualisation Graphique

### `revit_get_current_selection`
- **Description** : Renvoie la liste des éléments actuellement sélectionnés par l'utilisateur dans l'interface Revit.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_set_current_selection`
- **Description** : Modifie la sélection utilisateur dans Revit avec une liste d'identifiants d'éléments.
- **Paramètres** : `elementIds` (liste d'entiers).
- **Lecture seule** : Non (impacte l'interface utilisateur).

### `revit_zoom_to_elements`
- **Description** : Centre la vue active et effectue un zoom ajusté sur une liste d'éléments spécifiés.
- **Paramètres** : `elementIds` (liste d'entiers).
- **Lecture seule** : Non (navigation visuelle).

### `revit_isolate_elements`
- **Description** : Active l'isolation temporaire des éléments spécifiés dans la vue active (masque tout le reste).
- **Paramètres** : `elementIds` (liste d'entiers).
- **Lecture seule** : Non.

### `revit_reset_temporary_override`
- **Description** : Rétablit l'affichage normal de la vue active en désactivant le mode Masquer/Isoler temporaire.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.

### `revit_color_elements`
- **Description** : Applique une surcharge graphique de couleur (RGB) unie sur les éléments spécifiés dans la vue active.
- **Paramètres** :
  - `elementIds` (liste d'entiers)
  - `red`, `green`, `blue` (entiers entre 0 et 255)
- **Lecture seule** : Non.

### `revit_reset_color_overrides`
- **Description** : Réinitialise les surcharges de couleur personnalisées appliquées sur les éléments dans la vue active.
- **Paramètres** : `elementIds` (liste d'entiers).
- **Lecture seule** : Non.

---

## 4. Exploration, Recherche & Matériaux

### `revit_get_elements`
- **Description** : Recherche et filtre les éléments du modèle selon des critères combinés (catégorie, famille, niveau, vue).
- **Paramètres** :
  - `category` (chaîne, ex: `'Walls'`, `'Floors'`)
  - `familyName` (chaîne optionnelle)
  - `levelName` (chaîne optionnelle)
  - `viewName` (chaîne optionnelle)
  - `limit` (entier, défaut: 100)
- **Lecture seule** : Oui.

### `revit_count_elements`
- **Description** : Compte le nombre d'éléments dans le modèle, avec filtre optionnel par catégorie.
- **Paramètres** : `category` (chaîne optionnelle).
- **Lecture seule** : Oui.

### `revit_list_categories`
- **Description** : Liste l'ensemble des catégories BIM disponibles dans le modèle.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_element_parameters`
- **Description** : Extrait l'intégralité des paramètres d'instance et de type pour un élément donné avec valeurs et types de données.
- **Paramètres** : `elementId` (entier).
- **Lecture seule** : Oui.

### `revit_get_families`
- **Description** : Liste les familles chargées dans le modèle et leurs types disponibles.
- **Paramètres** : `category` (chaîne optionnelle).
- **Lecture seule** : Oui.

### `revit_find_elements_by_parameter`
- **Description** : Recherche transversale d'éléments dont une valeur de paramètre correspond ou contient une chaîne de texte recherchée.
- **Paramètres** :
  - `parameterName` (chaîne)
  - `searchValue` (chaîne)
  - `exactMatch` (booléen, défaut: `false`)
  - `limit` (entier, défaut: 100)
- **Lecture seule** : Oui.

### `revit_get_materials`
- **Description** : Liste l'ensemble des matériaux du projet avec leur classe, teinte RGB, transparence, brillance et lissage.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

---

## 5. Création d'Éléments & Dessin BIM

### `revit_create_wall`
- **Description** : Crée un mur linéaire entre deux points sur un niveau de référence.
- **Paramètres** : `startX`, `startY`, `endX`, `endY`, `levelId`, `heightMm` (défaut: 3000), `wallTypeName`.
- **Lecture seule** : Non.

### `revit_create_floor`
- **Description** : Crée une dalle / sol horizontal rectangulaire sur un niveau de référence.
- **Paramètres** : `levelId`, `widthMm` (défaut: 5000), `lengthMm` (défaut: 5000), `originX`, `originY`.
- **Lecture seule** : Non.

### `revit_place_family_instance`
- **Description** : Insère une instance de famille (porte, fenêtre, mobilier, équipement) aux coordonnées 3D spécifiées.
- **Paramètres** : `familyName`, `typeName`, `levelId`, `x`, `y`, `z`, `hostElementId` (optionnel).
- **Lecture seule** : Non.

### `revit_create_room`
- **Description** : Place une pièce (*Room*) avec calcul automatique de la surface aux coordonnées données.
- **Paramètres** : `levelId`, `xMm`, `yMm`, `name`, `number`.
- **Lecture seule** : Non.

### `revit_create_grid`
- **Description** : Crée une file de quadrillage linéaire (*Grid*) repérée par son nom.
- **Paramètres** : `startX`, `startY`, `endX`, `endY`, `name` (optionnel).
- **Lecture seule** : Non.

### `revit_create_detail_line`
- **Description** : Dessine une ligne de détail 2D dans la vue active en plan, coupe ou légende.
- **Paramètres** : `startX`, `startY`, `endX`, `endY` (coordonnées en mm).
- **Lecture seule** : Non.

---

## 6. Modification & Automatisation de Paramètres

### `revit_set_parameter`
- **Description** : Modifie la valeur d'un paramètre spécifique sur un élément (gère texte, nombres, booléens, IDs).
- **Paramètres** : `elementId`, `parameterName`, `value`.
- **Lecture seule** : Non.

### `revit_batch_set_parameters`
- **Description** : Modifie un paramètre en masse sur plusieurs éléments en une seule transaction sécurisée.
- **Paramètres** : `elementIds`, `parameterName`, `value`.
- **Lecture seule** : Non.

### `revit_copy_parameter_values`
- **Description** : Copie par lot la valeur d'un paramètre source vers un paramètre cible sur une sélection d'éléments ou sur l'ensemble de la maquette.
- **Paramètres** : `sourceParameter`, `targetParameter`, `elementIds` (optionnel).
- **Lecture seule** : Non.

### `revit_import_csv_parameters`
- **Description** : Importe et applique des modifications de paramètres à partir d'un fichier CSV tabulé (`ElementId` ou `UniqueId` en première colonne).
- **Paramètres** : `csvFilePath`, `idColumnName` (défaut: `'ElementId'`).
- **Lecture seule** : Non.

### `revit_move_element`
- **Description** : Déplace un élément selon un vecteur 3D (`dx`, `dy`, `dz` en mm).
- **Paramètres** : `elementId`, `dx`, `dy`, `dz`.
- **Lecture seule** : Non.

### `revit_rotate_element`
- **Description** : Applique une rotation à un élément autour de l'axe vertical Z passant par son centre.
- **Paramètres** : `elementId`, `angleDegrees`.
- **Lecture seule** : Non.

### `revit_delete_element`
- **Description** : Supprime un ou plusieurs éléments spécifiés par leur ID.
- **Paramètres** : `elementId`.
- **Lecture seule** : Non (Destructif).

---

## 7. Pièces & Finitions

### `revit_get_rooms`
- **Description** : Liste toutes les pièces du projet avec nom, numéro, surface, périmètre et niveau.
- **Paramètres** : `levelName` (optionnel).
- **Lecture seule** : Oui.

### `revit_get_room_finishes`
- **Description** : Récupère les paramètres de finitions intérieures (sol, mur, plafond, plinthes) pour une pièce donnée.
- **Paramètres** : `roomId` (entier).
- **Lecture seule** : Oui.

### `revit_set_room_finishes_bulk`
- **Description** : Applique en masse les finitions intérieures sur un ensemble de pièces.
- **Paramètres** : `roomIds`, `floorFinish`, `wallFinish`, `ceilingFinish`, `baseboardFinish`.
- **Lecture seule** : Non.

---

## 8. Vues, Boîtes de Coupe & Gabarits

### `revit_get_views`
- **Description** : Liste les vues du document (plans, coupes, 3D, élévations, légendes) avec leur statut de placement sur feuille.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_activate_view`
- **Description** : Bascule la vue active de Revit sur la vue spécifiée.
- **Paramètres** : `viewId`.
- **Lecture seule** : Non.

### `revit_create_plan_views_for_levels`
- **Description** : Génère automatiquement les plans d'étage pour une sélection de niveaux avec gabarit de vue optionnel et mode `dryRun`.
- **Paramètres** : `levelIds`, `viewTemplateName`, `dryRun` (défaut: `true`).
- **Lecture seule** : Non.

### `revit_duplicate_views_bulk`
- **Description** : Duplique un ensemble de vues (duplication simple, avec détails ou dépendante).
- **Paramètres** : `viewIds`, `duplicateOption` (`'Duplicate'`, `'WithDetailing'`, `'AsDependent'`).
- **Lecture seule** : Non.

### `revit_bulk_rename_views`
- **Description** : Renomme des vues par lot selon des règles de remplacement de motif ou préfixes/suffixes.
- **Paramètres** : `viewIds`, `findText`, `replaceText`, `prefix`, `suffix`, `dryRun`.
- **Lecture seule** : Non.

### `revit_apply_view_template_bulk`
- **Description** : Applique un gabarit de vue (*View Template*) à une liste de vues sélectionnées.
- **Paramètres** : `viewIds`, `templateName`.
- **Lecture seule** : Non.

### `revit_get_view_templates`
- **Description** : Liste l'ensemble des gabarits de vue disponibles dans le projet avec leur type et leur nom.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_create_section_box`
- **Description** : Crée ou ajuste une boîte de coupe 3D isométrique cadrant précisément les éléments spécifiés ou la sélection en cours, avec marge paramétrable en millimètres.
- **Paramètres** : `elementIds` (optionnel), `paddingMm` (défaut: 300 mm).
- **Lecture seule** : Non.

---

## 9. Feuilles, Cartouches & Carnets de Plans

### `revit_get_sheets`
- **Description** : Liste les feuilles du projet avec numéros, noms, cartouches associés et liste des vues placées.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_create_sheet`
- **Description** : Crée une nouvelle feuille avec son cartouche, son numéro et son titre.
- **Paramètres** : `sheetNumber`, `sheetName`, `titleBlockName`.
- **Lecture seule** : Non.

### `revit_duplicate_sheet`
- **Description** : Duplique une feuille existante avec son cartouche sous un nouveau numéro et un nouveau nom.
- **Paramètres** : `sheetId`, `sheetNumber`, `sheetName`.
- **Lecture conscience** : Non.

### `revit_place_view_on_sheet`
- **Description** : Positionne une vue sur une feuille existante aux coordonnées X, Y du cartouche.
- **Paramètres** : `sheetId`, `viewId`, `pointX`, `pointY`.
- **Lecture seule** : Non.

### `revit_auto_populate_sheets`
- **Description** : Automatise la génération d'un carnet complet de feuilles en créant et disposant les vues selon une nomenclature standard.
- **Paramètres** : `plansList`, `titleBlockName`, `dryRun` (défaut: `true`).
- **Lecture seule** : Non.

---

## 10. Métrés, Nomenclatures & Coordination

### `revit_get_material_takeoff`
- **Description** : Calcule les quantitatifs de surfaces et volumes de matériaux pour une catégorie donnée.
- **Paramètres** : `category`.
- **Lecture seule** : Oui.

### `revit_export_material_takeoff_csv`
- **Description** : Exporte les quantitatifs de matériaux vers un fichier CSV exploitable sous Excel ou Google Sheets.
- **Paramètres** : `category`, `outputCsvPath`.
- **Lecture seule** : Non.

### `revit_create_material_schedule`
- **Description** : Crée une vue de nomenclature native dans Revit dédiée au relevé de matériaux d'une catégorie.
- **Paramètres** : `category`, `scheduleName`.
- **Lecture seule** : Non.

### `revit_detect_clashes`
- **Description** : Analyse les boîtes englobantes (Bounding Box) et collisions géométriques entre deux catégories du modèle.
- **Paramètres** : `categoryA`, `categoryB`, `toleranceMm` (défaut: 0).
- **Lecture seule** : Oui.

### `revit_export_clash_report`
- **Description** : Génère un rapport de synthèse des collisions détectées au format HTML ou CSV.
- **Paramètres** : `clashData`, `reportFilePath`, `format` (`'html'` ou `'csv'`).
- **Lecture seule** : Non.

### `revit_issue_drawings`
- **Description** : Émet une série de feuilles pour diffusion en leur appliquant un indice de révision officiel.
- **Paramètres** : `sheetIds`, `revisionSequenceNumber`.
- **Lecture seule** : Non.

### `revit_get_revision_history`
- **Description** : Affiche l'historique chronologique des révisions et indices émis sur l'ensemble du projet.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_revisions`
- **Description** : Liste détaillée de toutes les révisions créées dans Revit (date, description, émis à, émis par, numéro).
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

---

## 11. Exports & Formats d'Échange

### `revit_export_pdf`
- **Description** : Exporte la vue active ou un ensemble de feuilles au format PDF vectoriel.
- **Paramètres** : `viewsOrSheetsIds`, `outputFolder`, `combineIntoSingleFile` (booléen).
- **Lecture seule** : Non.

### `revit_export_dwg`
- **Description** : Exporte la vue ou les feuilles au format AutoCAD DWG/DXF.
- **Paramètres** : `viewIds`, `outputFolder`, `exportSetupName`.
- **Lecture seule** : Non.

### `revit_export_ifc`
- **Description** : Exporte la maquette au format IFC standard openBIM (IFC2x3 / IFC4).
- **Paramètres** : `outputFilePath`, `ifcVersion` (optionnel).
- **Lecture seule** : Non.

### `revit_export_schedule`
- **Description** : Exporte les données d'une nomenclature existante vers un fichier CSV ou texte tabulé.
- **Paramètres** : `scheduleId`, `outputFilePath`.
- **Lecture seule** : Non.

### `revit_export_view_image`
- **Description** : Effectue un rendu/export d'image haute résolution (PNG) de la vue active ou d'une vue spécifiée.
- **Paramètres** : `viewId` (optionnel), `filePath` (optionnel), `pixelSize` (défaut: 1920).
- **Lecture seule** : Non.

---

## 12. Audit Avancé, Commandes & Scripts

### `revit_audit_families`
- **Description** : Analyse le poids et la complexité des familles chargées dans le modèle pour identifier les ralentissements de performance.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_run_command`
- **Description** : Exécute une commande postable native de Revit (`PostableCommand`) via l'interface UI (ex: zoom étendu, purge, synchronisation).
- **Paramètres** : `commandName`.
- **Lecture seule** : Non.

### `revit_run_dynamo`
- **Description** : Déclenche l'ouverture de l'environnement Dynamo pour l'exécution d'algorithmes visuels.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.
