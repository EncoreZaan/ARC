using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using RevitMCP.UI;
using System;
using System.Linq;

namespace RevitMCP;

/// <summary>
/// Écoute en arrière-plan la sélection active de l'utilisateur dans Revit (via Idling)
/// et synchronise en direct les détails dans le panneau Revit AI Copilot.
/// </summary>
public static class ActiveSelectionTracker
{
    private static RevitAiDockablePanel? _panel;
    private static long _lastSelectedId = -1;

    public static void Initialize(UIControlledApplication application, RevitAiDockablePanel panel)
    {
        _panel = panel;
        application.Idling += OnIdling;
    }

    private static void OnIdling(object? sender, IdlingEventArgs e)
    {
        if (sender is not UIApplication uiApp) return;

        try
        {
            var uiDoc = uiApp.ActiveUIDocument;
            if (uiDoc == null || uiDoc.Document == null)
            {
                if (_lastSelectedId != -1)
                {
                    _lastSelectedId = -1;
                    _panel?.UpdateActiveSelection(null, null);
                }
                return;
            }

            var selection = uiDoc.Selection.GetElementIds();
            var firstId = selection.FirstOrDefault();

            var currentIdVal = firstId != null ? firstId.Value : -1;
            if (currentIdVal != _lastSelectedId)
            {
                _lastSelectedId = currentIdVal;
                var elem = firstId != null ? uiDoc.Document.GetElement(firstId) : null;
                _panel?.UpdateActiveSelection(elem, uiDoc.Document);
            }
        }
        catch
        {
            // Silencieux pour ne pas impacter les performances de l'inactivité Revit
        }
    }
}
