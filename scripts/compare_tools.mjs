import fs from 'fs';

const auditTools = JSON.parse(fs.readFileSync('scripts/tools_audit.json', 'utf8')).map(t => t.name);
const currContent = fs.readFileSync('mcp-server/src/index.ts', 'utf8');

const regex = /server\.registerTool\(\s*["']([^"']+)["']/g;
const currTools = [...currContent.matchAll(regex)].map(m => m[1]);

console.log('Audit 70 count:', auditTools.length);
console.log('Current count:', currTools.length);

const missingFromCurrent = auditTools.filter(t => !currTools.includes(t));
console.log('Missing from current:', missingFromCurrent);
