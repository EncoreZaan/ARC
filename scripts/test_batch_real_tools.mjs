import { revit } from "../mcp-server/dist/bridge.js";

async function testBatchRealTools() {
  const info = await revit("model/info");
  console.log("Active Document:", info.filePath);
  if (!info.filePath.includes("MCP TEST")) {
    throw new Error("ABORT: Not test copy!");
  }

  const results = {};

  // 1. Zoom to test wall
  try {
    const zoomRes = await revit("views/zoom-elements", { elementIds: [264388] });
    results.zoom = { success: true, data: zoomRes };
    console.log("1. views/zoom-elements: OK", zoomRes);
  } catch (e) {
    results.zoom = { success: false, error: e.message };
  }

  // 2. Color test wall red
  try {
    const colorRes = await revit("elements/set-color", { elementIds: [264388], red: 255, green: 0, blue: 0 });
    results.color = { success: true, data: colorRes };
    console.log("2. elements/set-color: OK", colorRes);
  } catch (e) {
    results.color = { success: false, error: e.message };
  }

  // 3. Reset color overrides
  try {
    const resetRes = await revit("elements/reset-color", { elementIds: [264388] });
    results.resetColor = { success: true, data: resetRes };
    console.log("3. elements/reset-color: OK", resetRes);
  } catch (e) {
    results.resetColor = { success: false, error: e.message };
  }

  // 4. Find elements by parameter
  try {
    const findRes = await revit("elements/find-by-parameter", {
      parameterName: "Commentaires",
      searchValue: "CABANE_SCRIPT",
      exactMatch: true,
      limit: 10
    });
    results.findParam = { success: true, matches: findRes.matches?.length };
    console.log("4. elements/find-by-parameter: OK (matches: " + (findRes.matches?.length || 0) + ")");
  } catch (e) {
    results.findParam = { success: false, error: e.message };
  }

  // 5. Materials list
  try {
    const matRes = await revit("materials/list");
    results.materials = { success: true, count: matRes.materials?.length };
    console.log("5. materials/list: OK (count: " + (matRes.materials?.length || 0) + ")");
  } catch (e) {
    results.materials = { success: false, error: e.message };
  }

  // 6. View templates
  try {
    const vtRes = await revit("views/list-templates");
    results.viewTemplates = { success: true, count: vtRes.templates?.length };
    console.log("6. views/list-templates: OK (count: " + (vtRes.templates?.length || 0) + ")");
  } catch (e) {
    results.viewTemplates = { success: false, error: e.message };
  }

  // 7. Unused types audit
  try {
    const unusedRes = await revit("model/purge-unused");
    results.unusedTypes = { success: true, count: unusedRes.unusedCount };
    console.log("7. model/purge-unused: OK (unused count: " + unusedRes.unusedCount + ")");
  } catch (e) {
    results.unusedTypes = { success: false, error: e.message };
  }

  // 8. Material takeoff
  try {
    const takeoffRes = await revit("takeoff/materials", { category: "Walls" });
    results.takeoff = { success: true, materialsCount: takeoffRes.materials?.length };
    console.log("8. takeoff/materials: OK (materials: " + (takeoffRes.materials?.length || 0) + ")");
  } catch (e) {
    results.takeoff = { success: false, error: e.message };
  }

  // 9. Links list
  try {
    const linksRes = await revit("links/list");
    results.links = { success: true, data: linksRes };
    console.log("9. links/list: OK");
  } catch (e) {
    results.links = { success: false, error: e.message };
  }

  console.log("\nBatch tools summary:", JSON.stringify(results, null, 2));
}

testBatchRealTools().catch(console.error);
