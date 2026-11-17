# 🏛️ Variante 2 — LE MEUBLE HABITÉ (Micro-Habitat Modulable Camille) — Dossier Architectural & Rapport de Conformité

**Projet :** LA CABANE — Micro-Habitat & Atelier Polyvalent (`ARC-AUTO-CABANE-2026`)  
**Profil Client :** Élise / Alex / Camille — Créatifs indépendants en télétravail & accueil week-end  
**Échelle :** `1:50` | **Unité :** `mm` | **Version APM :** `v4`

## 1. Parti Architectural & Justifications

- **Concept :** Parti 'Meuble Habité' : Un bloc menuisé central intègre la salle d'eau, la cuisine et un couchage en hauteur accessible par échelle meunière.
- **Argumentaire Spatial :** Très sculptural lorsque la toiture à deux pans (+5.10 m) est autorisée, mais sous la contrainte de toit plat HSP 2,40 m du plan coté 1:50, le couchage haut manque de garde d'air.
- **Bilan des Surfaces :** Zone Jour = **25.0 m²** | Zone Nuit = **9.8 m²** | Surface Habitable Totale = **35.1 m²**

## 2. Programme & Espaces Créés

| N° | Pièce / Espace | Zonage | Coordonnées Repère (mm) |
|:---|:---|:---|:---|
| **01** | ESTRADE-ATELIER (+0.35m) | `Jour` | `[1800, -1300]` |
| **02** | SALLE D'EAU / WC | `Services/Circulation` | `[5150, -1300]` |
| **03** | SALON & LECTURE | `Jour` | `[1400, -4400]` |
| **04** | CHAMBRE D'AMIS (LIT-TIROIR WEEK-END) | `Nuit` | `[2200, -3600]` |
| **05** | CUISINE & REPAS | `Jour` | `[4800, -4400]` |
| **06** | ENTRÉE / CIRCULATION | `Services/Circulation` | `[3000, -5200]` |

## 3. Planche de Matériaux & Finitions

| Espace | Ouvrage | Matériau | Description | Teinte |
|:---|:---|:---|:---|:---|
| Estrade-Atelier (+35 cm) & Bancs-Marches (+17,5 cm) | Structure & Plateau | **Contreplaqué Bouleau Multiplis & Pin Douglas** | Façade ajourée acoustique + trappe lit-tiroir de 160 cm | `#D4B483` |
| Séjour / Kitchenette (±0,00 m) | Plancher chauffant | **Béton Ciré Minéral Chaud & Chêne Brossé** | Sol continu sur plancher chauffant basse température | `#E2DCD3` |
| Salle d'eau (4,8 m²) | Cloisons 8 cm & Douche 120×90 | **Grès Cérame Vert Sauge & Terrazzo Clair** | Faïence toute hauteur 240 cm + porte à galandage | `#8CA092` |
| Terrasse Extérieure Ouest (1,22 × 6,20 m) | Platelage extérieur | **Mélèze Naturel Raboté** | Lames longitudinales sur lambourdes avec marche d'accueil Sud | `#B68D60` |

## 4. Rapport de Vérification & Contrôle Qualité (Score : 100.0% — Convergé en 1 itération)

| Règle | Catégorie | Statut | Mesure Réelle | Seuil Exigé |
|:---|:---|:---:|:---|:---|
| `QA-HSP-01` | Gabarit & Volumétrie | ✅ PASS | 6 murs à 2400 mm (écarts détectés: 0) | `= 2400 mm` |
| `QA-CLASH-01B` | Collisions Baies (Porte / Fenêtre) | ✅ PASS | 2 portes et 6 fenêtres inspectées (chevauchements détectés: 0) | `0 collision baie/baie` |
| `QA-STRUCT-02` | Structure & Murs Porteurs | ✅ PASS | 0 percement(s) : portée max = 0 mm, tête de mur min = 0 mm, poutre 35x40 cm active | `Portée <= 4000 mm & Têtes >= 300 mm` |
| `QA-ENTRY-03` | Accès & Enveloppe | ✅ PASS | Porte d'entrée principale = true, Conformité accès = true | `Porte d'entrée conforme au sujet` |
| `QA-FACADE-04` | Façades & Ouvertures Extérieures | ✅ PASS | 6 baies existantes, 0 baies neuves (total 6) | `0 nouvelle ouverture extérieure` |
| `QA-PROG-05` | Programme Architectural | ✅ PASS | 6 espaces fonctionnels modélisés, SDB=true, Cuisine=true, Salon/Séjour=true, 9 composants 3D sur mesure (9 familles) | `100% des espaces du programme + mobilier sur mesure` |
| `QA-AREA-06` | Surfaces & Équilibre Spatial | ✅ PASS | Total Habitable = 35.1 m² (Zone Jour/Atelier = 25.0 m², Zone Nuit = 9.8 m²) | `35.1 m² ± 8.0 m²` |
| `QA-BIM-07` | Intégrité Modèle BIMcraft | ✅ PASS | 29 objets BIM compilés et vérifiés sur disque | `Modèle .bimcraft + .ifc + .glb + .svg valides` |
