using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitMCP;

/// <summary>
/// High-performance Windows Named Pipe Server providing zero-latency IPC between
/// the native Rust revit-mcp-server and RevitMCP on the local machine.
/// Bypasses HTTP.sys and Windows network stack entirely for sub-millisecond execution.
/// </summary>
public class RevitNamedPipeServer
{
    private const string PipeName = "revit_mcp_ipc";
    private readonly RevitEventHandler _eventHandler;
    private readonly ExternalEvent _externalEvent;
    private readonly CancellationTokenSource _cts = new();
    private Thread? _listenerThread;
    private bool _running;

    public RevitNamedPipeServer(UIControlledApplication uiApp, RevitEventHandler eventHandler, ExternalEvent externalEvent)
    {
        _eventHandler = eventHandler;
        _externalEvent = externalEvent;
    }

    public void Start()
    {
        _running = true;
        _listenerThread = new Thread(ListenLoop) { IsBackground = true, Name = "RevitMCP_NamedPipeServer" };
        _listenerThread.Start();
        ConfigManager.LogAudit("PIPE_SERVER", $"Named Pipe IPC server started on \\\\.\\pipe\\{PipeName}");
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

    private void ListenLoop()
    {
        while (_running && !_cts.IsCancellationRequested)
        {
            try
            {
                var pipeSecurity = new PipeSecurity();
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    WindowsIdentity.GetCurrent().User!,
                    PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
                    AccessControlType.Allow));

                var pipeServer = NamedPipeServerStreamAcl.Create(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    inBufferSize: 1024 * 1024,
                    outBufferSize: 1024 * 1024,
                    pipeSecurity);

                pipeServer.WaitForConnection();

                var connectedPipe = pipeServer;
                ThreadPool.QueueUserWorkItem(_ => ProcessClient(connectedPipe));
            }
            catch (Exception ex)
            {
                if (!_running) break;
                ConfigManager.LogAudit("PIPE_ERROR", $"Named Pipe server loop error: {ex.Message}");
                Thread.Sleep(500);
            }
        }
    }

    private void ProcessClient(NamedPipeServerStream pipe)
    {
        try
        {
            using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 65536, leaveOpen: true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), bufferSize: 65536, leaveOpen: true) { AutoFlush = true };

            while (pipe.IsConnected && _running)
            {
                string? line;
                try
                {
                    line = reader.ReadLine();
                }
                catch (IOException)
                {
                    break;
                }

                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                JObject requestJson;
                try
                {
                    requestJson = JObject.Parse(line);
                }
                catch (Exception ex)
                {
                    var errResp = RevitMcpException.CreateErrorResponse("INVALID_ARGUMENT", $"Invalid JSON: {ex.Message}");
                    try { writer.WriteLine(errResp.ToString(Formatting.None)); } catch { break; }
                    continue;
                }

                var action = requestJson["action"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(action))
                {
                    var errResp = RevitMcpException.CreateErrorResponse("INVALID_ARGUMENT", "Missing 'action' in IPC request.");
                    try { writer.WriteLine(errResp.ToString(Formatting.None)); } catch { break; }
                    continue;
                }

                if (action == "healthz")
                {
                    var healthObj = new JObject
                    {
                        ["ok"] = true,
                        ["service"] = "RevitMCP",
                        ["version"] = "2.3.0",
                        ["transport"] = "named_pipe",
                        ["openDocuments"] = JArray.FromObject(OpenDocumentsCache.GetList())
                    };
                    try { writer.WriteLine(healthObj.ToString(Formatting.None)); } catch { break; }
                    continue;
                }

                if (!RevitEventHandler.RequestGate.Wait(TimeSpan.FromSeconds(10)))
                {
                    var busyResp = RevitMcpException.CreateErrorResponse("TIMEOUT", "RevitMCP is busy. Try again in a few seconds.");
                    try { writer.WriteLine(busyResp.ToString(Formatting.None)); } catch { break; }
                    continue;
                }

                RevitEventHandler.Request req;
                try
                {
                    req = _eventHandler.SetRequest(action, requestJson);
                    _externalEvent.Raise();

                    if (!RevitEventHandler.WaitForResult(req, TimeSpan.FromSeconds(120)))
                    {
                        var timeoutResp = RevitMcpException.CreateErrorResponse("TIMEOUT", "Revit API execution timed out.");
                        try { writer.WriteLine(timeoutResp.ToString(Formatting.None)); } catch { break; }
                        continue;
                    }
                }
                finally
                {
                    RevitEventHandler.RequestGate.Release();
                }

                var (result, error) = RevitEventHandler.GetResult(req);
                JObject responseObj;
                if (result != null)
                {
                    responseObj = result;
                    if (responseObj["success"] == null)
                        responseObj["success"] = true;
                }
                else if (error != null)
                {
                    responseObj = RevitMcpException.CreateErrorResponse("REVIT_API_ERROR", error);
                }
                else
                {
                    responseObj = RevitMcpException.CreateErrorResponse("REVIT_API_ERROR", "Unknown error during Revit execution.");
                }

                try
                {
                    writer.WriteLine(responseObj.ToString(Formatting.None));
                }
                catch (IOException)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            ConfigManager.LogAudit("PIPE_CLIENT_EXCEPTION", ex.Message);
        }
        finally
        {
            try { pipe.Dispose(); } catch { }
        }
    }
}
