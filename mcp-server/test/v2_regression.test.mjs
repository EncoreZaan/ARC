import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

test("V2.0.0 Regression Test - All 70 original tools must exist in MCP server", () => {
  const root = path.resolve("..");
  const auditPath = path.join(root, "scripts", "tools_audit.json");
  const auditTools = JSON.parse(fs.readFileSync(auditPath, "utf8")).map(t => t.name);

  assert.equal(auditTools.length, 70, "tools_audit.json must contain exactly 70 tools");

  const indexPath = path.join(process.cwd(), "src", "index.ts");
  const indexContent = fs.readFileSync(indexPath, "utf8");

  const registeredToolNames = [...indexContent.matchAll(/server\.registerTool\(\s*["']([^"']+)["']/g)].map(m => m[1]);

  const missingTools = [];
  for (const toolName of auditTools) {
    if (!registeredToolNames.includes(toolName)) {
      missingTools.push(toolName);
    }
  }

  assert.deepEqual(missingTools, [], `Missing v2.0.0 tools: ${missingTools.join(", ")}`);
});
