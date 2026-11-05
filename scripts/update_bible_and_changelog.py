with open('PROJECT_STATE.md', 'r', encoding='utf-8') as f:
    ps = f.read()

# Update version and tool count
ps = ps.replace('v2.7.0 — Dual-Engine Architecture', 'v2.8.0 — Dual-Engine, 7 Cutting-Edge Pillars & Multi-Instance Zero-Conflict')
ps = ps.replace('236 outils', '243 outils')
ps = ps.replace('236', '243')

# Add 7 cutting-edge modules to the Architecture section if not present
modules_addition = """
### 2.4. Les 7 Piliers Technologiques de Rupture & Multi-Instance (v2.8.0)
1. **Multi-Instance Silent Resilience :**
   - Revit C# Add-in : suppression définitive de `TaskDialog.Show` d'erreur, balayage dynamique des ports `http://localhost:6543/` à `6555/`, écoute sur pipes nommés partagés (`revit_mcp_ipc`) et dédiés (`revit_mcp_ipc_<pid>`), registre actif `%APPDATA%\\RevitMCP\\active_instances.json`.
   - BIMcraft Multi-Instance : allocation dynamique de ports par instance, détection multi-processus, tolérance aux pannes.
   - Mixte Revit + BIMcraft sans conflit.
2. **`live_mirror.rs` (Live Mirror & Fusion Différentielle 3 Voies) :**
   - Comparaison AABB, centroïdes et paramètres multithreadée via `rayon`.
   - Détection des deltas (Ajouts, Modifications, Suppressions) et arbitrage de conflits.
   - Outil MCP : `revit_live_mirror_sync`.
3. **`sketch_vectorizer.rs` (Vectoriseur Neuronal Croquis-vers-BIM) :**
   - Orthogonalisation de tracés 2D, détection des épaisseurs (porteurs 30cm / cloisons 10cm), placement des ouvertures et fermeture de pièces.
   - Outil MCP : `revit_sketch_to_bim`.
4. **`thermal_sim.rs` (Simulation Thermique Dynamique & RE2020) :**
   - Conforme ISO 52016-1, calcul instantané Bbio, Cep, Cep,nr et DH avec fichiers météo officiels français H1a-H3.
   - Outil MCP : `revit_simulate_thermal_dynamic_re2020`.
5. **`structural_fea.rs` (RDM Éléments Finis & Eurocodes 2/3) :**
   - Poutre Bernoulli-Euler, sollicitations ELU/ELS ($M_{Ed}, V_{Ed}$), flèches $L/350$, dimensionnement et allègement carbone automatique.
   - Outil MCP : `revit_solve_structural_fea_instant`.
6. **`plu_checker.rs` (Audit Réglementaire PLU & Sécurité Incendie) :**
   - Hauteur faîtage, prospect $H = L + R$, retraits séparatifs $D \ge H/2$, CES, CBS et évacuation incendie ERP/CCH.
   - Outil MCP : `revit_audit_urban_plu_compliance`.
7. **`spatial_usd_gltf.rs` (Export Spatial USD/USDZ Apple Vision Pro & glTF 2.0 PBR) :**
   - USDA/USDZ natif avec métadonnées spatiales visionOS et glTF 2.0 PBR pour Unreal Engine 5.5 (Nanite & Lumen).
   - Outil MCP : `revit_export_usd_visionpro`.
8. **`walkthrough_director.rs` (Réalisateur IA de Visites Virtuelles & Compagnon Vocal) :**
   - Trajectoires caméra cinématiques Catmull-Rom à 1.65m, course solaire dynamique, bascule de finitions et script de narration audio pour Gemini Live API / TTS.
   - Outil MCP : `revit_walkthrough_direct`.
"""

anchor = '## 3. Inventaire'
if anchor in ps:
    ps = ps.replace(anchor, modules_addition + '\n' + anchor)

with open('PROJECT_STATE.md', 'w', encoding='utf-8') as f:
    f.write(ps)

print("PROJECT_STATE.md updated successfully")

# Update CHANGELOG.md
with open('CHANGELOG.md', 'r', encoding='utf-8') as f:
    cl = f.read()

