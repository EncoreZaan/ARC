import { spawn } from "node:child_process";
import { resolve } from "node:path";

async function testMcpProtocol() {
  const serverPath = resolve("./mcp-server/dist/index.js");
  console.log("Lancement du serveur MCP:", serverPath);

  const proc = spawn("node", [serverPath], {
    stdio: ["pipe", "pipe", "inherit"],
  });

  let buffer = "";

  const send = (msg) => {
    const json = JSON.stringify(msg);
    proc.stdin.write(json + "\n");
  };

  proc.stdout.on("data", (chunk) => {
    buffer += chunk.toString();
    const lines = buffer.split("\n");
    buffer = lines.pop(); // keep remainder

    for (const line of lines) {
      if (!line.trim()) continue;
      try {
        const parsed = JSON.parse(line);
        handleMessage(parsed);
      } catch (err) {
        console.error("Non-JSON stdout:", line);
      }
    }
  });

  function handleMessage(msg) {
    if (msg.id === 1) {
      console.log("Reponse INITIALIZE recue avec succes:", msg.result.serverInfo);
      // Now list tools
      send({
        jsonrpc: "2.0",
        id: 2,
        method: "tools/list",
        params: {}
      });
    } else if (msg.id === 2) {
      console.log(`Reponse TOOLS/LIST recue : ${msg.result.tools.length} outils exposes.`);
      console.log("Premiers outils:", msg.result.tools.slice(0, 5).map(t => t.name));
      proc.kill();
      process.exit(0);
    }
  }

  // Send initialize request
  send({
    jsonrpc: "2.0",
    id: 1,
    method: "initialize",
    params: {
      protocolVersion: "2024-11-05",
      capabilities: {},
      clientInfo: { name: "test-client", version: "1.0.0" }
    }
  });
}

testMcpProtocol().catch(err => {
  console.error("Test MCP echoue:", err);
  process.exit(1);
});
