import os
import re

# 1. Update App.cs
with open('revit-addin/RevitMCP/App.cs', 'r', encoding='utf-8') as f:
    app_cs = f.read()

# Replace TaskDialog blocks in App.cs
app_cs_new = re.sub(
    r'var serverStarted = _server\.Start\(\);[\s\S]*?return Result\.Succeeded;\s*\}\s*catch\s*\(Exception ex\)\s*\{[\s\S]*?return Result\.Succeeded;\s*\}',
    '''var serverStarted = _server.Start();
            if (serverStarted)
            {
                ConfigManager.LogAudit("STARTUP", $"Serveur HTTP démarré avec succès sur {_server.ActivePrefix} (PID: {Environment.ProcessId})");
            }
            else
            {
                ConfigManager.LogAudit("STARTUP", $"Serveur HTTP non démarré sur ports 6543-6555, Named Pipe IPC actif (PID: {Environment.ProcessId})");
            }

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            ConfigManager.LogAudit("STARTUP_ERROR", $"Erreur silencieuse lors de l'initialisation de RevitMCP : {ex.Message}");
            return Result.Succeeded;
        }''',
    app_cs
)

with open('revit-addin/RevitMCP/App.cs', 'w', encoding='utf-8') as f:
    f.write(app_cs_new)
print("App.cs updated successfully.")

# 2. Update RevitHttpServer.cs
with open('revit-addin/RevitMCP/RevitHttpServer.cs', 'r', encoding='utf-8') as f:
    http_cs = f.read()

# Replace Prefix and listener initialization
http_cs = http_cs.replace(
    '    private const string Prefix = "http://localhost:6543/";',
    '''    public int BoundPort { get; private set; } = 0;
    public string ActivePrefix => BoundPort > 0 ? $"http://localhost:{BoundPort}/" : "";'''
)

http_cs = http_cs.replace(
    '    private readonly HttpListener _listener = new();',
    '    private HttpListener? _listener;'
)

# Replace Start() and Stop()
old_start_stop = re.search(r'    public bool Start\(\)[\s\S]*?    private void Listen\(\)', http_cs)
if old_start_stop:
    new_start_stop = '''    public bool Start()
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

    private void Listen()'''
    http_cs = http_cs[:old_start_stop.start()] + new_start_stop + http_cs[old_start_stop.end() - len('    private void Listen()'):]

# Also ensure Listen checks _listener != null && _listener.IsListening
http_cs = http_cs.replace(
    '        while (_running)\n        {\n            HttpListenerContext ctx;\n            try { ctx = _listener.GetContext(); }',
    '        while (_running && _listener != null && _listener.IsListening)\n        {\n            HttpListenerContext ctx;\n            try { ctx = _listener.GetContext(); }'
)

with open('revit-addin/RevitMCP/RevitHttpServer.cs', 'w', encoding='utf-8') as f:
    f.write(http_cs)
print("RevitHttpServer.cs updated successfully.")

# 3. Update RevitNamedPipeServer.cs
with open('revit-addin/RevitMCP/RevitNamedPipeServer.cs', 'r', encoding='utf-8') as f:
    pipe_cs = f.read()

# Replace Start and ListenLoop
old_pipe_start = re.search(r'    public void Start\(\)[\s\S]*?    private void ListenLoop\(\)', pipe_cs)
if old_pipe_start:
    new_pipe_start = '''    public void Start()
    {
        _running = true;
        _listenerThread = new Thread(() => ListenLoop(PipeName)) { IsBackground = true, Name = "RevitMCP_NamedPipeServer" };
        _listenerThread.Start();

        var specificPipe = $"{PipeName}_{Environment.ProcessId}";
        var specificThread = new Thread(() => ListenLoop(specificPipe)) { IsBackground = true, Name = $"RevitMCP_NamedPipeServer_{Environment.ProcessId}" };
        specificThread.Start();

        ConfigManager.LogAudit("PIPE_SERVER", $"Named Pipe IPC servers started on \\\\\\\\.\\\\pipe\\\\{PipeName} and \\\\\\\\.\\\\pipe\\\\{specificPipe}");
    }

    public void Stop()
    {
        _running = false;
        try
        {
            _cts.Cancel();
        }
        catch { }
    }

    private void ListenLoop(string targetPipeName)'''
    
    # Also adjust ListenLoop to use targetPipeName instead of PipeName in NamedPipeServerStreamAcl.Create
    pipe_cs = pipe_cs[:old_pipe_start.start()] + new_pipe_start + pipe_cs[old_pipe_start.end() - len('    private void ListenLoop()'):]
    pipe_cs = pipe_cs.replace(
        '                var pipeServer = NamedPipeServerStreamAcl.Create(\n                    PipeName,',
        '                var pipeServer = NamedPipeServerStreamAcl.Create(\n                    targetPipeName,'
    )
    pipe_cs = pipe_cs.replace(
        'ConfigManager.LogAudit("PIPE_ERROR", $"Named Pipe server loop error: {ex.Message}");',
        'ConfigManager.LogAudit("PIPE_ERROR", $"Named Pipe ({targetPipeName}) server loop error: {ex.Message}");'
    )

with open('revit-addin/RevitMCP/RevitNamedPipeServer.cs', 'w', encoding='utf-8') as f:
    f.write(pipe_cs)
print("RevitNamedPipeServer.cs updated successfully.")
