import json

tools_to_add = [
    {
        "name": "revit_live_mirror_sync",
        "description": "Bi-Directional Live Sync & Differential Merging Engine between Autodesk Revit and BIMcraft. Performs multithreaded 3-way geometric and parametric delta tracking, conflict detection, and generates zero-latency sync patches.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "sourceElements": {
                    "type": "array",
                    "description": "List of source elements with coordinates, category and parameters",
                    "items": { "type": "object" }
                },
                "targetElements": {
                    "type": "array",
                    "description": "List of target elements with coordinates, category and parameters",
                    "items": { "type": "object" }
                },
                "strategy": {
                    "type": "string",
                    "description": "Conflict resolution strategy: 'SmartGeometricSnap', 'SourceWins', 'TargetWins', or 'UnionMerge'",
                    "enum": ["SmartGeometricSnap", "SourceWins", "TargetWins", "UnionMerge"]
                }
            }
        },
        "action": "live-mirror/sync",
        "isHealthCheck": False
    },
    {
        "name": "revit_sketch_to_bim",
        "description": "Neural Sketch-to-BIM Vectorizer. Converts 2D hand-drawn sketches, scanned blueprints, or vector strokes into native 3D architectural elements (walls with thicknesses, doors, windows, and closed rooms) ready for Revit and BIMcraft.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "strokes": {
                    "type": "array",
                    "description": "List of strokes with x1, y1, x2, y2, thickness and type ('wall', 'door', 'window')",
                    "items": { "type": "object" }
                },
                "scaleFactor": {
                    "type": "number",
                    "description": "Pixel to meter conversion factor (default: 0.01)"
                },
                "ceilingHeight": {
                    "type": "number",
                    "description": "Ceiling height in meters (default: 2.60m)"
                },
                "snapTolerance": {
                    "type": "number",
                    "description": "Endpoint snap tolerance in meters (default: 0.15m)"
                }
            }
        },
        "action": "sketch/vectorize",
        "isHealthCheck": False
    },
    {
        "name": "revit_simulate_thermal_dynamic_re2020",
        "description": "Dynamic Thermal Simulation (STD) & French RE2020 Engine (ISO 52016-1). Computes bioclimatic needs Bbio, primary energy consumption Cep & Cep,nr, and summer comfort degree-hours DH with regional French climate files.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "zoneName": { "type": "string", "description": "Zone or apartment name" },
                "floorAreaM2": { "type": "number", "description": "Net habitable floor area in m²" },
                "ceilingHeightMeters": { "type": "number", "description": "Ceiling height in meters" },
                "climateZone": {
                    "type": "string",
                    "description": "French climate zone (H1a, H1b, H1c, H2a, H2b, H2c, H2d, H3)",
                    "enum": ["H1a", "H1b", "H1c", "H2a", "H2b", "H2c", "H2d", "H3"]
                },
                "thermalInertia": {
                    "type": "string",
                    "description": "Inertia class ('TresLourde', 'Lourde', 'Moyenne', 'Legere')",
                    "enum": ["TresLourde", "Lourde", "Moyenne", "Legere"]
                },
                "envelopeSurfaces": {
                    "type": "array",
                    "description": "Detailed wall, window, roof, and floor envelope definitions with areas, U-values, orientations, and solar factors",
                    "items": { "type": "object" }
                }
            }
        },
        "action": "simulation/thermal-re2020",
        "isHealthCheck": False
    },
    {
        "name": "revit_solve_structural_fea_instant",
        "description": "Instant Structural Finite Element Analysis (FEA / RDM) and Eurocode 2 & 3 Compliance Solver. Solves internal forces (M_Ed, V_Ed), checks deflection limits (L/350), stress utilization ratio, and provides intelligent cross-section resizing recommendations.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "members": {
                    "type": "array",
                    "description": "Structural members (beams, joists, columns) with span length, section (IPE, HEA), material, and dead/live loads",
                    "items": { "type": "object" }
                }
            }
        },
        "action": "structural/solve-fea",
        "isHealthCheck": False
    },
    {
        "name": "revit_audit_urban_plu_compliance",
        "description": "Automated Urban Permitting (PLU / RNU) and Fire Safety Compliance Auditor. Verifies building envelope height, prospect rule (H = L), boundary setbacks (D >= H/2), footprint ratio (CES), biotope permeability (CBS), and fire escape evacuation distances.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "zonePlu": { "type": "string", "description": "PLU zone code (UB, UA, UC, etc.)" },
                "parcelAreaM2": { "type": "number", "description": "Cadastral plot area in m²" },
                "streetWidthMeters": { "type": "number", "description": "Street width facing parcel in meters" },
                "buildingHeightMeters": { "type": "number", "description": "Total building height in meters" },
                "footprintAreaM2": { "type": "number", "description": "Building ground footprint in m²" },
                "boundaryDistanceMeters": { "type": "number", "description": "Distance to neighboring plot boundaries in meters" },
                "openGroundAreaM2": { "type": "number", "description": "Open ground vegetated surface in m²" },
                "greenRoofAreaM2": { "type": "number", "description": "Green roof area in m²" },
                "maxEscapeDistanceMeters": { "type": "number", "description": "Max walking distance to nearest exit stair in meters" },
                "mainExitDoorWidthMeters": { "type": "number", "description": "Main exit width in meters" }
            }
        },
        "action": "permits/audit-plu",
        "isHealthCheck": False
    },
    {
        "name": "revit_export_usd_visionpro",
        "description": "Native USD/USDZ Spatial Exporter for Apple Vision Pro & glTF 2.0 PBR for Unreal Engine 5.5. Generates optimized scene hierarchies with PBR Metallic-Roughness materials, metersPerUnit=1.0, and VisionOS spatial anchors.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "projectName": { "type": "string", "description": "Project or exported model name" },
                "exportUsd": { "type": "boolean", "description": "Generate USDA/USDZ for Apple Vision Pro" },
                "exportGltf": { "type": "boolean", "description": "Generate glTF 2.0 PBR for Unreal Engine 5.5 / Blender" },
                "outputDirectory": { "type": "string", "description": "Directory to save generated files" },
                "meshes": {
                    "type": "array",
                    "description": "List of 3D meshes / elements with bounding box and category",
                    "items": { "type": "object" }
                }
            }
        },
        "action": "export/spatial-usd-visionpro",
        "isHealthCheck": False
    },
    {
        "name": "revit_walkthrough_direct",
        "description": "Conversational AI Walkthrough Director & Voice Companion. Generates cinematic Catmull-Rom camera waypoints at eye height (1.65m), dynamic solar time-of-day progression, interactive material variant toggles, and synchronized audio narration scripts.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "projectName": { "type": "string", "description": "Project title" },
                "totalDurationSec": { "type": "number", "description": "Total duration of the walkthrough in seconds (e.g. 45 or 60)" },
                "eyeHeightMeters": { "type": "number", "description": "Camera eye height in meters (default: 1.65m)" }
            }
        },
        "action": "walkthrough/direct",
        "isHealthCheck": False
    }
]

manifest_path = 'revit-mcp-server/tools_manifest.json'
with open(manifest_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

existing_names = {t['name'] for t in data['tools']}
added = 0
for tool in tools_to_add:
    if tool['name'] not in existing_names:
        data['tools'].append(tool)
        added += 1

with open(manifest_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)

print(f"Added {added} new tools. Total tools: {len(data['tools'])}")
