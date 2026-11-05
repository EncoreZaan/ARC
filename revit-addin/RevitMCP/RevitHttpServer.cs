using System.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitMCP;

/// <summary>
/// Embedded HttpListener that receives JSON requests from the MCP Node server
/// and dispatches them on Revit's UI thread via ExternalEvent.
/// </summary>
public class RevitHttpServer
{
    public int BoundPort { get; private set; } = 0;
    public string ActivePrefix => BoundPort > 0 ? $"http://localhost:{BoundPort}/" : "";
    private const string ServiceName = "RevitMCP";
    private const string ServiceVersion = "2.6.0";
    private static readonly DateTime StartedUtc = DateTime.UtcNow;
    private static readonly Regex SafeActionPattern = new("^[a-z0-9-]+(?:/[a-z0-9-]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly TimeSpan QueueTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RevitApiTimeout = TimeSpan.FromSeconds(120);
    private const long MaxRequestBytes = 2 * 1024 * 1024;

    private HttpListener? _listener;
    private Thread? _thread;
    private bool _running;

    // The ExternalEvent mechanism lets us marshal calls onto Revit's main thread.
    private readonly RevitEventHandler _eventHandler;
    public RevitEventHandler EventHandler => _eventHandler;
    private readonly ExternalEvent _externalEvent;
    // RevitEventHandler.RequestGate moved to RevitEventHandler.RequestGate
    private readonly RevitNamedPipeServer _pipeServer;

    public RevitHttpServer(UIControlledApplication uiApp)
    {
        _eventHandler = new RevitEventHandler();
        _externalEvent = ExternalEvent.Create(_eventHandler);
        _pipeServer = new RevitNamedPipeServer(uiApp, _eventHandler, _externalEvent);
    }

    public bool Start()
    {
        try
        {
            _pipeServer.Start();
        }
        catch (Exception ex)
        {
            ConfigManager.LogAudit("PIPE_SERVER_ERROR", $"Failed to start Named Pipe server: {ex.Message}");
        }

        // Dynamic port fallback for seamless multi-instance Revit without conflict (6543 to 6555)
        for (int port = 6543; port <= 6555; port++)
        {
            var prefix = $"http://localhost:{port}/";
            HttpListener? candidate = null;
            try
            {
                candidate = new HttpListener();
                candidate.Prefixes.Add(prefix);
                candidate.Start();

                _listener = candidate;
                BoundPort = port;
                _running = true;
                _thread = new Thread(Listen) { IsBackground = true, Name = $"RevitMCP_HttpServer_{port}" };
                _thread.Start();

                ConfigManager.LogAudit("HTTP_SERVER", $"Revit AI Copilot bridge started on {prefix} (PID: {Environment.ProcessId})");
                RegisterInstance(port);
                return true;
            }
            catch (HttpListenerException hle)
            {
                try { candidate?.Close(); } catch { }
                ConfigManager.LogAudit("HTTP_SERVER_PORT_BUSY", $"Port {port} busy ({hle.Message}), attempting next port...");
                continue;
            }
            catch (Exception ex)
            {
                try { candidate?.Close(); } catch { }
                ConfigManager.LogAudit("HTTP_SERVER_PORT_FAIL", $"Port {port} error: {ex.Message}");
                continue;
            }
        }

        _running = false;
        ConfigManager.LogAudit("HTTP_SERVER_START_FAILED", "Could not bind to any port in range 6543-6555. Named Pipe IPC remains active.");
        return false;
    }

    public void Stop()
    {
        _running = false;
        try { _pipeServer.Stop(); } catch { }
        try
        {
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
        catch { }

        if (BoundPort > 0)
        {
            UnregisterInstance(BoundPort);
        }
    }

    private static void RegisterInstance(int port)
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var mcpDir = Path.Combine(appData, "RevitMCP");
            if (!Directory.Exists(mcpDir))
                Directory.CreateDirectory(mcpDir);

            var regFile = Path.Combine(mcpDir, "active_instances.json");
            var instances = new JArray();

            if (File.Exists(regFile))
            {
                try
                {
                    var text = File.ReadAllText(regFile);
                    var parsed = JObject.Parse(text);
                    if (parsed["instances"] is JArray arr)
                    {
                        var runningPids = System.Diagnostics.Process.GetProcesses().Select(p => p.Id).ToHashSet();
                        foreach (var item in arr)
                        {
                            var pid = item["pid"]?.Value<int>() ?? 0;
                            if (pid > 0 && runningPids.Contains(pid) && pid != Environment.ProcessId)
                            {
                                instances.Add(item);
                            }
                        }
                    }
                }
                catch { }
            }

            instances.Add(new JObject
            {
                ["pid"] = Environment.ProcessId,
                ["port"] = port,
                ["pipe"] = "revit_mcp_ipc",
                ["specific_pipe"] = $"revit_mcp_ipc_{Environment.ProcessId}",
                ["started_at"] = DateTime.UtcNow.ToString("o")
            });

            var outputObj = new JObject { ["instances"] = instances };
            File.WriteAllText(regFile, outputObj.ToString(Formatting.Indented));
        }
        catch { }
    }

