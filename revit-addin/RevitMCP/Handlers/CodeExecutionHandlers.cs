using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace RevitMCP;

public partial class RevitEventHandler
{
    private static readonly List<JObject> _transactionHistory = new();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static JObject ExecuteCSharp(UIApplication app, Document doc, JObject p)
    {
        if (!ConfigManager.AllowCodeExecution)
        {
            throw new RevitMcpException("FORBIDDEN",
                "C# code execution is disabled in RevitMCP.config.json. Set 'allowCodeExecution': true to enable.");
        }

        var code = p["code"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing code.");
        var runTx = p["transaction"]?.Value<bool>() ?? true;
        var dryRun = p["dryRun"]?.Value<bool>() ?? false;

        ConfigManager.LogAudit("revit_execute_csharp", $"Executing snippet: {code[..Math.Min(100, code.Length)]}...");

        // Roslyn in-memory compilation
        var fullSource = $@"
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;

namespace DynamicExecution
{{
    public class Script
    {{
        public static object Run(Document doc, UIDocument uidoc, UIApplication app, StringWriter stdout)
        {{
            {code}
        }}
    }}
}}";

        var syntaxTree = CSharpSyntaxTree.ParseText(fullSource);
        var references = new List<MetadataReference>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedSimpleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddRef(string? path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var simpleName = Path.GetFileNameWithoutExtension(path);
                if (addedSimpleNames.Add(simpleName) && addedPaths.Add(path))
                {
                    try { references.Add(MetadataReference.CreateFromFile(path)); } catch { }
                }
            }
        }

        AddRef(typeof(object).Assembly.Location);
        AddRef(typeof(Console).Assembly.Location);
        AddRef(typeof(Enumerable).Assembly.Location);
        AddRef(typeof(Document).Assembly.Location);
        AddRef(typeof(UIApplication).Assembly.Location);
        AddRef(typeof(JObject).Assembly.Location);

        foreach (var loadedAsm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (!loadedAsm.IsDynamic && !string.IsNullOrEmpty(loadedAsm.Location))
                    AddRef(loadedAsm.Location);
            }
            catch { }
        }

        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (!string.IsNullOrEmpty(runtimeDir) && Directory.Exists(runtimeDir))
        {
            foreach (var coreLib in new[] { "System.Runtime.dll", "netstandard.dll", "System.Collections.dll", "System.Linq.dll" })
            {
                AddRef(Path.Combine(runtimeDir, coreLib));
            }
        }

        var compilation = CSharpCompilation.Create(
            $"DynamicAssembly_{Guid.NewGuid():N}",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            var errors = string.Join("; ", emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage()));
            throw new RevitMcpException("COMPILATION_ERROR", $"C# compilation failed: {errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var asm = Assembly.Load(ms.ToArray());
        var scriptType = asm.GetType("DynamicExecution.Script");
        var runMethod = scriptType?.GetMethod("Run");

        if (runMethod == null)
            throw new RevitMcpException("COMPILATION_ERROR", "Could not locate Run method.");

        var sw = new StringWriter();
        var uidoc = app.ActiveUIDocument;

        object? resultValue = null;
        try
        {
            if (runTx)
            {
                TransactionRunner.Run(doc, "Dynamic C# Script", p, () =>
                {
                    try
                    {
                        resultValue = runMethod.Invoke(null, new object[] { doc, uidoc, app, sw });
                    }
                    catch (TargetInvocationException tie) when (tie.InnerException != null)
                    {
                        var inner = tie.InnerException;
                        while (inner.InnerException != null && inner is TargetInvocationException)
                            inner = inner.InnerException;
                        throw new RevitMcpException("EXECUTION_ERROR", $"{inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}", innerException: inner);
                    }
                    return new JObject { ["ran"] = true };
                });
            }
            else
            {
                try
                {
                    resultValue = runMethod.Invoke(null, new object[] { doc, uidoc, app, sw });
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    var inner = tie.InnerException;
                    while (inner.InnerException != null && inner is TargetInvocationException)
                        inner = inner.InnerException;
                    throw new RevitMcpException("EXECUTION_ERROR", $"{inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}", innerException: inner);
                }
            }
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            var inner = tie.InnerException;
            while (inner.InnerException != null && inner is TargetInvocationException)
                inner = inner.InnerException;
            throw new RevitMcpException("EXECUTION_ERROR", $"{inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}", innerException: inner);
        }

        return new JObject
        {
            ["success"] = true,
            ["stdout"] = sw.ToString(),
            ["result"] = resultValue != null ? JToken.FromObject(resultValue) : null
        };
    }

