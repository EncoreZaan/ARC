import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..');

async function getToolsFromMcp() {
  return new Promise((resolve, reject) => {
    const bundlePath = path.join(repoRoot, 'mcp-server', 'dist', 'index.bundle.mjs');
    const proc = spawn('node', [bundlePath]);
    let stdoutBuffer = '';
    let stderrBuffer = '';

    proc.stdout.on('data', (chunk) => {
      stdoutBuffer += chunk.toString();
      const lines = stdoutBuffer.split('\n');
      for (const line of lines) {
        if (!line.trim()) continue;
        try {
          const msg = JSON.parse(line);
          if (msg.id === 2 && msg.result && msg.result.tools) {
            proc.kill();
            resolve(msg.result.tools);
            return;
          }
        } catch {}
      }
    });

    proc.stderr.on('data', (chunk) => {
      stderrBuffer += chunk.toString();
    });

    proc.on('error', reject);
    proc.on('close', (code) => {
      // If closed before resolving
      if (!stdoutBuffer.includes('"tools"')) {
        reject(new Error(`Process closed with code ${code}. Stderr: ${stderrBuffer}`));
      }
    });

    // Send handshake
    proc.stdin.write(JSON.stringify({
      jsonrpc: '2.0',
      id: 1,
      method: 'initialize',
      params: {
        protocolVersion: '2024-11-05',
        capabilities: {},
        clientInfo: { name: 'manifest-extractor', version: '1.0' }
      }
    }) + '\n');

    setTimeout(() => {
      proc.stdin.write(JSON.stringify({
        jsonrpc: '2.0',
        method: 'notifications/initialized'
      }) + '\n');

      proc.stdin.write(JSON.stringify({
        jsonrpc: '2.0',
        id: 2,
        method: 'tools/list'
      }) + '\n');
    }, 400);

    setTimeout(() => {
      proc.kill();
      reject(new Error('Timeout extracting tools from MCP server'));
    }, 10000);
  });
}

function parseActionsFromSource() {
  const indexPath = path.join(repoRoot, 'mcp-server', 'src', 'index.ts');
  const code = fs.readFileSync(indexPath, 'utf8');

  // Match: server.registerTool("name", ... async (...) => { ... revit("action") ... });
  const toolBlocks = code.split(/server\.registerTool\s*\(/);
  const mappings = {};

  for (let i = 1; i < toolBlocks.length; i++) {
    const block = toolBlocks[i];
    const nameMatch = block.match(/^\s*["']([^"']+)["']/);
    if (!nameMatch) continue;
    const name = nameMatch[1];

    if (block.includes('revitHealth(')) {
      mappings[name] = 'healthz';
      continue;
    }

    const actionMatch = block.match(/revit\s*\(\s*["']([^"']+)["']/);
    if (actionMatch) {
      mappings[name] = actionMatch[1];
    } else {
      mappings[name] = 'UNKNOWN';
    }
  }

  return mappings;
}

async function main() {
  console.log('Extracting tools from MCP server bundle...');
  const tools = await getToolsFromMcp();
  console.log(`Retrieved ${tools.length} tools via MCP protocol.`);

  const actions = parseActionsFromSource();
  console.log(`Parsed ${Object.keys(actions).length} actions from index.ts.`);

  const manifest = {
    generatedAt: new Date().toISOString(),
    totalTools: tools.length,
    tools: []
  };

  let unmappedCount = 0;
  for (const t of tools) {
    const action = actions[t.name];
    if (!action || action === 'UNKNOWN') {
      console.warn(`WARNING: Tool ${t.name} has no detected Revit action!`);
      unmappedCount++;
    }

    manifest.tools.push({
      name: t.name,
      description: t.description || '',
      inputSchema: t.inputSchema,
      action: action || '',
      isHealthCheck: action === 'healthz'
    });
  }

  const outDir = path.join(repoRoot, 'revit-mcp-server');
  if (!fs.existsSync(outDir)) {
    fs.mkdirSync(outDir, { recursive: true });
  }

  const outPath = path.join(outDir, 'tools_manifest.json');
  fs.writeFileSync(outPath, JSON.stringify(manifest, null, 2), 'utf8');
  console.log(`Tools manifest successfully written to: ${outPath}`);
  console.log(`Summary: ${tools.length} tools, ${unmappedCount} unmapped.`);
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