    private static void UnregisterInstance(int port)
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var regFile = Path.Combine(appData, "RevitMCP", "active_instances.json");
            if (!File.Exists(regFile)) return;

            var text = File.ReadAllText(regFile);
            var parsed = JObject.Parse(text);
            if (parsed["instances"] is JArray arr)
            {
                var remaining = new JArray();
                foreach (var item in arr)
                {
                    var pid = item["pid"]?.Value<int>() ?? 0;
                    if (pid != Environment.ProcessId)
                        remaining.Add(item);
                }
                parsed["instances"] = remaining;
                File.WriteAllText(regFile, parsed.ToString(Formatting.Indented));
            }
        }
        catch { }
    }

    private void Listen()
    {
        while (_running && _listener != null && _listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = _listener.GetContext(); }
            catch { break; }

            // Handle each request on a thread-pool thread
            ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx));
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        try
        {
            var path = req.Url?.AbsolutePath ?? "";

            if (path == "/healthz")
            {
                WriteJson(res, 200, new
                {
                    ok = true,
                    service = ServiceName,
                    version = ServiceVersion,
                    startedUtc = StartedUtc.ToString("O"),
                    uptimeSeconds = Math.Round((DateTime.UtcNow - StartedUtc).TotalSeconds, 3),
                    queueTimeoutSeconds = QueueTimeout.TotalSeconds,
                    revitApiTimeoutSeconds = RevitApiTimeout.TotalSeconds,
                    maxRequestBytes = MaxRequestBytes,
                    openDocuments = OpenDocumentsCache.GetList()
                });
                return;
            }

            if (req.HttpMethod != "POST")
            {
                WriteJson(res, 405, RevitMcpException.CreateErrorResponse("METHOD_NOT_ALLOWED", "Only POST requests are supported."));
                return;
            }

            if (!path.StartsWith("/api/"))
            {
                WriteJson(res, 404, RevitMcpException.CreateErrorResponse("NOT_FOUND", $"Path '{path}' not found."));
                return;
            }

            if (req.ContentLength64 > MaxRequestBytes)
            {
                WriteJson(res, 413, RevitMcpException.CreateErrorResponse("PAYLOAD_TOO_LARGE", "Request body is too large (max 2 MB)."));
                return;
            }

            // Read body
            string body;
            using (var reader = new StreamReader(req.InputStream, Encoding.UTF8))
                body = reader.ReadToEnd();

            JObject payload;
            try
            {
                payload = string.IsNullOrWhiteSpace(body)
                    ? new JObject()
                    : JObject.Parse(body);
            }
            catch (JsonReaderException ex)
            {
                WriteJson(res, 400, RevitMcpException.CreateErrorResponse("INVALID_ARGUMENT", $"Invalid JSON body: {ex.Message}"));
                return;
            }

            var action = path["/api/".Length..].TrimEnd('/');
            if (string.IsNullOrWhiteSpace(action))
            {
                WriteJson(res, 400, RevitMcpException.CreateErrorResponse("INVALID_ARGUMENT", "Missing Revit action in /api/<action>."));
                return;
            }

            if (!SafeActionPattern.IsMatch(action))
            {
                WriteJson(res, 400, RevitMcpException.CreateErrorResponse("INVALID_ARGUMENT", "Invalid Revit action. Expected lowercase path segments like 'model/info'."));
                return;
            }

            if (!RevitEventHandler.RequestGate.Wait(QueueTimeout))
            {
                WriteJson(res, 429, RevitMcpException.CreateErrorResponse("TIMEOUT", "RevitMCP is busy. Try again in a few seconds."));
                return;
            }

            try
            {
                // Dispatch on Revit thread and wait for result.
                var request = _eventHandler.SetRequest(action, payload);
                _externalEvent.Raise();

                if (!RevitEventHandler.WaitForResult(request, RevitApiTimeout))
                {
                    WriteJson(res, 504, RevitMcpException.CreateErrorResponse("TIMEOUT", $"Revit API timeout after {RevitApiTimeout.TotalSeconds:0} s"));
                    return;
                }

                var (result, error) = RevitEventHandler.GetResult(request);
                if (result != null)
                {
                    WriteJson(res, 200, result);
                }
                else if (error != null)
                {
                    WriteJson(res, 200, RevitMcpException.CreateErrorResponse("REVIT_API_ERROR", error));
                }
                else
                {
                    WriteJson(res, 200, RevitMcpException.CreateErrorResponse("REVIT_API_ERROR", "Unknown error during Revit execution."));
                }
            }
            finally
            {
                RevitEventHandler.RequestGate.Release();
            }
        }
        catch (Exception ex)
        {
            WriteJson(res, 500, RevitMcpException.FromException(ex));
        }
    }

    private static void WriteJson(HttpListenerResponse res, int status, object data)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        var bytes = Encoding.UTF8.GetBytes(json);
        res.StatusCode = status;
        res.ContentType = "application/json";
        res.ContentLength64 = bytes.Length;
        res.OutputStream.Write(bytes, 0, bytes.Length);
        res.OutputStream.Close();
    }
}
