# CAD-MCP — Serveur MCP DXF / CAD compagnon

Serveur MCP autonome écrit en Python (3.10+) pour inspecter, modifier, auditer, rendre et comparer des fichiers DXF (2D/3D) directement avec une maquette Revit active pilotée par RevitMCP.

## Fonctionnalités

1. **`dxf_info(path)`** : Métadonnées du fichier DXF (version AutoCAD, calques avec couleurs/visibilité, limites `extmin`/`extmax` en millimètres, unités insunits, compteurs d'entités).
2. **`dxf_list_entities(path, layers?, types?, bbox?)`** : Liste détaillée des entités géométriques (lignes, polylignes, arcs, cercles, textes, inserts/blocs, dimensions) avec coordonnées converties en millimètres.
3. **`dxf_render(path, outputPath, layers?, width?, height?, dpi?)`** : Rendu vectoriel du plan DXF en PNG avec légende des calques via matplotlib.
4. **`dxf_audit(path, toleranceMm?)`** : Détection des anomalies géométriques (lignes dupliquées, presque colinéaires, polylignes non fermées, textes vides, calques orphelins).
5. **`dxf_edit(path, operations, outputPath?)`** : Modification sûre d'un DXF (supprimer, déplacer, changer de calque, ajouter polylignes/textes) avec gestion de verrou de fichier (`FILE_LOCKED`).
6. **`dxf_compare_with_revit(dxfPath, revitCategory?, toleranceMm?)`** : Comparaison spatiale directe entre un fond de plan DXF et les éléments du modèle Revit actif via l'API RevitMCP (`http://localhost:6543`), signalant les écarts et éléments manquants/en trop.

## Installation

```bash
cd cad-mcp
pip install -r requirements.txt
```

## Lancement autonome

```bash
python server.py
```

## Configuration pour Claude Desktop (`claude_desktop_config.json`)

Ajoutez l'entrée suivante dans votre configuration Claude Desktop :

```json
{
  "mcpServers": {
    "cad-mcp": {
      "command": "python",
      "args": [
        "C:\\Users\\teoba\\Documents\\Devs\\MCP Revit\\cad-mcp\\server.py"
      ]
    }
  }
}
```
*(Adaptez le chemin vers l'exécutable Python ou votre environnement virtuel si nécessaire).*
