import { revit, revitHealth } from "../dist/bridge.js";

async function run() {
  console.log("=== 1. Health Check ===");
  const h = await revitHealth();
  console.log(JSON.stringify(h, null, 2));

  console.log("\n=== 2. Model Info ===");
  const info = await revit("model/info");
  console.log(JSON.stringify(info, null, 2));

  console.log("\n=== 3. Levels ===");
  const levels = await revit("levels/list");
  console.log(JSON.stringify(levels, null, 2));

  console.log("\n=== 4. Selection ===");
  const sel = await revit("selection/get");
  console.log(JSON.stringify(sel, null, 2));

  console.log("\n=== 5. Elements Count ===");
  const count = await revit("elements/count");
  console.log(JSON.stringify(count, null, 2));

  console.log("\n=== 6. Query sample walls ===");
  const walls = await revit("elements/query", { category: "Walls", limit: 3 });
  console.log(JSON.stringify(walls, null, 2));

  console.log("\n=== 7. Sheets List ===");
  const sheets = await revit("sheets/list");
  console.log(JSON.stringify(sheets, null, 2));

  console.log("\n=== 8. Model Warnings ===");
  const warnings = await revit("model/warnings");
  console.log(JSON.stringify(warnings, null, 2));
}

run().catch(err => {
  console.error("TEST FAILED:", err);
  process.exit(1);
});
