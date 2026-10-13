import { readFile } from "node:fs/promises";

async function auditTools() {
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
  const actionRegex = /"([^"]+)"\s*=>\s*([^,\n\r]+)/g;
  const actions = new Map();
  while ((m = actionRegex.exec(handlerCs)) !== null) {
    actions.set(m[1], m[2].trim());
  }

  console.log(`Total MCP Tools registered: ${tools.length}`);
  console.log(`Total Revit Actions handled in C#: ${actions.size}\n`);

  for (const tool of tools) {
    // Find what action revit("...") this tool calls
    const toolBlockRegex = new RegExp(`server\\.registerTool\\(\\s*"${tool}"[\\s\\S]*?async\\s*\\(([^)]*)\\)\\s*=>\\s*{([\\s\\S]*?}\\s*\\);)`, "g");
    const blockMatch = toolBlockRegex.exec(indexTs);
    let calledAction = "Unknown";
    if (blockMatch) {
      const body = blockMatch[2];
      const callMatch = /revit\("([^"]+)"/.exec(body);
      if (callMatch) {
        calledAction = callMatch[1];
      } else if (body.includes("revitHealth")) {
        calledAction = "healthz (HTTP directly)";
      } else if (body.includes("Promise.all")) {
        calledAction = "Composite (model/info + model/warnings)";
      }
    }

    const csharpImpl = actions.get(calledAction);
    const hasCs = csharpImpl ? "OK (C# method: " + csharpImpl + ")" : (calledAction.includes("healthz") ? "OK (RevitHttpServer.cs)" : (calledAction.includes("Composite") ? "OK (Multi-action)" : "MISSING"));

    console.log(`- ${tool.padEnd(35)} -> Action: ${calledAction.padEnd(25)} -> Impl: ${hasCs}`);
  }
}

auditTools().catch(console.error);
