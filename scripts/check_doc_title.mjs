import fs from 'fs';

const content = fs.readFileSync('mcp-server/src/index.ts', 'utf8');
const auditTools = JSON.parse(fs.readFileSync('scripts/tools_audit.json', 'utf8'));

console.log('Total audit tools to check:', auditTools.length);

const missingDocTitle = [];

for (const t of auditTools) {
  const toolName = t.name;
  const regex = new RegExp(`server\\.registerTool\\(\\s*["']${toolName}["'][\\s\\S]*?inputSchema:\\s*\\{([\\s\\S]*?)\\}\\s*,`, 'm');
  const match = content.match(regex);
  if (!match) {
    console.log(`Tool NOT FOUND in index.ts: ${toolName}`);
    continue;
  }
  const schemaBody = match[1];
  if (!schemaBody.includes('documentTitle')) {
    missingDocTitle.push(toolName);
  }
}

console.log('Tools missing documentTitle in inputSchema:', missingDocTitle);
