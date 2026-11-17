# 🏛️ Proposition V3 — PASSAGE (Miroir Gauche — Rue intérieure de fenêtre à fenêtre & Meubles-cloisons) — Dossier Architectural & Rapport de Conformité

**Projet :** REFLET — L'Appartement Familial Londonien (`ARC-AUTO-LONDON-2026`)  
**Profil Client :** Famille contemporaine londonienne (parents + 2 enfants)  
**Échelle :** `1:50` | **Unité :** `mm` | **Version APM :** `v4`

## 1. Parti Architectural & Justifications

- **Concept :** Parti 'Passage' : Création d'une grande perspective transversale ('rue intérieure' de 11.50 m de fenêtre Est à fenêtre Ouest) et d'espaces de réception en enfilade au Sud, structurés par des meubles-cloisons toute hauteur (60 cm d'épaisseur) dessinés sur mesure.
- **Argumentaire Spatial :** Valorise la double orientation Est-Sud-Ouest et intègre l'architecture intérieure au gros-œuvre via des meubles-cloisons acoustiques servant à la fois de séparation et de rangements massifs.
- **Bilan des Surfaces :** Zone Jour = **25.9 m²** | Zone Nuit = **40.0 m²** | Surface Habitable Totale = **65.9 m²**

## 2. Programme & Espaces Créés

| N° | Pièce / Espace | Zonage | Coordonnées Repère (mm) |
|:---|:---|:---|:---|
| **01** | ENTRÉE | `Services/Circulation` | `[2370, -1600]` |
| **02** | SALLE D'EAU | `Services/Circulation` | `[980, -1080]` |
| **03** | BUANDERIE | `Services/Circulation` | `[1180, -2620]` |
| **04** | CHAMBRE ENFANT 1 | `Nuit` | `[3850, -2320]` |
| **05** | CUISINE - REPAS | `Jour` | `[4350, -4650]` |
| **06** | SALON | `Jour` | `[600, -4650]` |
| **07** | SDB | `Services/Circulation` | `[-1600, -1000]` |
| **08** | DÉGT | `Services/Circulation` | `[-1220, -2400]` |
| **09** | HALL NUIT | `Nuit` | `[-2720, -1900]` |
| **10** | CHAMBRE ENFANT 2 | `Nuit` | `[-4300, -2320]` |
| **11** | CHAMBRE PARENTS | `Nuit` | `[-4280, -6020]` |

## 3. Planche de Matériaux & Finitions

| Espace | Ouvrage | Matériau | Description | Teinte |
|:---|:---|:---|:---|:---|
| Rue Intérieure & Enfilade Sud | Sol | **Parquet Chêne Blond Continu** | Lames filantes 11.50 m de fenêtre à fenêtre | `#D4B483` |
| Meubles-Cloisons Toute Hauteur (60 cm) | Menuiserie architecturale | **Bouleau Multiplis & Laque Vert Sauge Londonien** | Meubles-cloisons acoustiques 280 cm intégrant penderies et bibliothèque | `#7D8C7D` |

## 4. Rapport de Vérification & Contrôle Qualité (Score : 100.0% — Convergé en 1 itération)

| Règle | Catégorie | Statut | Mesure Réelle | Seuil Exigé |
|:---|:---|:---:|:---|:---|
| `QA-HSP-01` | Gabarit & Volumétrie | ✅ PASS | 64 murs à 2800 mm (écarts détectés: 0) | `= 2800 mm` |
| `QA-CLASH-01B` | Collisions Baies (Porte / Fenêtre) | ✅ PASS | 9 portes et 8 fenêtres inspectées (chevauchements détectés: 0) | `0 collision baie/baie` |
| `QA-STRUCT-02` | Structure & Murs Porteurs | ✅ PASS | 1 percement(s) : portée max = 2400 mm, tête de mur min = 575 mm, poutre 35x40 cm active | `Portée <= 4000 mm & Têtes >= 300 mm` |
| `QA-ENTRY-03` | Accès & Enveloppe | ✅ PASS | Porte d'entrée principale = true, Conformité accès = true | `Porte d'entrée conforme au sujet` |
| `QA-FACADE-04` | Façades & Ouvertures Extérieures | ✅ PASS | 8 baies existantes, 0 baies neuves (total 8) | `0 nouvelle ouverture extérieure` |
| `QA-PROG-05` | Programme Architectural | ✅ PASS | 11 espaces fonctionnels modélisés, SDB=true, Cuisine=true, Salon/Séjour=true, 37 composants 3D sur mesure (25 familles) | `100% des espaces du programme + mobilier sur mesure` |
| `QA-AREA-06` | Surfaces & Équilibre Spatial | ✅ PASS | Total Habitable = 65.9 m² (Zone Jour/Atelier = 25.9 m², Zone Nuit = 40.0 m²) | `70.0 m² ± 8.0 m²` |
| `QA-BIM-07` | Intégrité Modèle BIMcraft | ✅ PASS | 130 objets BIM compilés et vérifiés sur disque | `Modèle .bimcraft + .ifc + .glb + .svg valides` |
