import json

# 1. Update revit-mcp-server/src/main.rs
with open('revit-mcp-server/src/main.rs', 'r', encoding='utf-8') as f:
    main_code = f.read()

# Add module declarations
mod_anchor = 'mod walkthrough_director;'
new_mods = '''mod walkthrough_director;
mod auto_dimension;
mod variant_diff_visual;
mod geometry_doctor;
mod casework_layout;
mod artisan_takeoff;'''

main_code = main_code.replace(mod_anchor, new_mods)

# Add tool dispatch handlers
dispatch_anchor = '    if tool_name == "revit_walkthrough_direct" {'
end_walkthrough = '''        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }'''

new_tools_dispatch = '''        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    // ── 5 PRAGMATIC DAILY ARCHITECTURE TOOLS ─────────────────────────────────
    if tool_name == "revit_auto_dimension_plan" {
        let res = auto_dimension::generate_auto_dimensions(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_compare_variants_visual" {
        let res = variant_diff_visual::compare_variants_visual(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_geometry_doctor" {
        let res = geometry_doctor::diagnose_and_heal_geometry(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_layout_casework_millwork" {
        let res = casework_layout::generate_casework_layout(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }

    if tool_name == "revit_extract_takeoff_artisan_table" {
        let res = artisan_takeoff::generate_artisan_takeoff(&post_body);
        let text = serde_json::to_string_pretty(&res).unwrap_or_default();
        return (json!({ "content": [{ "type": "text", "text": text }] }), false);
    }'''

# Replace the walkthrough return block that belongs to revit_walkthrough_direct
idx_start = main_code.find(dispatch_anchor)
if idx_start != -1:
    idx_end = main_code.find(end_walkthrough, idx_start)
    if idx_end != -1:
        main_code = main_code[:idx_end] + new_tools_dispatch + main_code[idx_end + len(end_walkthrough):]

with open('revit-mcp-server/src/main.rs', 'w', encoding='utf-8') as f:
    f.write(main_code)

print("main.rs updated with 5 pragmatic tools dispatch.")

