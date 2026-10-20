# Journal des modifications (Changelog) — RevitMCP

Toutes les modifications notables apportées au projet RevitMCP sont documentées dans ce fichier.

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
