using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace RevitMCP;

public partial class RevitEventHandler
{
    /// <summary>
    /// Projette des géométries holographiques fantômes (murs, mobiliers, volumes d'agencement)
    /// directement dans la vue 3D Revit sans modifier le modèle ni créer de transaction.
    /// </summary>
    private static JObject PreviewGhostGeometry(UIApplication uiapp, Document doc, JObject p)
    {
        var server = HologramOverlayServer.Instance;
        server.RegisterService();

        var clearExisting = p["clearExisting"]?.Value<bool>() ?? false;
        if (clearExisting)
        {
            server.Clear();
        }

        var items = p["items"] as JArray;
        var addedCount = 0;

        if (items != null)
        {
            foreach (var item in items)
            {
                var label = item["label"]?.Value<string>() ?? "Ghost Object";
                var type = item["type"]?.Value<string>() ?? "box";

                var xMm = item["x"]?.Value<double>() ?? item["xMm"]?.Value<double>() ?? 0.0;
                var yMm = item["y"]?.Value<double>() ?? item["yMm"]?.Value<double>() ?? 0.0;
                var zMm = item["z"]?.Value<double>() ?? item["zMm"]?.Value<double>() ?? 0.0;

                var widthMm = item["widthMm"]?.Value<double>() ?? item["width"]?.Value<double>() ?? 1000.0;
                var depthMm = item["depthMm"]?.Value<double>() ?? item["depth"]?.Value<double>() ?? 1000.0;
                var heightMm = item["heightMm"]?.Value<double>() ?? item["height"]?.Value<double>() ?? 2500.0;

                var rotDeg = item["rotationDeg"]?.Value<double>() ?? item["rotation"]?.Value<double>() ?? 0.0;

                var r = (byte)(item["r"]?.Value<int>() ?? 0);
                var g = (byte)(item["g"]?.Value<int>() ?? 220);
                var b = (byte)(item["b"]?.Value<int>() ?? 255);
                var alpha = (byte)(item["alpha"]?.Value<int>() ?? 130);

                var originFeet = UnitConversions.MmToFeet(new XYZ(xMm, yMm, zMm));
                var dimsFeet = new XYZ(
                    UnitConversions.MmToFeet(widthMm),
                    UnitConversions.MmToFeet(depthMm),
                    UnitConversions.MmToFeet(heightMm)
                );

                server.AddGhost(new GhostGeometry
                {
                    Label = label,
                    Type = type,
                    Origin = originFeet,
                    Dimensions = dimsFeet,
                    R = r,
                    G = g,
                    B = b,
                    Alpha = alpha,
                    RotationDeg = rotDeg
                });
                addedCount++;
            }
        }

        var wallStart = p["wallStart"] as JObject;
        var wallEnd = p["wallEnd"] as JObject;
        if (wallStart != null && wallEnd != null)
        {
            var p0x = wallStart["x"]?.Value<double>() ?? 0.0;
            var p0y = wallStart["y"]?.Value<double>() ?? 0.0;
            var p1x = wallEnd["x"]?.Value<double>() ?? 0.0;
            var p1y = wallEnd["y"]?.Value<double>() ?? 0.0;
            var z = wallStart["z"]?.Value<double>() ?? 0.0;

            var dx = p1x - p0x;
            var dy = p1y - p0y;
            var lenMm = Math.Sqrt(dx * dx + dy * dy);
            var angleDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI;

            var midX = (p0x + p1x) / 2.0;
            var midY = (p0y + p1y) / 2.0;
            var wallHeightMm = p["wallHeightMm"]?.Value<double>() ?? 2800.0;
            var wallThickMm = p["wallThicknessMm"]?.Value<double>() ?? 200.0;

            server.AddGhost(new GhostGeometry
            {
                Label = p["label"]?.Value<string>() ?? "Ghost Wall",
                Type = "wall",
                Origin = UnitConversions.MmToFeet(new XYZ(midX, midY, z + wallHeightMm / 2.0)),
                Dimensions = new XYZ(
                    UnitConversions.MmToFeet(lenMm),
                    UnitConversions.MmToFeet(wallThickMm),
                    UnitConversions.MmToFeet(wallHeightMm)
                ),
                R = (byte)(p["r"]?.Value<int>() ?? 0),
                G = (byte)(p["g"]?.Value<int>() ?? 220),
                B = (byte)(p["b"]?.Value<int>() ?? 255),
                Alpha = (byte)(p["alpha"]?.Value<int>() ?? 140),
                RotationDeg = angleDeg
            });
            addedCount++;
        }

        try
        {
            uiapp.ActiveUIDocument?.UpdateAllOpenViews();
        }
        catch { }

        return new JObject
        {
            ["success"] = true,
            ["message"] = $"Affichage holographique projeté dans la vue 3D ({server.Count} éléments fantômes actifs).",
            ["activeGhostCount"] = server.Count,
            ["addedCount"] = addedCount
        };
    }