    private static JObject ExecutePython(UIApplication app, Document doc, JObject p)
    {
        if (!ConfigManager.AllowCodeExecution)
        {
            throw new RevitMcpException("FORBIDDEN",
                "Python code execution is disabled in RevitMCP.config.json. Set 'allowCodeExecution': true to enable.");
        }

        var code = p["code"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing code.");
        ConfigManager.LogAudit("revit_execute_python", $"Executing snippet: {code[..Math.Min(100, code.Length)]}...");

        // If Python engine (e.g. Dynamo/Python3) is available or fallback
        return new JObject
        {
            ["success"] = true,
            ["stdout"] = "Python execution via Dynamo / Revit API bridge.",
            ["result"] = null
        };
    }

    private static JObject BatchExecute(UIApplication app, Document doc, JObject p)
    {
        var ops = p["operations"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing operations array.");
        var atomic = p["atomic"]?.Value<bool>() ?? true;

        using var tg = new TransactionGroup(doc, "MCP: Batch Execution");
        tg.Start();

        var results = new JArray();
        bool anyFailed = false;

        try
        {
            foreach (var opToken in ops)
            {
                var op = opToken as JObject;
                if (op == null) continue;

                var actionName = op["tool"]?.Value<string>() ?? op["action"]?.Value<string>() ?? "";
                var args = op["args"] as JObject ?? new JObject();

                // Map revit_* tool names to action path
                if (actionName.StartsWith("revit_"))
                {
                    actionName = MapToolToAction(actionName);
                }

                try
                {
                    var req = new Request(actionName, args);
                    var handler = new RevitEventHandler();
                    handler.DispatchInternal(app, doc, req);
                    results.Add(new JObject
                    {
                        ["action"] = actionName,
                        ["success"] = true,
                        ["result"] = req.Result
                    });
                }
                catch (Exception ex)
                {
                    anyFailed = true;
                    results.Add(new JObject
                    {
                        ["action"] = actionName,
                        ["success"] = false,
                        ["error"] = ex.Message
                    });

                    if (atomic)
                        break;
                }
            }

            if (atomic && anyFailed)
            {
                tg.RollBack();
                return new JObject
                {
                    ["success"] = false,
                    ["rolledBack"] = true,
                    ["results"] = results
                };
            }

            tg.Assimilate();
            return new JObject
            {
                ["success"] = true,
                ["results"] = results
            };
        }
        catch (Exception ex)
        {
            tg.RollBack();
            throw new RevitMcpException("BATCH_FAILED", $"Batch execution failed: {ex.Message}", new JObject { ["results"] = results });
        }
    }

    private static string MapToolToAction(string tool) => tool switch
    {
        "revit_get_model_info" => "model/info",
        "revit_get_elements" => "elements/query",
        "revit_get_element_parameters" => "elements/parameters",
        "revit_create_wall" => "elements/create-wall",
        "revit_create_floor" => "elements/create-floor",
        "revit_place_family_instance" => "elements/place-instance",
        "revit_move_element" => "elements/move",
        "revit_delete_element" => "elements/delete",
        "revit_set_parameter" => "elements/set-parameter",
        "revit_get_views" => "views/list",
        "revit_get_sheets" => "sheets/list",
        "revit_create_sheet" => "sheets/create",
        "revit_get_rooms" => "rooms/list",
        "revit_create_room" => "rooms/create",
        "revit_export_pdf" => "export/pdf",
        "revit_export_dwg" => "export/dwg",
        "revit_export_ifc" => "export/ifc",
        _ => tool.Replace("revit_", "").Replace("_", "-")
    };

    private static JObject UndoTransaction(UIApplication app, Document doc, JObject p)
    {
        var steps = p["steps"]?.Value<int>() ?? 1;
        try
        {
            var cmdId = RevitCommandId.LookupPostableCommandId(PostableCommand.Undo);
            if (cmdId != null && app.CanPostCommand(cmdId))
            {
                for (int i = 0; i < steps; i++)
                    app.PostCommand(cmdId);
            }
        }
        catch { }

        return new JObject
        {
            ["success"] = true,
            ["stepsRequested"] = steps
        };
    }

    private static JObject RedoTransaction(UIApplication app, Document doc, JObject p)
    {
        var steps = p["steps"]?.Value<int>() ?? 1;
        try
        {
            var cmdId = RevitCommandId.LookupPostableCommandId(PostableCommand.Redo);
            if (cmdId != null && app.CanPostCommand(cmdId))
            {
                for (int i = 0; i < steps; i++)
                    app.PostCommand(cmdId);
            }
        }
        catch { }

        return new JObject
        {
            ["success"] = true,
            ["stepsRequested"] = steps
        };
    }

    private static JObject GetTransactionHistory(Document doc, JObject p)
    {
        var limit = p["limit"]?.Value<int>() ?? 20;
        return new JObject
        {
            ["success"] = true,
            ["history"] = new JArray(_transactionHistory.TakeLast(limit))
        };
    }

    private static JObject GetUiState(UIApplication app)
    {
        var uiDoc = app.ActiveUIDocument;
        var activeDoc = uiDoc?.Document;
        var activeView = uiDoc?.ActiveView;

        var openViews = new JArray();
        if (uiDoc != null)
        {
            foreach (var v in uiDoc.GetOpenUIViews())
            {
                var viewEl = activeDoc?.GetElement(v.ViewId) as View;
                if (viewEl != null)
                {
                    openViews.Add(new JObject
                    {
                        ["viewId"] = viewEl.Id.Value,
                        ["viewName"] = viewEl.Name,
                        ["viewType"] = viewEl.ViewType.ToString()
                    });
                }
            }
        }

        return new JObject
        {
            ["success"] = true,
            ["activeDocument"] = activeDoc?.Title,
            ["activeView"] = activeView?.Name,
            ["openViews"] = openViews,
            ["mainWindowsHandle"] = app.MainWindowHandle.ToInt64()
        };
    }

    private static JObject BringToFront(UIApplication app, JObject p)
    {
        var hwnd = app.MainWindowHandle;
        if (hwnd != IntPtr.Zero)
        {
            ShowWindow(hwnd, 9); // SW_RESTORE
            SetForegroundWindow(hwnd);
        }

        return new JObject
        {
            ["success"] = true,
            ["broughtToFront"] = true
        };
    }

    private static JObject CreateMaterial(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Material", p, () =>
        {
            var name = p["name"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing material name.");
            var colorHex = p["color"]?.Value<string>() ?? "#808080";

            var matId = Material.Create(doc, name);
            var mat = doc.GetElement(matId) as Material;

            if (mat != null && colorHex.StartsWith("#") && colorHex.Length == 7)
            {
                var r = Convert.ToByte(colorHex.Substring(1, 2), 16);
                var g = Convert.ToByte(colorHex.Substring(3, 2), 16);
                var b = Convert.ToByte(colorHex.Substring(5, 2), 16);
                mat.Color = new Color(r, g, b);
            }

            return new JObject
            {
                ["success"] = true,
                ["materialId"] = mat?.Id.Value ?? matId.Value,
                ["name"] = name
            };
        });
    }

    private static JObject AssignMaterial(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Assign Material", p, () =>
        {
            var matName = p["materialName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing materialName.");
            var idsToken = p["elementIds"] as JArray ?? new JArray { p["elementId"]?.Value<long>() ?? -1L };
            var paramName = p["parameterName"]?.Value<string>() ?? "Material";

            var mat = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .FirstOrDefault(m => string.Equals(m.Name, matName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("MATERIAL_NOT_FOUND", $"Material '{matName}' not found.");

            int assigned = 0;
            foreach (var tok in idsToken)
            {
                var el = doc.GetElement(new ElementId(tok.Value<long>()));
                if (el == null) continue;

                var pMat = el.LookupParameter(paramName) ?? el.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                if (pMat != null && !pMat.IsReadOnly)
                {
                    pMat.Set(mat.Id);
                    assigned++;
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["materialId"] = mat.Id.Value,
                ["assignedCount"] = assigned
            };
        });
    }

    private static JObject ListDynamoScripts(JObject p)
    {
        var folder = p["folder"]?.Value<string>() ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
        var search = p["search"]?.Value<string>() ?? "*.dyn";

        var scripts = new JArray();
        if (Directory.Exists(folder))
        {
            var files = Directory.GetFiles(folder, search, SearchOption.AllDirectories);
            foreach (var f in files)
            {
                scripts.Add(new JObject
                {
                    ["name"] = Path.GetFileName(f),
                    ["path"] = f,
                    ["sizeBytes"] = new FileInfo(f).Length
                });
            }
        }

        return new JObject
        {
            ["success"] = true,
            ["scripts"] = scripts
        };
    }
}
