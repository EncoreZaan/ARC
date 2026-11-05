# 1. Update README.md
with open('README.md', 'r', encoding='utf-8') as f:
    readme = f.read()

readme = readme.replace('243', '248')

pragmatic_section = """### 🎯 3. Les 5 Outils Métier Indispensables au Quotidien (v2.8.1)
Conçus spécifiquement pour le travail réel de l'architecte, sans aucune "usine à gaz" :
1. **Cotation Automatique Intelligente des Plans (`revit_auto_dimension_plan`) :** Génère instantanément les 3 lignes de cotes orthogonales professionnelles (Ligne 1 : baies et trumeaux, Ligne 2 : cotes intérieures nettes de pièce, Ligne 3 : cote hors-tout).
2. **Comparateur Visuel Existant / Projet (`revit_compare_variants_visual`) :** Superpose instantanément deux maquettes et génère le plan de démolition/création couleur : **Rouge** (démoli), **Vert** (créé/neuf), **Gris** (conservé) avec métré linéaire exact pour les devis d'artisans.
3. **Docteur Géométrique & Nettoyeur d'Avertissements (`revit_geometry_doctor`) :** Recale automatiquement les murs légèrement hors-axe (89.9° vers 90.0° net), fusionne les micro-espacements (< 25 mm) et supprime les segments microscopiques parasites qui déclenchent les alertes Revit.
4. **Calepinage Rapide d'Agencement Intérieur (`revit_layout_casework_millwork`) :** Dessine en un clic un linéaire de dressing, placard, cuisine ou meuble vasque sur mesure avec division modulaire propre et nomenclature quincaillerie/panneaux.
5. **Tableau Récapitulatif Artisans & Clients (`revit_extract_takeoff_artisan_table`) :** Extrait un tableau propre prêt pour devis (surfaces Loi Carrez, linéaires réels de plinthes déduction faite des portes, surfaces de murs à peindre, et nomenclature ordonnée des menuiseries).
"""

anchor_readme = '## 🚀 Architecture Dual-Engine'
if anchor_readme in readme:
    readme = readme.replace(anchor_readme, pragmatic_section + '\n' + anchor_readme)
elif '## 🏛️ Architecture Dual-Engine' in readme:
    readme = readme.replace('## 🏛️ Architecture Dual-Engine', pragmatic_section + '\n## 🏛️ Architecture Dual-Engine')

with open('README.md', 'w', encoding='utf-8') as f:
    f.write(readme)
print("README.md updated with 5 pragmatic tools.")

# 2. Update PROJECT_STATE.md
with open('PROJECT_STATE.md', 'r', encoding='utf-8') as f:
    ps = f.read()

ps = ps.replace('243 outils', '248 outils')
ps = ps.replace('243', '248')
ps = ps.replace('v2.8.0', 'v2.8.1')

ps_addition = """
### 2.5. Les 5 Outils Métier Quotidien d'Architecte (v2.8.1)
1. **`auto_dimension.rs` (`revit_auto_dimension_plan`) :** Chaines de cotes automatiques (détails ouvertures, cotes nettes, hors-tout).
2. **`variant_diff_visual.rs` (`revit_compare_variants_visual`) :** Plan de démolition/création SVG/PDF (Rouge = démoli, Vert = créé, Gris = conservé).
3. **`geometry_doctor.rs` (`revit_geometry_doctor`) :** Orthogonalisation des murs hors-axe, fusion des micro-gaps, purge des avertissements.
4. **`casework_layout.rs` (`revit_layout_casework_millwork`) :** Calepinage paramétrique placards, dressings, cuisines et meubles sur mesure.
5. **`artisan_takeoff.rs` (`revit_extract_takeoff_artisan_table`) :** Tableaux de métrés Loi Carrez, plinthes et menuiseries prêts pour clients et devis d'artisans.
"""

anchor_ps = '## 3. Inventaire'
if anchor_ps in ps:
    ps = ps.replace(anchor_ps, ps_addition + '\n' + anchor_ps)

with open('PROJECT_STATE.md', 'w', encoding='utf-8') as f:
    f.write(ps)
print("PROJECT_STATE.md updated to v2.8.1 and 248 tools.")

