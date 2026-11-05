# RÉFÉRENCE COMPLÈTE DES OUTILS MCP — REVIT AI BRIDGE (v2.5.0)

Ce document fournit la documentation de référence exhaustive de l'ensemble des **220 outils MCP** exposés par le serveur Revit AI Bridge à Claude (Claude Desktop, Claude Code et clients MCP).

---

## Sommaire des Catégories

1. [Diagnostic, Connexion & Santé (6 outils)](#1-diagnostic-connexion--santé)
2. [Gestion des Documents, Projet & Variantes (13 outils)](#2-gestion-des-documents-projet--variantes)
3. [Exécution de Code, Transactions & Automatisation (8 outils)](#3-exécution-de-code-transactions--automatisation)
4. [Géométrie, Espace & Requêtes Spatiales (9 outils)](#4-géométrie-espace--requêtes-spatiales)
5. [Exploration & Filtrage du Modèle BIM (8 outils)](#5-exploration--filtrage-du-modèle-bim)
6. [Modification d'Éléments & Positionnement (14 outils)](#6-modification-déléments--positionnement)
7. [Création & Modification d'Architecture & Structure (18 outils)](#7-création--modification-darchitecture--structure)
8. [Niveaux, Pièces, Cloisonnement & Finitions (15 outils)](#8-niveaux-pièces-cloisonnement--finitions)
9. [Types, Familles & Bibliothèque (6 outils)](#9-types-familles--bibliothèque)
10. [Vues, Caméras, Filtres & Gabarits (19 outils)](#10-vues-caméras-filtres--gabarits)
11. [Annotations, Cotations, Symboles & Dessin 2D (14 outils)](#11-annotations-cotations-symboles--dessin-2d)
12. [Feuilles, Cartouches & Mises en Page (12 outils)](#12-feuilles-cartouches--mises-en-page)
13. [Réservations, Trémies, Escaliers & Formes Libres (4 outils)](#13-réservations-trémies-escaliers--formes-libres)
14. [Matériaux, Nomenclatures & Quantitatifs (6 outils)](#14-matériaux-nomenclatures--quantitatifs)
15. [Paramètres & Données BIM (5 outils)](#15-paramètres--données-bim)
16. [Imports, Exports & Formats Vectoriels (11 outils)](#16-imports-exports--formats-vectoriels)
17. [Graphismes, Révisions, Sous-projets & Variantes (13 outils)](#17-graphismes-révisions-sous-projets--variantes)
18. [Serveur Compagnon CAD-MCP — Outils DXF / DWG (8 outils)](#18-serveur-compagnon-cad-mcp--outils-dxf--dwg)
19. [Super-Pouvoirs v2.5.0 : Hologrammes 3D, Solveur Rust, Three.js, Speech-to-BIM & ACV RE2020 (10 outils)](#19-super-pouvoirs-v250--hologrammes-3d-solveur-rust-threejs-speech-to-bim--acv-re2020)

---

## Conventions Générales & Standards

- **Unités** :
  - Longueurs : **millimètres (mm)**
  - Angles : **degrés décimaux (°)**
  - Surfaces : **mètres carrés (m²)**
  - Volumes : **mètres cubes (m³)**
- **Bilinguisme FR / EN** : Toutes les catégories peuvent être spécifiées en français (`Murs`, `Portes`, `Fenêtres`, `Toits`, `Sols`, `Plafonds`, `Mobilier`), en anglais (`Walls`, `Doors`, `Windows`, `Roofs`, `Floors`, `Ceilings`, `Furniture`) ou via le nom interne Revit (`OST_Walls`).
- **Support Multi-Documents** : Chaque outil accepte les paramètres optionnels `documentTitle` (nom du fichier) ou `documentPath`. Si omis, l'outil s'applique au document actif.
- **Mode Simulation (`dryRun`)** : Tous les outils d'écriture acceptent `dryRun: true` pour calculer et valider l'opération avec rollback automatique sans modifier la maquette.
- **Format de Réponse Normalisé** :
  - Succès : `{ "success": true, ...données, "bimWarnings": [...] }`
  - Erreur : `{ "success": false, "error": { "code": "...", "message": "...", "details": {...} } }`

---

## 1. Diagnostic, Connexion & Santé

### `revit_health_check`
- **Description** : Vérifie l'état de santé du pont loopback HTTP et renvoie la liste complète des documents ouverts avec leur statut actif (`isActive`) et de modification (`isModified`).
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_model_info`
- **Description** : Renvoie les métadonnées globales : titre, chemin, version de Revit (2027), statut lecture seule, auteur, nom et numéro de projet.
- **Paramètres** : `documentTitle` (optionnel).
- **Lecture seule** : Oui.

### `revit_get_ui_state`
- **Description** : Retourne la vue active courante, le niveau actif et les éléments actuellement sélectionnés par l'utilisateur.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_bring_to_front`
- **Description** : Force la fenêtre principale d'Autodesk Revit au premier plan de l'écran Windows.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.

### `revit_dismiss_dialogs`
- **Description** : Tente de fermer ou valider les boîtes de dialogue secondaires non bloquantes ouvertes dans l'interface Revit.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.

### `revit_get_log`
- **Description** : Extrait les dernières lignes du journal d'audit de l'add-in (`RevitMCP.audit.log`).
- **Paramètres** : `linesCount` (entier, défaut: 50).
- **Lecture seule** : Oui.

---

## 2. Gestion des Documents, Projet & Variantes

### `revit_list_open_documents`
- **Description** : Liste tous les documents (projets et familles) actuellement ouverts dans la session Revit, avec leur statut actif et d'enregistrement.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_activate_document`
- **Description** : Bascule le document actif de Revit vers le document désigné par son titre ou son chemin.
- **Paramètres** : `documentTitle` (chaîne) ou `documentPath` (chaîne).
- **Lecture seule** : Non.

### `revit_open_document`
- **Description** : Ouvre un fichier `.rvt` ou `.rfa` sur disque dans la session Revit.
- **Paramètres** : `filePath` (chaîne), `audit` (booléen optionnel), `detachFromCentral` (booléen optionnel).
- **Lecture seule** : Non.

### `revit_close_document`
- **Description** : Ferme le document spécifié ou actif, avec ou sans enregistrement des modifications.
- **Paramètres** : `saveModified` (booléen, défaut: false), `documentTitle` (optionnel).
- **Lecture seule** : Non.

### `revit_save_document`
- **Description** : Enregistre le document actif.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.

### `revit_save_as`
- **Description** : Effectue un "Enregistrer sous" du document vers un nouveau chemin de fichier.
- **Paramètres** : `filePath` (chaîne), `overwrite` (booléen, défaut: false).
- **Lecture seule** : Non.

### `revit_purge_unused`
- **Description** : Purge les types, familles et styles non utilisés dans le projet pour réduire la taille du fichier.
- **Paramètres** : `dryRun` (optionnel).
- **Lecture seule** : Non.

### `revit_set_project_info`
- **Description** : Renseigne les métadonnées officielles du projet (nom du client, adresse, nom du projet, numéro, statut).
- **Paramètres** : `clientName`, `projectAddress`, `projectName`, `projectNumber`, `projectStatus`.
- **Lecture seule** : Non.

### `revit_copy_elements_between_documents`
- **Description** : Copie des éléments d'un document source vers un document cible avec transformation géométrique optionnelle.
- **Paramètres** : `sourceDocumentTitle`, `targetDocumentTitle`, `elementIds` (liste d'IDs).
- **Lecture seule** : Non.

### `revit_get_design_options`
- **Description** : Liste l'ensemble des jeux d'options de conception (`DesignOptionSet`) et leurs variantes (`DesignOption`).
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_create_design_option`
- **Description** : Crée un nouveau jeu d'options ou ajoute une variante à un jeu existant.
- **Paramètres** : `setName` (chaîne), `optionName` (chaîne), `isPrimary` (booléen optionnel).
- **Lecture seule** : Non.

### `revit_move_elements_to_design_option`
- **Description** : Affecte une sélection d'éléments à une option de conception spécifiée (ou au modèle principal si null).
- **Paramètres** : `elementIds` (liste d'IDs), `optionId` (entier optionnel).
- **Lecture seule** : Non.

### `revit_set_view_design_option`
- **Description** : Configure quelle option de conception est affichée dans une vue donnée.
- **Paramètres** : `viewId` (entier), `optionId` (entier).
- **Lecture seule** : Non.

---

## 3. Exécution de Code, Transactions & Automatisation

> [!NOTE]
> L'exécution de code dynamique requiert `"allowCodeExecution": true` dans le fichier `RevitMCP.config.json` de l'add-in.

### `revit_execute_csharp`
- **Description** : Compile (via Roslyn) et exécute un script C# dynamique en mémoire au sein de la transaction Revit active.
- **Paramètres** : `code` (chaîne C#), `dryRun` (optionnel).
- **Lecture seule** : Non.

### `revit_execute_python`
- **Description** : Exécute un script Python dans le contexte de l'API Revit.
- **Paramètres** : `code` (chaîne Python), `dryRun` (optionnel).
- **Lecture seule** : Non.

### `revit_batch`
- **Description** : Exécute une série séquentielle d'appels d'outils MCP dans une seule transaction groupée, avec mode atomique optionnel (`atomic: true`).
- **Paramètres** : `operations` (liste d'opérations `{ tool, parameters }`), `atomic` (booléen).
- **Lecture seule** : Non.

### `revit_undo`
- **Description** : Annule la dernière transaction effectuée dans Revit.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.

### `revit_redo`
- **Description** : Rétablit la dernière transaction annulée.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.

### `revit_get_transaction_history`
- **Description** : Renvoie l'historique chronologique des transactions récentes exécutées dans la session Revit.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_list_dynamo_scripts`
- **Description** : Recherche et liste les scripts Dynamo (`.dyn`) disponibles dans les répertoires du projet.
- **Paramètres** : `folderPath` (optionnel).
- **Lecture seule** : Oui.

### `revit_run_dynamo_inline`
- **Description** : Lance l'exécution d'un script Dynamo avec injection de paramètres.
- **Paramètres** : `scriptPath` (chaîne), `inputs` (objet clé-valeur).
- **Lecture seule** : Non.

---

## 4. Géométrie, Espace & Requêtes Spatiales

### `revit_get_height_at_point`
- **Description** : Effectue un lancer de rayon vertical (Raycast) depuis un point (x, y, z en mm) pour détecter la face inférieure d'un élément horizontal (toit, plancher, plafond).
- **Comportement certifié** :
  - **Ne modifie jamais les vues de l'utilisateur** : utilise une vue 3D dédiée `MCP_Raycast_3D` sans gabarit de vue, garantissant que les toits, planchers et plafonds restent visibles et que la zone de coupe est désactivée.
  - Le résultat est rigoureusement constant même si les toits ou planchers sont masqués dans la vue 3D active de l'utilisateur.
- **Paramètres** : `x` (mm), `y` (mm), `z` (mm, optionnel, défaut: niveau 0), `direction` (optionnel: `"UP"` ou `"DOWN"`, défaut: `"UP"`).
- **Retour** : `{ point, heightMm, surfaceZ, hitElementId, hitCategory, hitName, success }`.
- **Lecture seule** : Oui.

### `revit_get_wall_openings`
- **Description** : Analyse détaillée de toutes les ouvertures (portes, fenêtres, trémies) hébergées dans un mur, optimisée pour la génération de plans cotés.
- **Paramètres** : `wallId` (entier).
- **Retour** :
  - Données du mur : `startPoint`, `endPoint` (x, y en mm), `direction` unitaire, `exteriorCardinal` (orientation cardinale N/S/E/O selon le Nord du projet), `thicknessMm` (épaisseur), `locationLine` (ligne de justification).
  - Coordonnées des angles extérieurs réels du mur : `exteriorCornerStart`, `exteriorCornerEnd`.
  - Pour chaque ouverture :
    - `id`, `category`, `typeName`, `widthMm`, `heightMm`, `sillHeightMm`, `headHeightMm` (hauteur de linteau).
    - `typeNameMismatch: boolean` (vrai si la géométrie réelle diffère du nom commercial du type).
    - Bords gauche et droit en coordonnées projet absolues : `leftEdgeProject`, `rightEdgeProject`.
    - Distances cotées depuis les deux angles extérieurs du mur : `distanceFromExteriorStartCornerMm`, `distanceFromExteriorEndCornerMm`.
- **Lecture seule** : Oui.

### `revit_get_element_location`
- **Description** : Renvoie la position spatiale exacte d'un élément (point XYZ en mm et rotation en degrés pour les occurrences ponctuelles, ou polyligne/courbe pour les éléments linéaires).
- **Paramètres** : `elementId` (entier).
- **Lecture seule** : Oui.

### `revit_get_element_geometry`
- **Description** : Extrait la géométrie solide d'un élément (arêtes, faces, maillage triangulé) avec coordonnées en millimètres.
- **Paramètres** : `elementId` (entier), `detailLevel` (optionnel: `"Coarse"`, `"Medium"`, `"Fine"`).
- **Lecture seule** : Oui.

### `revit_get_elements_in_region`
- **Description** : Sélectionne tous les éléments intersectant une région 3D (boîte englobante min/max ou polygone 2D extrudé).
- **Paramètres** : `min` `{x,y,z}`, `max` `{x,y,z}` (en mm), `category` (optionnel).
- **Lecture seule** : Oui.

### `revit_measure_distance`
- **Description** : Calcule la distance euclidienne minimale 3D exacte entre deux éléments ou deux points XYZ.
- **Paramètres** : `elementId1`, `elementId2` (ou `point1`, `point2`).
- **Lecture seule** : Oui.

### `revit_check_clearance`
- **Description** : Vérifie le respect d'une distance de garde minimale (dégagement de sécurité, passage de gaine ou accès PMR) autour d'un élément.
- **Paramètres** : `elementId` (entier), `requiredClearanceMm` (nombre).
- **Lecture seule** : Oui.

### `revit_get_room_at_point`
- **Description** : Détecte quelle pièce (`Room`) ou espace englobe un point XYZ donné.
- **Paramètres** : `point` `{x, y, z}` (mm).
- **Lecture seule** : Oui.

### `revit_detect_clashes`
- **Description** : Moteur de détection de conflits géométriques 3D entre deux ensembles de catégories ou d'éléments (ex: Réseaux CVC vs Structure).
- **Paramètres** : `categorySetA` (chaîne ou liste), `categorySetB` (chaîne ou liste), `toleranceMm` (nombre, optionnel).
- **Lecture seule** : Oui.

---

## 5. Exploration & Filtrage du Modèle BIM

### `revit_get_elements`
- **Description** : Outil d'interrogation universel du modèle BIM avec filtrage multi-critères : catégorie bilingue, niveau, type, texte de recherche et boîte spatiale (`bboxFilter`).
- **Paramètres** : `category` (chaîne, ex: `"Murs"`, `"Portes"`), `levelName` (optionnel), `typeId` (optionnel), `bboxFilter` (optionnel), `limit` (défaut: 100), `includeGeometry` (booléen optionnel).
- **Lecture seule** : Oui.

### `revit_get_element_parameters`
- **Description** : Renvoie l'intégralité des paramètres d'instance d'un élément avec typage précis (`String`, `Double`, `Integer`, `ElementId`), lecture seule (`isReadOnly`), nom et groupe.
- **Paramètres** : `elementId` (entier).
- **Lecture seule** : Oui.

### `revit_count_elements`
- **Description** : Dénombre rapidement les éléments d'une ou plusieurs catégories sans surcoût de transfert réseau.
- **Paramètres** : `category` (chaîne ou liste optionnelle).
- **Lecture seule** : Oui.

### `revit_list_categories`
- **Description** : Liste toutes les catégories BIM actives et présentes dans le document courant avec leur nom en français, anglais et leur identifiant BuiltIn.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_levels`
- **Description** : Renvoie la liste ordonnée des niveaux du projet avec leur élévation en millimètres.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_families`
- **Description** : Liste les familles chargées dans le projet par catégorie, avec leurs types et l'option `includeTypeParameters: true`.
- **Paramètres** : `category` (optionnel), `includeTypeParameters` (booléen optionnel).
- **Lecture seule** : Oui.

### `revit_find_elements_by_parameter`
- **Description** : Recherche les éléments dont un paramètre spécifique correspond à une valeur ou une expression régulière.
- **Paramètres** : `parameterName` (chaîne), `value` (chaîne ou nombre), `category` (optionnel).
- **Lecture seule** : Oui.

### `revit_get_materials`
- **Description** : Liste les matériaux définis dans le projet avec leurs propriétés physiques, thermiques et graphiques.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

---

## 6. Modification d'Éléments & Positionnement

### `revit_set_element_location`
- **Description** : Modifie la position absolue XYZ d'un élément ponctuel ou les points de début/fin d'un élément linéaire.
- **Paramètres** : `elementId` (entier), `point` `{x,y,z}` (mm), `rotationDeg` (optionnel).
- **Lecture seule** : Non.

### `revit_move_element`
- **Description** : Déplace un élément selon un vecteur relatif `delta` `{dx, dy, dz}` en mm.
- **Paramètres** : `elementId` (entier), `delta` `{dx, dy, dz}`.
- **Lecture seule** : Non.

### `revit_rotate_element`
- **Description** : Effectue une rotation d'un élément d'un angle donné en degrés autour d'un axe vertical passant par un point (ou par son centre).
- **Paramètres** : `elementId` (entier), `angleDeg` (nombre), `centerPoint` (optionnel).
- **Lecture seule** : Non.

### `revit_change_element_type`
- **Description** : Modifie le type de famille affecté à une instance existante.
- **Paramètres** : `elementId` (entier), `newTypeId` (entier).
- **Lecture seule** : Non.

### `revit_copy_elements`
- **Description** : Duplique un ou plusieurs éléments avec un vecteur de translation relatif.
- **Paramètres** : `elementIds` (liste), `offset` `{dx, dy, dz}`.
- **Lecture seule** : Non.

### `revit_mirror_elements`
- **Description** : Applique une symétrie miroir (avec ou sans copie) selon un axe ou un plan.
- **Paramètres** : `elementIds` (liste), `axisPoint1`, `axisPoint2`, `copy` (booléen).
- **Lecture seule** : Non.

### `revit_align_elements`
- **Description** : Aligne une face ou une référence d'un élément sur une référence d'un autre élément.
- **Paramètres** : `referenceElementId`, `targetElementId`, `alignmentType`.
- **Lecture seule** : Non.

### `revit_flip_element`
- **Description** : Inverse l'orientation d'une porte, fenêtre ou mur (sens d'ouverture, face intérieure/extérieure, sens de la main).
- **Paramètres** : `elementId` (entier), `flipDirection` (`"Facing"`, `"Hand"` ou `"Both"`).
- **Lecture seule** : Non.

### `revit_hosted_set_position`
- **Description** : Repositionne une instance hébergée le long de son élément hôte (ex: distance d'une porte le long d'un mur).
- **Paramètres** : `instanceId` (entier), `distanceMm` (nombre).
- **Lecture seule** : Non.

### `revit_set_element_pinned`
- **Description** : Verrouille (Pin) ou déverrouille (Unpin) un élément pour empêcher son déplacement accidentel.
- **Paramètres** : `elementId` (entier), `pinned` (booléen).
- **Lecture seule** : Non.

### `revit_join_geometry`
- **Description** : Joint ou sépare la géométrie de deux éléments sécants (ex: Mur et Poteau, Sol et Mur).
- **Paramètres** : `elementId1`, `elementId2`, `unjoin` (booléen optionnel).
- **Lecture seule** : Non.

### `revit_group_elements`
- **Description** : Crée un groupe Revit à partir d'une liste d'éléments sélectionnés.
- **Paramètres** : `elementIds` (liste), `groupName` (chaîne).
- **Lecture seule** : Non.

### `revit_ungroup_elements`
- **Description** : Dégroupe une occurrence de groupe en rétablissant les éléments indépendants.
- **Paramètres** : `groupId` (entier).
- **Lecture seule** : Non.

### `revit_delete_element`
- **Description** : Supprime un ou plusieurs éléments du modèle BIM.
- **Paramètres** : `elementIds` (entier ou liste d'entiers).
- **Lecture seule** : Non.

---

## 7. Création & Modification d'Architecture & Structure

### `revit_create_wall`
- **Description** : Crée un nouveau mur droit ou courbe reliant deux points avec niveau de base, niveau supérieur, hauteur non contrainte et décalages.
- **Paramètres** : `startPoint` `{x,y,z}`, `endPoint` `{x,y,z}` (mm), `levelName` (ou `levelId`), `wallTypeName` (optionnel), `heightMm` (optionnel), `isStructural` (optionnel).
- **Lecture seule** : Non.

### `revit_modify_wall`
- **Description** : Modifie la ligne de justification, la contrainte supérieure ou la hauteur d'un mur existant.
- **Paramètres** : `wallId` (entier), `heightMm` (optionnel), `topLevelName` (optionnel).
- **Lecture seule** : Non.

### `revit_split_wall`
- **Description** : Scinde un mur existant en deux segments distincts à un point spécifié.
- **Paramètres** : `wallId` (entier), `splitPoint` `{x,y,z}` (mm).
- **Lecture seule** : Non.

### `revit_attach_wall`
- **Description** : Attache le haut ou le bas d'un mur à un toit ou un plancher.
- **Paramètres** : `wallId` (entier), `targetElementId` (entier), `attachTop` (booléen).
- **Lecture seule** : Non.

### `revit_create_floor`
- **Description** : Crée un plancher à partir d'un polygone de contour fermé sur un niveau donné.
- **Paramètres** : `points` (liste de `{x, y}` en mm), `levelName` (ou `levelId`), `floorTypeName` (optionnel).
- **Lecture seule** : Non.

### `revit_modify_floor_boundary`
- **Description** : Met à jour le contour fermé d'un plancher existant.
- **Paramètres** : `floorId` (entier), `points` (liste de `{x, y}` en mm).
- **Lecture seule** : Non.

### `revit_create_roof_footprint`
- **Description** : Crée un toit par tracé à partir d'un contour polygonal fermé avec définition optionnelle des pentes par rive.
- **Paramètres** : `points` (liste de `{x, y}`), `levelName`, `slopeDeg` (optionnel), `roofTypeName` (optionnel).
- **Lecture seule** : Non.

### `revit_create_roof_extrusion`
- **Description** : Crée un toit par extrusion selon un profil vertical défini sur un plan de référence.
- **Paramètres** : `profilePoints`, `referencePlaneName`, `extrusionStartMm`, `extrusionEndMm`.
- **Lecture seule** : Non.

### `revit_modify_roof`
- **Description** : Modifie la pente, l'épaisseur ou le type d'un toit existant.
- **Paramètres** : `roofId` (entier), `slopeDeg` (optionnel), `newTypeId` (optionnel).
- **Lecture seule** : Non.

### `revit_create_ceiling`
- **Description** : Crée un plafond suspendu ou plâtre à partir d'un contour ou basé sur les limites d'une pièce.
- **Paramètres** : `points` (ou `roomId`), `levelName`, `offsetFromLevelMm` (nombre).
- **Lecture seule** : Non.

### `revit_create_structural_column`
- **Description** : Place un poteau porteur (béton, acier ou bois) entre deux niveaux.
- **Paramètres** : `point` `{x, y}`, `baseLevelName`, `topLevelName`, `typeName` (optionnel).
- **Lecture seule** : Non.

### `revit_create_structural_beam`
- **Description** : Place une poutre porteuse horizontale ou inclinée entre deux points XYZ.
- **Paramètres** : `startPoint`, `endPoint` (mm), `levelName`, `beamTypeName` (optionnel).
- **Lecture seule** : Non.

### `revit_create_beam_system`
- **Description** : Génère un système de poutrelles parallèles équidistantes à l'intérieur d'un contour de dalle.
- **Paramètres** : `boundaryPoints`, `beamSpacingMm`, `beamTypeName`, `levelName`.
- **Lecture seule** : Non.

### `revit_create_railing`
- **Description** : Crée un garde-corps le long d'une trajectoire linéaire sur un niveau ou un escalier hôte.
- **Paramètres** : `pathPoints` (liste de `{x, y}`), `levelName`, `railingTypeName` (optionnel).
- **Lecture seule** : Non.

### `revit_create_stair`
- **Description** : Crée un escalier droit ou balancé entre deux niveaux avec calcul automatique des girons et contremarches.
- **Paramètres** : `startPoint`, `endPoint`, `baseLevelName`, `topLevelName`, `stairWidthMm`.
- **Lecture seule** : Non.

### `revit_place_family_instance`
- **Description** : Place une occurrence de composant de famille (porte, fenêtre, mobilier, équipement) à des coordonnées XYZ avec rotation optionnelle en degrés. Charge automatiquement la famille si elle n'est pas encore présente.
- **Paramètres** : `familyName` (chaîne), `typeName` (chaîne), `point` `{x,y,z}` (mm), `levelName` (optionnel), `hostId` (optionnel), `rotationDeg` (optionnel), `familyPath` (optionnel).
- **Lecture seule** : Non.

### `revit_create_simple_family`
- **Description** : Génère une famille paramétrique basique (forme géométrique extrudée) et la charge dans le projet.
- **Paramètres** : `familyName`, `categoryName`, `widthMm`, `depthMm`, `heightMm`.
- **Lecture seule** : Non.

### `revit_edit_family_parameters`
- **Description** : Ouvre une famille en arrière-plan pour éditer ses cotes ou paramètres intrinsèques puis la recharge dans le projet.
- **Paramètres** : `familyName`, `parameters` (objet clé-valeur).
- **Lecture seule** : Non.

---

## 8. Niveaux, Pièces, Cloisonnement & Finitions

### `revit_create_level`
- **Description** : Crée un nouveau niveau dans le projet à une élévation spécifique en millimètres.
- **Paramètres** : `name` (chaîne), `elevationMm` (nombre), `createPlanViews` (booléen optionnel).
- **Lecture seule** : Non.

### `revit_modify_level`
- **Description** : Modifie l'élévation ou renomme un niveau existant.
- **Paramètres** : `levelId` (entier), `name` (optionnel), `elevationMm` (optionnel).
- **Lecture seule** : Non.

### `revit_create_room`
- **Description** : Crée et place une nouvelle pièce (`Room`) à l'intérieur d'un contour fermé délimité par des murs.
- **Paramètres** : `point` `{x, y}` (mm), `levelName`, `name` (chaîne), `number` (chaîne optionnelle).
- **Lecture seule** : Non.

### `revit_create_room_separation_lines`
- **Description** : Trace des lignes de séparation de pièces pour partitionner un grand espace sans construire de mur physique.
- **Paramètres** : `lines` (liste de segments `[{ start: {x,y}, end: {x,y} }]`), `viewId` (ou `levelName`).
- **Lecture seule** : Non.

### `revit_place_room_at_point`
- **Description** : Place une pièce non encore placée issue de la nomenclature de pièces sur un point du plan.
- **Paramètres** : `roomId` (entier), `point` `{x, y}` (mm), `levelName`.
- **Lecture seule** : Non.

### `revit_delete_unplaced_rooms`
- **Description** : Recherche et supprime toutes les pièces non placées ou redondantes dans le modèle.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.

### `revit_tag_rooms`
- **Description** : Place automatiquement les étiquettes de pièces (nom, numéro, surface en m²) dans une vue en plan.
- **Paramètres** : `viewId` (entier), `tagTypeName` (optionnel).
- **Lecture seule** : Non.

### `revit_get_rooms`
- **Description** : Liste l'ensemble des pièces du projet avec nom, numéro, niveau, surface (m²), périmètre et l'option `includeBoundary: true` pour extraire les coordonnées de contour.
- **Paramètres** : `levelName` (optionnel), `includeBoundary` (booléen optionnel).
- **Lecture seule** : Oui.

### `revit_get_room_finishes`
- **Description** : Extrait la liste des finitions renseignées pour les pièces (finition de sol, mur, plafond, plinthe).
- **Paramètres** : `roomId` (optionnel).
- **Lecture seule** : Oui.

### `revit_set_room_finishes_bulk`
- **Description** : Applique en masse les finitions architecturales sur une liste de pièces selon une matrice de données.
- **Paramètres** : `finishes` (liste d'objets `{ roomId, floorFinish, wallFinish, ceilingFinish }`).
- **Lecture seule** : Non.

### `revit_set_room_color_fill`
- **Description** : Configure le schéma de couleurs appliqué aux pièces dans une vue en plan (par nom, fonction ou service).
- **Paramètres** : `viewId` (entier), `schemeName` (chaîne).
- **Lecture seule** : Non.

### `revit_create_color_fill_scheme`
- **Description** : Crée un nouveau schéma de remplissage couleur basé sur un paramètre personnalisé de pièce.
- **Paramètres** : `schemeName`, `parameterName`.
- **Lecture seule** : Non.

### `revit_apply_color_fill`
- **Description** : Associe un schéma de couleurs existant à une vue en plan désignée.
- **Paramètres** : `viewId`, `schemeId`.
- **Lecture seule** : Non.

### `revit_create_area_plan`
- **Description** : Génère un plan de surface réglementaire (Surface de Plancher, SDP, SHAB, Emprise au sol) pour un niveau.
- **Paramètres** : `levelName`, `areaSchemeName`.
- **Lecture seule** : Non.

### `revit_create_area`
- **Description** : Crée et place un contour de surface réglementaire dans un plan de surface.
- **Paramètres** : `viewId`, `point` `{x, y}`, `name`.
- **Lecture seule** : Non.

---

## 9. Types, Familles & Bibliothèque

### `revit_duplicate_type`
- **Description** : Duplique un type de famille existant sous un nouveau nom.
- **Paramètres** : `sourceTypeId` (entier), `newTypeName` (chaîne).
- **Retour** : `{ success, newTypeId, newTypeName }`.
- **Lecture seule** : Non.

### `revit_set_type_parameters`
- **Description** : Modifie un ensemble de paramètres d'un type de famille avec validation stricte des paramètres inexistants et en lecture seule.
- **Paramètres** : `typeId` (entier), `parameters` (objet clé-valeur).
- **Retour** : `{ success, updatedParameters, unknownParameters, readOnlyParameters }`.
- **Lecture seule** : Non.

### `revit_get_type_parameters`
- **Description** : Liste tous les paramètres d'un type avec valeur, unité et statut d'édition.
- **Paramètres** : `typeId` (entier).
- **Lecture seule** : Oui.

### `revit_create_window_or_door_type`
- **Description** : Crée rapidement un nouveau type de porte ou fenêtre avec largeur et hauteur en millimètres.
- **Paramètres** : `baseFamilyName`, `newTypeName`, `widthMm`, `heightMm`.
- **Lecture seule** : Non.

### `revit_load_family`
- **Description** : Charge un fichier de famille `.rfa` depuis le disque dans le projet Revit actif.
- **Paramètres** : `filePath` (chaîne).
- **Lecture seule** : Non.

### `revit_list_family_library`
- **Description** : Parcourt un répertoire de bibliothèques sur disque et liste les familles `.rfa` disponibles avec leurs catégories.
- **Paramètres** : `directoryPath` (chaîne).
- **Lecture seule** : Oui.

---

## 10. Vues, Caméras, Filtres & Gabarits

### `revit_get_views`
- **Description** : Liste l'ensemble des vues du projet (plans d'étage, plafonds, coupes, façades, 3D, légendes, détails) avec nom, type, niveau associé et gabarit actif.
- **Paramètres** : `viewType` (optionnel).
- **Lecture seule** : Oui.

### `revit_activate_view`
- **Description** : Active la vue spécifiée dans la fenêtre graphique principale de Revit.
- **Paramètres** : `viewId` (entier).
- **Lecture seule** : Non.

### `revit_create_plan_views_for_levels`
- **Description** : Crée les plans d'étage et plans de plafond pour tous les niveaux n'en possédant pas encore.
- **Paramètres** : `levelIds` (liste optionnelle).
- **Lecture seule** : Non.

### `revit_create_section_view`
- **Description** : Crée une vue en coupe orthogonale ou inclinée passant par deux points XYZ avec profondeur de champ en millimètres.
- **Paramètres** : `startPoint`, `endPoint` (mm), `viewName` (chaîne), `depthMm` (optionnel).
- **Lecture seule** : Non.

### `revit_create_3d_view`
- **Description** : Crée une nouvelle vue 3D axonométrique ou perspective avec orientation de caméra.
- **Paramètres** : `viewName` (chaîne), `eyePosition` (optionnel), `targetPosition` (optionnel).
- **Lecture seule** : Non.

### `revit_create_elevation_views`
- **Description** : Place un symbole d'élévation et génère les 4 vues de façades (Nord, Sud, Est, Ouest).
- **Paramètres** : `point` `{x, y}`, `prefix` (optionnel).
- **Lecture seule** : Non.

### `revit_create_drafting_view`
- **Description** : Crée une vue de dessin 2D indépendante du modèle 3D à une échelle spécifiée.
- **Paramètres** : `viewName` (chaîne), `scale` (entier, ex: 50 pour 1:50).
- **Lecture seule** : Non.

### `revit_create_legend_view`
- **Description** : Crée une vue de légende réutilisable sur plusieurs feuilles.
- **Paramètres** : `legendName` (chaîne), `scale` (entier).
- **Lecture seule** : Non.

### `revit_place_legend_component`
- **Description** : Place un composant de légende (symbole de porte, fenêtre ou matériau) dans une vue de légende.
- **Paramètres** : `legendViewId`, `familySymbolId`, `point` `{x, y}`.
- **Lecture seule** : Non.

### `revit_duplicate_view`
- **Description** : Duplique une vue existante (simple, avec les détails ou en tant que vue dépendante).
- **Paramètres** : `viewId` (entier), `duplicateOption` (`"Duplicate"`, `"WithDetailing"`, `"AsDependent"`), `newViewName` (optionnel).
- **Lecture seule** : Non.

### `revit_set_view_properties`
- **Description** : Modifie l'échelle (ex: 100 pour 1:100), le niveau de détail (`"Coarse"`, `"Medium"`, `"Fine"`) ou le style visuel (`"Wireframe"`, `"HiddenLine"`, `"Shaded"`, `"Realistic"`) d'une vue.
- **Paramètres** : `viewId` (entier), `scale` (optionnel), `detailLevel` (optionnel), `visualStyle` (optionnel).
- **Lecture seule** : Non.

### `revit_set_category_visibility`
- **Description** : Active ou masque une catégorie BIM dans une vue spécifique.
- **Paramètres** : `viewId` (entier), `category` (chaîne bilingue), `visible` (booléen).
- **Lecture seule** : Non.

### `revit_hide_elements_in_view`
- **Description** : Masque définitivement une liste d'éléments dans une vue donnée.
- **Paramètres** : `viewId` (entier), `elementIds` (liste).
- **Lecture seule** : Non.

### `revit_isolate_elements_in_view`
- **Description** : Masque tous les éléments de la vue à l'exception de ceux passés en paramètre.
- **Paramètres** : `viewId` (entier), `elementIds` (liste).
- **Lecture seule** : Non.

### `revit_crop_view_to_elements`
- **Description** : Ajuste automatiquement le cadrage d'une vue (Crop Box) au plus près d'une sélection d'éléments avec marge en mm.
- **Paramètres** : `viewId` (entier), `elementIds` (liste), `marginMm` (nombre).
- **Lecture seule** : Non.

### `revit_create_section_box`
- **Description** : Active et redimensionne la zone de coupe 3D (Section Box) d'une vue 3D autour d'une boîte englobante min/max.
- **Paramètres** : `viewId` (entier), `min` `{x,y,z}`, `max` `{x,y,z}` (mm).
- **Lecture seule** : Non.

### `revit_apply_view_template_bulk`
- **Description** : Applique un gabarit de vue à une liste de vues sélectionnées.
- **Paramètres** : `viewIds` (liste), `templateName` (ou `templateId`).
- **Lecture seule** : Non.

### `revit_create_view_template_from_view`
- **Description** : Crée un nouveau gabarit de vue basé sur la configuration graphique de la vue actuelle.
- **Paramètres** : `sourceViewId`, `templateName`.
- **Lecture seule** : Non.

### `revit_create_view_filter`
- **Description** : Crée un filtre de vue basé sur des règles de paramètres (ex: Murs coupe-feu >= 60 min).
- **Paramètres** : `filterName`, `categories`, `rules` (critères de filtres).
- **Lecture seule** : Non.

### `revit_apply_view_filter`
- **Description** : Associe un filtre de vue à une vue en définissant sa visibilité et sa couleur de surcharge.
- **Paramètres** : `viewId`, `filterId`, `visible`, `color` `{r,g,b}` (optionnel).
- **Lecture seule** : Non.

### `revit_delete_views`
- **Description** : Supprime une ou plusieurs vues du projet.
- **Paramètres** : `viewIds` (liste).
- **Lecture seule** : Non.

### `revit_close_inactive_views`
- **Description** : Ferme toutes les vues ouvertes en arrière-plan pour libérer la mémoire vive (RAM) de Revit.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.

---

## 11. Annotations, Cotations, Symboles & Dessin 2D

### `revit_create_linear_dimension`
- **Description** : Crée une chaîne de cotes linéaires alignées entre plusieurs références géométriques dans une vue en plan ou coupe.
- **Paramètres** : `viewId` (entier), `line` `{ start: {x,y}, end: {x,y} }`, `elementIds` (liste de références).
- **Lecture seule** : Non.

### `revit_auto_dimension_walls`
- **Description** : Génère automatiquement la ligne de cotation complète d'une façade ou d'une pièce (murs totaux, retraits, épaisseurs).
- **Paramètres** : `viewId` (entier), `wallIds` (liste d'entiers), `offsetMm` (nombre).
- **Lecture seule** : Non.

### `revit_dimension_openings`
- **Description** : Cote automatiquement l'ensemble des ouvertures (portes et fenêtres) hébergées dans un mur.
- **Comportement certifié** :
  - Si `viewId` est omis, sélectionne automatiquement le plan d'étage associé au niveau du mur.
  - Si un `viewId` d'une vue 3D est fourni, bascule automatiquement sur le plan d'étage adéquat.
  - Si le mur ne possède aucune ouverture, renvoie immédiatement `{ success: false, error: { code: "NO_OPENINGS", message: "..." } }`.
- **Paramètres** : `wallId` (entier), `viewId` (optionnel), `offsetMm` (optionnel, défaut: 600 mm).
- **Lecture seule** : Non.

### `revit_create_spot_elevation`
- **Description** : Place une cote de niveau (Spot Elevation) en un point donné d'une dalle, rampe ou toit.
- **Paramètres** : `viewId` (entier), `point` `{x,y,z}`, `leader` (booléen optionnel).
- **Lecture seule** : Non.

### `revit_create_tag`
- **Description** : Place une étiquette d'annotation sur un élément dans une vue (porte, fenêtre, mur, équipement).
- **Paramètres** : `viewId` (entier), `targetElementId` (entier), `headPoint` `{x,y}` (mm), `hasLeader` (booléen).
- **Lecture seule** : Non.

### `revit_tag_all`
- **Description** : Exécute la fonction native "Tout étiqueter" pour une ou plusieurs catégories dans une vue.
- **Paramètres** : `viewId` (entier), `category` (chaîne), `tagTypeName` (optionnel).
- **Lecture seule** : Non.

### `revit_create_text_note`
- **Description** : Place une note textuelle d'annotation dans une vue avec taille de police et justification.
- **Paramètres** : `viewId` (entier), `point` `{x, y}`, `text` (chaîne), `typeName` (optionnel).
- **Lecture seule** : Non.

### `revit_create_detail_curve`
- **Description** : Trace une courbe de détail 2D (ligne, arc, ellipse) dans une vue.
- **Paramètres** : `viewId` (entier), `startPoint`, `endPoint`, `lineStyle` (optionnel).
- **Lecture seule** : Non.

### `revit_create_filled_region`
- **Description** : Crée une zone remplie (hachure 2D) à partir d'un polygone fermé.
- **Paramètres** : `viewId` (entier), `boundaryPoints`, `fillPatternName`.
- **Lecture seule** : Non.

### `revit_create_grid`
- **Description** : Crée une file de trame structurelle (quadrillage) droite reliant deux points.
- **Paramètres** : `startPoint`, `endPoint` (mm), `name` (chaîne, ex: `"A"`, `"1"`).
- **Lecture seule** : Non.

### `revit_create_detail_line`
- **Description** : Raccourci pour tracer une ligne de détail simple entre deux points.
- **Paramètres** : `viewId`, `startPoint`, `endPoint`.
- **Lecture seule** : Non.

### `revit_create_line_style`
- **Description** : Crée un nouveau style de ligne personnalisé (couleur, épaisseur de 1 à 16, motif de trait).
- **Paramètres** : `styleName`, `weight` (1-16), `color` `{r,g,b}`, `patternName`.
- **Lecture seule** : Non.

### `revit_place_annotation_symbol`
- **Description** : Place un symbole générique d'annotation (flèche Nord, niveau, symbole de détail).
- **Paramètres** : `viewId`, `symbolName`, `point` `{x, y}`.
- **Lecture seule** : Non.

### `revit_place_detail_component`
- **Description** : Insère un composant de détail 2D issu de la bibliothèque (coupe de profilé, étanchéité).
- **Paramètres** : `viewId`, `familySymbolId`, `point` `{x, y}`.
- **Lecture seule** : Non.

---

## 12. Feuilles, Cartouches & Mises en Page

### `revit_get_sheets`
- **Description** : Liste l'ensemble des feuilles du projet avec numéro de plan, titre, indice de révision et cartouche associé.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_create_sheet`
- **Description** : Crée une nouvelle feuille dans le projet avec numéro de plan, nom et famille de cartouche.
- **Paramètres** : `sheetNumber` (chaîne), `sheetName` (chaîne), `titleBlockFamilyName` (optionnel).
- **Lecture seule** : Non.

### `revit_duplicate_sheet`
- **Description** : Duplique une feuille existante avec option de duplication des vues placées.
- **Paramètres** : `sheetId` (entier), `newSheetNumber`, `newSheetName`, `duplicateViews` (booléen).
- **Lecture seule** : Non.

### `revit_change_title_block`
- **Description** : Remplace le cartouche d'une feuille par un autre type ou format papier.
- **Paramètres** : `sheetId` (entier), `newTitleBlockTypeId` (entier).
- **Lecture seule** : Non.

### `revit_set_sheet_parameters`
- **Description** : Renseigne les paramètres spécifiques d'une feuille (auteur, dessinateur, vérificateur, date).
- **Paramètres** : `sheetId` (entier), `drawnBy`, `checkedBy`, `designedBy`, `issueDate`.
- **Lecture seule** : Non.

### `revit_place_view_on_sheet`
- **Description** : Positionne une vue (plan, coupe, 3D, légende) sur une feuille avec coordonnées du centre de la fenêtre en mm.
- **Paramètres** : `sheetId` (entier), `viewId` (entier), `centerPoint` `{x, y}` (mm).
- **Lecture seule** : Non.

### `revit_get_viewports`
- **Description** : Liste les fenêtres de vues (`Viewport`) placées sur une feuille avec leurs dimensions et positions.
- **Paramètres** : `sheetId` (entier).
- **Lecture seule** : Oui.

### `revit_move_viewport`
- **Description** : Déplace une fenêtre de vue existante sur la feuille.
- **Paramètres** : `viewportId` (entier), `newCenterPoint` `{x, y}` (mm).
- **Lecture seule** : Non.

### `revit_set_viewport_type`
- **Description** : Modifie le type de titre de fenêtre de vue (sans titre, avec ligne de titre, avec échelle).
- **Paramètres** : `viewportId`, `viewportTypeId`.
- **Lecture seule** : Non.

### `revit_align_viewports`
- **Description** : Aligne plusieurs fenêtres de vue sur plusieurs feuilles (ex: calage exact du niveau 0 et du niveau 1 sur le même repère cartouche).
- **Paramètres** : `referenceViewportId`, `targetViewportIds` (liste).
- **Lecture seule** : Non.

### `revit_place_schedule_on_sheet`
- **Description** : Insère une nomenclature existante sous forme de tableau sur une feuille.
- **Paramètres** : `sheetId`, `scheduleId`, `point` `{x, y}`.
- **Lecture seule** : Non.

### `revit_auto_populate_sheets`
- **Description** : Automatise la génération complète d'un carnet de plans : création des feuilles par niveau, placement centré des plans et application des cartouches.
- **Paramètres** : `sheetPrefix`, `titleBlockName`.
- **Lecture seule** : Non.

---

## 13. Réservations, Trémies, Escaliers & Formes Libres

### `revit_create_opening`
- **Description** : Perce une trémie ou réservation rectangulaire ou polygonale dans un plancher, un toit ou un mur.
- **Paramètres** : `hostElementId` (entier), `boundaryPoints` (liste de `{x, y}` ou `{x, y, z}`).
- **Lecture seule** : Non.

### `revit_create_ladder`
- **Description** : Crée une échelle de meunier ou échelle à crinoline paramétrique entre deux niveaux.
- **Paramètres** : `startPoint`, `endPoint`, `ladderWidthMm`.
- **Lecture seule** : Non.

### `revit_create_direct_shape`
- **Description** : Insère une géométrie solide 3D libre (`DirectShape`) issue de calculs mathématiques ou maillages polygonaux sous une catégorie BIM choisie.
- **Paramètres** : `category` (chaîne), `geometryJson` (définition des faces et sommets en mm).
- **Lecture seule** : Non.

### `revit_paint_face`
- **Description** : Applique un matériau de finition spécifique sur une face isolée d'un mur ou d'une dalle sans modifier la composition du type.
- **Paramètres** : `elementId` (entier), `faceIndex` (entier), `materialId` (entier).
- **Lecture seule** : Non.

---

## 14. Matériaux, Nomenclatures & Quantitatifs

### `revit_create_material`
- **Description** : Crée un nouveau matériau BIM avec nom, couleur de rendu, motif de surface et motif de coupe.
- **Paramètres** : `name` (chaîne), `color` `{r,g,b}`, `transparency` (0-100).
- **Lecture seule** : Non.

### `revit_assign_material`
- **Description** : Affecte un matériau à une couche de structure ou à un paramètre de matériau d'élément.
- **Paramètres** : `elementId`, `parameterName`, `materialId`.
- **Lecture seule** : Non.

### `revit_create_schedule`
- **Description** : Crée une nouvelle nomenclature pour une catégorie donnée (ex: Pièces, Fenêtres, Murs) avec sélection des champs de colonnes.
- **Paramètres** : `category` (chaîne bilingue), `scheduleName` (chaîne), `fields` (liste de noms de paramètres).
- **Lecture seule** : Non.

### `revit_get_schedule_data`
- **Description** : Extrait l'intégralité des lignes et colonnes d'une nomenclature sous forme de tableau JSON structuré.
- **Paramètres** : `scheduleId` (entier).
- **Lecture seule** : Oui.

### `revit_get_material_takeoff`
- **Description** : Calcule le relevé complet des matériaux du projet avec surfaces cumulées (m²) et volumes (m³).
- **Paramètres** : `category` (optionnel).
- **Lecture seule** : Oui.

### `revit_export_material_takeoff_csv`
- **Description** : Exporte le métré quantitatif des matériaux dans un fichier CSV tabulé ou délimité par des points-virgules.
- **Paramètres** : `outputPath` (chaîne).
- **Lecture seule** : Non.

---

## 15. Paramètres & Données BIM

### `revit_set_parameter`
- **Description** : Modifie la valeur d'un paramètre d'instance sur un élément avec conversion automatique d'unités (longueur en mm, texte, entier, booléen).
- **Paramètres** : `elementId` (entier), `parameterName` (chaîne), `value` (chaîne, nombre ou booléen).
- **Lecture seule** : Non.

### `revit_batch_set_parameters`
- **Description** : Applique en une seule requête un ensemble de modifications de paramètres sur plusieurs éléments.
- **Paramètres** : `modifications` (liste d'objets `{ elementId, parameterName, value }`).
- **Lecture seule** : Non.

### `revit_import_csv_parameters`
- **Description** : Met à jour les paramètres des éléments à partir d'un fichier CSV en utilisant une colonne clé d'identifiant (ID ou Code).
- **Paramètres** : `csvFilePath`, `keyColumnName`.
- **Lecture seule** : Non.

### `revit_copy_parameter_values`
- **Description** : Copie la valeur d'un paramètre source vers un paramètre cible sur une sélection d'éléments.
- **Paramètres** : `elementIds`, `sourceParamName`, `targetParamName`.
- **Lecture seule** : Non.

### `revit_set_element_phase`
- **Description** : Affecte la phase de création ou de démolition d'un élément (ex: Projet, Démolition, Existant).
- **Paramètres** : `elementId` (entier), `createdPhaseName`, `demolishedPhaseName`.
- **Lecture seule** : Non.

---

## 16. Imports, Exports & Formats Vectoriels

### `revit_export_pdf`
- **Description** : Exporte une vue ou un ensemble de feuilles en PDF vectoriel haute fidélité avec choix du format papier et option de fusion.
- **Paramètres** : `viewOrSheetIds` (liste), `outputPath` (chaîne), `combine` (booléen), `paperSize` (optionnel: `"A4"`, `"A3"`, etc.).
- **Lecture seule** : Non.

### `revit_export_dwg`
- **Description** : Exporte les vues ou feuilles en format AutoCAD DWG avec configuration des calques.
- **Paramètres** : `viewIds` (liste), `outputFolder` (chaîne), `exportSetupName` (optionnel).
- **Lecture seule** : Non.

### `revit_export_dxf`
- **Description** : Exporte les vues au format standard DXF vectoriel.
- **Paramètres** : `viewIds`, `outputFolder`.
- **Lecture seule** : Non.

### `revit_export_ifc`
- **Description** : Exporte la maquette au format openBIM IFC (IFC2x3 / IFC4) pour coordination externe.
- **Paramètres** : `outputPath` (chaîne), `ifcVersion` (optionnel).
- **Lecture seule** : Non.

### `revit_export_view_image`
- **Description** : Réalise un export d'image haute résolution (PNG) de n'importe quelle vue spécifiée avec fond transparent optionnel.
- **Paramètres** : `viewId` (entier), `outputPath` (chaîne), `pixelSize` (optionnel, défaut: 1920), `transparentBackground` (booléen).
- **Lecture seule** : Non.

### `revit_render_view`
- **Description** : Lance un rendu photoréaliste de la vue 3D avec choix des lumières et de la qualité.
- **Paramètres** : `viewId`, `quality` (`"Draft"`, `"Medium"`, `"High"`), `outputPath`.
- **Lecture seule** : Non.

### `revit_import_cad`
- **Description** : Importe ou lie un fond de plan CAO (DWG, DXF) sur un niveau donné.
- **Paramètres** : `filePath`, `levelName`, `link` (booléen optionnel).
- **Lecture seule** : Non.

### `revit_get_cad_geometry`
- **Description** : Analyse la géométrie vectorielle contenue dans un import CAO présent dans le modèle.
- **Paramètres** : `cadImportElementId`.
- **Lecture seule** : Oui.

### `revit_create_walls_from_cad`
- **Description** : Génère automatiquement des murs Revit le long des polylignes d'un calque CAO importé.
- **Paramètres** : `cadElementId`, `layerName`, `wallTypeName`, `levelName`.
- **Lecture seule** : Non.

### `revit_import_image`
- **Description** : Importe une image matricielle (JPG, PNG) dans une vue de plan ou de dessin.
- **Paramètres** : `filePath`, `viewId`, `point` `{x, y}`.
- **Lecture seule** : Non.

### `revit_export_clash_report`
- **Description** : Exporte les résultats d'un rapport de détection de conflits au format HTML ou CSV.
- **Paramètres** : `outputPath`, `clashResults`.
- **Lecture seule** : Non.

### `revit_export_schedule`
- **Description** : Exporte une nomenclature existante au format CSV.
- **Paramètres** : `scheduleId`, `outputPath`.
- **Lecture seule** : Non.

---

## 17. Graphismes, Révisions, Sous-projets & Variantes

### `revit_color_elements`
- **Description** : Applique une surcharge graphique de couleur unie (RGB) sur une liste d'éléments dans la vue active.
- **Paramètres** : `elementIds` (liste), `color` `{ r, g, b }`.
- **Lecture seule** : Non.

### `revit_reset_color_overrides`
- **Description** : Réinitialise les surcharges de couleur des éléments dans la vue active.
- **Paramètres** : `elementIds` (liste).
- **Lecture seule** : Non.

### `revit_zoom_to_elements`
- **Description** : Cadre et centre immédiatement la vue active sur une liste d'éléments spécifiés.
- **Paramètres** : `elementIds` (liste).
- **Lecture seule** : Non.

### `revit_get_current_selection`
- **Description** : Retourne les éléments actuellement sélectionnés par l'utilisateur dans l'interface Revit.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_set_current_selection`
- **Description** : Modifie la sélection utilisateur dans Revit.
- **Paramètres** : `elementIds` (liste).
- **Lecture seule** : Non.

### `revit_isolate_elements`
- **Description** : Isole temporairement les éléments dans la vue active (mode "Isoler la catégorie/l'élément").
- **Paramètres** : `elementIds` (liste).
- **Lecture seule** : Non.

### `revit_reset_temporary_override`
- **Description** : Désactive le mode d'isolation ou de masquage temporaire dans la vue active.
- **Paramètres** : Aucun.
- **Lecture seule** : Non.

### `revit_get_view_templates`
- **Description** : Liste l'ensemble des gabarits de vues configurés dans le document.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_bulk_rename_views`
- **Description** : Renomme en masse un ensemble de vues selon un motif de recherche/remplacement ou un préfixe.
- **Paramètres** : `renamingRules` (liste d'objets `{ viewId, newName }`).
- **Lecture seule** : Non.

### `revit_duplicate_views_bulk`
- **Description** : Duplique une liste de vues sélectionnées en appliquant un suffixe commun.
- **Paramètres** : `viewIds` (liste), `suffix` (chaîne).
- **Lecture seule** : Non.

### `revit_get_revision_history`
- **Description** : Renvoie l'historique chronologique des révisions et indices émis sur le projet.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_get_revisions`
- **Description** : Liste détaillée de toutes les révisions créées dans Revit (date, description, numéro, statut).
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_add_revision_cloud`
- **Description** : Trace un nuage de révision autour d'une zone modifiée avec affectation à une révision officielle.
- **Paramètres** : `viewId`, `revisionId`, `points` (contour).
- **Lecture seule** : Non.

### `revit_get_worksets`
- **Description** : Liste les sous-projets (Worksets) d'un projet partagé avec statut d'ouverture et modifiabilité.
- **Paramètres** : Aucun.
- **Lecture seule** : Oui.

### `revit_issue_drawings`
- **Description** : Émet une série de feuilles pour diffusion officielle en leur appliquant le dernier indice de révision.
- **Paramètres** : `sheetIds` (liste), `revisionNumber`.
- **Lecture seule** : Non.

---

## 18. Serveur Compagnon CAD-MCP — Outils DXF / DWG

Le serveur compagnon Python autonome (`cad-mcp/server.py`) expose 8 outils dédiés au traitement vectoriel et à la coordination :

### `dxf_info`
- **Description** : Analyse complète d'un fichier DXF : version AutoCAD, calques actifs, boîtes limites min/max en millimètres et compteurs d'entités.
- **Paramètres** : `filePath` (chaîne).

### `dxf_list_entities`
- **Description** : Lecture détaillée de la géométrie vectorielle 2D/3D (lignes, polylignes, arcs, cercles, blocs, textes) avec filtrage par calque.
- **Paramètres** : `filePath` (chaîne), `layer` (optionnel), `entityType` (optionnel).

### `dxf_render`
- **Description** : Rendu vectoriel haute résolution en image PNG ou en document PDF à l'échelle métrique exacte avec gestion des tirets réels.
- **Paramètres** : `filePath`, `outputPath`, `format` (`"PNG"` ou `"PDF"`), `widthPx`, `heightPx`.

### `dxf_render_all_layouts_pdf`
- **Description** : Rendu de toutes les présentations (Model Space et Paper Spaces) dans un fichier PDF vectoriel multi-pages.
- **Paramètres** : `filePath`, `outputPath`.

### `dxf_audit`
- **Description** : Audit de qualité de dessin : détection des lignes superposées, des polylignes non fermées et des décalages millimétriques.
- **Paramètres** : `filePath`, `toleranceMm` (optionnel).

### `dxf_edit`
- **Description** : Édition vectorielle sûre (ajout, déplacement ou suppression d'entités vectorielles) avec détection des fichiers verrouillés.
- **Paramètres** : `filePath`, `operations` (liste de modifications).

### `dxf_diff`
- **Description** : Comparaison géométrique et structurelle fine entre deux indices ou versions d'un même fichier DXF.
- **Paramètres** : `filePathA`, `filePathB`.

### `dxf_compare_with_revit`
- **Description** : Comparaison spatiale croisée directe entre le fond de plan DXF et la maquette 3D Revit active via le pont loopback HTTP.
- **Paramètres** : `dxfFilePath`, `levelName` (optionnel), `toleranceMm` (optionnel).


---

## 19. Outils Spécialisés v2.2.0 : Vision, Audits PMR, Macro-Conception & Copilote

Cette catégorie introduit les 5 super-pouvoirs majeurs de la version 2.2.0, conçus pour transformer Claude / Cursor en un architecte augmenté autonome et réactif.

### `revit_screenshot_active_view`
- **Description** : Capture instantanément la vue active (vue 3D, plan d'étage, coupe ou élévation) ou la vue spécifiée par son identifiant. L'image est encodée en Base64 et retransmise nativement au client MCP via le format officiel multimodal (`type: "image", mimeType: "image/png"`), permettant à Claude 3.5 Sonnet ou GPT-4o de l'inspecter visuellement pour détecter des défauts de proportions ou s'auto-corriger.
- **Paramètres** :
  - `viewId` *(entier, optionnel)* : ID de la vue cible (par défaut : vue active du document).
  - `widthPixels` *(entier, optionnel, défaut : 1920)* : Largeur de l'image capturée (entre 320 et 4096 pixels).
  - `format` *(chaîne, optionnel, défaut : "png")* : Format de l'image (`"png"` ou `"jpeg"`).
  - `includeBase64` *(booléen, optionnel, défaut : true)* : Inclure la chaîne Base64 dans la réponse.
  - `saveFilePath` *(chaîne, optionnel)* : Chemin optionnel sur le disque local pour sauvegarder une copie permanente du fichier PNG/JPEG.
  - `documentTitle` *(chaîne, optionnel)* : Titre ou chemin du document cible.
- **Lecture seule** : Oui.
- **Exemple de payload** :
```json
{
  "widthPixels": 1920,
  "format": "png",
  "saveFilePath": "C:/Projets/RenduFaçade.png"
}
```
- **Exemple de retour** :
```json
{
  "success": true,
  "viewId": 150126,
  "viewName": "{3D}",
  "viewType": "ThreeD",
  "widthPixels": 1920,
  "filePath": "C:/Projets/RenduFaçade.png",
  "fileSizeBytes": 34515,
  "mimeType": "image/png",
  "imageBase64": "iVBORw0KGgoAAAANSUhEUg..."
}
```

---

### `revit_audit_pmr`
- **Description** : Audit réglementaire automatisé en 1 clic de l'accessibilité aux personnes à mobilité réduite (PMR) conforme aux normes françaises (Arrêté du 24 décembre 2015) et européennes. L'outil scanne les portes (passage utile $\ge 83$ cm), les pièces clés (sanitaires, SdB, dégagements pour aire de giration libre de $\varnothing 1{,}50$ m) et les pentes de rampes ($< 5\%$). Il peut surligner instantanément les éléments dans la vue Revit (Vert = Conforme, Rouge = Non-conforme).
- **Paramètres** :
  - `minDoorClearWidthMm` *(nombre, optionnel, défaut : 830)* : Largeur de passage utile minimale en mm (défaut : 830 mm pour porte de 90 cm nominal).
  - `minTurningCircleDiameterMm` *(nombre, optionnel, défaut : 1500)* : Diamètre minimal de l'aire de rotation libre en mm (défaut : 1500 mm).
  - `maxRampSlopePercent` *(nombre, optionnel, défaut : 5)* : Pente maximale admissible pour les rampes en pourcentage.
  - `applyColorOverrides` *(booléen, optionnel, défaut : true)* : Appliquer immédiatement la surbrillance graphique (vert / rouge) dans la vue Revit.
  - `viewId` *(entier, optionnel)* : Vue cible pour les surcharges graphiques (défaut : vue active).
  - `documentTitle` *(chaîne, optionnel)* : Document cible.
- **Lecture seule** : Non (applique des surcharges graphiques réversibles dans la vue).
- **Exemple de payload** :
```json
{
  "minDoorClearWidthMm": 830,
  "applyColorOverrides": true
}
```
- **Exemple de retour** :
```json
{
  "success": true,
  "standard": "Accessibilité PMR (Arrêté du 24 décembre 2015)",
  "summary": {
    "complianceRatePercent": 50.0,
    "totalDoors": 2,
    "compliantDoors": 1,
    "nonCompliantDoors": 1,
    "colorOverridesApplied": true,
    "targetViewName": "{3D}"
  },
  "doors": [
    {
      "id": 296771,
      "name": "Porte 90x215",
      "clearWidthMm": 830,
      "isCompliant": true
    },
    {
      "id": 305995,
      "name": "Porte 73x204",
      "clearWidthMm": 680,
      "isCompliant": false,
      "recommendation": "Remplacer par un bloc-porte ≥ 900 mm nominal (passage utile ≥ 830 mm)."
    }
  ]
}
```

---

### `revit_generate_spatial_layout`
- **Description** : Moteur de macro-conception architecturale générative. Permet de concevoir en une seule commande un projet spatial cohérent : résolution de l'organigramme fonctionnel, modélisation de la dalle de sol (`Floor`), murs extérieurs thermiquement isolés, cloisons séparatrices intérieures, placement des pièces (`Room`) avec calcul de surface réelle, et insertion des baies vitrées (optimisation bioclimatique RE2020 plein Sud) et portes conformes PMR.
- **Paramètres** :
  - `template` *(chaîne, optionnel, défaut : "pavillon_t4_115m2")* : Gabarit architectural (`"pavillon_t4_115m2"`, etc.).
  - `originX` *(nombre, optionnel, défaut : 0)* : Coordonnée X de l'angle Sud-Ouest en mm.
  - `originY` *(nombre, optionnel, défaut : 0)* : Coordonnée Y de l'angle Sud-Ouest en mm.
  - `widthMm` *(nombre, optionnel, défaut : 12500)* : Largeur de la façade en mm.
  - `depthMm` *(nombre, optionnel, défaut : 9500)* : Profondeur du bâtiment en mm.
  - `wallHeightMm` *(nombre, optionnel, défaut : 2800)* : Hauteur sous plafond en mm.
  - `levelName` *(chaîne, optionnel)* : Nom du niveau d'implantation (défaut : rez-de-chaussée).
  - `includeFloor` *(booléen, optionnel, défaut : true)* : Générer automatiquement la dalle de sol.
  - `includeDoorsAndWindows` *(booléen, optionnel, défaut : true)* : Insérer les menuiseries et baies vitrées.
  - `dryRun` *(booléen, optionnel, défaut : false)* : Mode simulation sans altérer la maquette.
  - `documentTitle` *(chaîne, optionnel)* : Document cible.
- **Lecture seule** : Non (outil de modélisation complet).
- **Exemple de payload** :
```json
{
  "template": "pavillon_t4_115m2",
  "widthMm": 12500,
  "depthMm": 9500,
  "dryRun": false
}
```
- **Exemple de retour** :
```json
{
  "success": true,
  "template": "pavillon_t4_115m2",
  "levelName": "Niveau 0",
  "floorId": 313211,
  "createdWallsCount": 7,
  "createdRoomsCount": 4,
  "rooms": [
    { "name": "Séjour / Cuisine traversant (Sud)", "areaM2": 40.71 },
    { "name": "Garage / Cellier technique (Nord)", "areaM2": 21.5 },
    { "name": "Suite Parentale (Est)", "areaM2": 22.73 },
    { "name": "Espace Nuit / Chambres (Nord-Est)", "areaM2": 27.94 }
  ],
  "totalHabitableAreaM2": 91.38,
  "openingsCount": 5,
  "environmentalComplianceRE2020": {
    "estimatedGlazedAreaM2": 14.5,
    "glazedRatioPercent": 15.9,
    "minRequiredRatioPercent": 16.7,
    "isCompliant": true,
    "solarOrientation": "Optimisée Sud / Traversant"
  }
}
```

---

### `revit_get_proactive_alerts`
- **Description** : Interroge le moteur de veille proactif (`ProactiveGuardian`) de Revit. Renvoie l'historique chronologique des alertes et anomalies détectées en temps réel pendant le travail de l'utilisateur (déplacement de mur provoquant la rupture de délimitation d'une pièce, suppression d'une pièce laissant des séparateurs orphelins, dépassement de linteau d'ouverture sous dalle, segments de murs trop courts).
- **Paramètres** :
  - `maxCount` *(entier, optionnel, défaut : 50)* : Nombre maximal d'alertes à récupérer.
  - `clearAfterRead` *(booléen, optionnel, défaut : false)* : Vider la file d'alertes après lecture.
  - `documentTitle` *(chaîne, optionnel)* : Document cible.
- **Lecture seule** : Oui.
- **Exemple de retour** :
```json
{
  "success": true,
  "totalAlerts": 1,
  "alerts": [
    {
      "id": "e4f1a8c2",
      "timestamp": "2026-10-20 19:46:12",
      "severity": "WARNING",
      "category": "OPENINGS",
      "message": "L'ouverture 'Fenêtre 120x140' (#305120) a une hauteur de linteau inhabituelle (2950 mm).",
      "recommendation": "Vérifiez l'altimétrie de la sous-face de dalle supérieure pour éviter un clash structurel.",
      "elementId": 305120
    }
  ]
}
```

---

### `revit_clear_proactive_alerts`
- **Description** : Purge l'intégralité du journal d'alertes du copilote proactif.
- **Paramètres** :
  - `documentTitle` *(chaîne, optionnel)* : Document cible.
- **Lecture seule** : Non.
- **Exemple de retour** :
```json
{
  "success": true,
  "message": "Journal des alertes proactives effacé avec succès."
}
```

---

## 19. Super-Pouvoirs v2.5.0 : Hologrammes 3D, Solveur Rust, Three.js, Speech-to-BIM & ACV RE2020

### `revit_overlay_ghost_preview`
- **Description** : Projette des géométries fantômes 3D semi-transparentes colorées (murs, cloisons, cubes d'agencement, volumes de mobilier) directement dans la fenêtre DirectX 3D de Revit via l'API native `IDirectContext3DServer`, sans modifier le modèle Revit ni créer de transaction.
- **Paramètres** :
  - `clearExisting` (booléen, optionnel) : efface les projections fantômes précédentes.
  - `items` (tableau d'objets, optionnel) : `{ label, type, xMm, yMm, zMm, widthMm, depthMm, heightMm, rotationDeg, r, g, b, alpha }`.
  - `wallStart` / `wallEnd` (objets `{x, y, z}`, optionnels) : trace un mur fantôme direct.
  - `wallHeightMm` / `wallThicknessMm` (nombres, optionnels).
- **Exemple** : `revit_overlay_ghost_preview({ wallStart: {x: 0, y: 0}, wallEnd: {x: 5000, y: 0}, wallHeightMm: 2800, r: 0, g: 220, b: 255, alpha: 130 })`.

### `revit_overlay_clash_heatmap`
- **Description** : Projette une heatmap volumétrique 3D en surimpression directe sur les faces ou boîtes englobantes des éléments (rouge = clash dur, jaune/orange = dégagement insuffisant, vert = sécurisé) sans altérer les matériaux Revit.
- **Paramètres** :
  - `clearExisting` (booléen, optionnel, défaut `true`).
  - `clashes` (tableau, optionnel) : résultats issus de `revit_fast_clash_detection`.
  - `elementIds` (tableau d'entiers, optionnel) : liste d'IDs d'éléments Revit à mettre en surbrillance.
- **Exemple** : `revit_overlay_clash_heatmap({ elementIds: [10234, 10235], r: 255, g: 30, b: 30, alpha: 160 })`.

### `revit_overlay_clear`
- **Description** : Efface instantanément toutes les projections holographiques DirectContext3D et heatmaps 3D du viewport Revit.
- **Paramètres** : Aucun.

### `revit_overlay_get_status`
- **Description** : Retourne l'état du serveur DirectContext3D, le nombre d'hologrammes actifs et leur inventaire géométrique.
- **Paramètres** : Aucun.

### `revit_solve_parametric_layout`
- **Description** : Moteur d'optimisation mathématique multithreadé en Rust (`rayon`). Évalue 100 000 permutations spatiales d'aménagement en 36 ms pour implanter postes de travail, tables de réunion et rangements en maximisant la lumière naturelle et garantissant 1,50 m de passage PMR.
- **Paramètres** :
  - `roomWidthMm` (nombre, défaut `8000`).
  - `roomLengthMm` (nombre, défaut `6000`).
  - `minClearanceMm` (nombre, défaut `1500`).
  - `workstations` (entier, défaut `6`).
  - `meetingTables` (entier, défaut `1`).
  - `storageUnits` (entier, défaut `2`).
  - `iterations` (entier, défaut `100000`).
  - `autoGhostPreview` (booléen, défaut `true`) : projette immédiatement la solution en hologramme 3D dans Revit.
- **Exemple** : `revit_solve_parametric_layout({ roomWidthMm: 9000, roomLengthMm: 7000, workstations: 6, meetingTables: 1, autoGhostPreview: true })`.

### `revit_export_interactive_3d_viewer`
- **Description** : Exporte la vue ou la maquette 3D Revit sous forme d'un visualiseur Web interactif autonome Three.js (Generative UI) avec OrbitControls (rotation, zoom, panoramique), modes fil de fer / fantôme et inspection des paramètres au clic.
- **Paramètres** :
  - `viewId` (entier, optionnel) : ID de la vue Revit.
  - `maxElements` (entier, optionnel, défaut `500`).
  - `outputDir` (chaîne, optionnelle).
- **Exemple** : `revit_export_interactive_3d_viewer({})`.

### `revit_execute_voice_command`
- **Description** : Contrôle vocal temps réel (Speech-to-BIM). Interprète des instructions orales en français ou anglais et les exécute sur Revit en moins de 300 ms via Windows Named Pipes (< 0.5 ms de latence IPC).
- **Paramètres** :
  - `command` (chaîne, obligatoire) : phrase orale transcrite (ex: *"Isole les gaines de ventilation du R+1 et masque le mobilier"*, *"Réinitialise la vue"*, *"Passe en 3D"*, *"Efface les hologrammes"*).
- **Exemple** : `revit_execute_voice_command({ command: "Isole les gaines de ventilation et masque le mobilier" })`.

### `revit_calculate_lca_carbon_footprint`
- **Description** : Calcule le bilan carbone ACV complet du projet Revit selon la méthodologie RE2020 ($IC_{construction}$ en $kgCO_2eq/m^2$ SDP) et la base de données officielle française FDES/INIES.
- **Paramètres** : Aucun.
- **Exemple** : `revit_calculate_lca_carbon_footprint({})`.

### `revit_suggest_low_carbon_alternatives`
- **Description** : Analyse les points chauds d'émissions carbone du projet (cloisons BA13 standard, menuiseries alu, bétons traditionnels) et calcule le gain chiffré exact en tonnes de $CO_2$ économisées et en stockage de carbone biogénique pour des alternatives biosourcées (ossature bois, menuiserie bois PEFC, béton CEM III).
- **Paramètres** : Aucun.
- **Exemple** : `revit_suggest_low_carbon_alternatives({})`.

### `revit_inies_database_lookup`
- **Description** : Recherche et consulte les données environnementales officielles FDES de la base française INIES (facteurs d'émissions, stockage biogénique, durée de vie typique).
- **Paramètres** :
  - `query` (chaîne, optionnelle) : matériau ou mot-clé (ex: *"bois"*, *"plâtre"*, *"isolant"*, *"béton"*).
  - `category` (chaîne, optionnelle) : *"Structure"*, *"Cloisonnement"*, *"Isolation"*, *"Menuiserie"*, *"Finitions"*.
- **Exemple** : `revit_inies_database_lookup({ query: "ossature bois" })`.
