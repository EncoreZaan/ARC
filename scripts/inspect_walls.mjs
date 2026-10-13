import { revit } from "../mcp-server/dist/bridge.js";

async function inspectWalls() {
  const info = await revit("model/info");
  console.log("Active Document:", info.filePath);
  if (!info.filePath.includes("MCP TEST")) {
    throw new Error("CRITICAL: Active document is NOT the test copy!");
  }

  const walls = await revit("elements/query", { category: "Walls" });
  console.log("Existing walls count:", walls.elements?.length);
  const wallTypes = new Set();
  for (const w of walls.elements || []) {
    wallTypes.add(w.typeName);
    console.log(`Wall ID ${w.id}: Type=${w.typeName}, Level=${w.levelName}, BBox=${JSON.stringify(w.boundingBox)}`);
  }
  console.log("Available wall types:", [...wallTypes]);
}

inspectWalls().catch(console.error);
