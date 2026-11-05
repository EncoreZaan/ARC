import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { resolveRevitBridgeConfig, revit, revitHealth } from "./bridge.js";

const bridgeConfig = resolveRevitBridgeConfig();

// ─── Server ──────────────────────────────────────────────────────────────────

const server = new McpServer({
  name: "revit-claude-mcp",
  version: "2.5.0",
});

// ════════════════════════════════════════════════════════════════════════════
// CONNECTION / DIAGNOSTIC TOOLS
// ════════════════════════════════════════════════════════════════════════════


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_health_check",
  {
    description:
      "Vérifie l'état du pont local RevitMCP 2.1.0, la disponibilité de Revit et la liste des documents ouverts (titre, chemin, actif, modifié). Exemple : revit_health_check({}).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revitHealth(bridgeConfig);
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify({ host: bridgeConfig.host, timeoutMs: bridgeConfig.timeoutMs, health: data }, null, 2),
        },
      ],
    };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// READ / QUERY TOOLS
// ════════════════════════════════════════════════════════════════════════════


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_model_info",
  {
    description:
      "Returns basic information about the currently open Revit document: file path, project name, number, author, phases, and active view.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("model/info");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_elements",
  {
    description:
      "Interroge les éléments du modèle Revit (catégories FR/EN/OST ex: 'Mobilier', 'Walls', 'OST_Doors'). Renvoie position (mm), boîte englobante bbox (mm), orientation, hôte et type. Exemple : revit_get_elements({ category: 'Mobilier', limit: 50 }).",
    inputSchema: {
      category: z.string().optional().describe("Nom de catégorie FR ('Mobilier', 'Portes'), EN ('Furniture', 'Walls') ou BuiltInCategory"),
      familyName: z.string().optional().describe("Filtrer par nom de famille"),
      levelName: z.string().optional().describe("Filtrer par nom de niveau"),
      viewName: z.string().optional().describe("Filtrer les éléments visibles dans cette vue"),
      bboxFilter: z.object({
        min: z.object({ x: z.number().describe("Min X en mm"), y: z.number().describe("Min Y en mm"), z: z.number().describe("Min Z en mm") }),
        max: z.object({ x: z.number().describe("Max X en mm"), y: z.number().describe("Max Y en mm"), z: z.number().describe("Max Z en mm") })
      }).optional().describe("Filtre spatial : éléments intersectant la boîte englobante en mm"),
      includeGeometry: z.boolean().optional().describe("Inclure les propriétés géométriques (volume m³, surfaces m²)"),
      limit: z.number().int().min(1).max(500).default(100).describe("Nombre max d'éléments à renvoyer"),
      offset: z.number().int().min(0).default(0).optional().describe("Pagination offset"),
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("elements/query", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_element_parameters",
  {
    description: "Get all parameters (built-in and shared) for a specific element by its integer Element ID.",
    inputSchema: {
      elementId: z.number().int().describe("The Revit element ID (integer)"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async ({ elementId }) => {
    const data = await revit("elements/parameters", { elementId });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_views",
  {
    description: "List all views in the model. Optionally filter by view type: FloorPlan, CeilingPlan, Elevation, Section, 3D, DraftingView, Schedule, etc.",
    inputSchema: {
      viewType: z.string().optional().describe("Filter by Revit ViewType enum name, e.g. 'FloorPlan', '3D', 'Section'"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async ({ viewType }) => {
    const data = await revit("views/list", { viewType });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_sheets",
  {
    description: "List all sheets in the project with their sheet number, name, and which views are placed on each sheet.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("sheets/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_levels",
  {
    description: "List all levels in the project with their name, elevation, and ID.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("levels/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_families",
  {
    description:
      "Liste les familles chargées et leurs types pour une catégorie (accepte FR 'Mobilier', EN 'Furniture', 'OST_Furniture'). Renvoie aussi les paramètres de type si demandé. Exemple : revit_get_families({ category: 'Mobilier', includeTypeParameters: true }).",
    inputSchema: {
      category: z.string().optional().describe("Catégorie en français ('Mobilier', 'Fenêtres'), anglais ou BuiltInCategory"),
      includeTypeParameters: z.boolean().optional().describe("Si vrai, extrait tous les paramètres de type avec unités et lecture seule"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("families/list", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_rooms",
  {
    description:
      "Liste les pièces du projet avec nom, numéro, surface (m²), périmètre (mm) et contour géométrique polygonal optionnel en mm. Exemple : revit_get_rooms({ includeBoundary: true }).",
    inputSchema: {
      levelName: z.string().optional().describe("Filtrer par niveau"),
      includeBoundary: z.boolean().optional().describe("Si vrai, renvoie les coordonnées fermées des contours de pièce en mm"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("rooms/list", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// CREATE / MODIFY TOOLS
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_create_wall",
  {
    description:
      "Create a straight wall in the Revit model. Coordinates are in millimetres (model units). Returns the new element ID.",
    inputSchema: {
      startX: z.number().describe("Start point X in mm"),
      startY: z.number().describe("Start point Y in mm"),
      endX: z.number().describe("End point X in mm"),
      endY: z.number().describe("End point Y in mm"),
      levelName: z.string().describe("Name of the level to host the wall, e.g. 'Level 1'"),
      wallTypeName: z.string().optional().describe("Wall type name; omit to use the project default"),
      height: z.number().optional().describe("Unconnected height in mm; defaults to level-to-level"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/create-wall", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_place_family_instance",
  {
    description:
      "Place une occurrence de famille à un point (coordonnées en mm). Rotation en degrés autour du point d'insertion. Charge automatiquement la famille si familyPath est fourni. Exemple : revit_place_family_instance({ familyName: 'Canapé', typeName: '180', x: 2000, y: 3500, levelName: 'Niveau 1', rotation: 90 }).",
    inputSchema: {
      familyName: z.string().describe("Nom de famille Revit"),
      typeName: z.string().describe("Nom du type dans la famille"),
      familyPath: z.string().optional().describe("Chemin .rfa pour chargement automatique si absent du projet"),
      x: z.number().describe("Position X d'insertion en mm"),
      y: z.number().describe("Position Y d'insertion en mm"),
      z: z.number().optional().describe("Position Z d'insertion en mm (défaut : niveau)"),
      levelName: z.string().describe("Nom du niveau d'accueil"),
      hostElementId: z.number().int().optional().describe("ID de l'hôte (mur, sol, plafond)"),
      rotation: z.number().optional().describe("Rotation en degrés autour de l'axe vertical passant par le point d'insertion"),
      dryRun: z.boolean().optional().describe("Simule la création sans modifier le modèle"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/place-instance", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_parameter",
  {
    description: "Set a parameter value on a Revit element. Supports text, integer, double, and Yes/No parameters.",
    inputSchema: {
      elementId: z.number().int().describe("Element ID to modify"),
      parameterName: z.string().describe("Parameter name (built-in name or shared parameter name)"),
      value: z.union([z.string(), z.number(), z.boolean()]).describe("Value to set"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false, idempotentHint: true },
  },
  async (args) => {
    const data = await revit("elements/set-parameter", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_delete_element",
  {
    description: "Delete an element from the model by its element ID. This operation is permanent (but undoable inside Revit with Ctrl+Z).",
    inputSchema: {
      elementId: z.number().int().describe("Element ID to delete"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: true },
  },
  async (args) => {
    const data = await revit("elements/delete", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_move_element",
  {
    description: "Move an element by a translation vector in mm.",
    inputSchema: {
      elementId: z.number().int().describe("Element ID to move"),
      deltaX: z.number().describe("X displacement in mm"),
      deltaY: z.number().describe("Y displacement in mm"),
      deltaZ: z.number().optional().default(0).describe("Z displacement in mm"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/move", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SHEETS & VIEWS MANAGEMENT
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_create_sheet",
  {
    description: "Create a new sheet in the project with a given sheet number, name, and title block family.",
    inputSchema: {
      sheetNumber: z.string().describe("Sheet number, e.g. 'A-101'"),
      sheetName: z.string().describe("Sheet name, e.g. 'Ground Floor Plan'"),
      titleBlockName: z.string().optional().describe("Title block family type name; omit to use project default"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("sheets/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_place_view_on_sheet",
  {
    description: "Place an existing view onto a sheet at the given viewport centre coordinates (in mm on the sheet).",
    inputSchema: {
      sheetId: z.number().int().describe("Sheet element ID"),
      viewId: z.number().int().describe("View element ID to place"),
      centerX: z.number().describe("Viewport centre X on sheet in mm"),
      centerY: z.number().describe("Viewport centre Y on sheet in mm"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("sheets/place-view", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_activate_view",
  {
    description: "Set the active view in the Revit UI to the specified view by its element ID or name.",
    inputSchema: {
      viewId: z.number().int().optional().describe("View element ID"),
      viewName: z.string().optional().describe("View name (used if viewId not provided)"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/activate", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// EXPORT TOOLS
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_export_pdf",
  {
    description: "Export one or more sheets or views to PDF. Returns the output file path.",
    inputSchema: {
      outputPath: z.string().describe("Full folder path where PDFs will be saved, e.g. 'C:\\\\Exports\\\\PDFs'"),
      sheetIds: z.array(z.number().int()).optional().describe("Sheet element IDs to export; omit for all sheets"),
      viewIds: z.array(z.number().int()).optional().describe("View element IDs to export (non-sheet views)"),
      combineIntoOne: z.boolean().optional().default(false).describe("Combine all pages into a single PDF file"),
      paperSize: z.string().optional().default("A1").describe("Paper size: A0, A1, A2, A3, A4"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("export/pdf", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_export_dwg",
  {
    description: "Export views or sheets to DWG/DXF format. Returns output file paths.",
    inputSchema: {
      outputPath: z.string().describe("Full folder path for DWG files"),
      viewIds: z.array(z.number().int()).optional().describe("View/sheet element IDs; omit for active view"),
      format: z.enum(["DWG", "DXF"]).optional().default("DWG"),
      exportSetupName: z.string().optional().describe("Name of saved DWG export settings; omit for default"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("export/dwg", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_export_ifc",
  {
    description: "Export the model or a subset of elements to IFC format.",
    inputSchema: {
      outputPath: z.string().describe("Full file path for the IFC output, e.g. 'C:\\\\Exports\\\\model.ifc'"),
      ifcVersion: z.enum(["IFC2x3", "IFC4", "IFC4x3"]).optional().default("IFC4").describe("IFC schema version"),
      exportBaseQuantities: z.boolean().optional().default(true),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("export/ifc", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_export_schedule",
  {
    description: "Export a schedule view to CSV or TXT.",
    inputSchema: {
      scheduleViewId: z.number().int().describe("Element ID of the Schedule view"),
      outputPath: z.string().describe("Full file path, e.g. 'C:\\\\Exports\\\\schedule.csv'"),
      delimiter: z.enum(["comma", "tab", "semicolon"]).optional().default("comma"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("export/schedule", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// MACRO / COMMAND RUNNER
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_run_command",
  {
    description:
      "Execute a built-in Revit command by its PostableCommandId name (e.g. 'ID_REVIT_THIN_LINES', 'ID_VIEW_GRAPHIC_DISPLAY_OPTIONS'). Use for UI commands not exposed via other tools.",
    inputSchema: {
      commandId: z.string().describe("PostableCommandId string, e.g. 'ID_REVIT_THIN_LINES'"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("commands/run", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_run_dynamo_script",
  {
    description: "Run a Dynamo script (.dyn file) on the current Revit model. Returns stdout/stderr from Dynamo.",
    inputSchema: {
      scriptPath: z.string().describe("Absolute path to the .dyn file, e.g. 'C:\\\\Scripts\\\\myscript.dyn'"),
      journalData: z.record(z.string(), z.string()).optional().describe("Key-value pairs injected into the Dynamo journal"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("commands/run-dynamo", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// MATERIAL TAKEOFF
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_get_material_takeoff",
  {
    description:
      "Generate a full material takeoff for the project. Collects all structural/architectural elements, reads their material layers, area, volume, and count — grouped and summed per material. Optionally filter by category or level. Returns a ready-to-use table. Much faster than querying elements one by one.",
    inputSchema: {
      categories: z.array(z.string()).optional().describe("Categories to include, e.g. ['Walls','Floors','Roofs']. Omit for all."),
      levelName: z.string().optional().describe("Filter by level name"),
      includeLayerBreakdown: z.boolean().optional().default(true).describe("If true, break down compound walls/floors by their individual material layers"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("takeoff/materials", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_export_material_takeoff_csv",
  {
    description:
      "Run a material takeoff and save the result directly to a CSV file. Columns: Material, Category, TotalAreaM2, TotalVolM3, ElementCount, Unit. Ready to open in Excel.",
    inputSchema: {
      outputPath: z.string().describe("Full CSV file path, e.g. 'C:\\\\Exports\\\\takeoff.csv'"),
      categories: z.array(z.string()).optional().describe("Categories to include; omit for all"),
      levelName: z.string().optional().describe("Filter by level"),
      includeLayerBreakdown: z.boolean().optional().default(true),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("takeoff/export-csv", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_material_schedule",
  {
    description:
      "Create a proper Revit Material Takeoff schedule view inside the project for a given category (e.g. 'Walls'). The schedule will appear in the Project Browser under Schedules and can be placed on sheets.",
    inputSchema: {
      category: z.string().describe("Category to schedule, e.g. 'Walls', 'Floors', 'Roofs'"),
      scheduleName: z.string().optional().describe("Name for the new schedule view; defaults to '<Category> Material Takeoff'"),
      fields: z.array(z.string()).optional().describe("Parameter names to include as columns. Defaults to: ['Material: Name','Material: Area','Material: Volume','Count']"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("takeoff/create-schedule", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// CLASH DETECTION
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_detect_clashes",
  {
    description:
      "Detect geometric clashes (intersections) between two sets of elements. Typical use: check structural columns vs MEP ducts, or walls vs beams. Returns clash pairs with element IDs, categories, locations, and severity. Saves hours of manual coordination.",
    inputSchema: {
      categoryA: z.string().describe("First category, e.g. 'Walls'"),
      categoryB: z.string().describe("Second category to check against, e.g. 'Columns'"),
      levelName: z.string().optional().describe("Limit clash check to a specific level"),
      tolerance: z.number().optional().default(0).describe("Overlap tolerance in mm — clashes smaller than this are ignored"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("clash/detect", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_export_clash_report",
  {
    description:
      "Run clash detection and export a detailed HTML or CSV report with clash pairs, element info, and 3D coordinates. One command replaces hours of manual coordination review.",
    inputSchema: {
      outputPath: z.string().describe("Full file path, e.g. 'C:\\\\Reports\\\\clash-report.html'"),
      categoryA: z.string().describe("First category"),
      categoryB: z.string().describe("Second category"),
      format: z.enum(["html", "csv"]).optional().default("html"),
      tolerance: z.number().optional().default(0),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("clash/export-report", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// DRAWING ISSUE / REVISION AUTOMATION
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_issue_drawings",
  {
    description:
      "Automate a full drawing issue: add a revision to the project, stamp selected sheets with that revision, update the issue date parameter on all sheets, and optionally export to PDF. Replaces 30–60 minutes of manual sheet management on deadline day.",
    inputSchema: {
      revisionDescription: z.string().describe("Revision description, e.g. 'Issued for Construction'"),
      revisionDate: z.string().describe("Issue date as string, e.g. '2026-05-15'"),
      revisionNumber: z.string().optional().describe("Revision number/letter, e.g. 'B' or '02'"),
      sheetIds: z.array(z.number().int()).optional().describe("Sheet element IDs to stamp; omit for all sheets"),
      exportPdfPath: z.string().optional().describe("If provided, export issued sheets to PDF at this folder path"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("revisions/issue-drawings", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_revision_history",
  {
    description: "List all revisions in the project with sequence, number, description, date, and which sheets carry each revision.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("revisions/history");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// ROOM FINISH SCHEDULE
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_get_room_finishes",
  {
    description:
      "Read the finish parameters (Floor Finish, Wall Finish, Ceiling Finish, Base Finish) for all rooms. Returns a complete room finish matrix grouped by level — ready to verify or export.",
    inputSchema: {
      levelName: z.string().optional().describe("Filter by level"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("rooms/finishes", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_room_finishes_bulk",
  {
    description:
      "Set finish parameters on multiple rooms in one call. Pass an array of room updates with room ID and the finish values to apply. Replaces clicking through each room one by one — saves hours on large projects.",
    inputSchema: {
      updates: z.array(z.object({
        roomId: z.number().int().describe("Room element ID"),
        floorFinish: z.string().optional().describe("Floor Finish parameter value"),
        wallFinish: z.string().optional().describe("Wall Finish parameter value"),
        ceilingFinish: z.string().optional().describe("Ceiling Finish parameter value"),
        baseFinish: z.string().optional().describe("Base Finish parameter value"),
        comments: z.string().optional().describe("Comments parameter value"),
      })).describe("Array of room finish updates"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("rooms/set-finishes-bulk", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// VIEW TEMPLATE & BULK VIEW MANAGEMENT
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_apply_view_template_bulk",
  {
    description:
      "Apply a view template to multiple views in one operation. Filter target views by type, name pattern, or level. Replaces clicking through dozens of views manually — a 5-minute job becomes one command.",
    inputSchema: {
      templateName: z.string().describe("Name of the view template to apply"),
      viewIds: z.array(z.number().int()).optional().describe("Specific view IDs to update; if omitted, use filters below"),
      viewType: z.string().optional().describe("Apply only to views of this type, e.g. 'FloorPlan'"),
      nameContains: z.string().optional().describe("Apply only to views whose name contains this string"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/apply-template-bulk", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_bulk_rename_views",
  {
    description:
      "Rename multiple views or sheets using find-and-replace or a prefix/suffix pattern. Useful for standardising naming conventions across the whole project at once.",
    inputSchema: {
      targets: z.enum(["views", "sheets", "both"]).default("views"),
      findText: z.string().optional().describe("Text to find in existing names"),
      replaceText: z.string().optional().describe("Text to replace with"),
      addPrefix: z.string().optional().describe("Prefix to prepend to all matching names"),
      addSuffix: z.string().optional().describe("Suffix to append to all matching names"),
      nameContains: z.string().optional().describe("Only rename views/sheets whose name contains this string"),
      dryRun: z.boolean().optional().default(false).describe("If true, return what would be renamed without making changes"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/bulk-rename", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// FILE HEALTH / AUDIT
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_audit_families",
  {
    description:
      "Audit all loaded families in the project. Reports: total family count, families not used in the model (candidates for purging), families with large instance counts, and non-standard families (not from the office library path). Helps reduce file bloat.",
    inputSchema: {
      libraryPath: z.string().optional().describe("Office library root path to identify in-house vs foreign families, e.g. 'C:\\\\RevitLibrary'"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("audit/families", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_import_parameters_from_csv",
  {
    description:
      "Bulk-import parameter values from a CSV file onto elements. CSV format: first column = ElementId, remaining columns = parameter name/value pairs. Replaces manual data entry for large datasets — e.g. populating asset tags, fire ratings, or specifications from a spreadsheet.",
    inputSchema: {
      csvPath: z.string().describe("Full path to the CSV file, e.g. 'C:\\\\Data\\\\params.csv'"),
      delimiter: z.enum(["comma", "tab", "semicolon"]).optional().default("comma"),
      dryRun: z.boolean().optional().default(false).describe("If true, validate the CSV and report what would change without writing to the model"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/import-csv-params", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// AUTO SHEET POPULATION
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_batch_set_parameters",
  {
    description:
      "Set one or more parameters on many matching elements at once. Use dryRun first to preview. Saves repetitive property edits across rooms, doors, windows, equipment, sheets, or any filtered category.",
    inputSchema: {
      parameterValues: z.record(z.string(), z.union([z.string(), z.number(), z.boolean()])).describe("Parameter name/value pairs to set, e.g. { Comments: 'Checked', Mark: 'A-01' }"),
      category: z.string().optional().describe("Revit category name, e.g. 'Rooms', 'Doors', 'Windows', 'Mechanical Equipment'"),
      familyName: z.string().optional().describe("Only match FamilyInstance elements from this family"),
      levelName: z.string().optional().describe("Only match elements hosted on this level"),
      viewName: z.string().optional().describe("Only match elements visible in this view"),
      nameContains: z.string().optional().describe("Only match elements whose name contains this text"),
      limit: z.number().int().min(1).max(5000).optional().default(1000).describe("Safety cap for matched elements"),
      dryRun: z.boolean().optional().default(true).describe("Preview matched elements without writing. Set false to apply."),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false, idempotentHint: true },
  },
  async (args) => {
    const data = await revit("elements/batch-set-parameters", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_plan_views_for_levels",
  {
    description:
      "Create floor, ceiling, or structural plan views for multiple levels using a naming template and optional view template. This turns setup of drawing views from repetitive clicking into one dry-run/apply flow.",
    inputSchema: {
      levelNames: z.array(z.string()).optional().describe("Specific level names. Omit for all levels."),
      viewFamily: z.enum(["FloorPlan", "CeilingPlan", "StructuralPlan"]).optional().default("FloorPlan"),
      nameTemplate: z.string().optional().default("{levelName} - {viewFamily}").describe("Use {levelName} and {viewFamily} placeholders"),
      viewTemplateName: z.string().optional().describe("Optional Revit view template to apply"),
      skipExisting: z.boolean().optional().default(true).describe("Skip views whose target name already exists"),
      dryRun: z.boolean().optional().default(true).describe("Preview the views that would be created. Set false to apply."),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/create-plans-for-levels", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_duplicate_views_bulk",
  {
    description:
      "Duplicate many matching views with detailing, as dependent views, or plain duplicates, then optionally apply a template. Useful for creating permit/construction/coordination drawing sets fast.",
    inputSchema: {
      viewIds: z.array(z.number().int()).optional().describe("Specific source view IDs. Omit to use filters."),
      viewType: z.string().optional().describe("Filter by Revit ViewType enum, e.g. 'FloorPlan', 'CeilingPlan', 'Section'"),
      nameContains: z.string().optional().describe("Only duplicate source views whose name contains this text"),
      duplicateOption: z.enum(["WithDetailing", "Duplicate", "AsDependent"]).optional().default("WithDetailing"),
      nameTemplate: z.string().optional().default("{viewName} - Copy").describe("Use {viewName} and {index} placeholders"),
      viewTemplateName: z.string().optional().describe("Optional view template to apply to duplicated views"),
      dryRun: z.boolean().optional().default(true).describe("Preview duplicated views without writing. Set false to apply."),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/duplicate-bulk", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_auto_populate_sheets",
  {
    description:
      "Automatically create sheets and place matching views based on a naming convention. For example: for every floor plan view named 'FP - Level N', create sheet 'A-1N0 - Level N Floor Plan' and place the view on it. Handles an entire drawing set in seconds instead of hours.",
    inputSchema: {
      rules: z.array(z.object({
        viewNamePattern: z.string().describe("Substring or prefix that identifies matching views, e.g. 'Floor Plan - Level'"),
        sheetNumberPrefix: z.string().describe("Sheet number prefix, e.g. 'A-1'"),
        sheetNameTemplate: z.string().describe("Sheet name template; use {viewName} as placeholder, e.g. '{viewName} Plan'"),
        titleBlockName: z.string().optional().describe("Title block to use; omit for project default"),
      })).describe("Matching rules — each rule pairs view name patterns to sheet naming"),
      skipExistingSheets: z.boolean().optional().default(true).describe("Skip if a sheet with the same number already exists"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("sheets/auto-populate", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SELECTION & INTERACTIVE TOOLS
// ════════════════════════════════════════════════════════════════════════════


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_current_selection",
  {
    description:
      "Get currently selected elements in the active Revit document. Returns element IDs, unique IDs, names, and categories.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("selection/get");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_current_selection",
  {
    description:
      "Select specific elements in the active Revit view by their integer element IDs.",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Array of Revit element IDs to select"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("selection/set", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_isolate_elements",
  {
    description:
      "Temporarily isolate specific elements in the current active Revit view (hides all other elements).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Array of element IDs to isolate"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/isolate", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_reset_temporary_override",
  {
    description:
      "Reset temporary hide/isolate mode in the active Revit view, restoring full element visibility.",
    inputSchema: {
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/reset-temporary-override", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_list_categories",
  {
    description:
      "List all available BIM categories in the active Revit project (model categories and bound categories).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("categories/list", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_count_elements",
  {
    description:
      "Count elements in the active Revit document, optionally filtered by category (e.g. 'Walls', 'Doors', 'Windows').",
    inputSchema: {
      category: z.string().optional().describe("Category name in English, French, or BuiltInCategory, e.g. 'Walls', 'Murs', 'OST_Walls'"),
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("elements/count", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_model_warnings",
  {
    description:
      "Retrieve all active warnings and failure messages in the Revit project (e.g. overlapping walls, unclosed room boundaries).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("model/warnings");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// MODEL CREATION & VALIDATION TOOLS
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_create_floor",
  {
    description:
      "Create a rectangular floor element on a given level with specified dimensions in millimeters.",
    inputSchema: {
      levelId: z.number().int().describe("Revit ElementId of the target level"),
      widthMm: z.number().positive().default(5000).describe("Width in millimeters along X axis"),
      lengthMm: z.number().positive().default(5000).describe("Length in millimeters along Y axis"),
      originX: z.number().default(0).describe("Origin X coordinate in millimeters"),
      originY: z.number().default(0).describe("Origin Y coordinate in millimeters"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule"),
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/create-floor", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_room",
  {
    description:
      "Create and place a Revit room on a specific level at coordinates (x, y) with custom name and number.",
    inputSchema: {
      levelId: z.number().int().describe("Target level ElementId"),
      xMm: z.number().describe("X coordinate in millimeters"),
      yMm: z.number().describe("Y coordinate in millimeters"),
      name: z.string().optional().describe("Room name, e.g. 'Living Room'"),
      number: z.string().optional().describe("Room number, e.g. '101'"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("rooms/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_validate_model_changes",
  {
    description:
      "Validate model state by checking for warnings, verifying critical elements, and reporting active document health.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const [info, warnings] = await Promise.all([
      revit("model/info"),
      revit("model/warnings")
    ]);
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify({ modelInfo: info, warningsSummary: warnings }, null, 2),
        },
      ],
    };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// EXPANDED BIM & VIEW WORKFLOW TOOLS
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_zoom_to_elements",
  {
    description:
      "Zoom and pan the active Revit view to frame specific elements by their IDs so the user can inspect them visually.",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Array of element IDs to frame"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/zoom-elements", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_color_elements",
  {
    description:
      "Highlight and color specific elements in the active Revit view using RGB color overrides (Color Splash).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Array of element IDs to override with color"),
      red: z.number().int().min(0).max(255).default(255).describe("Red channel (0-255)"),
      green: z.number().int().min(0).max(255).default(0).describe("Green channel (0-255)"),
      blue: z.number().int().min(0).max(255).default(0).describe("Blue channel (0-255)"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/set-color", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_reset_color_overrides",
  {
    description:
      "Reset graphics and color overrides for elements in the active Revit view.",
    inputSchema: {
      elementIds: z.array(z.number().int()).optional().describe("Specific element IDs to reset, or omit to reset active view"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/reset-color", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_grid",
  {
    description:
      "Create a datum grid line between two points with smart alphabetic or numeric naming.",
    inputSchema: {
      startX: z.number().default(0).describe("Start X in mm"),
      startY: z.number().default(0).describe("Start Y in mm"),
      endX: z.number().default(10000).describe("End X in mm"),
      endY: z.number().default(0).describe("End Y in mm"),
      name: z.string().optional().describe("Optional grid name (e.g. 'A', '1')"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("grids/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_phases",
  {
    description:
      "List project phases (e.g. Existing, Demolition, New Construction).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("phases/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_links",
  {
    description:
      "List all linked Revit models (.rvt) and CAD links/imports (.dwg, .ifc) with their loaded state.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("links/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_rotate_element",
  {
    description:
      "Rotate an element around its vertical central axis by a specified angle in degrees.",
    inputSchema: {
      elementId: z.number().int().describe("Element ID to rotate"),
      angleDegrees: z.number().default(90).describe("Rotation angle in degrees"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/rotate", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_export_view_image",
  {
    description:
      "Export a rendered or shaded image preview (PNG) of the active view or a sheet to disk for visual verification.",
    inputSchema: {
      viewId: z.number().int().optional().describe("Optional view ID, omit for active view"),
      filePath: z.string().optional().describe("Destination PNG file path on disk"),
      pixelSize: z.number().int().default(1920).describe("Image width in pixels"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("export/image", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_save_document",
  {
    description:
      "Save the currently active Revit document or save a backup copy under a new file path.",
    inputSchema: {
      filePath: z.string().optional().describe("Optional path for Save As; omit to save in place"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("model/save", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_summary_report",
  {
    description:
      "Get a consolidated high-level summary of the entire Revit project (document info, levels, views count, sheets count, warnings, elements count).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const [info, levels, views, sheets, warnings, count] = await Promise.all([
      revit("model/info"),
      revit("levels/list"),
      revit("views/list"),
      revit("sheets/list"),
      revit("model/warnings"),
      revit("elements/count")
    ]);
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify({
            document: info,
            levels,
            totalViews: (views as any)?.views?.length ?? 0,
            totalSheets: (sheets as any)?.sheets?.length ?? 0,
            warningsSummary: warnings,
            totalElements: (count as any)?.count ?? 0
          }, null, 2),
        },
      ],
    };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_worksets",
  {
    description:
      "List user worksets in workshared projects (or indicates that the project is not workshared).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("worksets/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_revisions",
  {
    description:
      "List all revisions and drawing issue sequences defined in the Revit project.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("revisions/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_materials",
  {
    description:
      "List materials defined in the Revit project with class, RGB colors, transparency and surface properties.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("materials/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_view_templates",
  {
    description:
      "List all View Templates available in the Revit project with view type and names.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("views/list-templates");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_section_box",
  {
    description:
      "Create or adjust a 3D isometric view section box tightly framing the specified elements or current selection with padding.",
    inputSchema: {
      elementIds: z.array(z.number()).optional().describe("Element IDs to frame; if omitted, uses current selection"),
      paddingMm: z.number().optional().describe("Margin around the bounding box in millimeters (default: 300 mm)"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/create-section-box", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_find_elements_by_parameter",
  {
    description:
      "Search for elements across the model matching a given parameter name and text string value.",
    inputSchema: {
      parameterName: z.string().describe("Parameter name to inspect (e.g. 'Comments', 'Mark', 'Manufacturer')"),
      searchValue: z.string().describe("String value or substring to look for"),
      exactMatch: z.boolean().optional().describe("True for exact match, false for substring matching (default: false)"),
      limit: z.number().optional().describe("Maximum elements to return (default: 100)"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("elements/find-by-parameter", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_copy_parameter_values",
  {
    description:
      "Batch copy parameter values from a source parameter to a target parameter across elements.",
    inputSchema: {
      sourceParameter: z.string().describe("Source parameter name to read value from"),
      targetParameter: z.string().describe("Target parameter name to write value to"),
      elementIds: z.array(z.number()).optional().describe("Element IDs; if omitted, attempts copy across all model elements"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("parameters/copy-values", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_duplicate_sheet",
  {
    description:
      "Duplicate an existing drawing sheet with its title block and assign a new number and name.",
    inputSchema: {
      sheetId: z.number().describe("ID of the sheet to duplicate"),
      sheetNumber: z.string().optional().describe("New sheet number"),
      sheetName: z.string().optional().describe("New sheet name"),
    
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("sheets/duplicate", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_unused_types",
  {
    description:
      "Model hygiene audit: identify family symbols and types that have 0 instances placed in the project.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("model/purge-unused");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_detail_line",
  {
    description:
      "Draw a 2D detail line in the active 2D view (floor plan, section, elevation, drafting view).",
    inputSchema: {
      startX: z.number().describe("Start X coordinate in millimeters"),
      startY: z.number().describe("Start Y coordinate in millimeters"),
      endX: z.number().describe("End X coordinate in millimeters"),
      endY: z.number().describe("End Y coordinate in millimeters"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/create-detail-line", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.A — EXÉCUTION DE CODE ET TRANSACTIONS (P0)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_execute_csharp",
  {
    description:
      "Compile et exécute dynamiquement du code C# via Roslyn avec accès direct à doc, uidoc et app. Sécurisé par allowCodeExecution. Exemple : revit_execute_csharp({ code: 'return doc.Title;' }).",
    inputSchema: {
      code: z.string().describe("Corps de la méthode C# avec accès à (doc, uidoc, app, stdout)"),
      transaction: z.boolean().optional().default(true).describe("Exécuter dans une transaction Revit"),
      dryRun: z.boolean().optional().describe("Exécute et annule pour simuler l'effet"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("code/csharp", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_execute_python",
  {
    description:
      "Exécute du code Python dans l'environnement Revit / Dynamo. Sécurisé par allowCodeExecution. Exemple : revit_execute_python({ code: 'print(doc.Title)' }).",
    inputSchema: {
      code: z.string().describe("Code Python à exécuter"),
      transaction: z.boolean().optional().default(true).describe("Exécuter dans une transaction"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("code/python", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_batch",
  {
    description:
      "Exécute une suite d'opérations MCP dans un groupe de transaction atomique unique. Si une opération échoue, tout est annulé. Exemple : revit_batch({ operations: [{ tool: 'revit_create_wall', args: {...} }] }).",
    inputSchema: {
      operations: z.array(z.object({
        tool: z.string().describe("Nom de l'outil ou action"),
        args: z.record(z.string(), z.unknown()).describe("Arguments de l'outil")
      })).describe("Liste des opérations séquentielles"),
      atomic: z.boolean().optional().default(true).describe("Si vrai, rollback complet en cas d'échec d'une opération"),
      documentTitle: z.string().optional().describe("Document cible"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("batch/execute", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_undo",
  {
    description:
      "Annule les dernières transactions effectuées dans Revit. Exemple : revit_undo({ steps: 1 }).",
    inputSchema: {
      steps: z.number().int().min(1).default(1).optional().describe("Nombre de transactions à annuler"),
      documentTitle: z.string().optional().describe("Document cible"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document")
    },
    annotations: { readOnlyHint: false, destructiveHint: true },
  },
  async (args) => {
    const data = await revit("transactions/undo", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_redo",
  {
    description:
      "Rétablit les dernières transactions annulées dans Revit. Exemple : revit_redo({ steps: 1 }).",
    inputSchema: {
      steps: z.number().int().min(1).default(1).optional().describe("Nombre de transactions à rétablir"),
      documentTitle: z.string().optional().describe("Document cible"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("transactions/redo", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_transaction_history",
  {
    description:
      "Consulte l'historique récent des transactions MCP exécutées sur le modèle. Exemple : revit_get_transaction_history({ limit: 10 }).",
    inputSchema: {
      limit: z.number().int().optional().default(20).describe("Nombre max de transactions à retourner"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("transactions/history", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.B — GESTION DES DOCUMENTS (P0)
// ════════════════════════════════════════════════════════════════════════════


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_list_open_documents",
  {
    description:
      "Renvoie la liste des documents ouverts dans la session Revit en cours (titre, chemin, statut actif, modifié). Exemple : revit_list_open_documents({}).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("documents/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_activate_document",
  {
    description:
      "Active un document spécifique déjà ouvert par son titre ou chemin. Exemple : revit_activate_document({ title: 'Projet_Cabane' }).",
    inputSchema: {
      title: z.string().optional().describe("Titre du document"),
      path: z.string().optional().describe("Chemin du fichier")
    ,
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("documents/activate", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_new_project",
  {
    description:
      "Crée un nouveau document projet Revit à partir d'un gabarit (.rte), l'enregistre sur le disque et l'ouvre dans l'interface active. Exemple : revit_new_project({ templatePath: 'C:\\Gabarits\\DOUBLE-BAY_gabarit.rte', savePath: 'C:\\Projets\\Nouveau.rvt' }).",
    inputSchema: {
      templatePath: z.string().optional().describe("Chemin absolu du gabarit de projet .rte"),
      savePath: z.string().describe("Chemin absolu où enregistrer le nouveau projet .rvt"),
      overwrite: z.boolean().optional().default(true).describe("Écraser si le fichier existe"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async ({ templatePath, savePath, overwrite }) => {
    const data = await revit("documents/new", { templatePath, savePath, overwrite });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_open_document",
  {
    description:
      "Ouvre un document Revit dans la session active sans lancer une nouvelle instance Revit. Exemple : revit_open_document({ path: 'C:\\Projets\\Maison.rvt' }).",
    inputSchema: {
      path: z.string().describe("Chemin absolu du fichier .rvt"),
      detachFromCentral: z.boolean().optional().describe("Détacher du fichier central"),
      audit: z.boolean().optional().describe("Effectuer un audit à l'ouverture"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document")
    ,
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("documents/open", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_close_document",
  {
    description:
      "Ferme un document ouvert avec option d'enregistrement. Exemple : revit_close_document({ title: 'Variante 1', save: true }).",
    inputSchema: {
      title: z.string().describe("Titre du document à fermer"),
      save: z.boolean().optional().default(false).describe("Enregistrer les modifications avant de fermer"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document")
    ,
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: false, destructiveHint: true },
  },
  async (args) => {
    const data = await revit("documents/close", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_save_as",
  {
    description:
      "Enregistre le document sous un nouveau chemin avec options d'écrasement et sauvegardes max. Exemple : revit_save_as({ path: 'C:\\Projets\\Variante_B.rvt' }).",
    inputSchema: {
      path: z.string().describe("Nouveau chemin de fichier .rvt"),
      overwrite: z.boolean().optional().default(true).describe("Écraser si le fichier existe"),
      maxBackups: z.number().int().optional().default(3).describe("Nombre max de fichiers de sauvegarde .0001"),
      documentTitle: z.string().optional().describe("Document cible"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("documents/save-as", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_purge_unused",
  {
    description:
      "Identifie ou supprime les types et familles inutilisés dans le projet (0 occurrences). Exemple : revit_purge_unused({ dryRun: true }).",
    inputSchema: {
      dryRun: z.boolean().optional().describe("Si vrai, liste seulement ce qui serait purgé sans supprimer"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: true },
  },
  async (args) => {
    const data = await revit("model/purge-unused", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_project_info",
  {
    description:
      "Définit les informations du projet (nom, numéro, auteur, client, statut, adresse) exploitées par les cartouches. Exemple : revit_set_project_info({ name: 'Micro-Habitat', number: '2026-01' }).",
    inputSchema: {
      name: z.string().optional().describe("Nom du projet"),
      number: z.string().optional().describe("Numéro du projet"),
      author: z.string().optional().describe("Auteur / dessinateur"),
      client: z.string().optional().describe("Nom du client"),
      status: z.string().optional().describe("Statut du projet (ex: APD, Permis)"),
      address: z.string().optional().describe("Adresse"),
      issueDate: z.string().optional().describe("Date d'émission"),
      custom: z.record(z.string(), z.unknown()).optional().describe("Paramètres personnalisés supplémentaires"),
      documentTitle: z.string().optional().describe("Document cible"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("documents/set-project-info", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.C — GÉOMÉTRIE ET REQUÊTES SPATIALES (P0)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_get_element_location",
  {
    description:
      "Récupère avec précision la position (point x,y,z en mm ou ligne), l'angle de rotation (degrés), la boîte englobante et l'hôte d'une liste d'éléments. Exemple : revit_get_element_location({ elementIds: [12345] }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Liste des identifiants 64 bits des éléments"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("elements/location", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_element_geometry",
  {
    description:
      "Extrait les solides, le volume (m³), la surface (m²), les faces et arêtes d'un élément (coordonnées en mm). Exemple : revit_get_element_geometry({ elementId: 12345, detail: 'faces' }).",
    inputSchema: {
      elementId: z.number().int().describe("ID de l'élément"),
      detail: z.enum(["bbox", "faces", "edges", "mesh"]).optional().default("bbox").describe("Niveau de détail géométrique"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("elements/geometry", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_elements_in_region",
  {
    description:
      "Trouve tous les éléments dont la boîte géométrique intersecte une boîte spatiale 3D spécifiée en millimètres. Exemple : revit_get_elements_in_region({ min: {x:0, y:0, z:0}, max: {x:6400, y:6200, z:3000} }).",
    inputSchema: {
      min: z.object({ x: z.number().describe("Min X en mm"), y: z.number().describe("Min Y en mm"), z: z.number().describe("Min Z en mm") }),
      max: z.object({ x: z.number().describe("Max X en mm"), y: z.number().describe("Max Y en mm"), z: z.number().describe("Max Z en mm") }),
      categories: z.array(z.string()).optional().describe("Catégories cibles optionnelles"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("elements/in-region", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_measure_distance",
  {
    description:
      "Mesure la distance minimale 3D et horizontale en plan entre deux éléments ou deux points (résultats en mm). Exemple : revit_measure_distance({ pointA: {x:0,y:0,z:0}, pointB: {x:1000,y:2000,z:0} }).",
    inputSchema: {
      elementIdA: z.number().int().optional().describe("ID du premier élément"),
      elementIdB: z.number().int().optional().describe("ID du second élément"),
      pointA: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Point A en mm"),
      pointB: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Point B en mm"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("geometry/measure-distance", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_height_at_point",
  {
    description:
      "Effectue un lancer de rayon vertical pour calculer la hauteur libre sous toiture, plancher ou plafond au point (x, y en mm). Exemple : revit_get_height_at_point({ x: 1700, y: 3000, fromZ: 2400 }).",
    inputSchema: {
      x: z.number().describe("Coordonnée X en mm"),
      y: z.number().describe("Coordonnée Y en mm"),
      fromZ: z.number().optional().default(0).describe("Élévation de départ Z en mm (ex: niveau de sol ou mezzanine)"),
      categories: z.array(z.string()).optional().describe("Catégories cibles à intercepter (Toits, Planchers, Plafonds)"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("geometry/height-at-point", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_check_clearance",
  {
    description:
      "Vérifie l'absence d'obstacles physiques dans une zone de dégagement devant ou à côté d'un meuble ou d'une porte (profondeur en mm). Exemple : revit_check_clearance({ elementId: 12345, side: 'front', depth: 900 }).",
    inputSchema: {
      elementId: z.number().int().describe("ID de l'élément (porte, mobilier, meuble cuisine)"),
      side: z.enum(["front", "back", "left", "right"]).optional().default("front").describe("Côté de vérification"),
      depth: z.number().optional().default(900).describe("Profondeur de dégagement en mm (ex: 900 mm devant cuisine)"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("geometry/check-clearance", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_wall_openings",
  {
    description:
      "Retourne la liste ordonnée des portes et fenêtres hébergées par un mur avec dimensions (largeur, hauteur, allège en mm) et distances depuis le début et la fin du mur. Exemple : revit_get_wall_openings({ wallId: 98765 }).",
    inputSchema: {
      wallId: z.number().int().describe("Identifiant 64 bits du mur"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("walls/openings", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_room_at_point",
  {
    description:
      "Identifie la pièce BIM qui contient le point 3D spécifié (coordonnées en mm). Exemple : revit_get_room_at_point({ x: 2000, y: 3000, z: 0 }).",
    inputSchema: {
      x: z.number().describe("Coordonnée X en mm"),
      y: z.number().describe("Coordonnée Y en mm"),
      z: z.number().optional().default(0).describe("Coordonnée Z en mm"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("rooms/at-point", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.D — MODIFICATION D'ÉLÉMENTS (P0)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_set_element_location",
  {
    description:
      "Définit la position absolue d'un élément par point (mm) ou courbe, avec orientation absolue optionnelle (degrés). Exemple : revit_set_element_location({ elementId: 12345, point: { x: 3000, y: 1500, z: 0 }, rotationDeg: 45 }).",
    inputSchema: {
      elementId: z.number().int().describe("ID de l'élément"),
      point: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Position absolue en mm"),
      curve: z.object({
        start: z.object({ x: z.number(), y: z.number(), z: z.number() }),
        end: z.object({ x: z.number(), y: z.number(), z: z.number() })
      }).optional().describe("Nouvelle ligne directrice en mm"),
      rotationDeg: z.number().optional().describe("Angle d'orientation absolu en degrés"),
      dryRun: z.boolean().optional().describe("Simule la modification"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/set-location", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_change_element_type",
  {
    description:
      "Modifie le type d'un ou plusieurs éléments existants (ex: remplacer un canapé 180 par un modèle 200). Exemple : revit_change_element_type({ elementIds: [12345], typeName: 'Canapé convertible 200' }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Identifiants des éléments à mettre à jour"),
      typeName: z.string().optional().describe("Nom du nouveau type"),
      typeId: z.number().int().optional().describe("ID du nouveau type"),
      familyName: z.string().optional().describe("Nom de famille optionnel"),
      dryRun: z.boolean().optional().describe("Simule la modification"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/change-type", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_copy_elements",
  {
    description:
      "Copie des éléments avec un vecteur de décalage delta (mm). Répète la copie si count > 1 pour créer un réseau linéaire. Exemple : revit_copy_elements({ elementIds: [123], delta: {x: 1000, y: 0, z: 0}, count: 3 }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Éléments à dupliquer"),
      delta: z.object({ x: z.number().describe("Déplacement X en mm"), y: z.number().describe("Déplacement Y en mm"), z: z.number().describe("Déplacement Z en mm") }),
      count: z.number().int().optional().default(1).describe("Nombre de copies à effectuer"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/copy", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_mirror_elements",
  {
    description:
      "Applique un miroir à des éléments selon un axe défini par deux points 2D (coordonnées en mm). Exemple : revit_mirror_elements({ elementIds: [123], axisStart: {x:0,y:0}, axisEnd: {x:0,y:5000}, copy: true }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Éléments à inverser"),
      axisStart: z.object({ x: z.number(), y: z.number() }).describe("Point de début de l'axe de symétrie en mm"),
      axisEnd: z.object({ x: z.number(), y: z.number() }).describe("Point de fin de l'axe de symétrie en mm"),
      copy: z.boolean().optional().default(false).describe("Conserver les éléments d'origine (duplication miroir)"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/mirror", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_align_elements",
  {
    description:
      "Aligne une sélection d'éléments sur une coordonnée ou par rapport à un autre élément cible (minX, maxX, centerX, minY, maxY, centerZ). Exemple : revit_align_elements({ elementIds: [10, 20], reference: 'centerX', value: 3000 }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Éléments à aligner"),
      reference: z.enum(["minX", "maxX", "centerX", "minY", "maxY", "centerY", "minZ", "maxZ"]).describe("Point de référence"),
      value: z.number().optional().describe("Valeur cible en mm"),
      toElementId: z.number().int().optional().describe("Élément cible de référence"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/align", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_copy_elements_between_documents",
  {
    description:
      "Transfère et copie des éléments d'un document ouvert à un autre (ex: copier du mobilier entre deux variantes). Exemple : revit_copy_elements_between_documents({ sourceDocument: 'Variante 1', targetDocument: 'Variante 2', elementIds: [12345] }).",
    inputSchema: {
      sourceDocument: z.string().describe("Titre du document source"),
      targetDocument: z.string().describe("Titre du document cible"),
      elementIds: z.array(z.number().int()).describe("IDs des éléments à copier"),
      dryRun: z.boolean().optional().describe("Simuler"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/copy-between-docs", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_flip_element",
  {
    description:
      "Inverse le sens d'ouverture d'une porte ou d'une fenêtre (sens de battement hand et/ou face intérieure/extérieure). Exemple : revit_flip_element({ elementId: 12345, hand: true }).",
    inputSchema: {
      elementId: z.number().int().describe("ID de l'occurrence"),
      hand: z.boolean().optional().describe("Inverser le sens d'ouverture de la poignée / battement"),
      facing: z.boolean().optional().describe("Inverser l'orientation intérieure/extérieure"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/flip", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_hosted_set_position",
  {
    description:
      "Positionne avec précision une porte ou une fenêtre le long de son mur hôte selon une distance cotée en millimètres depuis le début ou la fin du mur. Exemple : revit_hosted_set_position({ elementId: 12345, distanceFromWallStart: 820 }).",
    inputSchema: {
      elementId: z.number().int().describe("ID de la porte ou fenêtre"),
      distanceFromWallStart: z.number().optional().describe("Distance en mm depuis le point de départ du mur hôte"),
      distanceFromWallEnd: z.number().optional().describe("Distance en mm depuis la fin du mur hôte"),
      sillHeight: z.number().optional().describe("Hauteur d'allège en mm"),
      headHeight: z.number().optional().describe("Hauteur sous linteau en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/hosted-set-position", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_element_pinned",
  {
    description:
      "Verrouille ou déverrouille des éléments BIM pour interdire leur déplacement involontaire. Exemple : revit_set_element_pinned({ elementIds: [12345], pinned: true }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("IDs des éléments"),
      pinned: z.boolean().describe("État de verrouillage"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/set-pinned", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_join_geometry",
  {
    description:
      "Joint ou disjoint la géométrie de deux éléments sécants (murs, planchers, toitures). Exemple : revit_join_geometry({ elementIdA: 10, elementIdB: 20, join: true }).",
    inputSchema: {
      elementIdA: z.number().int().describe("Premier élément"),
      elementIdB: z.number().int().describe("Second élément"),
      join: z.boolean().optional().default(true).describe("Vrai pour joindre, faux pour disjoindre"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/join-geometry", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_group_elements",
  {
    description:
      "Regroupe une sélection d'éléments en un groupe nommé Revit. Exemple : revit_group_elements({ elementIds: [10, 20], groupName: 'Bloc Sanitaire' }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("Éléments à regrouper"),
      groupName: z.string().optional().describe("Nom du groupe"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/group", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_ungroup",
  {
    description:
      "Dissocie un groupe Revit en ses éléments constituants. Exemple : revit_ungroup({ groupId: 999 }).",
    inputSchema: {
      groupId: z.number().int().describe("ID de l'occurrence du groupe"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/ungroup", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.E — TYPES ET FAMILLES (P0)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_load_family",
  {
    description:
      "Charge un fichier de famille (.rfa) externe dans le projet Revit avec gestion de l'écrasement. Exemple : revit_load_family({ path: 'C:\\Familles\\Table.rfa' }).",
    inputSchema: {
      path: z.string().describe("Chemin absolu vers le fichier .rfa"),
      overwrite: z.enum(["none", "type", "all"]).optional().default("all").describe("Mode d'écrasement"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("families/load", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_list_family_library",
  {
    description:
      "Recherche les fichiers de familles Revit .rfa disponibles dans la bibliothèque standard ou un dossier donné. Exemple : revit_list_family_library({ search: 'Canapé' }).",
    inputSchema: {
      folder: z.string().optional().describe("Dossier racine à scanner (défaut : bibliothèque Revit FR)"),
      search: z.string().optional().describe("Mot clé de recherche (ex: 'Canapé', 'Table', 'Fenêtre')")
    ,
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("families/list-library", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_duplicate_type",
  {
    description:
      "Duplique un type de famille existant sous un nouveau nom et règle immédiatement ses dimensions en mm. Exemple : revit_duplicate_type({ familyName: 'Canapé 2 places', typeName: '180', newName: 'Canapé convertible 200', parameters: { 'Largeur': 2000 } }).",
    inputSchema: {
      typeId: z.number().int().optional().describe("ID du type source"),
      familyName: z.string().optional().describe("Nom de la famille"),
      typeName: z.string().optional().describe("Nom du type source"),
      newName: z.string().describe("Nouveau nom du type dérivé"),
      parameters: z.record(z.string(), z.unknown()).optional().describe("Dictionnaire des paramètres dimensionnels (valeurs en mm)"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("types/duplicate", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_type_parameters",
  {
    description:
      "Modifie les paramètres de type (largeur, hauteur, profondeur, etc. en mm) pour tous les éléments d'un type. Exemple : revit_set_type_parameters({ typeId: 54321, parameters: { 'Largeur': 2000 } }).",
    inputSchema: {
      typeId: z.number().int().describe("ID du type"),
      parameters: z.record(z.string(), z.unknown()).describe("Paramètres à mettre à jour"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("types/set-parameters", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_type_parameters",
  {
    description:
      "Extrait les paramètres d'un type d'élément avec leurs valeurs typées, unités et statut de lecture seule. Exemple : revit_get_type_parameters({ typeId: 54321 }).",
    inputSchema: {
      typeId: z.number().int().describe("ID du type"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("types/get-parameters", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_window_type",
  {
    description:
      "Raccourci spécialisé pour créer et calibrer un nouveau type de fenêtre avec largeur et hauteur en mm. Exemple : revit_create_window_type({ familyName: 'Fenêtre 1 vantail', newName: 'F60x95', width: 600, height: 950, sillHeight: 1100 }).",
    inputSchema: {
      familyName: z.string().describe("Nom de famille de fenêtre"),
      newName: z.string().describe("Nom du nouveau type"),
      width: z.number().describe("Largeur en mm"),
      height: z.number().describe("Hauteur en mm"),
      sillHeight: z.number().optional().describe("Hauteur d'allège en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("types/create-window-type", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_door_type",
  {
    description:
      "Raccourci spécialisé pour créer et calibrer un nouveau type de porte avec largeur et hauteur en mm. Exemple : revit_create_door_type({ familyName: 'Porte simple', newName: 'P83x204', width: 830, height: 2040 }).",
    inputSchema: {
      familyName: z.string().describe("Famille de porte"),
      newName: z.string().describe("Nom du nouveau type"),
      width: z.number().describe("Largeur en mm"),
      height: z.number().describe("Hauteur en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("types/create-door-type", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_directshape",
  {
    description:
      "Crée un volume géométrique direct (boîte, extrusion) sans fichier de famille externe, idéal pour mobilier sur mesure ou agencement. Dimensions en mm. Exemple : revit_create_directshape({ category: 'Mobilier', name: 'Meuble vasque', shape: { type: 'box', width: 600, length: 1200, height: 850, x: 1000, y: 2000, z: 0 } }).",
    inputSchema: {
      category: z.string().optional().default("Generic Models").describe("Catégorie Revit"),
      name: z.string().describe("Nom de l'élément"),
      shape: z.object({
        type: z.enum(["box", "extrusion"]).default("box"),
        width: z.number().optional().describe("Largeur Y en mm"),
        length: z.number().optional().describe("Longueur X en mm"),
        height: z.number().optional().describe("Hauteur Z en mm"),
        x: z.number().optional().describe("Origine X en mm"),
        y: z.number().optional().describe("Origine Y en mm"),
        z: z.number().optional().describe("Origine Z en mm")
      }).describe("Définition dimensionnelle du volume"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("elements/create-directshape", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.F — MURS, TOITS, SOLS, PLAFONDS, OUVERTURES (P0/P1)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_modify_wall",
  {
    description:
      "Modifie la géométrie ou les contraintes d'un mur existant (coordonnées départ/fin en mm, hauteur en mm, décalages, contrainte supérieure, type). Exemple : revit_modify_wall({ wallId: 12345, height: 3500 }).",
    inputSchema: {
      wallId: z.number().int().describe("ID du mur"),
      start: z.object({ x: z.number(), y: z.number() }).optional().describe("Point de départ en mm"),
      end: z.object({ x: z.number(), y: z.number() }).optional().describe("Point de fin en mm"),
      height: z.number().optional().describe("Hauteur non contrainte en mm"),
      baseOffset: z.number().optional().describe("Décalage inférieur en mm"),
      topOffset: z.number().optional().describe("Décalage supérieur en mm"),
      topConstraintLevel: z.string().optional().describe("Niveau de contrainte supérieure"),
      typeName: z.string().optional().describe("Nouveau type de mur"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("walls/modify", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_split_wall",
  {
    description:
      "Scinde un mur en deux segments distincts à une distance donnée en mm depuis son origine. Exemple : revit_split_wall({ wallId: 12345, distanceFromStart: 3200 }).",
    inputSchema: {
      wallId: z.number().int().describe("ID du mur"),
      distanceFromStart: z.number().describe("Distance de coupe en mm depuis le point de départ"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("walls/split", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_attach_wall",
  {
    description:
      "Attache ou détache le haut ou la base d'un mur à un toit ou un plancher. Exemple : revit_attach_wall({ wallIds: [12345], targetId: 67890, side: 'top' }).",
    inputSchema: {
      wallIds: z.array(z.number().int()).describe("IDs des murs"),
      targetId: z.number().int().describe("ID du toit ou plancher cible"),
      side: z.enum(["top", "base"]).optional().default("top").describe("Côté à attacher"),
      detach: z.boolean().optional().default(false).describe("Détacher si vrai"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("walls/attach", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_roof_footprint",
  {
    description:
      "Crée une toiture par tracé (contour polygonal fermé en mm) avec pentes configurables sur chaque arête (degrés). Exemple : revit_create_roof_footprint({ level: 'Niveau 2', boundary: [{x:0,y:0},{x:6400,y:0},{x:6400,y:6200},{x:0,y:6200}], slopedEdges: [{ edgeIndex: 1, slopeDeg: 35 }, { edgeIndex: 3, slopeDeg: 35 }] }).",
    inputSchema: {
      level: z.string().describe("Niveau d'implantation"),
      boundary: z.array(z.object({ x: z.number(), y: z.number(), z: z.number().optional() })).describe("Polygone fermé en mm"),
      slopedEdges: z.array(z.object({
        edgeIndex: z.number().int().describe("Index de l'arête (0 à n-1)"),
        slopeDeg: z.number().describe("Pente en degrés")
      })).optional().describe("Définition des pentes par arête"),
      baseOffset: z.number().optional().describe("Décalage d'élévation en mm"),
      typeName: z.string().optional().describe("Type de toiture"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("roofs/create-footprint", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_ceiling",
  {
    description:
      "Crée un plafond selon un contour fermé (coordonnées en mm) à une hauteur donnée au-dessus du niveau. Exemple : revit_create_ceiling({ level: 'Niveau 1', boundary: [{x:0,y:0},{x:3000,y:0},{x:3000,y:3000},{x:0,y:3000}], heightOffset: 2500 }).",
    inputSchema: {
      level: z.string().describe("Nom du niveau de référence"),
      boundary: z.array(z.object({ x: z.number(), y: z.number(), z: z.number().optional() })).describe("Points du contour fermé en mm"),
      heightOffset: z.number().describe("Hauteur du plafond au-dessus du sol en mm (ex: 2500 mm)"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("ceilings/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_railing",
  {
    description:
      "Crée un garde-corps le long d'un tracé linéaire en mm (ex: trémie, mezzanine). Exemple : revit_create_railing({ level: 'Mezzanine', path: [{x:0,y:0},{x:3000,y:0}] }).",
    inputSchema: {
      path: z.array(z.object({ x: z.number(), y: z.number(), z: z.number().optional() })).describe("Chemin du garde-corps en mm"),
      level: z.string().describe("Nom du niveau"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("railings/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_opening",
  {
    description:
      "Crée une réservation ou trémie dans un mur, un sol ou un plafond. Contour en mm. Exemple : revit_create_opening({ hostId: 12345, boundary: [{x:1000,y:1000},{x:2000,y:1000},{x:2000,y:2000},{x:1000,y:2000}] }).",
    inputSchema: {
      hostId: z.number().int().describe("ID de l'hôte (sol, mur, toit)"),
      boundary: z.array(z.object({ x: z.number(), y: z.number(), z: z.number().optional() })).describe("Contour de l'ouverture en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("openings/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_ladder",
  {
    description:
      "Crée une échelle de meunier droite dimensionnée (largeur, hauteur, espacement en mm). Exemple : revit_create_ladder({ base: {x: 1500, y: 2000}, width: 500, height: 2600, rungSpacing: 250 }).",
    inputSchema: {
      base: z.object({ x: z.number(), y: z.number() }).describe("Point au sol en mm"),
      width: z.number().optional().default(500).describe("Largeur en mm"),
      height: z.number().optional().default(2600).describe("Hauteur totale en mm"),
      rungSpacing: z.number().optional().default(250).describe("Espacement des échelons en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("ladders/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_level",
  {
    description:
      "Crée un nouveau niveau d'élévation dans le projet (élévation en mm). Exemple : revit_create_level({ name: 'Mezzanine +2.40', elevation: 2400 }).",
    inputSchema: {
      name: z.string().describe("Nom du niveau (ex: 'Mezzanine')"),
      elevation: z.number().describe("Élévation en millimètres par rapport au niveau zéro"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("levels/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_modify_level",
  {
    description:
      "Modifie le nom ou l'élévation (mm) d'un niveau existant. Exemple : revit_modify_level({ name: 'Niveau 1', elevation: 0 }).",
    inputSchema: {
      levelId: z.number().int().optional().describe("ID du niveau"),
      name: z.string().optional().describe("Nom actuel du niveau"),
      newName: z.string().optional().describe("Nouveau nom"),
      elevation: z.number().optional().describe("Nouvelle élévation en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("levels/modify", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.G — PIÈCES ET ZONING (P0)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_create_room_separation_lines",
  {
    description:
      "Dessine des lignes de séparation de pièces (segments en mm) pour subdiviser un espace sans cloison physique (ex: coin cuisine / salon). Exemple : revit_create_room_separation_lines({ lines: [{start: {x:0, y:2000}, end: {x:3000, y:2000}}] }).",
    inputSchema: {
      lines: z.array(z.object({
        start: z.object({ x: z.number(), y: z.number(), z: z.number().optional() }),
        end: z.object({ x: z.number(), y: z.number(), z: z.number().optional() })
      })).describe("Segments de séparation en mm"),
      viewId: z.number().int().optional().describe("Vue en plan associée"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("rooms/create-separation-lines", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_place_room_at_point",
  {
    description:
      "Place et nomme une pièce BIM dans le contour fermé contenant le point (x, y en mm). Exemple : revit_place_room_at_point({ x: 2500, y: 2000, level: 'Niveau 1', name: 'Séjour' }).",
    inputSchema: {
      x: z.number().describe("Position X en mm"),
      y: z.number().describe("Position Y en mm"),
      level: z.string().describe("Niveau d'accueil"),
      name: z.string().describe("Nom de la pièce (ex: 'Chambre', 'Séjour')"),
      number: z.string().optional().describe("Numéro de pièce optionnel"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("rooms/place-at-point", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_delete_unplaced_rooms",
  {
    description:
      "Supprime automatiquement toutes les pièces non placées ou superflues (surface nulle). Exemple : revit_delete_unplaced_rooms({ dryRun: false }).",
    inputSchema: {
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: true },
  },
  async (args) => {
    const data = await revit("rooms/delete-unplaced", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_tag_rooms",
  {
    description:
      "Ajoute des étiquettes de pièce (nom + surface m²) dans la vue en plan pour toutes les pièces non encore étiquetées. Exemple : revit_tag_rooms({ viewId: 150126 }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue en plan (défaut : active)"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("rooms/tag-rooms", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.H — VUES (P0)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_create_section_view",
  {
    description:
      "Crée une vue en coupe architecturale définie par une ligne de coupe (départ, fin en mm), profondeur de champ (mm) et échelle. Exemple : revit_create_section_view({ name: 'Coupe A-A', lineStart: {x: 0, y: 4600}, lineEnd: {x: 6400, y: 4600}, depth: 3000, scale: 50 }).",
    inputSchema: {
      name: z.string().describe("Nom de la coupe (ex: 'Coupe A-A')"),
      lineStart: z.object({ x: z.number(), y: z.number() }).describe("Point de début en mm"),
      lineEnd: z.object({ x: z.number(), y: z.number() }).describe("Point de fin en mm"),
      depth: z.number().optional().default(5000).describe("Profondeur de champ en mm"),
      zMin: z.number().optional().default(-500).describe("Limite basse en Z en mm"),
      zMax: z.number().optional().default(6000).describe("Limite haute en Z en mm"),
      scale: z.number().int().optional().default(50).describe("Échelle (ex: 50 pour 1:50)"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/create-section", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_3d_view",
  {
    description:
      "Crée une vue 3D axonométrique ou perspective avec réglage de caméra et d'orientation (SE, SO, NE, NO). Exemple : revit_create_3d_view({ name: 'Axo Sud-Est', type: 'iso', isoDirection: 'SE' }).",
    inputSchema: {
      name: z.string().describe("Nom de la vue 3D"),
      type: z.enum(["iso", "perspective"]).optional().default("iso").describe("Type de projection"),
      isoDirection: z.enum(["SE", "SO", "SW", "NE", "NO", "NW"]).optional().default("SE").describe("Direction axonométrique"),
      eye: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Position de caméra en mm"),
      target: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Cible en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/create-3d", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_duplicate_view",
  {
    description:
      "Duplique une vue existante (simple, avec détails d'annotation ou dépendante). Exemple : revit_duplicate_view({ viewId: 150126, mode: 'withDetailing', newName: 'Plan RDC - Coté' }).",
    inputSchema: {
      viewId: z.number().int().describe("ID de la vue à dupliquer"),
      mode: z.enum(["duplicate", "withDetailing", "dependent"]).optional().default("withDetailing").describe("Mode de duplication"),
      newName: z.string().optional().describe("Nouveau nom de la vue"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/duplicate", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_view_properties",
  {
    description:
      "Règle les propriétés d'affichage d'une vue : échelle, niveau de détail, visibilité du cadre de cadrage. Exemple : revit_set_view_properties({ viewId: 150126, scale: 50, detailLevel: 'fine', cropVisible: true }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue (défaut : active)"),
      scale: z.number().int().optional().describe("Échelle (ex: 50 pour 1:50)"),
      detailLevel: z.enum(["coarse", "medium", "fine"]).optional().describe("Niveau de détail"),
      cropVisible: z.boolean().optional().describe("Visibilité du cadre de cadrage"),
      cropActive: z.boolean().optional().describe("Activation du cadrage"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/set-properties", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_category_visibility",
  {
    description:
      "Règle la visibilité et le graphisme permanent par catégorie dans une vue (visible, demi-teinte, transparence). Exemple : revit_set_category_visibility({ viewId: 150126, categories: ['Mobilier'], visible: true, halftone: true }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue"),
      categories: z.array(z.string()).describe("Noms de catégories (FR/EN/OST)"),
      visible: z.boolean().optional().describe("Afficher ou masquer la catégorie"),
      halftone: z.boolean().optional().describe("Affichage en demi-teinte"),
      transparency: z.number().int().min(0).max(100).optional().describe("Pourcentage de transparence (0 à 100)"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/set-category-visibility", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_hide_elements_in_view",
  {
    description:
      "Masque ou affiche des éléments spécifiques uniquement dans une vue donnée (ex: masquer le lit escamotable sur le plan semaine). Exemple : revit_hide_elements_in_view({ viewId: 150126, elementIds: [12345], hide: true }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue cible"),
      elementIds: z.array(z.number().int()).describe("IDs des éléments"),
      hide: z.boolean().optional().default(true).describe("Vrai pour masquer, faux pour afficher"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/hide-elements", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.I — ANNOTATIONS ET COTATION (P0)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_create_linear_dimension",
  {
    description:
      "Crée une ligne de cote linéaire entre éléments dans une vue 2D. Position de la ligne en mm. Exemple : revit_create_linear_dimension({ viewId: 150126, linePosition: {x: 0, y: -500}, references: [{elementId: 10}, {elementId: 20}] }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue 2D"),
      linePosition: z.object({ x: z.number(), y: z.number() }).describe("Point de passage de la ligne de cote en mm"),
      references: z.array(z.object({
        elementId: z.number().int().describe("ID de l'élément à coter")
      })).describe("Références des éléments"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/create-linear-dimension", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_spot_elevation",
  {
    description:
      "Place une cote de niveau altimétrique (±0,00, +2,40 m) sur un élément ou point en coordonnées mm. Exemple : revit_create_spot_elevation({ viewId: 150126, elementId: 12345, point: {x: 2000, y: 1500, z: 2400} }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue"),
      elementId: z.number().int().describe("ID de l'élément hôte"),
      point: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point altimétrique en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/create-spot-elevation", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_text_note",
  {
    description:
      "Place une note textuelle 2D dans une vue à une position (mm). Exemple : revit_create_text_note({ viewId: 150126, text: 'Hauteur libre 2,40 m', position: {x: 2000, y: 3000, z: 0} }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue"),
      text: z.string().describe("Texte de la note"),
      position: z.object({ x: z.number(), y: z.number(), z: z.number().optional() }).describe("Position en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/create-text-note", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_tag",
  {
    description:
      "Place une étiquette d'annotation sur un élément (porte, fenêtre, mobilier). Exemple : revit_create_tag({ viewId: 150126, elementId: 12345, leader: false }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue"),
      elementId: z.number().int().describe("ID de l'élément cible"),
      leader: z.boolean().optional().default(false).describe("Ligne de repère"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/create-tag", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_tag_all",
  {
    description:
      "Étiquette automatiquement tous les éléments non étiquetés des catégories spécifiées dans la vue. Exemple : revit_tag_all({ viewId: 150126, categories: ['Portes', 'Fenêtres'] }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue"),
      categories: z.array(z.string()).describe("Catégories à étiqueter"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/tag-all", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_filled_region",
  {
    description:
      "Crée une zone remplie / hachurée 2D selon un contour polygonal fermé en millimètres (utile pour zoning graphique). Exemple : revit_create_filled_region({ viewId: 150126, boundary: [{x:0,y:0},{x:2000,y:0},{x:2000,y:2000},{x:0,y:2000}] }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue 2D"),
      boundary: z.array(z.object({ x: z.number(), y: z.number(), z: z.number().optional() })).describe("Polygone en mm"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/create-filled-region", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_detail_curve",
  {
    description:
      "Dessine une chaîne de lignes de détail 2D (polyligne) dans une vue en millimètres. Exemple : revit_create_detail_curve({ viewId: 150126, points: [{x:0,y:0},{x:1000,y:1000},{x:2000,y:0}], closed: true }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue"),
      points: z.array(z.object({ x: z.number(), y: z.number(), z: z.number().optional() })).describe("Sommets en mm"),
      closed: z.boolean().optional().default(false).describe("Fermer la polyligne"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/create-detail-curve", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.J — FEUILLES ET MISE EN PAGE (P1)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_get_viewports",
  {
    description:
      "Liste toutes les fenêtres de vue (viewports) placées sur une feuille avec leur centre et limites en mm papier. Exemple : revit_get_viewports({ sheetId: 12345 }).",
    inputSchema: {
      sheetId: z.number().int().describe("ID de la feuille (ViewSheet)"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("sheets/get-viewports", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_move_viewport",
  {
    description:
      "Déplace une fenêtre de vue (viewport) sur une feuille vers un nouveau centre en mm papier. Exemple : revit_move_viewport({ viewportId: 98765, center: {x: 150, y: 100} }).",
    inputSchema: {
      viewportId: z.number().int().describe("ID du viewport"),
      center: z.object({ x: z.number().describe("Centre X en mm papier"), y: z.number().describe("Centre Y en mm papier") }),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("sheets/move-viewport", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.K — NOMENCLATURES (P1)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_create_schedule",
  {
    description:
      "Crée une nomenclature de projet pour une catégorie donnée (pièces, portes, fenêtres, mobilier) avec champs spécifiés. Exemple : revit_create_schedule({ category: 'Rooms', name: 'Tableau des surfaces', fields: ['Name', 'Area'] }).",
    inputSchema: {
      category: z.string().describe("Catégorie de la nomenclature (ex: 'Rooms', 'Doors')"),
      name: z.string().describe("Nom de la nomenclature créée"),
      fields: z.array(z.string()).describe("Liste des noms de champs à inclure"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("schedules/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_schedule_data",
  {
    description:
      "Extrait les valeurs tabulaires d'une nomenclature existante sous forme de matrice lignes/colonnes. Exemple : revit_get_schedule_data({ scheduleId: 12345 }).",
    inputSchema: {
      scheduleId: z.number().int().describe("ID de la nomenclature ViewSchedule"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("schedules/get-data", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.L — MATÉRIAUX ET APPARENCE (P1)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_create_material",
  {
    description:
      "Crée un nouveau matériau BIM dans le projet avec couleur RGB (#RRGGBB). Exemple : revit_create_material({ name: 'Chêne clair', color: '#D2B48C' }).",
    inputSchema: {
      name: z.string().describe("Nom du matériau"),
      color: z.string().describe("Couleur hexadécimale, ex: '#D2B48C'"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("materials/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_assign_material",
  {
    description:
      "Assigne un matériau par son nom à des éléments ou un paramètre de matériau spécifique. Exemple : revit_assign_material({ elementIds: [12345], materialName: 'Chêne clair' }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("IDs des éléments cibles"),
      materialName: z.string().describe("Nom du matériau"),
      parameterName: z.string().optional().default("Material").describe("Nom du paramètre de matériau"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("materials/assign", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_paint_face",
  {
    description:
      "Applique une peinture de matériau sur la face d'un élément géométrique. Exemple : revit_paint_face({ elementId: 12345, materialName: 'Peinture blanche' }).",
    inputSchema: {
      elementId: z.number().int().describe("ID de l'élément"),
      materialName: z.string().describe("Nom du matériau de peinture"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("materials/paint-face", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.M — PHASES ET OPTIONS DE CONCEPTION (P1)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_get_design_options",
  {
    description:
      "Liste tous les jeux d'options de conception et options du projet (ex: variante 1, variante 2). Exemple : revit_get_design_options({}).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("design-options/list", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_design_option",
  {
    description:
      "Vérifie ou prépare la création d'une option de conception. Exemple : revit_create_design_option({ setName: 'Aménagement', optionName: 'Semaine' }).",
    inputSchema: {
      setName: z.string().describe("Nom du jeu d'options"),
      optionName: z.string().describe("Nom de l'option"),
      documentTitle: z.string().optional().describe("Document cible"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("design-options/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_move_elements_to_design_option",
  {
    description:
      "Affecte une sélection d'éléments à une option de conception spécifique. Exemple : revit_move_elements_to_design_option({ elementIds: [123], optionName: 'Option Week-end' }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("IDs des éléments à déplacer"),
      optionName: z.string().describe("Nom de l'option cible"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("design-options/move-elements", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_view_design_option",
  {
    description:
      "Configure la visibilité d'une option de conception pour une vue donnée (ex: Plan semaine vs Plan week-end). Exemple : revit_set_view_design_option({ viewId: 150126, optionName: 'Option Semaine' }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue cible"),
      optionName: z.string().describe("Nom de l'option à afficher"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("design-options/set-view-option", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_element_phase",
  {
    description:
      "Définit la phase de création ou de démolition d'éléments du projet (ex: Existant, Projeté). Exemple : revit_set_element_phase({ elementIds: [123], createdPhase: 'Nouvelle construction' }).",
    inputSchema: {
      elementIds: z.array(z.number().int()).describe("IDs des éléments"),
      createdPhase: z.string().optional().describe("Nom de la phase de création"),
      demolishedPhase: z.string().optional().describe("Nom de la phase de démolition"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("phases/set-element-phase", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.N — IMPORTS ET EXPORTS (P0)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_export_dxf",
  {
    description:
      "Exporte des vues ou feuilles au format DXF pour validation CAO à l'échelle. Exemple : revit_export_dxf({ outputPath: 'C:\\Exports\\Planche.dxf' }).",
    inputSchema: {
      outputPath: z.string().describe("Chemin du fichier DXF de sortie"),
      viewIds: z.array(z.number().int()).optional().describe("Vues à exporter"),
      version: z.string().optional().default("2018").describe("Version DXF (ex: '2018')"),
      documentTitle: z.string().optional().describe("Document cible"),
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("export/dxf", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_import_cad",
  {
    description:
      "Lie ou importe un fichier DWG ou DXF validé comme calque de fond de plan pour caler les modifications (unités en mm ou m). Exemple : revit_import_cad({ path: 'C:\\Dessins\\FondPlan.dxf', link: true, units: 'mm' }).",
    inputSchema: {
      path: z.string().describe("Chemin du fichier .dwg ou .dxf"),
      link: z.boolean().optional().default(true).describe("Lier le fichier (true) ou importer directement (false)"),
      viewId: z.number().int().optional().describe("ID de la vue d'accueil"),
      units: z.enum(["mm", "cm", "m"]).optional().default("mm").describe("Unités du fichier CAO"),
      currentViewOnly: z.boolean().optional().default(true).describe("Visible uniquement dans la vue actuelle"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("import/cad", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_cad_geometry",
  {
    description:
      "Extrait les lignes, arcs et polylignes d'un DWG/DXF lié dans Revit en coordonnées projet (mm). Exemple : revit_get_cad_geometry({ importInstanceId: 77777 }).",
    inputSchema: {
      importInstanceId: z.number().int().describe("ID de l'occurrence d'importation (ImportInstance)"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("import/get-cad-geometry", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_walls_from_cad",
  {
    description:
      "Convertit automatiquement les segments de lignes d'un calque DWG/DXF en véritables murs Revit (hauteur en mm). Exemple : revit_create_walls_from_cad({ importInstanceId: 77777, height: 2600 }).",
    inputSchema: {
      importInstanceId: z.number().int().describe("ID de l'instance CAO"),
      height: z.number().optional().default(3000).describe("Hauteur des murs créés en mm"),
      wallType: z.string().optional().describe("Nom du type de mur à appliquer"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("import/create-walls-from-cad", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_import_image",
  {
    description:
      "Importe une image (plan scanné, croquis) en fond de plan 2D avec largeur imposée en mm pour mise à l'échelle. Exemple : revit_import_image({ path: 'C:\\Images\\Plan.png', widthMm: 6400 }).",
    inputSchema: {
      path: z.string().describe("Chemin de l'image .png ou .jpg"),
      viewId: z.number().int().optional().describe("ID de la vue"),
      position: z.object({ x: z.number(), y: z.number() }).optional().describe("Centre en mm"),
      widthMm: z.number().optional().describe("Largeur de l'image en mm pour calage d'échelle"),
      dryRun: z.boolean().optional().describe("Simuler"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("import/image", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.O — INTERFACE ET DIAGNOSTIC (P1)
// ════════════════════════════════════════════════════════════════════════════


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_get_ui_state",
  {
    description:
      "Retourne l'état complet de l'interface Revit : document actif, vue active, onglets ouverts et handle de fenêtre. Exemple : revit_get_ui_state({}).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: true },
  },
  async () => {
    const data = await revit("ui/state");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),server.registerTool(
  "revit_bring_to_front",
  {
    description:
      "Restaure et place la fenêtre Autodesk Revit au premier plan sur l'écran principal. Exemple : revit_bring_to_front({}).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async () => {
    const data = await revit("ui/bring-to-front");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_capture_ui",
  {
    description:
      "Réalise une capture d'écran fiable de la fenêtre Autodesk Revit par l'add-in (évite les écrans noirs de l'OS). Exemple : revit_capture_ui({ outputPath: 'C:\\Screenshots\\Revit.png' }).",
    inputSchema: {
      outputPath: z.string().optional().describe("Chemin du fichier PNG de destination")
    ,
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("ui/capture", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_log",
  {
    description:
      "Récupère les dernières lignes du journal d'audit de l'add-in RevitMCP. Exemple : revit_get_log({ lines: 50 }).",
    inputSchema: {
      lines: z.number().int().optional().default(50).describe("Nombre de lignes récentes à extraire")
    ,
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("system/get-log", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_dismiss_dialogs",
  {
    description:
      "Vérifie et configure la suppression automatique des boîtes de dialogue modales bloquantes dans Revit. Exemple : revit_dismiss_dialogs({ policy: 'ok' }).",
    inputSchema: {
      policy: z.enum(["ok", "cancel", "report", "off"]).optional().default("ok").describe("Politique de réponse")
    ,
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("dialogs/dismiss", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ════════════════════════════════════════════════════════════════════════════
// SECTION 4.P — DYNAMO (P2)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_list_dynamo_scripts",
  {
    description:
      "Liste les scripts Dynamo (.dyn) disponibles dans un dossier ou le répertoire utilisateur. Exemple : revit_list_dynamo_scripts({ search: 'mezzanine' }).",
    inputSchema: {
      folder: z.string().optional().describe("Dossier à explorer"),
      search: z.string().optional().describe("Filtre de recherche")
    ,
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("dynamo/list-scripts", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


// ════════════════════════════════════════════════════════════════════════════
// SECTION COMPLÉMENTAIRE — ARCHITECTURE, ANNOTATION ET DOCUMENTATION (v2.1.0)
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_create_roof_extrusion",
  {
    description:
      "Crée une toiture par extrusion le long d'un profil vertical donné et l'extrude entre deux limites en millimètres. Exemple : revit_create_roof_extrusion({ profile: [{startX: 0, startY: 0, startZ: 2500, endX: 3200, endY: 0, endZ: 5000}], levelName: 'Niveau 1', endExtrusionMm: 6200 }).",
    inputSchema: {
      profile: z.array(z.object({
        startX: z.number().describe("Début X en mm"),
        startY: z.number().describe("Début Y en mm"),
        startZ: z.number().describe("Début Z en mm"),
        endX: z.number().describe("Fin X en mm"),
        endY: z.number().describe("Fin Y en mm"),
        endZ: z.number().describe("Fin Z en mm")
      })).describe("Profil vertical de la toiture"),
      levelName: z.string().describe("Niveau d'attache"),
      startExtrusionMm: z.number().optional().default(0).describe("Début d'extrusion en mm"),
      endExtrusionMm: z.number().optional().default(5000).describe("Fin d'extrusion en mm"),
      roofTypeName: z.string().optional().describe("Nom du type de toiture"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("roofs/create-extrusion", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_modify_roof",
  {
    description:
      "Modifie les caractéristiques d'une toiture existante (type, niveau de base). Exemple : revit_modify_roof({ roofId: 123456, typeName: 'Toiture chaude isolée 30cm' }).",
    inputSchema: {
      roofId: z.number().describe("ID 64 bits de la toiture"),
      typeName: z.string().optional().describe("Nouveau type de toiture"),
      baseLevelName: z.string().optional().describe("Nouveau niveau de base"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("roofs/modify", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_modify_floor_boundary",
  {
    description:
      "Modifie le contour fermé d'un sol ou d'une dalle existante à partir d'une liste ordonnée de points en millimètres. Exemple : revit_modify_floor_boundary({ floorId: 123456, boundary: [{x: 0, y: 0}, {x: 6400, y: 0}, {x: 6400, y: 6200}, {x: 0, y: 6200}] }).",
    inputSchema: {
      floorId: z.number().describe("ID 64 bits du sol"),
      boundary: z.array(z.object({ x: z.number(), y: z.number() })).describe("Polygone fermé en mm"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("floors/modify-boundary", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_stair",
  {
    description:
      "Crée un escalier droit standard entre deux niveaux du projet avec une volée définie par ses points de départ et d'arrivée en millimètres. Exemple : revit_create_stair({ baseLevelName: 'RDC', topLevelName: 'Mezzanine', startX: 1000, startY: 2000, endX: 3500, endY: 2000 }).",
    inputSchema: {
      baseLevelName: z.string().describe("Niveau de départ"),
      topLevelName: z.string().describe("Niveau d'arrivée"),
      startX: z.number().describe("Point de départ X en mm"),
      startY: z.number().describe("Point de départ Y en mm"),
      endX: z.number().describe("Point d'arrivée X en mm"),
      endY: z.number().describe("Point d'arrivée Y en mm"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("stairs/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_simple_family",
  {
    description:
      "Génère et insère un composant volumique paramétrique simple (forme parallélépipédique) dans une catégorie cible avec dimensions en millimètres. Exemple : revit_create_simple_family({ familyName: 'CaissonRangement', category: 'Mobilier', widthMm: 800, depthMm: 500, heightMm: 750 }).",
    inputSchema: {
      familyName: z.string().describe("Nom de la famille ou du composant"),
      category: z.string().optional().default("Mobilier").describe("Catégorie FR/EN"),
      widthMm: z.number().describe("Largeur en mm"),
      depthMm: z.number().describe("Profondeur en mm"),
      heightMm: z.number().describe("Hauteur en mm"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("families/create-simple", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_edit_family_parameters",
  {
    description:
      "Modifie directement les paramètres internes d'une famille RFA chargée dans le projet puis la recharge automatiquement. Exemple : revit_edit_family_parameters({ familyName: 'Table', parameters: { 'Largeur': 1600, 'Hauteur': 750 } }).",
    inputSchema: {
      familyName: z.string().describe("Nom de la famille chargée"),
      parameters: z.record(z.string(), z.unknown()).describe("Dictionnaire des paramètres à mettre à jour"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("families/edit-parameters", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_elevation_views",
  {
    description:
      "Crée un marqueur d'élévation et génère automatiquement les 4 vues d'élévation cardinales (Nord, Est, Sud, Ouest) associées à un plan. Exemple : revit_create_elevation_views({ x: 3200, y: 3100 }).",
    inputSchema: {
      x: z.number().describe("Coordonnée X du centre du marqueur en mm"),
      y: z.number().describe("Coordonnée Y du centre du marqueur en mm"),
      planViewId: z.number().optional().describe("ID de la vue en plan de référence"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/create-elevations", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_drafting_view",
  {
    description:
      "Crée une vue de dessin 2D indépendante du modèle (Drafting View) à une échelle donnée. Exemple : revit_create_drafting_view({ name: 'Détail étanchéité acrotère', scale: 10 }).",
    inputSchema: {
      name: z.string().describe("Nom de la vue de dessin"),
      scale: z.number().optional().default(50).describe("Échelle (ex: 20 pour 1:20)"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/create-drafting", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_view_filter",
  {
    description:
      "Crée un filtre de vue basé sur des règles pour cibler une ou plusieurs catégories d'éléments. Exemple : revit_create_view_filter({ filterName: 'Murs extérieurs', categories: ['Murs'] }).",
    inputSchema: {
      filterName: z.string().describe("Nom du filtre"),
      categories: z.array(z.string()).describe("Liste des catégories ciblées (FR ou EN)"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/filters/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_apply_view_filter",
  {
    description:
      "Applique un filtre de vue existant sur une vue avec contrôle de visibilité et remplacements graphiques. Exemple : revit_apply_view_filter({ viewId: 123456, filterId: 654321, visible: true }).",
    inputSchema: {
      viewId: z.number().describe("ID de la vue"),
      filterId: z.number().describe("ID du filtre"),
      visible: z.boolean().optional().describe("Visibilité des éléments filtrés"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/filters/apply", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_view_template_from_view",
  {
    description:
      "Génère un gabarit de vue (View Template) à partir des réglages d'affichage et de visibilité d'une vue existante. Exemple : revit_create_view_template_from_view({ viewId: 123456, templateName: 'Gabarit Plan Aménagement' }).",
    inputSchema: {
      viewId: z.number().describe("ID de la vue source"),
      templateName: z.string().describe("Nom du nouveau gabarit"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/create-template", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_delete_views",
  {
    description:
      "Supprime une liste de vues ou de feuilles du projet par leurs identifiants. Exemple : revit_delete_views({ viewIds: [123456, 123457] }).",
    inputSchema: {
      viewIds: z.array(z.number()).describe("Liste des identifiants 64 bits des vues à supprimer"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: true },
  },
  async (args) => {
    const data = await revit("views/delete", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_close_inactive_views",
  {
    description:
      "Ferme tous les onglets de vue inactifs pour libérer la mémoire graphique et optimiser les performances de Revit. Exemple : revit_close_inactive_views({}).",
    inputSchema: {
      dryRun: z.boolean().optional().describe("Mode simulation : exécute la transaction puis l'annule pour tester la validité sans modifier le document"),
    
      documentTitle: z.string().optional().describe("Titre ou chemin du document cible (défaut : document actif)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/auto-dimension-walls", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_dimension_openings",
  {
    description:
      "Crée une chaîne complète de cotes linéaires le long d'un mur reliant le départ du mur, chaque porte/fenêtre et la fin du mur. Exemple : revit_dimension_openings({ wallId: 123456 }).",
    inputSchema: {
      wallId: z.number().describe("ID du mur à coter"),
      viewId: z.number().optional().describe("ID de la vue"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/dimension-openings", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_line_style",
  {
    description:
      "Crée ou met à jour un style de ligne personnalisé dans la catégorie Lignes avec épaisseur et couleur RGB. Exemple : revit_create_line_style({ name: 'Axe Mezzanine', weight: 3, red: 255, green: 0, blue: 0 }).",
    inputSchema: {
      name: z.string().describe("Nom du style de ligne"),
      weight: z.number().optional().default(2).describe("Épaisseur de trait (1 à 16)"),
      red: z.number().min(0).max(255).optional().default(0),
      green: z.number().min(0).max(255).optional().default(0),
      blue: z.number().min(0).max(255).optional().default(0),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("lines/create-style", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_place_annotation_symbol",
  {
    description:
      "Insère un symbole d'annotation 2D (étiquette générique, flèche, repère) sur une vue à des coordonnées données en millimètres. Exemple : revit_place_annotation_symbol({ symbolName: 'Nord Géographique', x: 5000, y: 5000 }).",
    inputSchema: {
      symbolName: z.string().describe("Nom du symbole ou type d'annotation"),
      x: z.number().describe("Position X en mm"),
      y: z.number().describe("Position Y en mm"),
      viewId: z.number().optional().describe("ID de la vue"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("annotations/place-symbol", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_place_detail_component",
  {
    description:
      "Place une instance de composant de détail 2D (vis, tasseau, isolant, meuble 2D) sur une vue de détail ou de coupe. Exemple : revit_place_detail_component({ componentName: 'Chevron 60x80', x: 1200, y: 2400 }).",
    inputSchema: {
      componentName: z.string().describe("Nom du type de composant de détail"),
      x: z.number().describe("Position X en mm"),
      y: z.number().describe("Position Y en mm"),
      viewId: z.number().optional().describe("ID de la vue"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("details/place-component", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_legend_view",
  {
    description:
      "Crée une nouvelle vue de légende pour documenter les composants graphiques ou matériaux à une échelle définie. Exemple : revit_create_legend_view({ name: 'Légende Menuiseries', scale: 20 }).",
    inputSchema: {
      name: z.string().describe("Nom de la légende"),
      scale: z.number().optional().default(50).describe("Échelle"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/create-legend", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_place_legend_component",
  {
    description:
      "Place un composant de famille sur une vue de légende pour illustrer la nomenclature graphique. Exemple : revit_place_legend_component({ legendId: 123456, familyTypeName: 'Fenêtre 1 vantail 60x80', x: 200, y: 200 }).",
    inputSchema: {
      legendId: z.number().describe("ID de la vue de légende"),
      familyTypeName: z.string().describe("Nom du type de famille"),
      x: z.number().describe("Position X en mm"),
      y: z.number().describe("Position Y en mm"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("legends/place-component", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_color_fill_scheme",
  {
    description:
      "Configure ou interroge un schéma de couleur automatique pour les pièces ou surfaces du document. Exemple : revit_create_color_fill_scheme({ name: 'Schéma Destination', category: 'Pièces' }).",
    inputSchema: {
      name: z.string().describe("Nom du schéma de couleur"),
      category: z.string().optional().default("Pièces").describe("Catégorie ciblée (Pièces ou Surfaces)"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("colors/create-scheme", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_apply_color_fill",
  {
    description:
      "Applique un schéma de remplissage couleur sur une vue en plan et insère sa légende graphique à des coordonnées en millimètres. Exemple : revit_apply_color_fill({ viewId: 123456, legendX: 7000, legendY: 3000 }).",
    inputSchema: {
      viewId: z.number().optional().describe("ID de la vue"),
      legendX: z.number().optional().default(5000).describe("Position X de la légende en mm"),
      legendY: z.number().optional().default(5000).describe("Position Y de la légende en mm"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("colors/apply-legend", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_area_plan",
  {
    description:
      "Crée un plan de surface réglementaire (Surface Habitable / Emprise) associé à un niveau du projet. Exemple : revit_create_area_plan({ levelName: 'RDC' }).",
    inputSchema: {
      levelName: z.string().describe("Niveau associé"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("areas/create-plan", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_area",
  {
    description:
      "Place une zone de surface réglementaire calculée sur un plan de surface existant à des coordonnées données en mm. Exemple : revit_create_area({ areaPlanId: 123456, x: 2500, y: 3000 }).",
    inputSchema: {
      areaPlanId: z.number().describe("ID de la vue de plan de surface"),
      x: z.number().describe("Coordonnée X en mm"),
      y: z.number().describe("Coordonnée Y en mm"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("areas/create", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_change_title_block",
  {
    description:
      "Remplace ou met à jour le type de cartouche (fond de page A4, A3, A0...) présent sur une feuille de présentation. Exemple : revit_change_title_block({ sheetId: 123456, titleBlockName: 'Cartouche A4 Paysage' }).",
    inputSchema: {
      sheetId: z.number().describe("ID de la feuille"),
      titleBlockName: z.string().describe("Nom du nouveau type de cartouche"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("sheets/change-titleblock", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_sheet_parameters",
  {
    description:
      "Met à jour les paramètres de titre, numéro, date et intervenants d'une feuille de présentation. Exemple : revit_set_sheet_parameters({ sheetId: 123456, sheetNumber: 'A101', sheetName: 'Plan RDC et Mezzanine', drawnBy: 'Antigravity', issueDate: '2026-10-20' }).",
    inputSchema: {
      sheetId: z.number().describe("ID de la feuille"),
      sheetNumber: z.string().optional().describe("Numéro de feuille"),
      sheetName: z.string().optional().describe("Nom de la feuille"),
      drawnBy: z.string().optional().describe("Dessiné par"),
      checkedBy: z.string().optional().describe("Vérifié par"),
      approvedBy: z.string().optional().describe("Approuvé par"),
      issueDate: z.string().optional().describe("Date d'émission"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("sheets/set-parameters", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_viewport_type",
  {
    description:
      "Change le style graphique d'une fenêtre de vue sur feuille (avec titre, sans titre, ligne masquée). Exemple : revit_set_viewport_type({ viewportId: 123456, typeName: 'Sans titre' }).",
    inputSchema: {
      viewportId: z.number().describe("ID du viewport"),
      typeName: z.string().describe("Nom du type de viewport cible"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("viewports/set-type", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_place_schedule_on_sheet",
  {
    description:
      "Place une nomenclature existante sous forme de tableau graphique sur une feuille de présentation à des coordonnées en millimètres. Exemple : revit_place_schedule_on_sheet({ sheetId: 123456, scheduleId: 654321, x: 200, y: 150 }).",
    inputSchema: {
      sheetId: z.number().describe("ID de la feuille de présentation"),
      scheduleId: z.number().describe("ID de la nomenclature"),
      x: z.number().optional().default(100).describe("Position X sur feuille en mm"),
      y: z.number().optional().default(100).describe("Position Y sur feuille en mm"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("sheets/place-schedule", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_align_viewports",
  {
    description:
      "Aligne plusieurs fenêtres de vue (viewports) sur un axe horizontal ou vertical commun. Exemple : revit_align_viewports({ viewportIds: [123456, 123457], mode: 'horizontal' }).",
    inputSchema: {
      viewportIds: z.array(z.number()).describe("Liste des identifiants des viewports à aligner"),
      mode: z.enum(["horizontal", "vertical"]).optional().default("horizontal").describe("Axe d'alignement"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("viewports/align", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_render_view",
  {
    description:
      "Génère un rendu d'image haute résolution vectoriel/photoréaliste de n'importe quelle vue du projet. Exemple : revit_render_view({ viewId: 123456, width: 2560, outputPath: 'C:\Render\VueSalon.png' }).",
    inputSchema: {
      viewId: z.number().optional().describe("ID de la vue à rendre"),
      width: z.number().optional().default(1920).describe("Largeur en pixels"),
      outputPath: z.string().optional().describe("Chemin du fichier PNG de destination"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("views/render", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_run_dynamo_inline",
  {
    description:
      "Exécute du code inline Python/Dynamo directement au sein du document Revit actif. Exemple : revit_run_dynamo_inline({ code: 'result = doc.Title' }).",
    inputSchema: {
      code: z.string().describe("Code Python/Dynamo à exécuter"),
      dryRun: z.boolean().optional().describe("Mode simulation"),
      documentTitle: z.string().optional().describe("Document cible")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("dynamo/run-inline", args);;
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

// ─── Start ───────────────────────────────────────────────────────────────────


// ════════════════════════════════════════════════════════════════════════════
// NOUVEAUX OUTILS : FEEDBACK VISUEL, AUDIT PMR, MACRO-CONCEPTION & COPILOTE
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_screenshot_active_view",
  {
    description:
      "Capture visuelle instantanée de la vue active ou spécifiée dans Revit. Renvoie l'image encodée en Base64 et son chemin disque pour analyse multimodale directe par l'IA (Claude 3.5 Sonnet, GPT-4o). Exemple : revit_screenshot_active_view({ widthPixels: 1920, format: 'png' }).",
    inputSchema: {
      viewId: z.number().int().optional().describe("ID de la vue à capturer (défaut : vue active)"),
      widthPixels: z.number().int().optional().default(1920).describe("Largeur de l'image en pixels (320 à 4096, défaut : 1920)"),
      format: z.enum(["png", "jpeg"]).optional().default("png").describe("Format d'image ('png' ou 'jpeg')"),
      includeBase64: z.boolean().optional().default(false).describe("Inclure les données Base64 de l'image dans la réponse"),
      saveFilePath: z.string().optional().describe("Chemin cible optionnel sur le disque pour enregistrer l'image"),
      documentTitle: z.string().optional().describe("Document cible"),
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = (await revit("view/screenshot-active", args)) as any;
    if (data && data.imageBase64) {
      return {
        content: [
          {
            type: "image" as const,
            data: data.imageBase64 as string,
            mimeType: (data.mimeType as string) || "image/png",
          },
          {
            type: "text" as const,
            text: JSON.stringify(data, null, 2),
          },
        ],
      };
    }
    return {
      content: [
        {
          type: "text" as const,
          text: JSON.stringify(data, null, 2),
        },
      ],
    };
  }
);

server.registerTool(
  "revit_audit_pmr",
  {
    description:
      "Audit réglementaire automatisé d'accessibilité PMR (Handicap) en 1 clic. Vérifie la largeur utile de passage de toutes les portes (≥ 83 cm), l'aire de rotation libre de Ø1.50m dans les pièces d'eau et dégagements, et les pentes de rampes (< 5%). Surligne automatiquement en vert ce qui est conforme et en rouge les non-conformités directement dans la vue 3D ou plan de Revit. Exemple : revit_audit_pmr({ applyColorOverrides: true }).",
    inputSchema: {
      minDoorClearWidthMm: z.number().optional().default(830).describe("Largeur de passage libre minimale requise pour les portes en mm (défaut : 830)"),
      minTurningCircleDiameterMm: z.number().optional().default(1500).describe("Diamètre de l'aire de giration libre en mm (défaut : 1500)"),
      maxRampSlopePercent: z.number().optional().default(5).describe("Pente maximale autorisée pour les rampes en % (défaut : 5)"),
      applyColorOverrides: z.boolean().optional().default(true).describe("Colorer automatiquement la vue Revit (Vert = Conforme, Rouge = Non-conforme)"),
      viewId: z.number().int().optional().describe("ID de la vue où appliquer les couleurs (défaut : vue active)"),
      documentTitle: z.string().optional().describe("Document cible"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("audit/pmr", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_generate_spatial_layout",
  {
    description:
      "Macro-conception spatiale générative (Architecte Génératif). Conçoit un projet complet en 1 clic : résolution de l'organigramme fonctionnel, tracé de l'enveloppe thermique extérieure, dallage, cloisonnement intérieur, placement des pièces avec surfaces calculées, insertion des baies vitrées (optimisation RE2020 orientation Sud) et portes conformes PMR. Exemple : revit_generate_spatial_layout({ template: 'pavillon_t4_115m2', widthMm: 12500, depthMm: 9500 }).",
    inputSchema: {
      template: z.string().optional().default("pavillon_t4_115m2").describe("Gabarit architectural (ex: 'pavillon_t4_115m2')"),
      originX: z.number().optional().default(0).describe("Origine X en mm"),
      originY: z.number().optional().default(0).describe("Origine Y en mm"),
      widthMm: z.number().optional().default(12500).describe("Largeur globale en mm"),
      depthMm: z.number().optional().default(9500).describe("Profondeur globale en mm"),
      wallHeightMm: z.number().optional().default(2800).describe("Hauteur sous plafond en mm (défaut : 2800)"),
      levelName: z.string().optional().describe("Nom du niveau d'implantation"),
      includeFloor: z.boolean().optional().default(true).describe("Générer la dalle de sol"),
      includeDoorsAndWindows: z.boolean().optional().default(true).describe("Insérer automatiquement les menuiseries et baies vitrées"),
      dryRun: z.boolean().optional().describe("Mode simulation sans impact sur le document"),
      documentTitle: z.string().optional().describe("Document cible"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("generate/spatial-layout", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_proactive_alerts",
  {
    description:
      "Interroge en direct le copilote proactif en arrière-plan Revit. Renvoie les anomalies et clashs détectés pendant la modélisation (murs déplacés rompant la délimitation de pièces, séparateurs orphelins, dépassements de linteaux). Exemple : revit_get_proactive_alerts({ maxCount: 20 }).",
    inputSchema: {
      maxCount: z.number().int().optional().default(50).describe("Nombre maximal d'alertes à récupérer"),
      clearAfterRead: z.boolean().optional().default(false).describe("Purger les alertes après lecture"),
      documentTitle: z.string().optional().describe("Document cible"),
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("proactive/alerts", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_clear_proactive_alerts",
  {
    description:
      "Vide le journal des alertes proactives de Revit. Exemple : revit_clear_proactive_alerts({}).",
    inputSchema: {
      documentTitle: z.string().optional().describe("Document cible"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("proactive/clear", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_security_mode",
  {
    description: "Obtient le mode de sécurité actif de RevitMCP ('read-only', 'write', 'full') et les permissions d'accès au modèle.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("security/get", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_set_security_mode",
  {
    description: "Définit le mode de sécurité de RevitMCP : 'read-only' (bloque toute transaction modifiante), 'write' (standard), 'full' (étendu).",
    inputSchema: {
      mode: z.enum(["read-only", "write", "full"]).describe("Mode de sécurité désiré"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("security/set", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_cloud_info",
  {
    description: "Détecte si le document actif est hébergé sur Autodesk Construction Cloud (ACC) ou BIM 360, et extrait le Cloud Project GUID, Model GUID, chemin central et informations de sous-projets.",
    inputSchema: {
      documentTitle: z.string().optional().describe("Document cible"),
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("cloud/info", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_sync_with_central",
  {
    description: "Synchronise le modèle partagé (local ou cloud ACC/BIM 360) avec le fichier central (Synchronize with Central).",
    inputSchema: {
      comment: z.string().optional().describe("Commentaire de synchronisation"),
      compact: z.boolean().optional().default(false).describe("Compacter le fichier central"),
      saveLocalAfter: z.boolean().optional().default(true).describe("Enregistrer la copie locale après synchronisation"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("cloud/sync", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_create_snapshot",
  {
    description: "Crée un instantané (snapshot de sauvegarde / point de restauration) du document actif dans _outils/.snapshots/ avec horodatage et métadonnées avant d'effectuer des modifications lourdes.",
    inputSchema: {
      name: z.string().optional().describe("Nom ou étiquette de l'instantané"),
      description: z.string().optional().describe("Description de l'état ou raison de l'instantané"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("snapshots/create", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_list_snapshots",
  {
    description: "Liste l'ensemble des instantanés (snapshots de sauvegarde) créés pour le projet en cours.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("snapshots/list", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_bim_classification",
  {
    description: "Récupère les classifications BIM d'éléments Revit : code et description OmniClass, code d'assemblage Uniformat, Keynote et correspondance IFC fallback.",
    inputSchema: {
      elementId: z.number().int().optional().describe("Identifiant d'un élément Revit"),
      elementIds: z.array(z.number().int()).optional().describe("Liste d'identifiants d'éléments"),
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("bim/classification", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_get_diagnostics",
  {
    description: "Diagnostic complet de la session Revit : version/build exacte, utilisation mémoire du processus Revit (Mo), statistiques des avertissements (warnings groupés), état des liens CAO/Revit.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("diagnostics/info", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_fast_clash_detection",
  {
    description: "Détection de clashs géométriques ultra-rapide accélérée par le moteur multithreadé natif en Rust (Rayon).",
    inputSchema: {
      clearanceMm: z.number().optional().default(0).describe("Distance d'évitement ou tolérance en millimètres"),
      category: z.string().optional().describe("Filtrer par catégorie"),
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("geometry/clash", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_analyze_room_metrics",
  {
    description: "Analyse spatiale et bioclimatique haute performance calculée en Rust : surface Shoelace, compacité, ratio RE2020.",
    inputSchema: {
      vertices: z.array(z.array(z.number())).describe("Polygone 2D [X, Y] en millimètres"),
      glazedAreasM2: z.array(z.number()).optional().describe("Surfaces vitrées en m²"),
      windowOrientations: z.array(z.string()).optional().describe("Orientations cardinales"),
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("geometry/room-metrics", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_branch_create",
  {
    description: "Crée une branche d'exploration architecturale (Le Git du BIM) dans _outils/.snapshots/branches/ avec calcul immédiat des indicateurs clés.",
    inputSchema: {
      name: z.string().describe("Nom unique de la variante"),
      description: z.string().optional().describe("Description architecturale"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("branch/create", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_branch_list",
  {
    description: "Liste l'ensemble des branches d'alternatives architecturales (variantes IA) créées pour le projet en cours.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("branch/list", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_branch_compare",
  {
    description: "Compare deux branches d'alternatives architecturales (branchA et branchB) : deltas surfaces Loi Carrez, linéaires cloisons, PMR, coût et recommandation.",
    inputSchema: {
      branchA: z.string().describe("Nom de la première variante"),
      branchB: z.string().describe("Nom de la seconde variante"),
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("branch/compare", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);


// ════════════════════════════════════════════════════════════════════════════
// v2.5.0 SUPERPOWERS : DIRECTCONTEXT3D, SOLVER, THREE.JS, VOICE & RE2020 LCA
// ════════════════════════════════════════════════════════════════════════════

server.registerTool(
  "revit_overlay_ghost_preview",
  {
    description: "🔮 Affichage holographique 3D temps réel (DirectContext3D). Projette des murs et volumes fantômes semi-transparents colorés directement dans la fenêtre DirectX 3D de Revit sans modifier le modèle Revit.",
    inputSchema: {
      clearExisting: z.boolean().optional().describe("Effacer les projections fantômes précédentes (défaut: false)"),
      items: z.array(z.any()).optional().describe("Liste d'objets fantômes (boîtes, cloisons, agencements)"),
      wallStart: z.object({ x: z.number(), y: z.number(), z: z.number().optional() }).optional().describe("Départ mur mm"),
      wallEnd: z.object({ x: z.number(), y: z.number(), z: z.number().optional() }).optional().describe("Arrivée mur mm"),
      wallHeightMm: z.number().optional().describe("Hauteur mur mm (défaut 2800)"),
      wallThicknessMm: z.number().optional().describe("Épaisseur mur mm (défaut 200)")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("overlay/ghost-preview", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_overlay_clash_heatmap",
  {
    description: "🔴 Heatmap 3D en surimpression holographique (HUD Augmenté). Projette des volumes colorés directement sur les éléments en collision sans altérer les matériaux.",
    inputSchema: {
      clearExisting: z.boolean().optional().describe("Effacer les heatmaps précédentes"),
      clashes: z.array(z.any()).optional().describe("Résultats de clashs"),
      elementIds: z.array(z.number()).optional().describe("IDs Revit à colorer"),
      r: z.number().optional(),
      g: z.number().optional(),
      b: z.number().optional(),
      alpha: z.number().optional()
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("overlay/clash-heatmap", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_overlay_clear",
  {
    description: "🧹 Efface instantanément toutes les projections holographiques DirectContext3D et heatmaps 3D de la vue Revit.",
    inputSchema: {},
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("overlay/clear", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_overlay_get_status",
  {
    description: "Inspecte l'état du serveur d'hologrammes DirectContext3D et l'inventaire des objets fantômes actifs.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("overlay/status", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_solve_parametric_layout",
  {
    description: "🧠 Moteur d'optimisation mathématique en Rust (Auto-Layout Solver). Évalue 100 000 agencements spatiaux en moins de 50 ms pour disposer postes de travail, tables et rangements en maximisant la lumière naturelle et garantissant 1,50 m de passage PMR.",
    inputSchema: {
      roomWidthMm: z.number().optional().describe("Largeur de pièce mm"),
      roomLengthMm: z.number().optional().describe("Longueur de pièce mm"),
      minClearanceMm: z.number().optional().describe("Dégagement PMR mm (défaut 1500)"),
      workstations: z.number().optional().describe("Postes de travail (défaut 6)"),
      meetingTables: z.number().optional().describe("Tables de réunion (défaut 1)"),
      storageUnits: z.number().optional().describe("Armoires (défaut 2)"),
      iterations: z.number().optional().describe("Nombre d'itérations (défaut 100000)"),
      autoGhostPreview: z.boolean().optional().describe("Projeter en hologramme Revit (défaut true)")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("layout/solve-parametric", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_export_interactive_3d_viewer",
  {
    description: "🌐 Exporte la vue sous forme d'un visualiseur Web interactif Three.js autonome (Generative UI) avec OrbitControls et inspection des paramètres au clic.",
    inputSchema: {
      viewId: z.number().optional().describe("ID de la vue"),
      maxElements: z.number().optional().describe("Nombre maximal d'éléments"),
      outputDir: z.string().optional().describe("Dossier personnalisé")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("view/export-interactive-3d", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_execute_voice_command",
  {
    description: "🎙️ Contrôle vocal temps réel (Speech-to-BIM). Exécute instantanément des ordres oraux sur Revit en moins de 300 ms sans toucher au clavier.",
    inputSchema: {
      command: z.string().describe("Phrase orale en français ou anglais")
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
  },
  async (args) => {
    const data = await revit("voice/execute-command", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_calculate_lca_carbon_footprint",
  {
    description: "🍃 Bilan carbone ACV & Analyse de Cycle de Vie en direct (RE2020 / Base INIES). Calcule l'indice carbone IC_construction du projet en kgCO2eq/m2 SDP et le stockage biogénique.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("lca/carbon-footprint", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_suggest_low_carbon_alternatives",
  {
    description: "🌱 Détecte les points chauds d'émissions carbone et calcule les gains réels en tonnes de CO2 économisées (-40% à -75%) pour des alternatives biosourcées INIES.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("lca/low-carbon-alternatives", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

server.registerTool(
  "revit_inies_database_lookup",
  {
    description: "🔍 Recherche et consulte les données environnementales officielles FDES de la base française INIES.",
    inputSchema: {
      query: z.string().optional().describe("Matériau recherché"),
      category: z.string().optional().describe("Catégorie")
    },
    annotations: { readOnlyHint: true },
  },
  async (args) => {
    const data = await revit("lca/inies-lookup", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
  }
);

const transport = new StdioServerTransport();

await server.connect(transport);
