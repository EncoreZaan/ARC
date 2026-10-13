import { revit } from "../mcp-server/dist/bridge.js";

async function testCreateSheet() {
  const info = await revit("model/info");
  console.log("Active Document:", info.filePath);
  if (!info.filePath.includes("MCP TEST")) {
    throw new Error("ABORT: Not test copy!");
  }

  const sheetsBefore = await revit("sheets/list");
  console.log("Sheets before count:", sheetsBefore.sheets?.length);

  // Create test sheet
  const sheetPayload = {
    sheetNumber: "MCP-TEST-01",
    sheetName: "Validation Revit MCP",
    titleBlockName: "Paysage A3"
  };

  console.log("Creating test sheet with payload:", JSON.stringify(sheetPayload, null, 2));
  const sheetRes = await revit("sheets/create", sheetPayload);
  console.log("Create sheet response:", JSON.stringify(sheetRes, null, 2));

  if (!sheetRes.sheetId) {
    throw new Error("Failed to create sheet!");
  }

  const newSheetId = sheetRes.sheetId;

  // Place unplaced view 'Niveau 1' (ID: 1911)
  const placePayload = {
    sheetId: newSheetId,
    viewId: 1911,
    centerX: 210, // center of A3 sheet in mm
    centerY: 148
  };

  console.log("Placing view Niveau 1 (1911) on new sheet:", JSON.stringify(placePayload, null, 2));
  const placeRes = await revit("sheets/place-view", placePayload);
  console.log("Place view response:", JSON.stringify(placeRes, null, 2));

  // Verify sheets after
  const sheetsAfter = await revit("sheets/list");
  console.log("\n--- VERIFICATION OF CREATED SHEET ---");
  const createdSheet = sheetsAfter.sheets?.find(s => s.sheetNumber === "MCP-TEST-01");
  console.log("Created sheet found:", !!createdSheet);
  console.log("Sheet details:", JSON.stringify(createdSheet, null, 2));

  console.log("\n--- VERIFICATION OF EXISTING SHEETS INTEGRITY ---");
  const origSheetNumbers = sheetsBefore.sheets?.map(s => s.sheetNumber);
  const stillExist = origSheetNumbers.every(num => sheetsAfter.sheets?.some(s => s.sheetNumber === num));
  console.log("All original sheets intact:", stillExist);
  console.log("Total sheets before:", sheetsBefore.sheets?.length, "-> Total sheets after:", sheetsAfter.sheets?.length);
}

testCreateSheet().catch(console.error);
