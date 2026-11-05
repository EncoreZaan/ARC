with open('README.md', 'r', encoding='utf-8') as f:
    readme = f.read()

# Replace version and counts
readme = readme.replace('v2.7.0', 'v2.8.0')
readme = readme.replace('236', '243')

# Insert Multi-Instance and 7 Pillars section
pillars_section = """
---

## ⚡ Nouveautés Majeures de la v2.8.0

### 🔄 1. Résilience Multi-Instance Totale & Zéro-Conflit Réseau
Fini les blocages et les messages d'erreurs au démarrage de plusieurs instances :
- **Multi-Revit Silencieux :** Ouvrez autant de fenêtres Revit que souhaité. Le serveur interne teste automatiquement la plage de ports `http://localhost:6543/` à `6555/`, bascule silencieusement sur le premier port libre, et publie son état dans `%APPDATA%\\RevitMCP\\active_instances.json`. Aucune boîte de dialogue modale d'erreur ne vient interrompre l'architecte.
- **Multi-BIMcraft Dynamique :** Lancez plusieurs instances de BIMcraft côte à côte. Chaque instance alloue un port éphémère indépendant sans jamais entrer en collision avec Revit ni avec les autres fenêtres.
- **Environnements Mixtes Harmonieux :** Exécutez simultanément plusieurs Revit et plusieurs BIMcraft sur la même machine. Le pont Rust `revit-mcp-server` route les requêtes avec précision.

### 🏛️ 2. Les 7 Piliers Technologiques de Rupture
1. **Live Mirror & Fusion Différentielle 3 Voies (`revit_live_mirror_sync`) :** Synchronisation bidirectionnelle multithreadée avec détection fine des deltas géométriques et résolution automatique de conflits.
2. **Vectoriseur Neuronal Croquis-vers-BIM (`revit_sketch_to_bim`) :** Transformation automatique de croquis à main levée, scans de plans papier et tracés vectoriels en murs 3D paramétriques, ouvertures et pièces.
3. **Simulation Thermique Dynamique & RE2020 (`revit_simulate_thermal_dynamic_re2020`) :** Moteur conforme ISO 52016-1 évaluant en temps réel le Bbio, le Cep, le Cep,nr et le confort estival DH avec les fichiers météo officiels français.
4. **Calcul RDM Éléments Finis & Eurocodes 2 & 3 (`revit_solve_structural_fea_instant`) :** Résolution matricielle des moments $M_{Ed}$, efforts tranchants $V_{Ed}$, flèches $L/350$ et dimensionnement optimal des profilés acier/béton.
5. **Auditeur Réglementaire PLU & Sécurité Incendie (`revit_audit_urban_plu_compliance`) :** Vérification automatique de conformité pour permis de construire : hauteur faîtage, prospect $H = L + R$, retraits séparatifs, CES, CBS et distances d'évacuation ERP.
6. **Exportateur Spatial USD/USDZ & glTF 2.0 PBR (`revit_export_usd_visionpro`) :** Exportation native pour Apple Vision Pro (visionOS avec ancres spatiales) et Unreal Engine 5.5 (géométrie virtualisée Nanite et éclairage global Lumen).
7. **Réalisateur IA de Visites Virtuelles & Compagnon Vocal (`revit_walkthrough_direct`) :** Génération de trajectoires cinématiques de caméra (splines Catmull-Rom à 1.65 m), course solaire dynamically synchronisée, bascules de finitions et narration vocale synchronisée.
"""

anchor = '## 🚀 Architecture Dual-Engine'
if anchor in readme:
    readme = readme.replace(anchor, pillars_section + '\n' + anchor)
else:
    # Fallback anchor
    anchor_alt = '## 🏛️ Architecture Dual-Engine'
    if anchor_alt in readme:
        readme = readme.replace(anchor_alt, pillars_section + '\n' + anchor_alt)

with open('README.md', 'w', encoding='utf-8') as f:
    f.write(readme)

print("README.md updated successfully")
