# 🏛️ Proposition V2 — ORBITE (Miroir Droite — Boucle fluide autour de la croix porteuse) — Dossier Architectural & Rapport de Conformité

**Projet :** REFLET — L'Appartement Familial Londonien (`ARC-AUTO-LONDON-2026`)  
**Profil Client :** Famille contemporaine londonienne (parents + 2 enfants)  
**Échelle :** `1:50` | **Unité :** `mm` | **Version APM :** `v4`

## 1. Parti Architectural & Justifications

- **Concept :** Parti 'Orbite' : Réunion par la droite mettant en scène la croix porteuse centrale comme pivot sculptural. Le séjour-repas (18.8 m²) s'installe à cheval sur l'axe mitoyen au Sud, tandis que les deux pôles Nuit (Suite parentale à l'Ouest, Chambre enfants + SDB à l'Est) occupent les extrémités opposées.
- **Argumentaire Spatial :** Maximise la surface habitable (68.5 m² + galerie bibliothèque de 1.5 m²), crée une circulation fluide en boucle autour du noyau porteur et sépare totalement la suite parentale de la chambre des enfants.
- **Bilan des Surfaces :** Zone Jour = **35.2 m²** | Zone Nuit = **33.3 m²** | Surface Habitable Totale = **68.5 m²**

## 2. Programme & Espaces Créés

| N° | Pièce / Espace | Zonage | Coordonnées Repère (mm) |
|:---|:---|:---|:---|
| **01** | ENTRÉE | `Services/Circulation` | `[2550, -2450]` |
| **02** | WC | `Services/Circulation` | `[1100, -550]` |
| **03** | BUANDERIE | `Services/Circulation` | `[800, -1350]` |
| **04** | SALLE D'EAU | `Services/Circulation` | `[1330, -2820]` |
| **05** | CUISINE | `Jour` | `[4000, -1650]` |
| **06** | CHAMBRE PARENTS | `Nuit` | `[1100, -3980]` |
| **07** | REPAS | `Jour` | `[4550, -4700]` |
| **08** | SALON | `Jour` | `[6500, -4100]` |
| **09** | BUREAU - BIBLIO. | `Jour` | `[7120, -1750]` |
| **10** | DÉGAGEMENT | `Services/Circulation` | `[8770, -2250]` |
| **11** | SDB | `Services/Circulation` | `[10400, -1350]` |
| **12** | CHAMBRE ENFANTS | `Nuit` | `[9450, -6050]` |

## 3. Planche de Matériaux & Finitions

| Espace | Ouvrage | Matériau | Description | Teinte |
|:---|:---|:---|:---|:---|
| Cœur Central (Salon / Repas) | Sol continu | **Parquet Chêne Fumé Anglais** | Pose à l'anglaise traversante sur l'axe mitoyen | `#B59268` |
| Noyau Croix Porteuse | Murs & Poutres | **Enduit Chaux Ferrée & Béton Brut Lasuré** | Mise en valeur sculpturale de la croix porteuse centrale | `#D2CDC6` |
| Bibliothèque de Passage | Menuiserie sur mesure | **Noyer Canaletto & Acier Noir Sablé** | Étagères toute hauteur 240 cm dans la percée Nord | `#8A6240` |

## 4. Rapport de Vérification & Contrôle Qualité (Score : 100.0% — Convergé en 1 itération)

| Règle | Catégorie | Statut | Mesure Réelle | Seuil Exigé |
|:---|:---|:---:|:---|:---|
| `QA-HSP-01` | Gabarit & Volumétrie | ✅ PASS | 63 murs à 2800 mm (écarts détectés: 0) | `= 2800 mm` |
| `QA-CLASH-01B` | Collisions Baies (Porte / Fenêtre) | ✅ PASS | 7 portes et 4 fenêtres inspectées (chevauchements détectés: 0) | `0 collision baie/baie` |
| `QA-STRUCT-02` | Structure & Murs Porteurs | ✅ PASS | 2 percement(s) : portée max = 2400 mm, tête de mur min = 575 mm, poutre 35x40 cm active | `Portée <= 4000 mm & Têtes >= 300 mm` |
| `QA-ENTRY-03` | Accès & Enveloppe | ✅ PASS | Porte d'entrée principale = true, Conformité accès = true | `Porte d'entrée conforme au sujet` |
| `QA-FACADE-04` | Façades & Ouvertures Extérieures | ✅ PASS | 4 baies existantes, 0 baies neuves (total 4) | `0 nouvelle ouverture extérieure` |
| `QA-PROG-05` | Programme Architectural | ✅ PASS | 12 espaces fonctionnels modélisés, SDB=true, Cuisine=true, Salon/Séjour=true, 42 composants 3D sur mesure (31 familles) | `100% des espaces du programme + mobilier sur mesure` |
| `QA-AREA-06` | Surfaces & Équilibre Spatial | ✅ PASS | Total Habitable = 68.5 m² (Zone Jour/Atelier = 35.2 m², Zone Nuit = 33.3 m²) | `70.0 m² ± 8.0 m²` |
| `QA-BIM-07` | Intégrité Modèle BIMcraft | ✅ PASS | 130 objets BIM compilés et vérifiés sur disque | `Modèle .bimcraft + .ifc + .glb + .svg valides` |
