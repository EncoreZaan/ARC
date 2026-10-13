const DEFAULT_REVIT_HOST = "http://localhost:6543";
const DEFAULT_REVIT_TIMEOUT_MS = 120000;
const MIN_REVIT_TIMEOUT_MS = 1000;
const MAX_REVIT_TIMEOUT_MS = 600000;
export function resolveRevitBridgeConfig(env = process.env) {
    const host = normalizeRevitHost(env.REVIT_HOST ?? DEFAULT_REVIT_HOST, env.REVIT_ALLOW_REMOTE === "1");
    const timeoutMs = parseTimeoutMs(env.REVIT_TIMEOUT_MS ?? String(DEFAULT_REVIT_TIMEOUT_MS));
    return { host, timeoutMs };
}
export function normalizeRevitHost(rawHost, allowRemote = false) {
    let url;
    try {
        url = new URL(rawHost);
    }
    catch {
        throw new Error(`Invalid REVIT_HOST '${rawHost}'. Expected a URL like ${DEFAULT_REVIT_HOST}.`);
    }
    if (url.protocol !== "http:") {
        throw new Error("REVIT_HOST must use http:// because the Revit addin exposes a local HttpListener bridge.");
    }
    if (url.username || url.password) {
        throw new Error("REVIT_HOST must not include credentials.");
    }
    const hostname = url.hostname.toLowerCase();
    const isLoopback = hostname === "localhost" || hostname === "127.0.0.1" || hostname === "[::1]" || hostname === "::1";
    if (!allowRemote && !isLoopback) {
        throw new Error("REVIT_HOST must point to localhost/127.0.0.1. Set REVIT_ALLOW_REMOTE=1 only for an explicitly trusted remote bridge.");
    }
    return url.origin;
}
export function parseTimeoutMs(rawTimeout) {
    const timeoutMs = Number(rawTimeout);
    if (!Number.isFinite(timeoutMs) || timeoutMs < MIN_REVIT_TIMEOUT_MS) {
        throw new Error(`REVIT_TIMEOUT_MS must be a number of milliseconds >= ${MIN_REVIT_TIMEOUT_MS}.`);
    }
    if (timeoutMs > MAX_REVIT_TIMEOUT_MS) {
        throw new Error(`REVIT_TIMEOUT_MS must be <= ${MAX_REVIT_TIMEOUT_MS}.`);
    }
    return timeoutMs;
}
export function validateRevitAction(action) {
    if (!/^[a-z0-9-]+(?:\/[a-z0-9-]+)*$/.test(action)) {
        throw new Error(`Invalid Revit action '${action}'. Expected lowercase path segments like 'model/info' or 'views/create-plans-for-levels'.`);
    }
    return action;
}
export function buildRevitApiUrl(host, action) {
    return `${host}/api/${validateRevitAction(action)}`;
}
async function requestJson(url, init, timeoutMs, actionLabel) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), timeoutMs);
    let res;
    try {
        res = await fetch(url, { ...init, signal: controller.signal });
    }
    catch (e) {
        if (e instanceof Error && e.name === "AbortError") {
            throw new Error(`Timed out after ${timeoutMs} ms waiting for Revit ${actionLabel}.`);
        }
        throw new Error(`Cannot reach Revit addin at ${new URL(url).origin}. Make sure Autodesk Revit 2027 is open with a project loaded and the RevitMCP addin is running.`);
    }
    finally {
        clearTimeout(timeout);
    }
    const body = await res.json().catch(() => ({ error: "Non-JSON response from addin" }));
    if (!res.ok) {
        const msg = body.error ?? res.statusText;
        throw new Error(`Revit addin error (${res.status}): ${msg}`);
    }
    return body;
}
export async function revit(action, payload = {}, config = resolveRevitBridgeConfig()) {
    return requestJson(buildRevitApiUrl(config.host, action), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
    }, config.timeoutMs, `action '${action}'`);
}
export async function revitHealth(config = resolveRevitBridgeConfig()) {
    return requestJson(`${config.host}/healthz`, { method: "GET" }, Math.min(config.timeoutMs, 10000), "health check");
}
//# sourceMappingURL=bridge.js.map