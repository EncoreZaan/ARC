# Journal des modifications (Changelog) — RevitMCP

Toutes les modifications notables apportées au projet RevitMCP sont documentées dans ce fichier.

---

## [2.1.0] - 2026-10-20

### 🚀 Nouveautés majeures

- **189 outils MCP unifiés** (contre 70 dans la version 2.0.0) couvrant l'ensemble du cycle de conception architecturale, modélisation, calcul spatial, annotation, documentation, rendus et export dans Revit 2027.
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
