import { revit } from "../dist/bridge.js";

async function run() {
  console.log("=== TEST SELECTION NON-DESTRUCTIVE ===");
  
  // 1. Initial selection
  const before = await revit("selection/get");
  console.log("Selection avant:", before);

  // 2. Select wall 258440
  console.log("Selection de l'element 258440...");
  const setRes = await revit("selection/set", { elementIds: [258440] });
  console.log("Resultat set_current_selection:", setRes);

  // 3. Read back selection
  const after = await revit("selection/get");
  console.log("Selection lue en retour:", after);

  // 4. Restore selection to empty
  console.log("Remise a zero de la selection...");
  const restoreRes = await revit("selection/set", { elementIds: [] });
  console.log("Resultat reset:", restoreRes);

  const finalState = await revit("selection/get");
  console.log("Selection finale:", finalState);
}

run().catch(err => {
  console.error("ERREUR SELECTION:", err);
  process.exit(1);
});