new_changelog_entry = """## [2.8.0] - 2026-11-05

### ⚡ Résilience Multi-Instance Totale & Zéro-Conflit Réseau (Revit & BIMcraft)
- **Suppression Définitive des Modales d'Erreur :** L'add-in Revit C# ne déclenche plus jamais de `TaskDialog.Show` d'erreur lors du lancement d'une seconde ou troisième instance. Le démarrage est 100% silencieux avec audit logging.
- **Allocation Dynamique de Ports (6543 à 6555) :** Lorsqu'un port est occupé, Revit bascule silencieusement sur le port suivant disponible et enregistre son PID et port dans `%APPDATA%\\RevitMCP\\active_instances.json`.
- **Canaux Named Pipes Multiples :** Chaque instance écoute à la fois sur le canal global `revit_mcp_ipc` et sur son canal dédié `revit_mcp_ipc_<pid>`.
- **Multi-BIMcraft Dynamique :** Support de plusieurs fenêtres BIMcraft actives simultanément avec détection multi-processus et ports éphémères indépendants.
- **Routage Intelligent Côté Rust :** Le serveur `revit-mcp-server` interroge le registre actif et sonde automatiquement la plage de ports en cas de requêtes multi-instances.

### 🏛️ Les 7 Piliers Technologiques de Rupture (Rust Natif 100%)
- **243 Outils MCP Actifs au Total** (+7 nouveaux outils d'excellence architecturale) :
  1. `revit_live_mirror_sync` (`live_mirror.rs`) : Moteur de synchronisation bidirectionnelle & fusion différentielle 3 voies (Revit ↔ BIMcraft) avec calcul de deltas spatiaux multithreadé (`rayon`) et résolution de conflits (`SmartGeometricSnap`, `SourceWins`, `TargetWins`, `UnionMerge`).
  2. `revit_sketch_to_bim` (`sketch_vectorizer.rs`) : Vectoriseur neuronal transformant des croquis à main levée, scans de plans papier et tracés vectoriels en parois 3D réelles avec distinction porteurs 30cm / cloisons 10cm, portes, fenêtres et détection polygonale de pièces avec surface Loi Carrez.
  3. `revit_simulate_thermal_dynamic_re2020` (`thermal_sim.rs`) : Moteur de Simulation Thermique Dynamique (STD ISO 52016-1) calculant le bilan thermique en temps réel, les coefficients de déperditions $H_{tr}$ et $H_{ve}$, les indicateurs officiels RE2020 (**Bbio**, **Cep**, **Cep,nr**) et le confort estival en Degrés-Heures (**DH**) selon les zones climatiques françaises H1a à H3.
  4. `revit_solve_structural_fea_instant` (`structural_fea.rs`) : Calcul de structure RDM / Éléments Finis instantané (poutres Bernoulli-Euler) avec vérifications Eurocodes 2 (béton) et Eurocode 3 (acier), sollicitations ELU/ELS ($M_{Ed}, V_{Ed}$), flèches admissibles $L/350$ et recommandations de redimensionnement de section.
  5. `revit_audit_urban_plu_compliance` (`plu_checker.rs`) : Audit automatisé de conformité Permis de Construire : hauteur maximale au faîtage/acrotère, prospect urbain sur voie ($H = L + R$), retraits aux limites séparatives ($D \ge H/2$), emprise au sol (**CES**), biotope végétalisé (**CBS**), et sécurité incendie ERP/CCH (distances d'évacuation $\le 30\\text{ m}$, largeurs en Unités de Passage).
  6. `revit_export_usd_visionpro` (`spatial_usd_gltf.rs`) : Exportateur spatial natif produisant des fichiers USDA/USDZ pour Apple Vision Pro (visionOS avec ancres spatiales) et glTF 2.0 PBR pour Unreal Engine 5.5 (compatible Nanite et Lumen).
  7. `revit_walkthrough_direct` (`walkthrough_director.rs`) : Réalisateur IA de visites virtuelles générant des trajectoires cinématiques de caméra (splines Catmull-Rom à $1.65\\text{ m}$), course solaire dynamique selon l'heure de la journée, bascules interactives de finitions architecturales et narration vocale synchronisée pour intégration avec Gemini Live API et TTS.

### 🌐 Vitrine Web & Documentation
- **Landing Page 3D Three.js :** Mise à jour à 243 outils avec mise en avant des 7 piliers et des badges multi-plateformes macOS (Apple Silicon M1-M4 & Intel), Linux et Windows 11.
- **Guide Architectural Revit & BIMcraft :** Enrichi des sections 7 et 8 détaillant l'architecture multi-instance silencieuse et les 7 moteurs algorithmiques.

---

"""

cl = cl.replace('# Journal des modifications (Changelog) — ARC (Architecture & Revit Copilot)\n\nToutes les modifications notables apportées au projet ARC sont documentées dans ce fichier.\n\n---\n\n', '# Journal des modifications (Changelog) — ARC (Architecture & Revit Copilot)\n\nToutes les modifications notables apportées au projet ARC sont documentées dans ce fichier.\n\n---\n\n' + new_changelog_entry)

with open('CHANGELOG.md', 'w', encoding='utf-8') as f:
    f.write(cl)

print("CHANGELOG.md updated successfully")
