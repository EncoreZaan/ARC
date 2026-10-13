import { revit } from "../mcp-server/dist/bridge.js";

async function testCreateWall() {
  const info = await revit("model/info");
  console.log("Active Document:", info.filePath);
  if (!info.filePath.includes("MCP TEST")) {
    throw new Error("ABORT: Active document is NOT the test copy!");
  }

  // Count walls before
  const beforeCount = await revit("elements/count", { category: "Walls" });
  console.log("Walls count before:", beforeCount.count);

  const payload = {
    startX: 50000,
    startY: 50000,
    endX: 51000,
    endY: 50000,
    levelName: "Niveau 0",
    wallTypeName: "Générique - Ext. 200 mm",
    height: 2400
  };

  console.log("Creating test wall with payload:", JSON.stringify(payload, null, 2));
  const res = await revit("elements/create-wall", payload);
  console.log("Create wall response:", JSON.stringify(res, null, 2));

  if (!res.elementId) {
    throw new Error("Failed to get elementId from response");
  }

  const newWallId = res.elementId;
  console.log("New Wall ElementId:", newWallId);

  // Read back parameters
  const paramsRes = await revit("elements/parameters", { elementId: newWallId });
  const params = paramsRes.parameters || [];

  const lengthP = params.find(p => p.name === "Longueur");
  const heightP = params.find(p => p.name === "Hauteur sans contrainte" || p.name === "Hauteur");
  const levelP = params.find(p => p.name === "Contrainte inférieure");
  const typeP = params.find(p => p.name === "Type" || p.name === "Famille et type");
  const catP = params.find(p => p.name === "Catégorie");

  console.log("\n--- VERIFICATION OF CREATED WALL ---");
  console.log("ElementId:", newWallId);
  console.log("Catégorie:", catP ? catP.valueString : "Unknown");
  console.log("Type:", typeP ? typeP.valueString : "Unknown");
  console.log("Longueur:", lengthP ? lengthP.valueString : "Unknown");
  console.log("Hauteur sans contrainte:", heightP ? heightP.valueString : "Unknown");
  console.log("Contrainte inférieure:", levelP ? levelP.valueString : "Unknown");

  // Count walls after
  const afterCount = await revit("elements/count", { category: "Walls" });
  console.log("Walls count after:", afterCount.count);

  if (afterCount.count === beforeCount.count + 1) {
    console.log("SUCCESS: Wall count increased by exactly 1!");
  } else {
    console.warn("WARNING: Unexpected wall count change!");
  }

  return { newWallId, beforeCount: beforeCount.count, afterCount: afterCount.count };
}

testCreateWall().catch(console.error);
