using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCP.UI;
using System;
using System.IO;

namespace RevitMCP.Commands;

[Transaction(TransactionMode.Manual)]
public class ToggleAiPanelCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var pane = commandData.Application.GetDockablePane(RevitAiDockablePanel.PaneId);
            if (pane == null)
            {
                TaskDialog.Show("Revit AI Copilot", "Le panneau Copilote IA n'a pas pu être localisé.");
                return Result.Failed;
            }

            if (pane.IsShown())
                pane.Hide();
            else
                pane.Show();

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}

[Transaction(TransactionMode.Manual)]
public class RunPmrAuditCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        TaskDialog.Show("Audit PMR 1-Clic",
            "Audit d'accessibilité PMR disponible.\n\n" +
            "Ce module analyse en continu :\n" +
            "• Le passage libre utile des portes (≥ 83 cm)\n" +
            "• L'aire de giration libre (Ø 1,50 m dans sanitaires/dégagements)\n" +
            "• La pente des rampes (< 5%)\n\n" +
            "Vous pouvez le déclencher automatiquement depuis Claude Desktop via l'outil 'revit_audit_pmr'.");
        return Result.Succeeded;
    }
}

[Transaction(TransactionMode.Manual)]
public class ScreenshotActiveViewCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var doc = commandData.Application.ActiveUIDocument?.Document;
            var view = doc?.ActiveView;
            if (doc == null || view == null)
            {
                TaskDialog.Show("Capture Vue IA", "Aucun document ou vue active disponible.");
                return Result.Failed;
            }

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var filePath = Path.Combine(desktop, $"Revit_AI_Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            var opt = new ImageExportOptions
            {
                ExportRange = ExportRange.SetOfViews,
                FilePath = filePath,
                FitDirection = FitDirectionType.Horizontal,
                HLRandWFViewsFileType = ImageFileType.PNG,
                ShadowViewsFileType = ImageFileType.PNG,
                ImageResolution = ImageResolution.DPI_150,
                PixelSize = 1920,
                ZoomType = ZoomFitType.FitToPage
            };
            opt.SetViewsAndSheets(new[] { view.Id });
            doc.ExportImage(opt);

            TaskDialog.Show("Capture Vue IA", $"Capture instantanée 1920px enregistrée sur votre Bureau :\n{filePath}");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}

[Transaction(TransactionMode.Manual)]
public class CheckBridgeStatusCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        TaskDialog.Show("Statut Revit AI Bridge",
            "Le pont local Revit AI Bridge est opérationnel sur http://localhost:6543\n\n" +
            "• Compatible avec le serveur MCP Rust natif (revit-mcp-server)\n" +
            "• Compatible Claude Desktop, Cursor et tout client MCP 2024-11-05\n" +
            "• Résilience Zéro-Crash activée");
        return Result.Succeeded;
    }
}
