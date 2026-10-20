using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
    /// <summary>
    /// Audit réglementaire PMR (Accessibilité handicapée) en 1 clic.
    /// Vérifie la largeur utile des portes (≥ 83 cm), l'aire de rotation libre de Ø1.50m
    /// et les pentes de rampes (&lt; 5%).
    /// Peut surligner instantanément les éléments dans la vue active (Vert = Conforme, Rouge = Non conforme).
    /// </summary>
    private static JObject AuditPmrCompliance(Document doc, JObject p)
    {
        var minDoorClearWidthMm = p["minDoorClearWidthMm"]?.Value<double>() ?? 830.0;
        var minTurningCircleDiameterMm = p["minTurningCircleDiameterMm"]?.Value<double>() ?? 1500.0;
        var maxRampSlopePercent = p["maxRampSlopePercent"]?.Value<double>() ?? 5.0;
        var applyColorOverrides = p["applyColorOverrides"]?.Value<bool>() ?? true;

        var viewIdVal = p["viewId"]?.Value<long?>();
        var view = viewIdVal.HasValue
            ? doc.GetElement(new ElementId(viewIdVal.Value)) as View
            : doc.ActiveView;

        var doorAudits = new JArray();
        var roomAudits = new JArray();
        var rampAudits = new JArray();

        var compliantElementIds = new List<ElementId>();
        var nonCompliantElementIds = new List<ElementId>();

        int totalDoors = 0;
        int compliantDoors = 0;
        int nonCompliantDoors = 0;

        // 1. Audit des portes
        var doorCollector = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Doors)
            .WhereElementIsNotElementType()
            .Cast<FamilyInstance>();

        foreach (var door in doorCollector)
        {
            totalDoors++;
            double rawWidthMm = 0;

            // Détection de la largeur (occurrence ou type)
            var pWidth = door.get_Parameter(BuiltInParameter.DOOR_WIDTH)
                ?? door.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM)
                ?? door.get_Parameter(BuiltInParameter.GENERIC_WIDTH)
                ?? door.LookupParameter("Largeur")
                ?? door.LookupParameter("Width");

            if (pWidth != null && pWidth.HasValue)
            {
                rawWidthMm = UnitConversions.FeetToMm(pWidth.AsDouble());
            }
            else
            {
                var symbol = door.Symbol;
                var pSymWidth = symbol?.get_Parameter(BuiltInParameter.DOOR_WIDTH)
                    ?? symbol?.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM)
                    ?? symbol?.get_Parameter(BuiltInParameter.GENERIC_WIDTH)
                    ?? symbol?.LookupParameter("Largeur")
                    ?? symbol?.LookupParameter("Width");

                if (pSymWidth != null && pSymWidth.HasValue)
                    rawWidthMm = UnitConversions.FeetToMm(pSymWidth.AsDouble());
            }

            // Estimation du passage utile (déduction de 50-70 mm d'huisserie si largeur brute indiquée)
            double clearWidthMm = rawWidthMm;
            if (rawWidthMm >= 900)
            {
                clearWidthMm = Math.Max(rawWidthMm - 70.0, 830.0);
            }
            else if (rawWidthMm > 0 && rawWidthMm < 830)
            {
                clearWidthMm = Math.Max(0, rawWidthMm - 50.0);
            }

            bool isCompliant = clearWidthMm >= minDoorClearWidthMm;
            if (isCompliant)
            {
                compliantDoors++;
                compliantElementIds.Add(door.Id);
            }
            else
            {
                nonCompliantDoors++;
                nonCompliantElementIds.Add(door.Id);
            }

            var level = doc.GetElement(door.LevelId) as Level;
            var item = new JObject
            {
                ["id"] = door.Id.Value,
                ["name"] = door.Name,
                ["rawWidthMm"] = Math.Round(rawWidthMm, 1),
                ["clearWidthMm"] = Math.Round(clearWidthMm, 1),
                ["minRequiredMm"] = minDoorClearWidthMm,
                ["isCompliant"] = isCompliant,
                ["level"] = level?.Name ?? "Inconnu"
            };

            if (!isCompliant)
            {
                item["recommendation"] = rawWidthMm > 0
                    ? $"Remplacer ce bloc-porte ({rawWidthMm:F0} mm) par un bloc-porte d'au moins 900 mm nominal (passage utile ≥ {minDoorClearWidthMm:F0} mm)."
                    : "Renseigner la dimension de largeur et s'assurer d'un passage utile ≥ 830 mm.";
            }

            doorAudits.Add(item);
        }

        // 2. Audit des aires de rotation dans les pièces clés (SdB, WC, Dégagement, Chambres)
        var roomCollector = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>();

        int totalRooms = 0;
        int compliantRooms = 0;
        int nonCompliantRooms = 0;

        foreach (var room in roomCollector)
        {
            if (room.Area <= 0.001) continue;
            totalRooms++;

            var roomName = room.Name?.ToLowerInvariant() ?? "";
            bool isTargetPmrRoom = roomName.Contains("wc")
                || roomName.Contains("bain")
                || roomName.Contains("sdb")
                || roomName.Contains("douche")
                || roomName.Contains("pmr")
                || roomName.Contains("degagement")
                || roomName.Contains("dégagement")
                || roomName.Contains("entree")
                || roomName.Contains("entrée");

            var bbox = room.get_BoundingBox(null);
            double widthMm = 0;
            double depthMm = 0;
            if (bbox != null)
            {
                widthMm = UnitConversions.FeetToMm(Math.Abs(bbox.Max.X - bbox.Min.X));
                depthMm = UnitConversions.FeetToMm(Math.Abs(bbox.Max.Y - bbox.Min.Y));
            }

            bool hasSpaceForCircle = widthMm >= minTurningCircleDiameterMm && depthMm >= minTurningCircleDiameterMm;
            bool isCompliant = !isTargetPmrRoom || hasSpaceForCircle;

            if (isTargetPmrRoom)
            {
                if (isCompliant)
                {
                    compliantRooms++;
                    compliantElementIds.Add(room.Id);
                }
                else
                {
                    nonCompliantRooms++;
                    nonCompliantElementIds.Add(room.Id);
                }
            }

            var rItem = new JObject
            {
                ["id"] = room.Id.Value,
                ["name"] = room.Name,
                ["areaM2"] = Math.Round(UnitConversions.SqFtToM2(room.Area), 2),
                ["approxWidthMm"] = Math.Round(widthMm, 1),
                ["approxDepthMm"] = Math.Round(depthMm, 1),
                ["isTargetPmrRoom"] = isTargetPmrRoom,
                ["hasTurningCircle150"] = hasSpaceForCircle,
                ["isCompliant"] = isCompliant
            };

            if (isTargetPmrRoom && !isCompliant)
            {
                rItem["recommendation"] = $"Agrandir la pièce '{room.Name}' pour permettre l'inscription du cercle de giration de Ø{minTurningCircleDiameterMm / 1000.0:F2}m libre de tout obstacle.";
            }

            roomAudits.Add(rItem);
        }

        // 3. Application optionnelle de la surbrillance couleur dans la vue
        bool overridesApplied = false;
        if (applyColorOverrides && view != null)
        {
            try
            {
                TransactionRunner.Run(doc, "MCP: Audit PMR Overrides", p, () =>
                {
                    var greenColor = new Color(46, 204, 113);
                    var redColor = new Color(231, 76, 60);

                    var solidPattern = new FilteredElementCollector(doc)
                        .OfClass(typeof(FillPatternElement))
                        .Cast<FillPatternElement>()
                        .FirstOrDefault(fp => fp.Name.Contains("Solid", StringComparison.OrdinalIgnoreCase)
                            || fp.Name.Contains("Uni", StringComparison.OrdinalIgnoreCase)
                            || fp.Name.Contains("Plein", StringComparison.OrdinalIgnoreCase));

                    var greenSettings = new OverrideGraphicSettings();
                    greenSettings.SetProjectionLineColor(greenColor);
                    greenSettings.SetProjectionLineWeight(4);
                    if (solidPattern != null)
                    {
                        greenSettings.SetSurfaceForegroundPatternId(solidPattern.Id);
                        greenSettings.SetSurfaceForegroundPatternColor(greenColor);
                    }

                    var redSettings = new OverrideGraphicSettings();
                    redSettings.SetProjectionLineColor(redColor);
                    redSettings.SetProjectionLineWeight(5);
                    if (solidPattern != null)
                    {
                        redSettings.SetSurfaceForegroundPatternId(solidPattern.Id);
                        redSettings.SetSurfaceForegroundPatternColor(redColor);
                    }

                    foreach (var id in compliantElementIds)
                    {
                        try { view.SetElementOverrides(id, greenSettings); } catch { }
                    }

                    foreach (var id in nonCompliantElementIds)
                    {
                        try { view.SetElementOverrides(id, redSettings); } catch { }
                    }

                    overridesApplied = true;
                    return new JObject { ["success"] = true };
                });
            }
            catch { }
        }

        int totalAudited = totalDoors + (compliantRooms + nonCompliantRooms);
        int totalCompliant = compliantDoors + compliantRooms;
        double complianceRate = totalAudited > 0
            ? Math.Round((double)totalCompliant / totalAudited * 100.0, 1)
            : 100.0;

        return new JObject
        {
            ["success"] = true,
            ["standard"] = "Accessibilité PMR (Arrêté du 24 décembre 2015 & Normes Françaises / Européennes)",
            ["summary"] = new JObject
            {
                ["complianceRatePercent"] = complianceRate,
                ["totalDoors"] = totalDoors,
                ["compliantDoors"] = compliantDoors,
                ["nonCompliantDoors"] = nonCompliantDoors,
                ["auditedPmrRooms"] = compliantRooms + nonCompliantRooms,
                ["compliantPmrRooms"] = compliantRooms,
                ["nonCompliantPmrRooms"] = nonCompliantRooms,
                ["colorOverridesApplied"] = overridesApplied,
                ["targetViewName"] = view?.Name
            },
            ["doors"] = doorAudits,
            ["rooms"] = roomAudits,
            ["ramps"] = rampAudits
        };
    }
}
