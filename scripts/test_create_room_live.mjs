import { revit } from "../mcp-server/dist/bridge.js";

async function testRoomCreation() {
  const info = await revit("model/info");
  console.log("Active Document:", info.filePath);
  if (!info.filePath.includes("MCP TEST")) {
    throw new Error("ABORT: Not test copy!");
  }

  // Check rooms before
  const roomsBefore = await revit("rooms/list", {});
  console.log("Rooms before:", JSON.stringify(roomsBefore, null, 2));

  // Try creating a test room at (0, 0) on level Niveau 0
  const payload = {
    levelId: 608, // Niveau 0
    xMm: 0,
    yMm: 0,
    name: "PIECE_TEST_MCP",
    number: "T-01"
  };

  console.log("Attempting room creation with payload:", JSON.stringify(payload, null, 2));
  const res = await revit("rooms/create", payload);
  console.log("Create room response:", JSON.stringify(res, null, 2));

  // Verify parameters of the created room
  const roomId = res.roomId;
  const paramsRes = await revit("elements/parameters", { elementId: roomId });
  const params = paramsRes.parameters || [];

  const areaP = params.find(p => p.name === "Surface");
  const perimeterP = params.find(p => p.name === "Périmètre");
  const unboundedP = params.find(p => p.name === "Non délimitée" || p.name === "Non fermée");

  console.log("\n--- VERIFICATION OF ROOM STATUS ---");
  console.log("Room ID:", roomId);
  console.log("Room Name:", res.name);
  console.log("Room Number:", res.number);
  console.log("Area (Surface):", areaP ? areaP.valueString : "N/A");
  console.log("Perimeter (Périmètre):", perimeterP ? perimeterP.valueString : "N/A");

  if (res.areaSqM === 0 || (areaP && areaP.valueString && areaP.valueString.includes("Pas fermé") || areaP.valueString === "0.00 m²" || areaP.valueString === "Non fermée")) {
    console.log("RESULT: Room was created in the model database with ID " + roomId + ", but is UNENCLOSED (Non fermée / Surface = 0 m²).");
    console.log("EXPLICATION: The existing geometry does not form a fully enclosed boundary loop at (0,0). In strict BIM compliance, a room requires a completely closed polygon of room-bounding walls or room separation lines.");
  } else {
    console.log("RESULT: Room is FULLY ENCLOSED with surface:", res.areaSqM, "m²");
  }
}

testRoomCreation().catch(console.error);
