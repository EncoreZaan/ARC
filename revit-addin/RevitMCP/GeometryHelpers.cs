using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace RevitMCP;

/// <summary>
/// Geometry and spatial extraction utilities conforming to Rules 3 and 4.
/// </summary>
public static class GeometryHelpers
{
    public static JObject? ExtractBbox(Element element)
    {
        var bbox = element.get_BoundingBox(null);
        if (bbox == null) return null;

        var minMm = UnitConversions.FeetToMm(bbox.Min);
        var maxMm = UnitConversions.FeetToMm(bbox.Max);

        return new JObject
        {
            ["min"] = new JObject { ["x"] = Math.Round(minMm.X, 2), ["y"] = Math.Round(minMm.Y, 2), ["z"] = Math.Round(minMm.Z, 2) },
            ["max"] = new JObject { ["x"] = Math.Round(maxMm.X, 2), ["y"] = Math.Round(maxMm.Y, 2), ["z"] = Math.Round(maxMm.Z, 2) }
        };
    }

    public static JObject? ExtractLocation(Element element)
    {
        if (element.Location is LocationPoint lp)
        {
            var ptMm = UnitConversions.FeetToMm(lp.Point);
            double rotDeg = 0;
            try { rotDeg = UnitConversions.RadToDeg(lp.Rotation); } catch { }
            return new JObject
            {
                ["type"] = "point",
                ["point"] = new JObject
                {
                    ["x"] = Math.Round(ptMm.X, 2),
                    ["y"] = Math.Round(ptMm.Y, 2),
                    ["z"] = Math.Round(ptMm.Z, 2)
                },
                ["rotationDeg"] = Math.Round(rotDeg, 2)
            };
        }

        if (element.Location is LocationCurve lc)
        {
            var p0Mm = UnitConversions.FeetToMm(lc.Curve.GetEndPoint(0));
            var p1Mm = UnitConversions.FeetToMm(lc.Curve.GetEndPoint(1));
            return new JObject
            {
                ["type"] = "curve",
                ["curve"] = new JObject
                {
                    ["start"] = new JObject { ["x"] = Math.Round(p0Mm.X, 2), ["y"] = Math.Round(p0Mm.Y, 2), ["z"] = Math.Round(p0Mm.Z, 2) },
                    ["end"] = new JObject { ["x"] = Math.Round(p1Mm.X, 2), ["y"] = Math.Round(p1Mm.Y, 2), ["z"] = Math.Round(p1Mm.Z, 2) },
                    ["lengthMm"] = Math.Round(UnitConversions.FeetToMm(lc.Curve.Length), 2)
                }
            };
        }

        return null;
    }

