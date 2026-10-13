import { revit } from "../mcp-server/dist/bridge.js";

async function inspectTitleBlocks() {
  const info = await revit("model/info");
  console.log("Active Document:", info.filePath);

  const tbs = await revit("families/list", { category: "Title Blocks" });
  console.log("Title Blocks families:", JSON.stringify(tbs, null, 2));

  // Also query elements of category Title Blocks
  const tbElements = await revit("elements/query", { category: "Title Blocks" });
  console.log("Placed TitleBlock elements:", JSON.stringify(tbElements, null, 2));

  // Check existing sheets
  const sheets = await revit("sheets/list");
  console.log("Existing sheets before:", JSON.stringify(sheets, null, 2));
}

inspectTitleBlocks().catch(console.error);
