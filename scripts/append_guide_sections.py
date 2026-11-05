guide_addition = """

---

## 7. Architecture Multi-Instance & Zéro-Conflit Silencieux (v2.8.0)

L'une des avancées majeures de la version 2.8.0 est la **résilience multi-instance totale**, éliminant tout blocage ou boîte de dialogue d'erreur lors du lancement simultané de plusieurs fenêtres Autodesk Revit ou BIMcraft :

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    GESTIONNAIRE D'INSTANCES MULTIPLES ARC                   │
├─────────────────────────────────────────────────────────────────────────────┤
│ • Instance Revit #1 (PID 31484) -> Port HTTP 6543 | Named Pipe 'revit_mcp'  │
│ • Instance Revit #2 (PID 44210) -> Port HTTP 6544 | Pipe 'revit_mcp_44210' │
│ • Instance BIMcraft #1 (PID 1820) -> Port 31816 (bridge.json)              │
│ • Instance BIMcraft #2 (PID 9452) -> Port 32104 (bridge_9452.json)          │
│ • Registre Actif : %APPDATA%\\RevitMCP\\active_instances.json                 │
└─────────────────────────────────────────────────────────────────────────────┘
```

1. **Suppression définitive des modales d'erreur :** L'add-in C# ne déclenche plus jamais de `TaskDialog.Show` intempestif en cas de port occupé ou de démarrage d'une seconde instance. Toutes les informations sont consignées silencieusement dans le journal d'audit (`RevitMCP.audit.log`).
2. **Allocation dynamique de ports (6543 à 6555) :** Lorsqu'une nouvelle instance de Revit démarre et constate que le port 6543 est déjà lié à une instance existante, elle tente immédiatement et silencieusement les ports suivants (6544, 6545, etc.).
3. **Registre d'état dynamique (`active_instances.json`) :** Chaque instance en cours s'enregistre avec son PID, son port d'écoute et son pipe spécifique, et se désinscrit proprement à la fermeture. Les processus terminés anormalement sont purgés automatiquement.
4. **Découverte automatique côté Rust :** Le serveur `revit-mcp-server` consulte en priorité le Named Pipe à latence sub-milliseconde, puis sonde automatiquement le registre actif et les ports secondaires en cas d'indisponibilité du port 6543.
5. **Support multi-BIMcraft :** Détection multi-plateforme des processus `bimcraft.exe` ou `bimcraft` Unix/macOS via balayage de la table des processus sans blocage de socket.

---

## 8. Les 7 Piliers Technologiques & Innovations de Rupture (v2.8.0)

La version 2.8.0 intègre 7 nouveaux moteurs algorithmiques codés à 100% en Rust natif et multithreadé :

### 1. Moteur de Synchronisation Bidirectionnelle Live Mirror (`live_mirror.rs`)
- Compare les maquettes Autodesk Revit et BIMcraft à l'échelle de chaque entité.
- Analyse différentielle multithreadée avec `rayon` (empreintes géométriques AABB, centroïdes, paramètres).
- Génération de listes de patchs différentiels (`Added`, `Modified`, `Deleted`) avec stratégies de résolution de conflits : `SmartGeometricSnap`, `SourceWins`, `TargetWins`, `UnionMerge`.
- Outil MCP : `revit_live_mirror_sync`.

### 2. Vectoriseur Neuronal Croquis-vers-BIM 3D (`sketch_vectorizer.rs`)
- Transforme les croquis dessinés à la main, scans de plans papier et tracés vectoriels 2D en éléments architecturaux 3D natifs.
- Algorithme d'orthogonalisation automatique des segments, détection de l'épaisseur des parois (séparation automatique cloisons intérieures 10 cm / murs porteurs 30 cm), insertion des portes et baies vitrées, et détection des contours de pièces avec calcul des surfaces habitables.
- Outil MCP : `revit_sketch_to_bim`.

### 3. Simulation Thermique Dynamique & Réglementation RE2020 (`thermal_sim.rs`)
- Conforme à la norme internationale ISO 52016-1 et au moteur réglementaire français RE2020.
- Calcul en temps réel des coefficients de déperdition thermique par transmission ($H_{tr}$) et par renouvellement d'air ($H_{ve}$).
- Intégration des fichiers climatiques officiels français (zones H1a, H1b, H1c, H2a, H2b, H2c, H2d, H3) et prise en compte de l'inertie du bâtiment.
- Évaluation des seuils réglementaires : **Bbio** (Besoin Bioclimatique), **Cep** (Consommation d'Énergie Primaire), **Cep,nr** (Énergie non-renouvelable) et **DH** (Degrés-Heures d'inconfort estival).
- Outil MCP : `revit_simulate_thermal_dynamic_re2020`.

### 4. Moteur RDM / Éléments Finis Instantané & Eurocodes 2 & 3 (`structural_fea.rs`)
- Modèle de poutre Bernoulli-Euler direct pour poutres et poteaux.
- Combinaisons réglementaires aux États Limites Ultimes (ELU $1.35G + 1.50Q$) et de Service (ELS $1.00G + 1.00Q$).
- Calcul des moments fléchissants maximaux $M_{Ed}$, efforts tranchants $V_{Ed}$, flèches admissibles $L/350$ ou $L/500$.
- Diagnostic automatique de section selon les Eurocodes (IPE, HEA, béton armé) avec recommandation d'allègement ou de renforcement pour optimiser le bilan carbone.
- Outil MCP : `revit_solve_structural_fea_instant`.

### 5. Vérificateur Réglementaire PLU & Sécurité Incendie (`plu_checker.rs`)
- Audit complet des règles d'urbanisme du Code de l'Urbanisme et du Plan Local d'Urbanisme :
  - Hauteur maximale au faîtage et à l'acrotère.
  - Règle de prospect par rapport à la voirie ($H \\le L + R$).
  - Retraits aux limites séparatives ($D \\ge H/2$ avec minimum de 3 m).
  - Coefficient d'Emprise au Sol (**CES**) et Coefficient de Biotope par Surface (**CBS**).
  - Sécurité incendie (ERP et Habitation) : distances maximales d'évacuation ($\le 30\\text{ m}$ en cul-de-sac) et largeurs en Unités de Passage (1 UP = 0.90 m, 2 UP = 1.40 m).
- Outil MCP : `revit_audit_urban_plu_compliance`.

### 6. Exportateur Spatial USD/USDZ (Apple Vision Pro) & glTF 2.0 PBR (Unreal Engine 5.5) (`spatial_usd_gltf.rs`)
- Génération native en Rust sans dépendance externe lourde :
  - Format USDA/USDZ conforme avec shaders `UsdPreviewSurface`, unités métriques exactes (`metersPerUnit = 1.0`, `upAxis = "Z"`), et balises d'interaction spatiale pour visionOS et Apple Vision Pro.
  - Format glTF 2.0 PBR complet avec métadonnées d'intégration Nanite et Lumen pour Unreal Engine 5.5.
- Outil MCP : `revit_export_usd_visionpro`.

### 7. Réalisateur IA de Visites Virtuelles & Compagnon Vocal (`walkthrough_director.rs`)
- Génération autonome de trajectoires de caméra cinématiques par splines de Catmull-Rom à hauteur des yeux ($1.65\\text{ m}$).
- Animation solaire dynamique synchronisée (aube dorée, plein midi zénithal, crépuscule d'ambiance).
- Bascule interactive de variantes de finitions et génération du script de narration audio synchronisé pour intégration avec Gemini Live API ou moteurs TTS.
- Outil MCP : `revit_walkthrough_direct`.
"""

with open('docs/revit_architecture_guide.md', 'a', encoding='utf-8') as f:
    f.write(guide_addition)

print("revit_architecture_guide.md updated successfully")