# 3. Update CHANGELOG.md
with open('CHANGELOG.md', 'r', encoding='utf-8') as f:
    cl = f.read()

cl_entry = """## [2.8.1] - 2026-11-05

### 🎯 Les 5 Outils Métier Indispensables de l'Architecte (248 Outils Totaux)
- **Cotation Automatique des Plans (`revit_auto_dimension_plan` - `auto_dimension.rs`) :** Génération instantanée des 3 lignes de cotes orthogonales professionnelles (ouvertures/trumeaux, cotes intérieures nettes, hors-tout) sans aucun clic manuel répétitif.
- **Comparateur Visuel de Démolition / Création (`revit_compare_variants_visual` - `variant_diff_visual.rs`) :** Superposition instantanée des variantes de projet avec l'existant. Sortie vectorielle SVG/PDF immédiate : **Rouge** (démoli), **Vert** (créé), **Gris** (conservé) et métré des démolitions pour artisans.
- **Docteur Géométrique & Nettoyeur d'Avertissements (`revit_geometry_doctor` - `geometry_doctor.rs`) :** Recalage automatique des parois légèrement obliques (89.9° vers 90.0°), fusion des micro-écarts (< 25 mm) aux jonctions de murs, et purge des segments parasites invisibles.
- **Calepinage Paramétrique d'Agencement Intérieur (`revit_layout_casework_millwork` - `casework_layout.rs`) :** Conception instantanée de dressings, placards, linéaires de cuisine et meubles vasque sur mesure avec caissons modulaires (600mm / 800mm) et nomenclature quincaillerie.
- **Tableau de Métré Récapitulatif Clients & Artisans (`revit_extract_takeoff_artisan_table` - `artisan_takeoff.rs`) :** Extraction automatisée en Markdown et CSV des surfaces Loi Carrez pièce par pièce, linéaires réels de plinthes (déduction des portes), surfaces de murs à peindre et nomenclature ordonnée des menuiseries.

---

"""

cl = cl.replace('## [2.8.0] - 2026-11-05\n\n', cl_entry + '## [2.8.0] - 2026-11-05\n\n')

with open('CHANGELOG.md', 'w', encoding='utf-8') as f:
    f.write(cl)
print("CHANGELOG.md updated with v2.8.1 entry.")

# 4. Update docs/revit_architecture_guide.md
guide_addition = """
---

## 9. Les 5 Outils Métier Indispensables au Quotidien (v2.8.1)

Pour répondre aux besoins concrets de l'architecte sur les projets réels (rénovation d'appartements, permis de construire, consultations des entreprises) et éviter tout écueil d'usine à gaz, la version 2.8.1 enrichit le catalogue de 5 outils directement exploitables :

1. **Cotation Automatique des Plans (`auto_dimension.rs`) :** Trace automatiquement les 3 lignes de cotes normées : Ligne 1 (détails de baies et trumeaux), Ligne 2 (cotes intérieures nettes), Ligne 3 (cote générale hors-tout).
2. **Comparateur Visuel Démolition / Création (`variant_diff_visual.rs`) :** Produit le plan bicolore indispensable pour les artisans et le permis : Rouge pour les cloisons à abattre, Vert pour les parois neuves à monter, Gris pour les éléments conservés, avec calcul du ratio de démolition.
3. **Docteur Géométrique (`geometry_doctor.rs`) :** Assainit le fichier Revit en redressant à 90° ou 0° les murs imperceptiblement faux, en fermant les micro-espacements qui empêchent la détection de pièces, et en supprimant les lignes de détail orphelines.
4. **Agencement & Menuiserie sur Mesure (`casework_layout.rs`) :** Modélise en 1 seconde des linéaires de placards, dressings, meubles vasque ou cuisines complètes selon les standards des fabricants (modules de 600 ou 800 mm, plinthes de 100 mm).
5. **Métré Client & Artisans (`artisan_takeoff.rs`) :** Fournit un tableau synthétique immédiat (Loi Carrez, plinthes nettes, surfaces de cloisons à peindre, repères de menuiseries) exportable directement vers Excel ou le dossier client.
"""

with open('docs/revit_architecture_guide.md', 'a', encoding='utf-8') as f:
    f.write(guide_addition)
print("docs/revit_architecture_guide.md updated with section 9.")
