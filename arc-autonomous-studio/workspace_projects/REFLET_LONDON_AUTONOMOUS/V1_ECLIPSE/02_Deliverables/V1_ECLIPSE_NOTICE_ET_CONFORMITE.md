# 🏛️ Proposition V1 — ÉCLIPSE (Miroir Gauche — Dissociation franche Jour / Nuit) — Dossier Architectural & Rapport de Conformité

**Projet :** REFLET — L'Appartement Familial Londonien (`ARC-AUTO-LONDON-2026`)  
**Profil Client :** Famille contemporaine londonienne (parents + 2 enfants)  
**Échelle :** `1:50` | **Unité :** `mm` | **Version APM :** `v4`

## 1. Parti Architectural & Justifications

- **Concept :** Parti 'Éclipse' : Dissociation acoustique et fonctionnelle totale entre le Lot A consacré au Jour (33.0 m², seule entrée conservée) et le Lot B consacré à la Nuit (32.9 m², 3 chambres + SDB familiale), articulés par un seuil ponctuel de 93 cm percé dans le mur porteur mitoyen.
- **Argumentaire Spatial :** Offre la meilleure intimité familiale (aucune chambre ne donne sur le séjour), conserve 98% du refend porteur mitoyen (une seule baie de 93 cm avec tête de mur réglementaire de 30 cm et poutre 35x40 cm), et déploie un séjour/cuisine traversant Nord-Sud baigné par les deux bow-windows du Lot A.
- **Bilan des Surfaces :** Zone Jour = **33.0 m²** | Zone Nuit = **32.9 m²** | Surface Habitable Totale = **65.9 m²**

## 2. Programme & Espaces Créés

| N° | Pièce / Espace | Zonage | Coordonnées Repère (mm) |
|:---|:---|:---|:---|
| **01** | ENTRÉE | `Services/Circulation` | `[2220, -2150]` |
| **02** | DÉGAGEMENT | `Services/Circulation` | `[1000, -2620]` |
| **03** | WC | `Services/Circulation` | `[1370, -1400]` |
| **04** | BUANDERIE | `Services/Circulation` | `[730, -1280]` |
| **05** | CUISINE | `Jour` | `[4350, -1600]` |
| **06** | SÉJOUR / SALLE À MANGER | `Jour` | `[3000, -3720]` |
| **07** | SDB | `Services/Circulation` | `[-760, -920]` |
| **08** | DÉGAGEMENT NUIT | `Nuit` | `[-1180, -2620]` |
| **09** | PALIER NUIT | `Nuit` | `[-2720, -2050]` |
| **10** | CHAMBRE ENFANT 2 | `Nuit` | `[-4000, -1220]` |
| **11** | CHAMBRE ENFANT 1 | `Nuit` | `[-720, -6450]` |
| **12** | CHAMBRE PARENTS | `Nuit` | `[-5000, -4340]` |

## 3. Planche de Matériaux & Finitions

| Espace | Ouvrage | Matériau | Description | Teinte |
|:---|:---|:---|:---|:---|
| Séjour / Salle à manger | Sol | **Parquet Chêne Clair Point de Hongrie** | Chêne massif brossé verni mat naturel (14 mm) | `#C8A97E` |
| Cuisine & Entrée | Sol & Crédence | **Terrazzo Minéral Clair** | Granulats marbre blanc de Carrare & ocre | `#E6E1DA` |
| Chambres (Lot B Nuit) | Sol & Menuiseries | **Chêne Miel & Lin Naturel** | Parquet lames larges 18 cm + façades dressing lin tissé | `#D7B98C` |
| Salle de bain & WC | Sol & Murs | **Grès Cérame Pierre de Bath & Laiton Brossé** | Carrelage 60x60 cm antidérapant R10 + robinetterie laiton | `#CFC8BE` |

## 4. Rapport de Vérification & Contrôle Qualité (Score : 100.0% — Convergé en 1 itération)

| Règle | Catégorie | Statut | Mesure Réelle | Seuil Exigé |
|:---|:---|:---:|:---|:---|
| `QA-HSP-01` | Gabarit & Volumétrie | ✅ PASS | 64 murs à 2800 mm (écarts détectés: 0) | `= 2800 mm` |
| `QA-CLASH-01B` | Collisions Baies (Porte / Fenêtre) | ✅ PASS | 7 portes et 8 fenêtres inspectées (chevauchements détectés: 0) | `0 collision baie/baie` |
| `QA-STRUCT-02` | Structure & Murs Porteurs | ✅ PASS | 1 percement(s) : portée max = 930 mm, tête de mur min = 300 mm, poutre 35x40 cm active | `Portée <= 4000 mm & Têtes >= 300 mm` |
| `QA-ENTRY-03` | Accès & Enveloppe | ✅ PASS | Porte d'entrée principale = true, Conformité accès = true | `Porte d'entrée conforme au sujet` |
| `QA-FACADE-04` | Façades & Ouvertures Extérieures | ✅ PASS | 8 baies existantes, 0 baies neuves (total 8) | `0 nouvelle ouverture extérieure` |
| `QA-PROG-05` | Programme Architectural | ✅ PASS | 12 espaces fonctionnels modélisés, SDB=true, Cuisine=true, Salon/Séjour=true, 41 composants 3D sur mesure (30 familles) | `100% des espaces du programme + mobilier sur mesure` |
| `QA-AREA-06` | Surfaces & Équilibre Spatial | ✅ PASS | Total Habitable = 65.9 m² (Zone Jour/Atelier = 33.0 m², Zone Nuit = 32.9 m²) | `70.0 m² ± 8.0 m²` |
| `QA-BIM-07` | Intégrité Modèle BIMcraft | ✅ PASS | 133 objets BIM compilés et vérifiés sur disque | `Modèle .bimcraft + .ifc + .glb + .svg valides` |