# 2. Update revit-mcp-server/tools_manifest.json
manifest_path = 'revit-mcp-server/tools_manifest.json'
with open(manifest_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

new_manifest_tools = [
    {
        "name": "revit_auto_dimension_plan",
        "description": "Architectural Auto-Dimensioning Engine. Automatically computes and places orthogonal 3-chain dimensions on plans: Chain 1 (door/window openings and piers), Chain 2 (interior net room clearances), Chain 3 (overall building boundaries). Saves hours of tedious manual drafting.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "walls": {
                    "type": "array",
                    "description": "List of walls with coordinates x1, y1, x2, y2",
                    "items": { "type": "object" }
                },
                "openings": {
                    "type": "array",
                    "description": "List of door/window openings with center x, y and width",
                    "items": { "type": "object" }
                },
                "offsetBaseMeters": {
                    "type": "number",
                    "description": "Offset distance from wall to first dimension line in meters (default: 0.80m)"
                },
                "offsetSpacingMeters": {
                    "type": "number",
                    "description": "Spacing between parallel dimension lines in meters (default: 0.50m)"
                }
            }
        },
        "action": "dimension/auto-plan",
        "isHealthCheck": False
    },
    {
        "name": "revit_compare_variants_visual",
        "description": "Visual Demolition & Construction Plan Comparator (Red/Green Diff). Compares existing baseline model with new project variants. Generates an immediate high-resolution vector SVG overlay: Red = Demolished/Removed, Green = Created/New, Grey = Preserved, with exact linear meters and demolition metrics for permits and contractor quotes.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "viewTitle": {
                    "type": "string",
                    "description": "Title of the demolition/construction plan"
                },
                "existingElements": {
                    "type": "array",
                    "description": "List of existing baseline elements with id, coordinates x1, y1, x2, y2, thickness",
                    "items": { "type": "object" }
                },
                "projectElements": {
                    "type": "array",
                    "description": "List of new project variant elements with id, coordinates x1, y1, x2, y2, thickness",
                    "items": { "type": "object" }
                }
            }
        },
        "action": "variants/visual-diff",
        "isHealthCheck": False
    },
    {
        "name": "revit_geometry_doctor",
        "description": "Geometry Doctor & Model Cleaner. Scans and auto-heals architectural models: snaps slightly off-axis walls (e.g. 89.9° to true 90.0°/0.0°), fuses micro-gaps (<25mm) between disconnected wall junctions, purges micro-segments and orphan zero-length lines to prevent Revit warning dialogs.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "walls": {
                    "type": "array",
                    "description": "List of walls to inspect with id, x1, y1, x2, y2",
                    "items": { "type": "object" }
                },
                "angleToleranceDeg": {
                    "type": "number",
                    "description": "Angle tolerance threshold in degrees to snap to 0°/90°/45° (default: 0.85°)"
                },
                "gapToleranceMm": {
                    "type": "number",
                    "description": "Distance threshold in millimeters to snap and fuse disconnected endpoints (default: 25mm)"
                }
            }
        },
        "action": "geometry/doctor-clean",
        "isHealthCheck": False
    },
    {
        "name": "revit_layout_casework_millwork",
        "description": "Parametric Interior Millwork & Casework Layout Engine. Instantly drafts custom wardrobes, dressing closets, fitted kitchens, bookcases, and bathroom vanities along any wall with exact modular subdivisions (600mm/800mm), plinths, hardware, and bill of materials.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "layoutType": {
                    "type": "string",
                    "description": "Type of casework: 'DressingPlacard', 'CuisineLineaire', 'CuisineEnL', 'MeubleVasque', 'Bibliotheque'",
                    "enum": ["DressingPlacard", "CuisineLineaire", "CuisineEnL", "MeubleVasque", "Bibliotheque"]
                },
                "totalLengthMeters": {
                    "type": "number",
                    "description": "Total linear length of the wall or unit in meters (e.g. 3.20m)"
                },
                "depthMeters": {
                    "type": "number",
                    "description": "Cabinet depth in meters (e.g. 0.60m for wardrobe/kitchen, 0.35m for bookcase)"
                },
                "heightMeters": {
                    "type": "number",
                    "description": "Total height in meters (e.g. 2.50m ceiling height, 0.90m base cabinet)"
                },
                "finishMaterial": {
                    "type": "string",
                    "description": "Finish material ('Chêne Naturel Verni Mat', 'Blanc Mat', 'Fenix Noir', etc.)"
                },
                "numModules": {
                    "type": "integer",
                    "description": "Optional explicit number of modules (otherwise computed automatically)"
                }
            }
        },
        "action": "casework/layout-millwork",
        "isHealthCheck": False
    },
    {
        "name": "revit_extract_takeoff_artisan_table",
        "description": "Client & Contractor Quantity Takeoff Generator. Extracts exact room-by-room areas (Loi Carrez), net linear plinths (perimeter minus door widths), wall painting areas, ceiling areas, and detailed door/window joinery schedules. Generates formatted Markdown tables and clean CSV ready for Excel and quotes.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "projectTitle": {
                    "type": "string",
                    "description": "Project title (e.g. 'Appartement Londonien')"
                },
                "rooms": {
                    "type": "array",
                    "description": "List of rooms with number, name, floor area, perimeter, ceiling height, and floor finish",
                    "items": { "type": "object" }
                }
            }
        },
        "action": "takeoff/artisan-table",
        "isHealthCheck": False
    }
]

existing_names = {t['name'] for t in data['tools']}
added = 0
for tool in new_manifest_tools:
    if tool['name'] not in existing_names:
        data['tools'].append(tool)
        added += 1

with open(manifest_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)

print(f"Added {added} new pragmatic tools. Total tools count: {len(data['tools'])}")
