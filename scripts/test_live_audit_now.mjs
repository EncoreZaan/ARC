import { revit, revitHealth, resolveRevitBridgeConfig } from "../mcp-server/dist/bridge.js";

async function runLiveDiagnostics() {
  const config = resolveRevitBridgeConfig();
  const results = {};

  console.log("=================================================");
  console.log("   LIVE DIAGNOSTIC REVIT AI BRIDGE - 22:26       ");
  console.log("=================================================");

  // 1. revit_health_check
  try {
    const health = await revitHealth(config);
    results.health = { status: "SUCCESS", data: health };
    console.log("1. revit_health_check: SUCCESS");
  } catch (err) {
    results.health = { status: "FAILED", error: err.message };
    console.error("1. revit_health_check: FAILED -", err.message);
  }

  // 2. revit_get_model_info
  try {
    const info = await revit("model/info");
    results.model_info = { status: "SUCCESS", data: info };
    console.log("2. revit_get_model_info: SUCCESS");
  } catch (err) {
    results.model_info = { status: "FAILED", error: err.message };
    console.error("2. revit_get_model_info: FAILED -", err.message);
  }

  // 3. revit_get_levels
  try {
    const levels = await revit("levels/list");
    results.levels = { status: "SUCCESS", data: levels };
    console.log("3. revit_get_levels: SUCCESS");
  } catch (err) {
    results.levels = { status: "FAILED", error: err.message };
    console.error("3. revit_get_levels: FAILED -", err.message);
  }

  // 4. revit_get_views
  try {
    const views = await revit("views/list");
    results.views = { status: "SUCCESS", data: views };
    console.log("4. revit_get_views: SUCCESS");
  } catch (err) {
    results.views = { status: "FAILED", error: err.message };
    console.error("4. revit_get_views: FAILED -", err.message);
  }

  // 5. revit_get_sheets
  try {
    const sheets = await revit("sheets/list");
    results.sheets = { status: "SUCCESS", data: sheets };
    console.log("5. revit_get_sheets: SUCCESS");
  } catch (err) {
    results.sheets = { status: "FAILED", error: err.message };
    console.error("5. revit_get_sheets: FAILED -", err.message);
  }

  // 6. revit_count_elements
  try {
    const count = await revit("elements/count");
    results.elements_count = { status: "SUCCESS", data: count };
    console.log("6. revit_count_elements: SUCCESS");
  } catch (err) {
    results.elements_count = { status: "FAILED", error: err.message };
    console.error("6. revit_count_elements: FAILED -", err.message);
  }

  // 7. revit_get_model_warnings
  try {
    const warnings = await revit("model/warnings");
    results.warnings = { status: "SUCCESS", data: warnings };
    console.log("7. revit_get_model_warnings: SUCCESS");
  } catch (err) {
    results.warnings = { status: "FAILED", error: err.message };
    console.error("7. revit_get_model_warnings: FAILED -", err.message);
  }

  // 8. revit_get_phases (new v2 tool)
  try {
    const phases = await revit("phases/list");
    results.phases = { status: "SUCCESS", data: phases };
    console.log("8. revit_get_phases: SUCCESS");
  } catch (err) {
    results.phases = { status: "FAILED", error: err.message };
    console.error("8. revit_get_phases: FAILED -", err.message);
  }

  // 9. revit_get_worksets (new v2 tool)
  try {
    const worksets = await revit("worksets/list");
    results.worksets = { status: "SUCCESS", data: worksets };
    console.log("9. revit_get_worksets: SUCCESS");
  } catch (err) {
    results.worksets = { status: "FAILED", error: err.message };
    console.error("9. revit_get_worksets: FAILED -", err.message);
  }

  console.log("\n================ FULL DUMP ================");
  console.log(JSON.stringify(results, null, 2));
}

runLiveDiagnostics().catch(console.error);