    public static JObject ExtractGeometry(Element element, Document doc, string detail = "bbox")
    {
        var result = new JObject();
        var opt = new Options
        {
            ComputeReferences = true,
            DetailLevel = ViewDetailLevel.Fine,
            IncludeNonVisibleObjects = false
        };

        var geomElement = element.get_Geometry(opt);
        if (geomElement == null)
        {
            result["hasGeometry"] = false;
            return result;
        }

        result["hasGeometry"] = true;
        double totalVolumeCuFt = 0;
        double totalAreaSqFt = 0;
        int solidCount = 0;
        var facesList = new JArray();
        var edgesList = new JArray();

        void ProcessGeometry(GeometryElement ge)
        {
            foreach (var obj in ge)
            {
                if (obj is Solid solid && solid.Faces.Size > 0 && solid.Volume > 1e-6)
                {
                    solidCount++;
                    totalVolumeCuFt += solid.Volume;
                    totalAreaSqFt += solid.SurfaceArea;

                    if (detail == "faces" || detail == "mesh")
                    {
                        foreach (Face face in solid.Faces)
                        {
                            var faceObj = new JObject
                            {
                                ["areaM2"] = Math.Round(UnitConversions.SqFtToM2(face.Area), 4)
                            };

                            if (face is PlanarFace pf)
                            {
                                faceObj["normal"] = new JObject
                                {
                                    ["x"] = Math.Round(pf.FaceNormal.X, 4),
                                    ["y"] = Math.Round(pf.FaceNormal.Y, 4),
                                    ["z"] = Math.Round(pf.FaceNormal.Z, 4)
                                };
                                faceObj["origin"] = new JObject
                                {
                                    ["x"] = Math.Round(UnitConversions.FeetToMm(pf.Origin.X), 2),
                                    ["y"] = Math.Round(UnitConversions.FeetToMm(pf.Origin.Y), 2),
                                    ["z"] = Math.Round(UnitConversions.FeetToMm(pf.Origin.Z), 2)
                                };
                            }

                            if (detail == "mesh")
                            {
                                var mesh = face.Triangulate();
                                if (mesh != null)
                                {
                                    faceObj["triangleCount"] = mesh.NumTriangles;
                                    var vertices = new JArray();
                                    for (int i = 0; i < mesh.Vertices.Count; i++)
                                    {
                                        var vMm = UnitConversions.FeetToMm(mesh.Vertices[i]);
                                        vertices.Add(new JObject { ["x"] = Math.Round(vMm.X, 2), ["y"] = Math.Round(vMm.Y, 2), ["z"] = Math.Round(vMm.Z, 2) });
                                    }
                                    faceObj["vertices"] = vertices;
                                }
                            }

                            facesList.Add(faceObj);
                        }
                    }

                    if (detail == "edges")
                    {
                        foreach (Edge edge in solid.Edges)
                        {
                            var c = edge.AsCurve();
                            if (c != null)
                            {
                                var p0Mm = UnitConversions.FeetToMm(c.GetEndPoint(0));
                                var p1Mm = UnitConversions.FeetToMm(c.GetEndPoint(1));
                                edgesList.Add(new JObject
                                {
                                    ["start"] = new JObject { ["x"] = Math.Round(p0Mm.X, 2), ["y"] = Math.Round(p0Mm.Y, 2), ["z"] = Math.Round(p0Mm.Z, 2) },
                                    ["end"] = new JObject { ["x"] = Math.Round(p1Mm.X, 2), ["y"] = Math.Round(p1Mm.Y, 2), ["z"] = Math.Round(p1Mm.Z, 2) },
                                    ["lengthMm"] = Math.Round(UnitConversions.FeetToMm(c.Length), 2)
                                });
                            }
                        }
                    }
                }
                else if (obj is GeometryInstance gi)
                {
                    var instanceGeom = gi.GetInstanceGeometry();
                    if (instanceGeom != null)
                    {
                        ProcessGeometry(instanceGeom);
                    }
                }
            }
        }

        ProcessGeometry(geomElement);

        result["solidCount"] = solidCount;
        result["volumeM3"] = Math.Round(UnitConversions.CuFtToM3(totalVolumeCuFt), 4);
        result["surfaceAreaM2"] = Math.Round(UnitConversions.SqFtToM2(totalAreaSqFt), 4);

        if (detail == "faces" || detail == "mesh")
            result["faces"] = facesList;
        if (detail == "edges")
            result["edges"] = edgesList;

        var bbox = ExtractBbox(element);
        if (bbox != null)
            result["bbox"] = bbox;

        return result;
    }

    public static JArray ExtractParameters(Element el, Document doc)
    {
        var parms = new JArray();
        foreach (Parameter param in el.Parameters)
        {
            var pObj = new JObject
            {
                ["name"] = param.Definition.Name,
                ["value"] = param.StorageType switch
                {
                    StorageType.String => param.AsString(),
                    StorageType.Double => Math.Round(param.AsDouble(), 4),
                    StorageType.Integer => param.AsInteger(),
                    StorageType.ElementId => param.AsElementId()?.Value,
                    _ => null
                },
                ["valueString"] = param.AsValueString(),
                ["isReadOnly"] = param.IsReadOnly,
                ["storageType"] = param.StorageType.ToString()
            };
            parms.Add(pObj);
        }
        return parms;
    }
}