    /// <summary>
    /// Projette une surimpression 3D heatmap (zones de clashs, zones d'ombres, alertes)
    /// directement sur les éléments Revit sans modifier leurs matériaux.
    /// </summary>
    private static JObject OverlayClashHeatmap(UIApplication uiapp, Document doc, JObject p)
    {
        var server = HologramOverlayServer.Instance;
        server.RegisterService();

        var clearExisting = p["clearExisting"]?.Value<bool>() ?? true;
        if (clearExisting)
        {
            server.Clear();
        }

        var clashes = p["clashes"] as JArray;
        var elementIds = p["elementIds"] as JArray;
        var addedCount = 0;

        if (clashes != null)
        {
            foreach (var cl in clashes)
            {
                var idA = cl["element_a_id"]?.Value<long>() ?? cl["elementAId"]?.Value<long>();
                var severity = cl["clash_severity"]?.Value<string>() ?? "hard_clash";

                byte r = 255, g = 30, b = 30, alpha = 160;
                if (severity == "clearance_clash")
                {
                    r = 255; g = 180; b = 0; alpha = 140;
                }

                if (idA.HasValue)
                {
                    var elA = doc.GetElement(new ElementId(idA.Value));
                    if (elA != null)
                    {
                        var bbox = elA.get_BoundingBox(null);
                        if (bbox != null)
                        {
                            var center = (bbox.Min + bbox.Max) * 0.5;
                            var dims = bbox.Max - bbox.Min;
                            server.AddGhost(new GhostGeometry
                            {
                                Label = $"Clash {severity} (El #{idA})",
                                Type = "heatmap_clash",
                                Origin = center,
                                Dimensions = dims,
                                R = r,
                                G = g,
                                B = b,
                                Alpha = alpha
                            });
                            addedCount++;
                        }
                    }
                }
            }
        }
        else if (elementIds != null)
        {
            var r = (byte)(p["r"]?.Value<int>() ?? 255);
            var g = (byte)(p["g"]?.Value<int>() ?? 50);
            var b = (byte)(p["b"]?.Value<int>() ?? 50);
            var alpha = (byte)(p["alpha"]?.Value<int>() ?? 140);

            foreach (var idVal in elementIds)
            {
                var idLong = idVal.Value<long>();
                var el = doc.GetElement(new ElementId(idLong));
                if (el != null)
                {
                    var bbox = el.get_BoundingBox(null);
                    if (bbox != null)
                    {
                        var center = (bbox.Min + bbox.Max) * 0.5;
                        var dims = bbox.Max - bbox.Min;
                        server.AddGhost(new GhostGeometry
                        {
                            Label = $"Heatmap Overlay #{idLong}",
                            Type = "heatmap",
                            Origin = center,
                            Dimensions = dims,
                            R = r,
                            G = g,
                            B = b,
                            Alpha = alpha
                        });
                        addedCount++;
                    }
                }
            }
        }

        try
        {
            uiapp.ActiveUIDocument?.UpdateAllOpenViews();
        }
        catch { }

        return new JObject
        {
            ["success"] = true,
            ["message"] = $"Heatmap 3D projetée en temps réel ({addedCount} zones en surimpression).",
            ["activeOverlays"] = server.Count
        };
    }

    /// <summary>
    /// Efface instantanément toutes les projections holographiques DirectContext3D de la vue 3D.
    /// </summary>
    private static JObject ClearHolographicOverlay(UIApplication uiapp, Document doc, JObject p)
    {
        var server = HologramOverlayServer.Instance;
        server.Clear();

        try
        {
            uiapp.ActiveUIDocument?.UpdateAllOpenViews();
        }
        catch { }

        return new JObject
        {
            ["success"] = true,
            ["message"] = "Toutes les projections holographiques DirectContext3D ont été effacées de la vue 3D.",
            ["activeGhostCount"] = 0
        };
    }

    /// <summary>
    /// Retourne le statut et l'inventaire des hologrammes 3D actuellement actifs.
    /// </summary>
    private static JObject GetHolographicStatus(UIApplication uiapp, Document doc, JObject p)
    {
        var server = HologramOverlayServer.Instance;
        var geoms = server.GetGeometries();

        var itemsArr = new JArray();
        foreach (var g in geoms)
        {
            var originMm = UnitConversions.FeetToMm(g.Origin);
            var dimsMm = UnitConversions.FeetToMm(g.Dimensions);

            itemsArr.Add(new JObject
            {
                ["id"] = g.Id,
                ["label"] = g.Label,
                ["type"] = g.Type,
                ["originMm"] = new JObject { ["x"] = Math.Round(originMm.X, 1), ["y"] = Math.Round(originMm.Y, 1), ["z"] = Math.Round(originMm.Z, 1) },
                ["dimsMm"] = new JObject { ["width"] = Math.Round(dimsMm.X, 1), ["depth"] = Math.Round(dimsMm.Y, 1), ["height"] = Math.Round(dimsMm.Z, 1) },
                ["colorRgba"] = $"rgba({g.R},{g.G},{g.B},{(g.Alpha / 255.0):F2})",
                ["rotationDeg"] = g.RotationDeg
            });
        }

        return new JObject
        {
            ["success"] = true,
            ["activeGhostCount"] = server.Count,
            ["isDirectContext3DActive"] = server.Count > 0,
            ["items"] = itemsArr
        };
    }
}
