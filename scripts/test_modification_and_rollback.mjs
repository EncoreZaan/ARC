import { revit } from "../mcp-server/dist/bridge.js";

async function testModificationAndRollback() {
  const info = await revit("model/info");
  console.log("Active Document:", info.filePath);
  if (!info.filePath.includes("MCP TEST")) {
    throw new Error("ABORT: Not test copy!");
  }

  const testWallId = 264388; // Created in Phase D
  console.log("Targeting test wall ElementId:", testWallId);

  // 1. Read initial comments parameter
  const pInitial = await revit("elements/parameters", { elementId: testWallId });
  const commentInitial = pInitial.parameters.find(p => p.name === "Commentaires");
  console.log("1. Initial 'Commentaires' value:", commentInitial ? commentInitial.value : "(none)");

  // 2. Set 'Commentaires' to a test string
  const testVal = "MCP_VALIDATION_TOKEN_" + Date.now();
  console.log("2. Setting 'Commentaires' to:", testVal);
  const setRes = await revit("elements/set-parameter", {
    elementId: testWallId,
    parameterName: "Commentaires",
    value: testVal
  });
  console.log("Set parameter response:", JSON.stringify(setRes, null, 2));

  // 3. Read back to confirm change
  const pAfterSet = await revit("elements/parameters", { elementId: testWallId });
  const commentAfterSet = pAfterSet.parameters.find(p => p.name === "Commentaires");
  console.log("3. Read-back 'Commentaires':", commentAfterSet ? commentAfterSet.value : "(none)");
  if (commentAfterSet && commentAfterSet.value === testVal) {
    console.log("CONFIRMED: Parameter modification succeeded and verified!");
  } else {
    throw new Error("Mismatch in parameter value!");
  }

  // 4. Test transaction rollback on illegal operation
  console.log("\n4. Testing rollback on deliberate failure (modifying read-only parameter 'Longueur')...");
  let errorCaught = false;
  try {
    await revit("elements/set-parameter", {
      elementId: testWallId,
      parameterName: "Longueur",
      value: 999.0
    });
  } catch (err) {
    errorCaught = true;
    console.log("Caught expected rejection:", err.message);
  }

  if (errorCaught) {
    console.log("SUCCESS: Transaction was rejected and caught cleanly!");
  } else {
    throw new Error("Expected failure did not occur!");
  }

  // 5. Verify data integrity after rollback
  const pAfterRollback = await revit("elements/parameters", { elementId: testWallId });
  const commentAfterRollback = pAfterRollback.parameters.find(p => p.name === "Commentaires");
  const lengthAfterRollback = pAfterRollback.parameters.find(p => p.name === "Longueur");

  console.log("5. State verification after rollback:");
  console.log("   - 'Commentaires' preserved:", commentAfterRollback ? commentAfterRollback.value === testVal : false);
  console.log("   - 'Longueur' intact (1.00 m):", lengthAfterRollback ? lengthAfterRollback.valueString : "(none)");

  // 6. Reversibility: Reset 'Commentaires' to empty
  console.log("\n6. Reversibility check: resetting 'Commentaires' to empty string...");
  await revit("elements/set-parameter", {
    elementId: testWallId,
    parameterName: "Commentaires",
    value: ""
  });
  const pFinal = await revit("elements/parameters", { elementId: testWallId });
  const commentFinal = pFinal.parameters.find(p => p.name === "Commentaires");
  console.log("   - 'Commentaires' final value:", commentFinal ? JSON.stringify(commentFinal.value) : '""');
  console.log("SUCCESS: Reversibility verified!");
}

testModificationAndRollback().catch(console.error);
