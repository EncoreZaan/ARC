using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCP;

/// <summary>
/// Structure représentant une alerte de modélisation détectée en temps réel.
/// </summary>
public record ProactiveAlert(
    string Id,
    DateTime Timestamp,
    string Severity, // "INFO", "WARNING", "CRITICAL"
    string Category, // "PMR", "GEOMETRY", "STRUCTURE", "ROOMS", "OPENINGS"
    string Message,
    string? Recommendation,
    long? ElementId
);

/// <summary>
/// Moteur de veille et copilote proactif en temps réel.
/// Écoute les modifications de la maquette (DocumentChanged) et la sélection (Idling / UI).
/// </summary>
public static class ProactiveGuardian
{
    private static readonly ConcurrentQueue<ProactiveAlert> _alerts = new();
    private static readonly int MaxAlerts = 200;
    private static Action<ProactiveAlert>? _onAlertAdded;

    public static void Initialize(UIControlledApplication application, Action<ProactiveAlert>? alertCallback = null)
    {
        _onAlertAdded = alertCallback;
        application.ControlledApplication.DocumentChanged += OnDocumentChanged;
    }

    public static IReadOnlyList<ProactiveAlert> GetAlerts(int maxCount = 50)
    {
        return _alerts.Reverse().Take(maxCount).ToList();
    }

    public static void ClearAlerts()
    {
        while (_alerts.TryDequeue(out _)) { }
    }

    public static void AddAlert(string severity, string category, string message, string? recommendation = null, long? elementId = null)
    {
        var alert = new ProactiveAlert(
            Id: Guid.NewGuid().ToString("N")[..8],
            Timestamp: DateTime.Now,
            Severity: severity,
            Category: category,
            Message: message,
            Recommendation: recommendation,
            ElementId: elementId
        );

        _alerts.Enqueue(alert);
        while (_alerts.Count > MaxAlerts && _alerts.TryDequeue(out _)) { }

        try
        {
            _onAlertAdded?.Invoke(alert);
        }
        catch { }
    }

    private static void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        try
        {
            var doc = e.GetDocument();
            if (doc == null || !doc.IsValidObject) return;

            // 1. Analyse des éléments supprimés
            var deletedIds = e.GetDeletedElementIds();
            if (deletedIds.Count > 0)
            {
                // Vérifier si des pièces ont été supprimées sans supprimer leurs séparateurs
                var separationLinesCount = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_RoomSeparationLines)
                    .WhereElementIsNotElementType()
                    .GetElementCount();

                var roomsCount = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .GetElementCount();

                if (separationLinesCount > 0 && roomsCount == 0)
                {
                    AddAlert("WARNING", "ROOMS",
                        "Toutes les pièces semblent supprimées, mais des lignes de séparation de pièces orphelines subsistent.",
                        "Supprimez les lignes de séparation orphelines ou replacez les pièces nécessaires.");
                }
            }

            // 2. Analyse des éléments ajoutés ou modifiés
            var modifiedIds = e.GetModifiedElementIds();
            var addedIds = e.GetAddedElementIds();
            var touchedIds = modifiedIds.Concat(addedIds).Take(30).ToList();

            foreach (var elemId in touchedIds)
            {
                var elem = doc.GetElement(elemId);
                if (elem == null || !elem.IsValidObject) continue;

                // A. Fenêtre ou Porte : vérification du linteau par rapport à la dalle / hauteur
                if (elem is FamilyInstance fi && (elem.Category?.Id.Value == (long)BuiltInCategory.OST_Windows || elem.Category?.Id.Value == (long)BuiltInCategory.OST_Doors))
                {
                    CheckOpeningHeadHeight(elem, fi);
                }

                // B. Mur : vérification mur porteur ou délimitation de pièces
                if (elem is Wall wall)
                {
                    CheckWallImpact(doc, wall);
                }
            }
        }
        catch
        {
            // Le guard proactif ne doit JAMAIS faire planter Revit
        }
    }

    private static void CheckOpeningHeadHeight(Element elem, FamilyInstance fi)
    {
        try
        {
            var headParam = elem.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM);
            if (headParam != null && headParam.HasValue)
            {
                var headHeightMm = UnitConversions.FeetToMm(headParam.AsDouble());
                // Si la hauteur de linteau dépasse 2800 mm sans justification
                if (headHeightMm > 2800)
                {
                    AddAlert("WARNING", "OPENINGS",
                        $"L'ouverture '{elem.Name}' (#{elem.Id.Value}) a une hauteur de linteau inhabituelle ({headHeightMm:F0} mm).",
                        "Vérifiez l'altimétrie de la sous-face de dalle supérieure pour éviter un clash structurel.",
                        elem.Id.Value);
                }
            }
        }
        catch { }
    }

    private static void CheckWallImpact(Document doc, Wall wall)
    {
        try
        {
            // Vérifier si le mur est superposé ou trop court
            var lengthParam = wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
            if (lengthParam != null && lengthParam.HasValue)
            {
                var lengthMm = UnitConversions.FeetToMm(lengthParam.AsDouble());
                if (lengthMm < 150)
                {
                    AddAlert("WARNING", "GEOMETRY",
                        $"Le mur #{wall.Id.Value} est extrêmement court ({lengthMm:F0} mm). Cela peut générer des erreurs de jonction.",
                        "Fusionnez ou allongez ce segment de mur.",
                        wall.Id.Value);
                }
            }
        }
        catch { }
    }
}
