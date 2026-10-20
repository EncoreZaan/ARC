# Protocole de Tests d'Acceptation — RevitMCP v2.1.0

Ce document détaille la procédure exacte de déploiement ainsi que les 10 scénarios d'acceptation opérationnelle du micro-habitat (6,40 × 6,20 m, murs 20 cm, toiture double pente 2,40 m → 5,00 m, mezzanine) sous Revit 2027 en français, à exécuter directement par Claude dans Revit.

---

## 0. Procédure de Déploiement Exacte

### A. Chemins des répertoires cibles
- **Dossier d'Addins Revit 2027** :
  `%APPDATA%\Autodesk\Revit\Addins\2027\`
  (soit : `C:\Users\teoba\AppData\Roaming\Autodesk\Revit\Addins\2027\`)
- **Dossier des binaires compilés (Source)** :
  `c:\Users\teoba\Documents\Devs\MCP Revit\revit-addin\RevitMCP\bin\Debug\net10.0-windows\`

### B. Fichiers à copier
1. **Manifeste Add-in** :
   Copier `revit-addin\RevitMCP\RevitMCP.addin` vers `%APPDATA%\Autodesk\Revit\Addins\2027\RevitMCP.addin`
2. **Bibliothèques compilées (.NET 10 / 8)** :
   Copier l'ensemble des fichiers depuis `bin\Debug\net10.0-windows\` vers `%APPDATA%\Autodesk\Revit\Addins\2027\` :
   - `RevitMCP.dll`
   - `RevitMCP.pdb`
   - `Newtonsoft.Json.dll`
   - `Microsoft.CodeAnalysis.dll`
   - `Microsoft.CodeAnalysis.CSharp.dll`
3. **Fichier de configuration** :
   Copier `RevitMCP.config.json` vers `%APPDATA%\Autodesk\Revit\Addins\2027\RevitMCP.config.json`

### C. Procédure de redémarrage obligatoire
1. **Fermer Autodesk Revit 2027** : Si Revit est en cours d'exécution, le processus système verrouille `RevitMCP.dll` en mémoire.
2. **Copier les fichiers** (via le script automatisé `.\scripts\install.ps1` ou copie manuelle).
3. **Relancer Autodesk Revit 2027** et ouvrir la maquette du projet (ex. `Conception variante 2.rvt`).
4. **Vérifier le statut du pont HTTP** :
   Exécuter `revit_health_check({})` ou ouvrir `http://localhost:6543/healthz` dans le navigateur. La réponse doit indiquer :
   ```json
   {
     "ok": true,
     "service": "RevitMCP",
     "version": "2.1.0",
     "openDocuments": [
       {
         "title": "Conception variante 2",
         "isActive": true
       }
     ]
   }
   ```
5. **Démarrer les scénarios S1 à S10** ci-dessous dans Claude.

---

## Scénario S1 : Lecture et vérification des baies du mur Sud

**Objectif** : Vérifier que `revit_get_wall_openings` extrait toutes les fenêtres hébergées avec leurs dimensions et cotes cumulées le long du mur en millimètres.

1. **Identification du mur Sud** :
   Appeler `revit_get_elements(category: "Murs")` ou filtrer avec `bboxFilter`.
2. **Requête** :
   ```json
   {
     "wallId": 123456
   }
   ```
3. **Validation attendue** :
   - Réponse `success: true`.
   - Liste ordonnée de portes/fenêtres avec : `typeId`, `typeName`, `width`, `height`, `sillHeight`, `distanceFromStart`, `distanceFromEnd`.
   - Vérification de la chaîne de cotes Sud (trumeaux et baies en cm : 82 / 60 / 24 / 60 / 149 / 180 / 45 cm, ou équivalent en mm).

---

## Scénario S2 : Déplacement de la porte d'entrée et annulation (Undo)

**Objectif** : Déplacer la porte d'entrée de 200 mm le long du mur Ouest, vérifier la nouvelle position, puis annuler la transaction.

1. **Localisation initiale** :
   `revit_get_element_location(elementIds: [doorId])` → noter `point.y`.
2. **Déplacement relatif** :
   `revit_move_element(elementId: doorId, translation: { "x": 0, "y": 200, "z": 0 })`
   - Vérifier `success: true`.
   - `revit_get_element_location` confirme le décalage de +200 mm.
3. **Annulation** :
   `revit_undo(steps: 1)`
   - Vérifier `success: true`.
   - `revit_get_element_location` confirme le retour exact à la coordonnée `y` initiale.

---

## Scénario S3 : Duplication d'un type de mobilier et remplacement

**Objectif** : Dupliquer un type de mobilier existant, modifier sa largeur et remplacer l'instance posée.

1. **Duplication du type** :
   `revit_duplicate_type(sourceTypeId: canapeTypeId, newTypeName: "Canapé convertible 200")`
   - Retourne `newTypeId`.
2. **Modification des paramètres de type** :
   `revit_set_type_parameters(typeId: newTypeId, parameters: { "Largeur": 2000 })`
3. **Remplacement d'instance** :
   `revit_change_element_type(elementIds: [canapeInstanceId], typeId: newTypeId)`
4. **Validation attendue** :
   `revit_get_elements(ids: [canapeInstanceId])` confirme `typeId: newTypeId` et la nouvelle boîte englobante (`bbox`) de largeur 2000 mm.

---

## Scénario S4 : Création de coupe, mise en page et export PDF vectoriel 100%

**Objectif** : Créer la coupe transversale A-A à y = 4600 mm, l'insérer sur une feuille de présentation A4 et exporter un PDF à l'échelle.

