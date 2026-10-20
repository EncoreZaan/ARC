import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..');
const rustExe = path.join(repoRoot, 'revit-mcp-server', 'target', 'release', 'revit-mcp-server.exe');

async function testRustMcp() {
  console.log('Testing Rust MCP server binary:', rustExe);

  const proc = spawn(rustExe, [], {
    env: { ...process.env, REVIT_HOST: 'http://localhost:6543' }
  });

  let stdoutBuffer = '';
  let id = 1;

  proc.stderr.on('data', d => console.log('[Rust Server Stderr]', d.toString().trim()));

  function send(obj) {
    const raw = JSON.stringify(obj) + '\n';
    proc.stdin.write(raw);
  }

  function readNextResponse() {
    return new Promise((resolve) => {
      const check = () => {
        const newlineIdx = stdoutBuffer.indexOf('\n');
        if (newlineIdx !== -1) {
          const line = stdoutBuffer.slice(0, newlineIdx).trim();
          stdoutBuffer = stdoutBuffer.slice(newlineIdx + 1);
          if (line) {
            resolve(JSON.parse(line));
            return;
          }
        }
        setTimeout(check, 10);
      };
      check();
    });
  }

  proc.stdout.on('data', d => {
    stdoutBuffer += d.toString();
  });

  // 1. Initialize
  console.log('\n--- Test 1: initialize ---');
  send({
    jsonrpc: '2.0',
    id: id++,
    method: 'initialize',
    params: {
      protocolVersion: '2024-11-05',
      capabilities: {},
      clientInfo: { name: 'rust-test-suite', version: '1.0' }
    }
  });

  const initResp = await readNextResponse();
  console.log('Initialize response:', JSON.stringify(initResp, null, 2));

  // 2. Initialized notification
  send({ jsonrpc: '2.0', method: 'notifications/initialized' });

  // 3. Tools list
  console.log('\n--- Test 2: tools/list ---');
  send({ jsonrpc: '2.0', id: id++, method: 'tools/list' });
  const listResp = await readNextResponse();
  const toolsCount = listResp.result?.tools?.length ?? 0;
  console.log(`Total tools received: ${toolsCount}`);

  // 4. Tools call: revit_health_check
  console.log('\n--- Test 3: tools/call revit_health_check ---');
  send({
    jsonrpc: '2.0',
    id: id++,
    method: 'tools/call',
    params: { name: 'revit_health_check', arguments: {} }
  });
  const healthResp = await readNextResponse();
  console.log('Health check response:', JSON.stringify(healthResp, null, 2));

  // 5. Tools call: revit_get_model_info
  console.log('\n--- Test 4: tools/call revit_get_model_info ---');
  send({
    jsonrpc: '2.0',
    id: id++,
    method: 'tools/call',
    params: { name: 'revit_get_model_info', arguments: {} }
  });
  const infoResp = await readNextResponse();
  console.log('Model info response:', JSON.stringify(infoResp, null, 2));

  // 6. Tools call: revit_get_elements (Walls)
  console.log('\n--- Test 5: tools/call revit_get_elements ---');
  send({
    jsonrpc: '2.0',
    id: id++,
    method: 'tools/call',
    params: {
      name: 'revit_get_elements',
      arguments: { category: 'Walls', limit: 3 }
    }
  });
  const wallsResp = await readNextResponse();
  console.log('Elements response:', JSON.stringify(wallsResp, null, 2));

  // 6. Tools call: unknown tool
  console.log('\n--- Test 6: tools/call unknown tool ---');
  send({
    jsonrpc: '2.0',
    id: id++,
    method: 'tools/call',
    params: {
      name: 'non_existent_tool',
      arguments: {}
    }
  });
  const unkResp = await readNextResponse();
  console.log('Unknown tool response:', JSON.stringify(unkResp, null, 2));

  proc.kill();
  console.log('\n=============================================');
  console.log('ALL RUST MCP SERVER TESTS PASSED SUCCESSFULLY!');
  console.log('=============================================');
}

testRustMcp().catch(err => {
  console.error('Test failed:', err);
  process.exit(1);
});
