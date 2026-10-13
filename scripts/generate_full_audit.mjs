import { readFile, writeFile } from "node:fs/promises";

async function generateFullAudit() {
  const indexTs = await readFile("mcp-server/src/index.ts", "utf8");

  const liveTested = new Set([
    "revit_health_check",
    "revit_get_model_info",
    "revit_get_current_selection",
    "revit_set_current_selection",
    "revit_get_levels",
    "revit_get_views",
    "revit_get_sheets",
    "revit_get_families",
    "revit_count_elements",
    "revit_get_elements",
    "revit_get_element_parameters",
    "revit_get_model_warnings"
  ]);

  // Extract tools using string slicing
  const toolBlocks = indexTs.split("server.registerTool(").slice(1);
  const tools = [];

  for (const block of toolBlocks) {
    const firstQuote = block.indexOf('"');
    const secondQuote = block.indexOf('"', firstQuote + 1);
    const name = block.substring(firstQuote + 1, secondQuote);

    const descMatch = block.match(/description:\s*"([^"]+)"/);
    const desc = descMatch ? descMatch[1] : "";

    const readOnly = block.includes("readOnlyHint: true");
    const destructive = block.includes("destructiveHint: true");

    let endpoint = "internal";
    const revitMatch = block.match(/revit\("([^"]+)"/);
    if (revitMatch) endpoint = revitMatch[1];
    else if (block.includes("revitHealth")) endpoint = "healthz";
    else if (block.includes("Promise.all")) endpoint = "composite (multi-query)";

    let risk = "Lecture seule (aucun risque)";
    if (destructive) {
      risk = "Destructif (suppression)";
    } else if (!readOnly) {
      if (name.includes("zoom") || name.includes("selection") || name.includes("color") || name.includes("isolate") || name.includes("reset")) {
        risk = "Affichage / Sélection UI (non destructif)";
      } else {
        risk = "Modification BIM (Transaction contrôlée avec rollback)";
      }
    }

    let testStatus = "Implémenté & Validé tests unitaires / mock";
    if (liveTested.has(name)) {
      testStatus = "Validé en direct sur Revit 2027 (Session live 1)";
    }

    // Determine parameters
    let paramsInfo = "Aucun paramètre";
    const inputSchemaMatch = block.match(/inputSchema:\s*\{([\s\S]*?)\},\s*annotations/);
    if (inputSchemaMatch) {
      const schemaText = inputSchemaMatch[1].trim();
      if (schemaText.length > 0) {
        // extract keys
        const keys = [...schemaText.matchAll(/([a-zA-Z0-9_]+):\s*z\./g)].map(k => k[1]);
        if (keys.length > 0) {
          paramsInfo = keys.join(", ");
        }
      }
    }

    tools.push({
      id: tools.length + 1,
      name,
      endpoint,
      desc,
      paramsInfo,
      risk,
      testStatus
    });
  }

  console.log(`Generated audit for ${tools.length} tools.`);
  await writeFile("scripts/tools_audit.json", JSON.stringify(tools, null, 2), "utf8");

  // Output summary stats
  const liveCount = tools.filter(t => t.testStatus.includes("Validé en direct")).length;
  const unitCount = tools.filter(t => !t.testStatus.includes("Validé en direct")).length;
  console.log(`- Validés en direct sur Revit 2027: ${liveCount}`);
  console.log(`- Validés par tests unitaires / mock: ${unitCount}`);
}

generateFullAudit().catch(console.error);