1. **Création de la coupe** :
   ```json
   {
     "viewName": "Coupe A-A",
     "startPoint": { "x": 500, "y": 4600, "z": 0 },
     "endPoint": { "x": 5900, "y": 4600, "z": 0 },
     "height": 5500,
     "depth": 2000
   }
   ```
   Appel de `revit_create_section_view`.
2. **Placement sur feuille** :
   `revit_place_view_on_sheet(sheetId: sheetId, viewId: sectionViewId, location: { "x": 148, "y": 105 })`
3. **Export PDF** :
   ```json
   {
     "viewIds": [sheetId],
     "outputPath": "C:\\Exports\\Coupe_A-A.pdf",
     "zoomType": "Zoom100",
     "paperFormat": "A4",
     "paperOrientation": "Landscape",
     "rasterQuality": "Presentation"
   }
   ```
   Appel de `revit_export_pdf`.

---

## Scénario S5 : Cotation automatique et cotes de niveau

**Objectif** : Poser des lignes de cotes linéaires et des cotes d'élévation sur les points caractéristiques.

1. **Lignes de cote** :
   `revit_create_linear_dimension` le long du mur Ouest reliant les références d'extrémités.
2. **Cote de niveau (Spot Elevation)** :
   - Faîtage toiture : `revit_create_spot_elevation(viewId: sectionViewId, point: { "x": 3200, "y": 4600, "z": 5000 })`
   - Plancher mezzanine : `revit_create_spot_elevation(viewId: sectionViewId, point: { "x": 3200, "y": 4600, "z": 2400 })`
3. **Validation attendue** : Les cotes retournent leurs IDs d'éléments créés sans avertissement bloquant.

---

## Scénario S6 : Variantes d'affichage d'aménagement (Semaine vs Week-end)

**Objectif** : Gérer la visibilité des éléments modulaires (ex. lit escamotable).

1. **Duplication de vue 3D** :
   `revit_duplicate_view(viewId: view3DId, newName: "Vue 3D - Semaine", withDetailing: false)`
2. **Masquage d'élément** :
   `revit_hide_elements_in_view(viewId: newViewId, elementIds: [litId], hide: true)`
3. **Validation attendue** :
   `revit_export_view_image(viewId: newViewId, outputPath: "C:\\Exports\\semaine.png")`
   L'image générée confirme l'absence visuelle du lit escamoté.

---

## Scénario S7 : Plan coloré des pièces

**Objectif** : Assigner et activer un schéma de couleur automatique sur le plan d'étage.

1. **Attribution de pièce** :
   Vérifier que les pièces sont closes (`revit_get_rooms`).
2. **Schéma de couleur** :
   `revit_set_room_color_fill(viewId: planViewId, colorSchemeParameter: "Nom", title: "Légende des pièces")`
3. **Validation attendue** :
   Export image de la vue en couleur avec légende intégrée.

---

## Scénario S8 : Comparaison spatiale DXF vs Maquette Revit

**Objectif** : Valider l'alignement entre le fond de plan géomètre / archi 2D (DXF) et les murs 3D modélisés dans Revit.

1. **Audit du DXF géomètre** :
   `dxf_audit(path: "plans/fond_de_plan.dxf")`
   - Confirme l'intégrité vectorielle (0 erreur bloquante).
2. **Comparaison croisée** :
   `dxf_compare_with_revit(dxfPath: "plans/fond_de_plan.dxf", revitElements: [...], toleranceMm: 20)`
   - Renvoie la liste des concordances et les éventuels écarts de tracé (`discrepancies`).

---

## Scénario S9 : Hauteur sous toiture (Mezzanine)

**Objectif** : Calculer par lancer de rayon la hauteur libre sous toiture au droit de la mezzanine.

1. **Mesure sous rampant Ouest (x = 1700 mm)** :
   `revit_get_height_at_point(x: 1700, y: 3100, fromZ: 2400, categories: ["Toits"])`
   - Retourne `clearanceHeightMm` (hauteur libre au-dessus du plancher mezzanine).
2. **Mesure au faîtage central (x = 3200 mm)** :
   `revit_get_height_at_point(x: 3200, y: 3100, fromZ: 2400, categories: ["Toits"])`
   - Vérifie la conformité avec la cote théorique de 5 000 - 2 400 - épaisseur ≈ 2 400 mm libre.

---

## Scénario S10 : Transaction atomique avec Rollback (`revit_batch`)

**Objectif** : Valider qu'un lot d'opérations MCP échoue proprement sans altérer le document.

1. **Préparation d'un lot avec erreur intentionnelle** :
   ```json
   {
     "atomic": true,
     "operations": [
       { "tool": "revit_set_project_info", "args": { "status": "En cours" } },
       { "tool": "revit_create_level", "args": { "name": "Niveau Test", "elevationMm": 1500 } },
       { "tool": "revit_set_element_location", "args": { "elementId": 9999999999, "point": { "x": 0, "y": 0, "z": 0 } } },
       { "tool": "revit_create_level", "args": { "name": "Niveau Test 2", "elevationMm": 3000 } }
     ]
   }
   ```
2. **Exécution** :
   Appel de `revit_batch`.
3. **Validation attendue** :
   - Réponse avec `success: false` ou exception signalée sur l'opération 3.
   - Le niveau "Niveau Test" n'a pas été créé dans le document (`revit_get_levels`).
   - Le statut du projet n'a pas été modifié.
   - Intégrité totale de la maquette préservée.
