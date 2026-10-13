import { readFile } from "node:fs/promises";

async function findUnexposedActions() {
  const indexTs = await readFile("./mcp-server/src/index.ts", "utf8");
  const handlerCs = await readFile("./revit-addin/RevitMCP/RevitEventHandler.cs", "utf8");

  // Extract all tools from index.ts
  const toolRegex = /server\.registerTool\(\s*"([^"]+)"/g;
  const tools = [];
  let m;
  while ((m = toolRegex.exec(indexTs)) !== null) {
    tools.push(m[1]);
  }

  // Extract all actions from RevitEventHandler.cs switch
  const actionRegex = /"([^"]+)"\s*=>/g;
  const actions = [];
  while ((m = actionRegex.exec(handlerCs)) !== null) {
    actions.push(m[1]);
  }

  console.log(`Actions in C#: ${actions.length}`);
  console.log(`Tools in index.ts: ${tools.length}`);

  const unexposed = actions.filter(a => !indexTs.includes(`"${a}"`));
  console.log("\nActions in C# NOT exposed to MCP yet:");
  unexposed.forEach(a => console.log(`  - ${a}`));
}

findUnexposedActions().catch(console.error);
