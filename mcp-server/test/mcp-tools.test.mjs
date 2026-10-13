import assert from "node:assert/strict";
import http from "node:http";
import test from "node:test";
import { resolveRevitBridgeConfig, revit, revitHealth, validateRevitAction } from "../dist/bridge.js";

test("Revit bridge client integration with mock HTTP server", async (t) => {
  let lastAction = "";
  let lastPayload = null;
  const mockPort = 6549;

  // Setup mock Revit addin HTTP server
  const server = http.createServer((req, res) => {
    if (req.url === "/healthz") {
      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify({
        ok: true,
        service: "RevitMCP",
        version: "2.0.0",
        uptimeSeconds: 42,
      }));
      return;
    }

    if (req.url?.startsWith("/api/")) {
      lastAction = req.url.replace("/api/", "");
      let body = "";
      req.on("data", (chunk) => { body += chunk; });
      req.on("end", () => {
        lastPayload = body ? JSON.parse(body) : {};
        res.writeHead(200, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ success: true, action: lastAction, received: lastPayload }));
      });
      return;
    }

    res.writeHead(404);
    res.end();
  });

  await new Promise((resolve) => server.listen(mockPort, resolve));

  const testConfig = {
    host: `http://localhost:${mockPort}`,
    timeoutMs: 5000,
  };

  await t.test("health check receives valid status from bridge", async () => {
    const health = await revitHealth(testConfig);
    assert.equal(health.ok, true);
    assert.equal(health.service, "RevitMCP");
  });

  await t.test("model/info dispatches to correct action route", async () => {
    const res = await revit("model/info", {}, testConfig);
    assert.equal(res.success, true);
    assert.equal(res.action, "model/info");
  });

  await t.test("selection/get dispatches correctly", async () => {
    const res = await revit("selection/get", {}, testConfig);
    assert.equal(res.success, true);
    assert.equal(res.action, "selection/get");
  });

  await t.test("elements/create-floor forwards dimensions", async () => {
    const payload = { levelId: 101, widthMm: 8000, lengthMm: 6000 };
    const res = await revit("elements/create-floor", payload, testConfig);
    assert.equal(res.success, true);
    assert.equal(res.action, "elements/create-floor");
    assert.equal(lastPayload.widthMm, 8000);
    assert.equal(lastPayload.lengthMm, 6000);
  });

  await t.test("action validation rejects dangerous or malformed paths", () => {
    assert.throws(() => validateRevitAction("../evil/path"));
    assert.throws(() => validateRevitAction("UPPERCASE"));
    assert.throws(() => validateRevitAction("action with spaces"));
  });

  // Cleanup
  await new Promise((resolve) => server.close(resolve));
});
