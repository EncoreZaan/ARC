using System;
using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;

namespace RevitMCP;

/// <summary>
/// IExternalApplication entry point.
/// Starts the embedded HTTP server when Revit loads and stops it on shutdown.
/// </summary>
public class App : IExternalApplication
{
    private RevitHttpServer? _server;
    internal static UIControlledApplication? UiApp;

    static App()
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    var asmName = new AssemblyName(args.Name).Name;
                    var addinDir = Path.GetDirectoryName(typeof(App).Assembly.Location);
                    if (!string.IsNullOrEmpty(addinDir))
                    {
                        var candidate = Path.Combine(addinDir, asmName + ".dll");
                        if (File.Exists(candidate))
                            return Assembly.LoadFrom(candidate);
                    }
                }
                catch { }
                return null;
            };
        }
        catch { }
    }

    public Result OnStartup(UIControlledApplication application)
    {
        UiApp = application;

        try
        {
            OpenDocumentsCache.Initialize(application);
            _server = new RevitHttpServer(application);
            _server.Start();

            if (Environment.GetEnvironmentVariable("REVIT_MCP_SHOW_STARTUP_DIALOG") == "1")
                TaskDialog.Show("RevitMCP", "RevitMCP bridge started on http://localhost:6543");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("RevitMCP", $"Failed to start RevitMCP bridge on http://localhost:6543\n\n{ex.Message}");
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        _server?.Stop();
        return Result.Succeeded;
    }
}
