using Autodesk.Revit.UI;
using RevitMCP.Commands;
using RevitMCP.UI;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RevitMCP;

/// <summary>
/// Point d'entrée IExternalApplication de RevitMCP.
/// Initialise :
/// 1. Le panneau latéral ancrable WPF (Dockable Panel Revit AI Copilot)
/// 2. L'onglet Ruban "Revit AI Copilot" et ses boutons d'action
/// 3. Le copilote proactif en temps réel (ProactiveGuardian)
/// 4. Le tracker de sélection active (ActiveSelectionTracker)
/// 5. Le serveur HTTP embarqué (pont avec revit-mcp-server en Rust)
/// </summary>
public class App : IExternalApplication
{
    private RevitHttpServer? _server;
    internal static UIControlledApplication? UiApp;
    internal static RevitAiDockablePanel? DockablePanel;

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
            // 1. Initialisation et Enregistrement du Dockable Panel WPF
            DockablePanel = new RevitAiDockablePanel(application);
            application.RegisterDockablePane(RevitAiDockablePanel.PaneId, "ARC — Architecture Copilot", DockablePanel);

            // 2. Création du Ruban Revit ("Revit AI Copilot")
            CreateRibbonTabAndPanel(application);

            // 3. Initialisation du Copilote Proactif (DocumentChanged)
            LiveDeltaMonitor.Initialize(application);
            ProactiveGuardian.Initialize(application, alert =>
            {
                DockablePanel?.AddProactiveAlert(alert);
            });

            // 4. Initialisation du Tracker de Sélection Active (Idling)
            ActiveSelectionTracker.Initialize(application, DockablePanel);

            // 5. Initialisation du cache des documents et Démarrage du serveur HTTP
            OpenDocumentsCache.Initialize(application);
            _server = new RevitHttpServer(application);
            var serverStarted = _server.Start();
            if (!serverStarted)
            {
                ConfigManager.LogAudit("STARTUP", "Serveur HTTP non démarré (le port 6543 est déjà utilisé par une autre instance Revit ou un processus tiers).");
            }

            if (Environment.GetEnvironmentVariable("REVIT_MCP_SHOW_STARTUP_DIALOG") == "1")
                TaskDialog.Show("ARC", "ARC Architecture Copilot bridge started on http://localhost:6543");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            ConfigManager.LogAudit("STARTUP_ERROR", $"Erreur lors de l'initialisation de RevitMCP : {ex}");
            if (Environment.GetEnvironmentVariable("REVIT_MCP_SHOW_ERROR_DIALOG") == "1")
            {
                TaskDialog.Show("ARC", $"Échec du démarrage de ARC sur http://localhost:6543\n\n{ex.Message}");
            }
            return Result.Succeeded;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        _server?.Stop();
        return Result.Succeeded;
    }

    private static void CreateRibbonTabAndPanel(UIControlledApplication application)
    {
        const string tabName = "ARC";
        try
        {
            application.CreateRibbonTab(tabName);
        }
        catch
        {
            // Onglet déjà existant
        }

        RibbonPanel? panel = null;
        try
        {
            panel = application.CreateRibbonPanel(tabName, "ARC Copilote & Audits");
        }
        catch
        {
            panel = application.GetRibbonPanels(tabName).FirstOrDefault(p => p.Name == "ARC Copilote & Audits");
        }

        if (panel == null) return;

        var asmLocation = Assembly.GetExecutingAssembly().Location;

        var btnTogglePanel = new PushButtonData(
            "BtnToggleAiPanel",
            "Panneau\nCopilote",
            asmLocation,
            typeof(ToggleAiPanelCommand).FullName);
        btnTogglePanel.ToolTip = "Affiche ou masque le panneau latéral interactif Revit AI Copilot.";

        var btnAuditPmr = new PushButtonData(
            "BtnAuditPmr",
            "Audit PMR\n1-Clic",
            asmLocation,
            typeof(RunPmrAuditCommand).FullName);
        btnAuditPmr.ToolTip = "Lance l'audit d'accessibilité PMR (largeurs utiles ≥ 83cm, aire Ø1.50m, pentes).";

        var btnScreenshot = new PushButtonData(
            "BtnScreenshotActive",
            "Capture Vue\nInstantanée",
            asmLocation,
            typeof(ScreenshotActiveViewCommand).FullName);
        btnScreenshot.ToolTip = "Capture instantanément la vue active pour transmission multimodale à l'IA.";

        var btnBridgeStatus = new PushButtonData(
            "BtnBridgeStatus",
            "Statut\nPont IA",
            asmLocation,
            typeof(CheckBridgeStatusCommand).FullName);
        btnBridgeStatus.ToolTip = "Vérifie l'état de la connexion locale avec le serveur MCP Rust natif.";

        panel.AddItem(btnTogglePanel);
        panel.AddItem(btnAuditPmr);
        panel.AddItem(btnScreenshot);
        panel.AddItem(btnBridgeStatus);
    }
}
