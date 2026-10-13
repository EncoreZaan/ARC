import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { resolveRevitBridgeConfig, revit, revitHealth } from "./bridge.js";
const bridgeConfig = resolveRevitBridgeConfig();
// ─── Server ──────────────────────────────────────────────────────────────────
const server = new McpServer({
    name: "revit-claude-mcp",
    version: "2.0.0",
});
// ════════════════════════════════════════════════════════════════════════════
// CONNECTION / DIAGNOSTIC TOOLS
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_health_check", {
    description: "Check whether the local RevitMCP addin bridge is reachable before running model read/write tools. Returns bridge host, timeout, and addin /healthz response.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revitHealth(bridgeConfig);
    return {
        content: [
            {
                type: "text",
                text: JSON.stringify({ host: bridgeConfig.host, timeoutMs: bridgeConfig.timeoutMs, health: data }, null, 2),
            },
        ],
    };
});
// ════════════════════════════════════════════════════════════════════════════
// READ / QUERY TOOLS
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_get_model_info", {
    description: "Returns basic information about the currently open Revit document: file path, project name, number, author, phases, and active view.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("model/info");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_elements", {
    description: "Query elements in the Revit model. Filter by category (e.g. 'Walls', 'Doors', 'Floors'), family name, level name, or view name. Returns element IDs, types, parameters and host info.",
    inputSchema: {
        category: z.string().optional().describe("Revit category name, e.g. 'Walls', 'Doors', 'Windows', 'Floors', 'Rooms'"),
        familyName: z.string().optional().describe("Filter by family name"),
        levelName: z.string().optional().describe("Filter by level name, e.g. 'Level 1'"),
        viewName: z.string().optional().describe("Filter elements visible in this view"),
        limit: z.number().int().min(1).max(500).default(100).describe("Max elements to return"),
    },
    annotations: { readOnlyHint: true },
}, async ({ category, familyName, levelName, viewName, limit }) => {
    const data = await revit("elements/query", { category, familyName, levelName, viewName, limit });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_element_parameters", {
    description: "Get all parameters (built-in and shared) for a specific element by its integer Element ID.",
    inputSchema: {
        elementId: z.number().int().describe("The Revit element ID (integer)"),
    },
    annotations: { readOnlyHint: true },
}, async ({ elementId }) => {
    const data = await revit("elements/parameters", { elementId });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_views", {
    description: "List all views in the model. Optionally filter by view type: FloorPlan, CeilingPlan, Elevation, Section, 3D, DraftingView, Schedule, etc.",
    inputSchema: {
        viewType: z.string().optional().describe("Filter by Revit ViewType enum name, e.g. 'FloorPlan', '3D', 'Section'"),
    },
    annotations: { readOnlyHint: true },
}, async ({ viewType }) => {
    const data = await revit("views/list", { viewType });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_sheets", {
    description: "List all sheets in the project with their sheet number, name, and which views are placed on each sheet.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("sheets/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_levels", {
    description: "List all levels in the project with their name, elevation, and ID.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("levels/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_families", {
    description: "List all loaded families and their types/symbols for a given category.",
    inputSchema: {
        category: z.string().optional().describe("Category to filter, e.g. 'Doors', 'Windows'"),
    },
    annotations: { readOnlyHint: true },
}, async ({ category }) => {
    const data = await revit("families/list", { category });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_rooms", {
    description: "List all rooms in the project with number, name, area, level, and bounding-box coordinates.",
    inputSchema: {
        levelName: z.string().optional().describe("Filter rooms by level name"),
    },
    annotations: { readOnlyHint: true },
}, async ({ levelName }) => {
    const data = await revit("rooms/list", { levelName });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// CREATE / MODIFY TOOLS
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_create_wall", {
    description: "Create a straight wall in the Revit model. Coordinates are in millimetres (model units). Returns the new element ID.",
    inputSchema: {
        startX: z.number().describe("Start point X in mm"),
        startY: z.number().describe("Start point Y in mm"),
        endX: z.number().describe("End point X in mm"),
        endY: z.number().describe("End point Y in mm"),
        levelName: z.string().describe("Name of the level to host the wall, e.g. 'Level 1'"),
        wallTypeName: z.string().optional().describe("Wall type name; omit to use the project default"),
        height: z.number().optional().describe("Unconnected height in mm; defaults to level-to-level"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("elements/create-wall", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_place_family_instance", {
    description: "Place a family instance (door, window, furniture, equipment, etc.) at a given point. For hosted families (doors/windows), provide the host wall element ID.",
    inputSchema: {
        familyName: z.string().describe("Family name, e.g. 'Single-Flush'"),
        typeName: z.string().describe("Type name within the family, e.g. '900 x 2100mm'"),
        x: z.number().describe("Insertion X coordinate in mm"),
        y: z.number().describe("Insertion Y coordinate in mm"),
        z: z.number().optional().describe("Insertion Z coordinate in mm; defaults to level elevation"),
        levelName: z.string().describe("Level name for the instance"),
        hostElementId: z.number().int().optional().describe("Element ID of the host wall/floor/ceiling (required for hosted families)"),
        rotation: z.number().optional().describe("Rotation angle in degrees around Z axis"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("elements/place-instance", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_set_parameter", {
    description: "Set a parameter value on a Revit element. Supports text, integer, double, and Yes/No parameters.",
    inputSchema: {
        elementId: z.number().int().describe("Element ID to modify"),
        parameterName: z.string().describe("Parameter name (built-in name or shared parameter name)"),
        value: z.union([z.string(), z.number(), z.boolean()]).describe("Value to set"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false, idempotentHint: true },
}, async (args) => {
    const data = await revit("elements/set-parameter", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_delete_element", {
    description: "Delete an element from the model by its element ID. This operation is permanent (but undoable inside Revit with Ctrl+Z).",
    inputSchema: {
        elementId: z.number().int().describe("Element ID to delete"),
    },
    annotations: { readOnlyHint: false, destructiveHint: true },
}, async ({ elementId }) => {
    const data = await revit("elements/delete", { elementId });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_move_element", {
    description: "Move an element by a translation vector in mm.",
    inputSchema: {
        elementId: z.number().int().describe("Element ID to move"),
        deltaX: z.number().describe("X displacement in mm"),
        deltaY: z.number().describe("Y displacement in mm"),
        deltaZ: z.number().optional().default(0).describe("Z displacement in mm"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("elements/move", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// SHEETS & VIEWS MANAGEMENT
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_create_sheet", {
    description: "Create a new sheet in the project with a given sheet number, name, and title block family.",
    inputSchema: {
        sheetNumber: z.string().describe("Sheet number, e.g. 'A-101'"),
        sheetName: z.string().describe("Sheet name, e.g. 'Ground Floor Plan'"),
        titleBlockName: z.string().optional().describe("Title block family type name; omit to use project default"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("sheets/create", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_place_view_on_sheet", {
    description: "Place an existing view onto a sheet at the given viewport centre coordinates (in mm on the sheet).",
    inputSchema: {
        sheetId: z.number().int().describe("Sheet element ID"),
        viewId: z.number().int().describe("View element ID to place"),
        centerX: z.number().describe("Viewport centre X on sheet in mm"),
        centerY: z.number().describe("Viewport centre Y on sheet in mm"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("sheets/place-view", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_activate_view", {
    description: "Set the active view in the Revit UI to the specified view by its element ID or name.",
    inputSchema: {
        viewId: z.number().int().optional().describe("View element ID"),
        viewName: z.string().optional().describe("View name (used if viewId not provided)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("views/activate", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// EXPORT TOOLS
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_export_pdf", {
    description: "Export one or more sheets or views to PDF. Returns the output file path.",
    inputSchema: {
        outputPath: z.string().describe("Full folder path where PDFs will be saved, e.g. 'C:\\\\Exports\\\\PDFs'"),
        sheetIds: z.array(z.number().int()).optional().describe("Sheet element IDs to export; omit for all sheets"),
        viewIds: z.array(z.number().int()).optional().describe("View element IDs to export (non-sheet views)"),
        combineIntoOne: z.boolean().optional().default(false).describe("Combine all pages into a single PDF file"),
        paperSize: z.string().optional().default("A1").describe("Paper size: A0, A1, A2, A3, A4"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("export/pdf", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_export_dwg", {
    description: "Export views or sheets to DWG/DXF format. Returns output file paths.",
    inputSchema: {
        outputPath: z.string().describe("Full folder path for DWG files"),
        viewIds: z.array(z.number().int()).optional().describe("View/sheet element IDs; omit for active view"),
        format: z.enum(["DWG", "DXF"]).optional().default("DWG"),
        exportSetupName: z.string().optional().describe("Name of saved DWG export settings; omit for default"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("export/dwg", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_export_ifc", {
    description: "Export the model or a subset of elements to IFC format.",
    inputSchema: {
        outputPath: z.string().describe("Full file path for the IFC output, e.g. 'C:\\\\Exports\\\\model.ifc'"),
        ifcVersion: z.enum(["IFC2x3", "IFC4", "IFC4x3"]).optional().default("IFC4").describe("IFC schema version"),
        exportBaseQuantities: z.boolean().optional().default(true),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("export/ifc", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_export_schedule", {
    description: "Export a schedule view to CSV or TXT.",
    inputSchema: {
        scheduleViewId: z.number().int().describe("Element ID of the Schedule view"),
        outputPath: z.string().describe("Full file path, e.g. 'C:\\\\Exports\\\\schedule.csv'"),
        delimiter: z.enum(["comma", "tab", "semicolon"]).optional().default("comma"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("export/schedule", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// MACRO / COMMAND RUNNER
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_run_command", {
    description: "Execute a built-in Revit command by its PostableCommandId name (e.g. 'ID_REVIT_THIN_LINES', 'ID_VIEW_GRAPHIC_DISPLAY_OPTIONS'). Use for UI commands not exposed via other tools.",
    inputSchema: {
        commandId: z.string().describe("PostableCommandId string, e.g. 'ID_REVIT_THIN_LINES'"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async ({ commandId }) => {
    const data = await revit("commands/run", { commandId });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_run_dynamo_script", {
    description: "Run a Dynamo script (.dyn file) on the current Revit model. Returns stdout/stderr from Dynamo.",
    inputSchema: {
        scriptPath: z.string().describe("Absolute path to the .dyn file, e.g. 'C:\\\\Scripts\\\\myscript.dyn'"),
        journalData: z.record(z.string(), z.string()).optional().describe("Key-value pairs injected into the Dynamo journal"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("commands/run-dynamo", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// MATERIAL TAKEOFF
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_get_material_takeoff", {
    description: "Generate a full material takeoff for the project. Collects all structural/architectural elements, reads their material layers, area, volume, and count — grouped and summed per material. Optionally filter by category or level. Returns a ready-to-use table. Much faster than querying elements one by one.",
    inputSchema: {
        categories: z.array(z.string()).optional().describe("Categories to include, e.g. ['Walls','Floors','Roofs']. Omit for all."),
        levelName: z.string().optional().describe("Filter by level name"),
        includeLayerBreakdown: z.boolean().optional().default(true).describe("If true, break down compound walls/floors by their individual material layers"),
    },
    annotations: { readOnlyHint: true },
}, async (args) => {
    const data = await revit("takeoff/materials", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_export_material_takeoff_csv", {
    description: "Run a material takeoff and save the result directly to a CSV file. Columns: Material, Category, TotalAreaM2, TotalVolM3, ElementCount, Unit. Ready to open in Excel.",
    inputSchema: {
        outputPath: z.string().describe("Full CSV file path, e.g. 'C:\\\\Exports\\\\takeoff.csv'"),
        categories: z.array(z.string()).optional().describe("Categories to include; omit for all"),
        levelName: z.string().optional().describe("Filter by level"),
        includeLayerBreakdown: z.boolean().optional().default(true),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("takeoff/export-csv", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_create_material_schedule", {
    description: "Create a proper Revit Material Takeoff schedule view inside the project for a given category (e.g. 'Walls'). The schedule will appear in the Project Browser under Schedules and can be placed on sheets.",
    inputSchema: {
        category: z.string().describe("Category to schedule, e.g. 'Walls', 'Floors', 'Roofs'"),
        scheduleName: z.string().optional().describe("Name for the new schedule view; defaults to '<Category> Material Takeoff'"),
        fields: z.array(z.string()).optional().describe("Parameter names to include as columns. Defaults to: ['Material: Name','Material: Area','Material: Volume','Count']"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("takeoff/create-schedule", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// CLASH DETECTION
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_detect_clashes", {
    description: "Detect geometric clashes (intersections) between two sets of elements. Typical use: check structural columns vs MEP ducts, or walls vs beams. Returns clash pairs with element IDs, categories, locations, and severity. Saves hours of manual coordination.",
    inputSchema: {
        categoryA: z.string().describe("First category, e.g. 'Walls'"),
        categoryB: z.string().describe("Second category to check against, e.g. 'Columns'"),
        levelName: z.string().optional().describe("Limit clash check to a specific level"),
        tolerance: z.number().optional().default(0).describe("Overlap tolerance in mm — clashes smaller than this are ignored"),
    },
    annotations: { readOnlyHint: true },
}, async (args) => {
    const data = await revit("clash/detect", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_export_clash_report", {
    description: "Run clash detection and export a detailed HTML or CSV report with clash pairs, element info, and 3D coordinates. One command replaces hours of manual coordination review.",
    inputSchema: {
        outputPath: z.string().describe("Full file path, e.g. 'C:\\\\Reports\\\\clash-report.html'"),
        categoryA: z.string().describe("First category"),
        categoryB: z.string().describe("Second category"),
        format: z.enum(["html", "csv"]).optional().default("html"),
        tolerance: z.number().optional().default(0),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("clash/export-report", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// DRAWING ISSUE / REVISION AUTOMATION
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_issue_drawings", {
    description: "Automate a full drawing issue: add a revision to the project, stamp selected sheets with that revision, update the issue date parameter on all sheets, and optionally export to PDF. Replaces 30–60 minutes of manual sheet management on deadline day.",
    inputSchema: {
        revisionDescription: z.string().describe("Revision description, e.g. 'Issued for Construction'"),
        revisionDate: z.string().describe("Issue date as string, e.g. '2026-05-15'"),
        revisionNumber: z.string().optional().describe("Revision number/letter, e.g. 'B' or '02'"),
        sheetIds: z.array(z.number().int()).optional().describe("Sheet element IDs to stamp; omit for all sheets"),
        exportPdfPath: z.string().optional().describe("If provided, export issued sheets to PDF at this folder path"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("revisions/issue-drawings", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_revision_history", {
    description: "List all revisions in the project with sequence, number, description, date, and which sheets carry each revision.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("revisions/history");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// ROOM FINISH SCHEDULE
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_get_room_finishes", {
    description: "Read the finish parameters (Floor Finish, Wall Finish, Ceiling Finish, Base Finish) for all rooms. Returns a complete room finish matrix grouped by level — ready to verify or export.",
    inputSchema: {
        levelName: z.string().optional().describe("Filter by level"),
    },
    annotations: { readOnlyHint: true },
}, async (args) => {
    const data = await revit("rooms/finishes", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_set_room_finishes_bulk", {
    description: "Set finish parameters on multiple rooms in one call. Pass an array of room updates with room ID and the finish values to apply. Replaces clicking through each room one by one — saves hours on large projects.",
    inputSchema: {
        updates: z.array(z.object({
            roomId: z.number().int().describe("Room element ID"),
            floorFinish: z.string().optional().describe("Floor Finish parameter value"),
            wallFinish: z.string().optional().describe("Wall Finish parameter value"),
            ceilingFinish: z.string().optional().describe("Ceiling Finish parameter value"),
            baseFinish: z.string().optional().describe("Base Finish parameter value"),
            comments: z.string().optional().describe("Comments parameter value"),
        })).describe("Array of room finish updates"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("rooms/set-finishes-bulk", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// VIEW TEMPLATE & BULK VIEW MANAGEMENT
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_apply_view_template_bulk", {
    description: "Apply a view template to multiple views in one operation. Filter target views by type, name pattern, or level. Replaces clicking through dozens of views manually — a 5-minute job becomes one command.",
    inputSchema: {
        templateName: z.string().describe("Name of the view template to apply"),
        viewIds: z.array(z.number().int()).optional().describe("Specific view IDs to update; if omitted, use filters below"),
        viewType: z.string().optional().describe("Apply only to views of this type, e.g. 'FloorPlan'"),
        nameContains: z.string().optional().describe("Apply only to views whose name contains this string"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("views/apply-template-bulk", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_bulk_rename_views", {
    description: "Rename multiple views or sheets using find-and-replace or a prefix/suffix pattern. Useful for standardising naming conventions across the whole project at once.",
    inputSchema: {
        targets: z.enum(["views", "sheets", "both"]).default("views"),
        findText: z.string().optional().describe("Text to find in existing names"),
        replaceText: z.string().optional().describe("Text to replace with"),
        addPrefix: z.string().optional().describe("Prefix to prepend to all matching names"),
        addSuffix: z.string().optional().describe("Suffix to append to all matching names"),
        nameContains: z.string().optional().describe("Only rename views/sheets whose name contains this string"),
        dryRun: z.boolean().optional().default(false).describe("If true, return what would be renamed without making changes"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("views/bulk-rename", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// FILE HEALTH / AUDIT
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_audit_families", {
    description: "Audit all loaded families in the project. Reports: total family count, families not used in the model (candidates for purging), families with large instance counts, and non-standard families (not from the office library path). Helps reduce file bloat.",
    inputSchema: {
        libraryPath: z.string().optional().describe("Office library root path to identify in-house vs foreign families, e.g. 'C:\\\\RevitLibrary'"),
    },
    annotations: { readOnlyHint: true },
}, async (args) => {
    const data = await revit("audit/families", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_import_parameters_from_csv", {
    description: "Bulk-import parameter values from a CSV file onto elements. CSV format: first column = ElementId, remaining columns = parameter name/value pairs. Replaces manual data entry for large datasets — e.g. populating asset tags, fire ratings, or specifications from a spreadsheet.",
    inputSchema: {
        csvPath: z.string().describe("Full path to the CSV file, e.g. 'C:\\\\Data\\\\params.csv'"),
        delimiter: z.enum(["comma", "tab", "semicolon"]).optional().default("comma"),
        dryRun: z.boolean().optional().default(false).describe("If true, validate the CSV and report what would change without writing to the model"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("elements/import-csv-params", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// AUTO SHEET POPULATION
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_batch_set_parameters", {
    description: "Set one or more parameters on many matching elements at once. Use dryRun first to preview. Saves repetitive property edits across rooms, doors, windows, equipment, sheets, or any filtered category.",
    inputSchema: {
        parameterValues: z.record(z.string(), z.union([z.string(), z.number(), z.boolean()])).describe("Parameter name/value pairs to set, e.g. { Comments: 'Checked', Mark: 'A-01' }"),
        category: z.string().optional().describe("Revit category name, e.g. 'Rooms', 'Doors', 'Windows', 'Mechanical Equipment'"),
        familyName: z.string().optional().describe("Only match FamilyInstance elements from this family"),
        levelName: z.string().optional().describe("Only match elements hosted on this level"),
        viewName: z.string().optional().describe("Only match elements visible in this view"),
        nameContains: z.string().optional().describe("Only match elements whose name contains this text"),
        limit: z.number().int().min(1).max(5000).optional().default(1000).describe("Safety cap for matched elements"),
        dryRun: z.boolean().optional().default(true).describe("Preview matched elements without writing. Set false to apply."),
    },
    annotations: { readOnlyHint: false, destructiveHint: false, idempotentHint: true },
}, async (args) => {
    const data = await revit("elements/batch-set-parameters", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_create_plan_views_for_levels", {
    description: "Create floor, ceiling, or structural plan views for multiple levels using a naming template and optional view template. This turns setup of drawing views from repetitive clicking into one dry-run/apply flow.",
    inputSchema: {
        levelNames: z.array(z.string()).optional().describe("Specific level names. Omit for all levels."),
        viewFamily: z.enum(["FloorPlan", "CeilingPlan", "StructuralPlan"]).optional().default("FloorPlan"),
        nameTemplate: z.string().optional().default("{levelName} - {viewFamily}").describe("Use {levelName} and {viewFamily} placeholders"),
        viewTemplateName: z.string().optional().describe("Optional Revit view template to apply"),
        skipExisting: z.boolean().optional().default(true).describe("Skip views whose target name already exists"),
        dryRun: z.boolean().optional().default(true).describe("Preview the views that would be created. Set false to apply."),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("views/create-plans-for-levels", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_duplicate_views_bulk", {
    description: "Duplicate many matching views with detailing, as dependent views, or plain duplicates, then optionally apply a template. Useful for creating permit/construction/coordination drawing sets fast.",
    inputSchema: {
        viewIds: z.array(z.number().int()).optional().describe("Specific source view IDs. Omit to use filters."),
        viewType: z.string().optional().describe("Filter by Revit ViewType enum, e.g. 'FloorPlan', 'CeilingPlan', 'Section'"),
        nameContains: z.string().optional().describe("Only duplicate source views whose name contains this text"),
        duplicateOption: z.enum(["WithDetailing", "Duplicate", "AsDependent"]).optional().default("WithDetailing"),
        nameTemplate: z.string().optional().default("{viewName} - Copy").describe("Use {viewName} and {index} placeholders"),
        viewTemplateName: z.string().optional().describe("Optional view template to apply to duplicated views"),
        dryRun: z.boolean().optional().default(true).describe("Preview duplicated views without writing. Set false to apply."),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("views/duplicate-bulk", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_auto_populate_sheets", {
    description: "Automatically create sheets and place matching views based on a naming convention. For example: for every floor plan view named 'FP - Level N', create sheet 'A-1N0 - Level N Floor Plan' and place the view on it. Handles an entire drawing set in seconds instead of hours.",
    inputSchema: {
        rules: z.array(z.object({
            viewNamePattern: z.string().describe("Substring or prefix that identifies matching views, e.g. 'Floor Plan - Level'"),
            sheetNumberPrefix: z.string().describe("Sheet number prefix, e.g. 'A-1'"),
            sheetNameTemplate: z.string().describe("Sheet name template; use {viewName} as placeholder, e.g. '{viewName} Plan'"),
            titleBlockName: z.string().optional().describe("Title block to use; omit for project default"),
        })).describe("Matching rules — each rule pairs view name patterns to sheet naming"),
        skipExistingSheets: z.boolean().optional().default(true).describe("Skip if a sheet with the same number already exists"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("sheets/auto-populate", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// SELECTION & INTERACTIVE TOOLS
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_get_current_selection", {
    description: "Get currently selected elements in the active Revit document. Returns element IDs, unique IDs, names, and categories.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("selection/get");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_set_current_selection", {
    description: "Select specific elements in the active Revit view by their integer element IDs.",
    inputSchema: {
        elementIds: z.array(z.number().int()).describe("Array of Revit element IDs to select"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async ({ elementIds }) => {
    const data = await revit("selection/set", { elementIds });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_isolate_elements", {
    description: "Temporarily isolate specific elements in the current active Revit view (hides all other elements).",
    inputSchema: {
        elementIds: z.array(z.number().int()).describe("Array of element IDs to isolate"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async ({ elementIds }) => {
    const data = await revit("elements/isolate", { elementIds });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_reset_temporary_override", {
    description: "Reset temporary hide/isolate mode in the active Revit view, restoring full element visibility.",
    inputSchema: {},
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async () => {
    const data = await revit("views/reset-temporary-override");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// CATEGORIES & INSPECTION TOOLS
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_list_categories", {
    description: "List all available BIM categories in the active Revit project (model categories and bound categories).",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("categories/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_count_elements", {
    description: "Count elements in the active Revit document, optionally filtered by category (e.g. 'Walls', 'Doors', 'Windows').",
    inputSchema: {
        category: z.string().optional().describe("Revit category name, e.g. 'Walls', 'Doors', 'Rooms'"),
    },
    annotations: { readOnlyHint: true },
}, async ({ category }) => {
    const data = await revit("elements/count", { category });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_model_warnings", {
    description: "Retrieve all active warnings and failure messages in the Revit project (e.g. overlapping walls, unclosed room boundaries).",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("model/warnings");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ════════════════════════════════════════════════════════════════════════════
// MODEL CREATION & VALIDATION TOOLS
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_create_floor", {
    description: "Create a rectangular floor element on a given level with specified dimensions in millimeters.",
    inputSchema: {
        levelId: z.number().int().describe("Revit ElementId of the target level"),
        widthMm: z.number().positive().default(5000).describe("Width in millimeters along X axis"),
        lengthMm: z.number().positive().default(5000).describe("Length in millimeters along Y axis"),
        originX: z.number().default(0).describe("Origin X coordinate in millimeters"),
        originY: z.number().default(0).describe("Origin Y coordinate in millimeters"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("elements/create-floor", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_create_room", {
    description: "Create and place a Revit room on a specific level at coordinates (x, y) with custom name and number.",
    inputSchema: {
        levelId: z.number().int().describe("Target level ElementId"),
        xMm: z.number().describe("X coordinate in millimeters"),
        yMm: z.number().describe("Y coordinate in millimeters"),
        name: z.string().optional().describe("Room name, e.g. 'Living Room'"),
        number: z.string().optional().describe("Room number, e.g. '101'"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("rooms/create", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_validate_model_changes", {
    description: "Validate model state by checking for warnings, verifying critical elements, and reporting active document health.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
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
});
// ════════════════════════════════════════════════════════════════════════════
// EXPANDED BIM & VIEW WORKFLOW TOOLS
// ════════════════════════════════════════════════════════════════════════════
server.registerTool("revit_zoom_to_elements", {
    description: "Zoom and pan the active Revit view to frame specific elements by their IDs so the user can inspect them visually.",
    inputSchema: {
        elementIds: z.array(z.number().int()).describe("Array of element IDs to frame"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async ({ elementIds }) => {
    const data = await revit("views/zoom-elements", { elementIds });
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_color_elements", {
    description: "Highlight and color specific elements in the active Revit view using RGB color overrides (Color Splash).",
    inputSchema: {
        elementIds: z.array(z.number().int()).describe("Array of element IDs to override with color"),
        red: z.number().int().min(0).max(255).default(255).describe("Red channel (0-255)"),
        green: z.number().int().min(0).max(255).default(0).describe("Green channel (0-255)"),
        blue: z.number().int().min(0).max(255).default(0).describe("Blue channel (0-255)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("elements/set-color", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_reset_color_overrides", {
    description: "Reset graphics and color overrides for elements in the active Revit view.",
    inputSchema: {
        elementIds: z.array(z.number().int()).optional().describe("Specific element IDs to reset, or omit to reset active view"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("elements/reset-color", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_create_grid", {
    description: "Create a datum grid line between two points with smart alphabetic or numeric naming.",
    inputSchema: {
        startX: z.number().default(0).describe("Start X in mm"),
        startY: z.number().default(0).describe("Start Y in mm"),
        endX: z.number().default(10000).describe("End X in mm"),
        endY: z.number().default(0).describe("End Y in mm"),
        name: z.string().optional().describe("Optional grid name (e.g. 'A', '1')"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("grids/create", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_phases", {
    description: "List project phases (e.g. Existing, Demolition, New Construction).",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("phases/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_links", {
    description: "List all linked Revit models (.rvt) and CAD links/imports (.dwg, .ifc) with their loaded state.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("links/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_rotate_element", {
    description: "Rotate an element around its vertical central axis by a specified angle in degrees.",
    inputSchema: {
        elementId: z.number().int().describe("Element ID to rotate"),
        angleDegrees: z.number().default(90).describe("Rotation angle in degrees"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("elements/rotate", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_export_view_image", {
    description: "Export a rendered or shaded image preview (PNG) of the active view or a sheet to disk for visual verification.",
    inputSchema: {
        viewId: z.number().int().optional().describe("Optional view ID, omit for active view"),
        filePath: z.string().optional().describe("Destination PNG file path on disk"),
        pixelSize: z.number().int().default(1920).describe("Image width in pixels"),
    },
    annotations: { readOnlyHint: true },
}, async (args) => {
    const data = await revit("export/image", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_save_document", {
    description: "Save the currently active Revit document or save a backup copy under a new file path.",
    inputSchema: {
        filePath: z.string().optional().describe("Optional path for Save As; omit to save in place"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("model/save", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_summary_report", {
    description: "Get a consolidated high-level summary of the entire Revit project (document info, levels, views count, sheets count, warnings, elements count).",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
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
                    totalViews: views?.views?.length ?? 0,
                    totalSheets: sheets?.sheets?.length ?? 0,
                    warningsSummary: warnings,
                    totalElements: count?.count ?? 0
                }, null, 2),
            },
        ],
    };
});
server.registerTool("revit_get_worksets", {
    description: "List user worksets in workshared projects (or indicates that the project is not workshared).",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("worksets/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_revisions", {
    description: "List all revisions and drawing issue sequences defined in the Revit project.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("revisions/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_materials", {
    description: "List materials defined in the Revit project with class, RGB colors, transparency and surface properties.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("materials/list");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_view_templates", {
    description: "List all View Templates available in the Revit project with view type and names.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("views/list-templates");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_create_section_box", {
    description: "Create or adjust a 3D isometric view section box tightly framing the specified elements or current selection with padding.",
    inputSchema: {
        elementIds: z.array(z.number()).optional().describe("Element IDs to frame; if omitted, uses current selection"),
        paddingMm: z.number().optional().describe("Margin around the bounding box in millimeters (default: 300 mm)"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("views/create-section-box", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_find_elements_by_parameter", {
    description: "Search for elements across the model matching a given parameter name and text string value.",
    inputSchema: {
        parameterName: z.string().describe("Parameter name to inspect (e.g. 'Comments', 'Mark', 'Manufacturer')"),
        searchValue: z.string().describe("String value or substring to look for"),
        exactMatch: z.boolean().optional().describe("True for exact match, false for substring matching (default: false)"),
        limit: z.number().optional().describe("Maximum elements to return (default: 100)"),
    },
    annotations: { readOnlyHint: true },
}, async (args) => {
    const data = await revit("elements/find-by-parameter", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_copy_parameter_values", {
    description: "Batch copy parameter values from a source parameter to a target parameter across elements.",
    inputSchema: {
        sourceParameter: z.string().describe("Source parameter name to read value from"),
        targetParameter: z.string().describe("Target parameter name to write value to"),
        elementIds: z.array(z.number()).optional().describe("Element IDs; if omitted, attempts copy across all model elements"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("parameters/copy-values", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_duplicate_sheet", {
    description: "Duplicate an existing drawing sheet with its title block and assign a new number and name.",
    inputSchema: {
        sheetId: z.number().describe("ID of the sheet to duplicate"),
        sheetNumber: z.string().optional().describe("New sheet number"),
        sheetName: z.string().optional().describe("New sheet name"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("sheets/duplicate", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_get_unused_types", {
    description: "Model hygiene audit: identify family symbols and types that have 0 instances placed in the project.",
    inputSchema: {},
    annotations: { readOnlyHint: true },
}, async () => {
    const data = await revit("model/purge-unused");
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
server.registerTool("revit_create_detail_line", {
    description: "Draw a 2D detail line in the active 2D view (floor plan, section, elevation, drafting view).",
    inputSchema: {
        startX: z.number().describe("Start X coordinate in millimeters"),
        startY: z.number().describe("Start Y coordinate in millimeters"),
        endX: z.number().describe("End X coordinate in millimeters"),
        endY: z.number().describe("End Y coordinate in millimeters"),
    },
    annotations: { readOnlyHint: false, destructiveHint: false },
}, async (args) => {
    const data = await revit("annotations/create-detail-line", args);
    return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
});
// ─── Start ───────────────────────────────────────────────────────────────────
const transport = new StdioServerTransport();
await server.connect(transport);
//# sourceMappingURL=index.js.map