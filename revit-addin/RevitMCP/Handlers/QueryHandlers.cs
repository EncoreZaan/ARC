using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
    private static JObject QueryElementsEnhanced(Document doc, JObject p)
    {
        var category = p["category"]?.Value<string>();
        var familyName = p["familyName"]?.Value<string>();
        var levelName = p["levelName"]?.Value<string>();
        var viewName = p["viewName"]?.Value<string>();
        var limit = p["limit"]?.Value<int>() ?? 100;
        var offset = p["offset"]?.Value<int>() ?? 0;
        var includeGeom = p["includeGeometry"]?.Value<bool>() ?? false;

        FilteredElementCollector col;
        if (!string.IsNullOrWhiteSpace(viewName))
        {
            var view = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .FirstOrDefault(v => string.Equals(v.Name, viewName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View '{viewName}' not found.");
            col = new FilteredElementCollector(doc, view.Id);
        }
        else
        {
            col = new FilteredElementCollector(doc);
        }

        col = col.WhereElementIsNotElementType();

        // Spatial bbox filter (D3)
        var bboxFilterObj = p["bboxFilter"] as JObject;
        if (bboxFilterObj != null && bboxFilterObj["min"] != null && bboxFilterObj["max"] != null)
        {
            var minMm = new XYZ(
                bboxFilterObj["min"]!["x"]?.Value<double>() ?? -1e9,
                bboxFilterObj["min"]!["y"]?.Value<double>() ?? -1e9,
                bboxFilterObj["min"]!["z"]?.Value<double>() ?? -1e9
            );
            var maxMm = new XYZ(
                bboxFilterObj["max"]!["x"]?.Value<double>() ?? 1e9,
                bboxFilterObj["max"]!["y"]?.Value<double>() ?? 1e9,
                bboxFilterObj["max"]!["z"]?.Value<double>() ?? 1e9
            );
            var outline = new Outline(UnitConversions.MmToFeet(minMm), UnitConversions.MmToFeet(maxMm));
            col = col.WherePasses(new BoundingBoxIntersectsFilter(outline));
        }

        // Category filter (D1)
        if (!string.IsNullOrWhiteSpace(category))
        {
            var bic = CategoryResolver.Resolve(category, doc);
            if (bic.HasValue)
            {
                col = col.OfCategory(bic.Value);
            }
        }

        var matched = col.ToElements()
            .Where(e =>
            {
                if (!string.IsNullOrWhiteSpace(category) && e.Category != null)
                {
                    if (!CategoryResolver.Matches(e.Category, category, doc))
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(familyName) && e is FamilyInstance fi)
                {
                    if (!string.Equals(fi.Symbol?.FamilyName, familyName, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(levelName))
                {
                    var lvl = doc.GetElement(e.LevelId) as Level;
                    var lvlParam = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                                 ?? e.get_Parameter(BuiltInParameter.LEVEL_PARAM)
                                 ?? e.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
                    var elLevelName = lvl?.Name ?? lvlParam?.AsValueString();
                    if (!string.Equals(elLevelName, levelName, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            });

        var totalCount = matched.Count();
        var paged = matched.Skip(offset).Take(limit);

        var elements = paged.Select(e =>
        {
            var item = new JObject
            {
                ["id"] = e.Id.Value,
                ["uniqueId"] = e.UniqueId,
                ["name"] = e.Name,
                ["category"] = e.Category?.Name,
                ["typeName"] = doc.GetElement(e.GetTypeId())?.Name,
                ["typeId"] = e.GetTypeId()?.Value ?? -1L,
            };

            var lvl = doc.GetElement(e.LevelId) as Level;
            var lvlParam = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                         ?? e.get_Parameter(BuiltInParameter.LEVEL_PARAM)
                         ?? e.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
            item["levelName"] = lvl?.Name ?? lvlParam?.AsValueString();

            // Location and BBox (D2)
            var loc = GeometryHelpers.ExtractLocation(e);
            if (loc != null) item["location"] = loc;

            var bbox = GeometryHelpers.ExtractBbox(e);
            if (bbox != null) item["bbox"] = bbox;

            if (e is FamilyInstance fi)
            {
                item["family"] = fi.Symbol?.FamilyName;
                item["hostId"] = fi.Host?.Id.Value;
                item["facingOrientation"] = new JObject
                {
                    ["x"] = Math.Round(fi.FacingOrientation.X, 4),
                    ["y"] = Math.Round(fi.FacingOrientation.Y, 4),
                    ["z"] = Math.Round(fi.FacingOrientation.Z, 4)
                };
                item["handOrientation"] = new JObject
                {
                    ["x"] = Math.Round(fi.HandOrientation.X, 4),
                    ["y"] = Math.Round(fi.HandOrientation.Y, 4),
                    ["z"] = Math.Round(fi.HandOrientation.Z, 4)
                };
                item["isFlipped"] = fi.FacingFlipped || fi.HandFlipped;
                item["facingFlipped"] = fi.FacingFlipped;
                item["handFlipped"] = fi.HandFlipped;
            }

            if (includeGeom)
            {
                item["geometry"] = GeometryHelpers.ExtractGeometry(e, doc, "bbox");
            }

            return item;
        });

        return new JObject
        {
            ["success"] = true,
            ["totalCount"] = totalCount,
            ["offset"] = offset,
            ["limit"] = limit,
            ["elements"] = new JArray(elements)
        };
    }

    private static JObject ListFamiliesEnhanced(Document doc, JObject p)
    {
        var category = p["category"]?.Value<string>();
        var includeTypeParams = p["includeTypeParameters"]?.Value<bool>() ?? false;

        var query = new FilteredElementCollector(doc)
            .OfClass(typeof(Family))
            .Cast<Family>();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(f => CategoryResolver.Matches(f.FamilyCategory, category, doc));
        }

        var families = query.Select(f =>
        {
            var fObj = new JObject
            {
                ["id"] = f.Id.Value,
                ["name"] = f.Name,
                ["category"] = f.FamilyCategory?.Name,
                ["isEditable"] = f.IsEditable,
                ["isInPlace"] = f.IsInPlace,
                ["types"] = new JArray(
                    f.GetFamilySymbolIds()
                     .Select(id => doc.GetElement(id) as FamilySymbol)
                     .Where(s => s != null)
                     .Select(s =>
                     {
                         var tObj = new JObject
                         {
                             ["id"] = s!.Id.Value,
                             ["name"] = s.Name
                         };
                         if (includeTypeParams)
                         {
                             tObj["parameters"] = GeometryHelpers.ExtractParameters(s, doc);
                         }
                         return tObj;
                     })
                )
            };
            return fObj;
        });

        return new JObject
        {
            ["success"] = true,
            ["families"] = new JArray(families)
        };
    }

    private static JObject ListRoomsEnhanced(Document doc, JObject p)
    {
        var levelName = p["levelName"]?.Value<string>();
        var includeBoundary = p["includeBoundary"]?.Value<bool>() ?? false;

        var query = new FilteredElementCollector(doc)
            .OfClass(typeof(SpatialElement))
            .OfType<Room>();

        if (!string.IsNullOrWhiteSpace(levelName))
        {
            query = query.Where(r => string.Equals(r.Level?.Name, levelName, StringComparison.OrdinalIgnoreCase));
        }

        var bOpt = new SpatialElementBoundaryOptions
        {
            SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
        };

        var rooms = query.Select(r =>
        {
            var rObj = new JObject
            {
                ["id"] = r.Id.Value,
                ["number"] = r.Number,
                ["name"] = r.Name,
                ["level"] = r.Level?.Name,
                ["isPlaced"] = r.Location != null && r.Area > 1e-4,
                ["areaM2"] = Math.Round(UnitConversions.SqFtToM2(r.Area), 2),
                ["perimeterMm"] = Math.Round(UnitConversions.FeetToMm(r.Perimeter), 1),
                ["unboundedHeightMm"] = Math.Round(UnitConversions.FeetToMm(r.UnboundedHeight), 1)
            };

            var loc = GeometryHelpers.ExtractLocation(r);
            if (loc != null) rObj["location"] = loc;
            var bbox = GeometryHelpers.ExtractBbox(r);
            if (bbox != null) rObj["bbox"] = bbox;

            if (includeBoundary && r.Area > 1e-4)
            {
                var loopsArray = new JArray();
                var segments = r.GetBoundarySegments(bOpt);
                if (segments != null)
                {
                    foreach (var loop in segments)
                    {
                        var loopPoints = new JArray();
                        foreach (var seg in loop)
                        {
                            var c = seg.GetCurve();
                            if (c != null)
                            {
                                var ptMm = UnitConversions.FeetToMm(c.GetEndPoint(0));
                                loopPoints.Add(new JObject
                                {
                                    ["x"] = Math.Round(ptMm.X, 2),
                                    ["y"] = Math.Round(ptMm.Y, 2),
                                    ["z"] = Math.Round(ptMm.Z, 2)
                                });
                            }
                        }
                        loopsArray.Add(loopPoints);
                    }
                }
                rObj["boundary"] = loopsArray;
            }

            return rObj;
        });

        return new JObject
        {
            ["success"] = true,
            ["rooms"] = new JArray(rooms)
        };
    }

    private static JObject GetElementLocation(Document doc, JObject p)
    {
        var idsToken = p["elementIds"] as JArray
            ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing 'elementIds' array.");

        var results = new JArray();
        foreach (var tok in idsToken)
        {
            var idVal = tok.Value<long>();
            var el = doc.GetElement(new ElementId(idVal));
            if (el == null) continue;

            var item = new JObject
            {
                ["elementId"] = el.Id.Value,
                ["name"] = el.Name,
                ["category"] = el.Category?.Name
            };

            var loc = GeometryHelpers.ExtractLocation(el);
            if (loc != null) item["location"] = loc;

            var bbox = GeometryHelpers.ExtractBbox(el);
            if (bbox != null) item["bbox"] = bbox;

            var lvl = doc.GetElement(el.LevelId) as Level;
            item["levelName"] = lvl?.Name;

            var offsetParam = el.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)
                           ?? el.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
            if (offsetParam != null)
            {
                item["offsetFromLevelMm"] = Math.Round(UnitConversions.FeetToMm(offsetParam.AsDouble()), 2);
            }

            if (el is FamilyInstance fi)
            {
                item["hostId"] = fi.Host?.Id.Value;
                if (fi.Location is LocationPoint lp)
                    item["rotationDeg"] = Math.Round(UnitConversions.RadToDeg(lp.Rotation), 2);
            }

            results.Add(item);
        }

        return new JObject
        {
            ["success"] = true,
            ["locations"] = results
        };
    }

    private static JObject GetElementGeometry(Document doc, JObject p)
    {
        var idVal = p["elementId"]?.Value<long>()
            ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing 'elementId'.");

        var el = doc.GetElement(new ElementId(idVal))
            ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element {idVal} not found.");

        var detail = p["detail"]?.Value<string>() ?? "bbox";
        var geom = GeometryHelpers.ExtractGeometry(el, doc, detail);

        geom["elementId"] = el.Id.Value;
        geom["name"] = el.Name;
        geom["category"] = el.Category?.Name;
        geom["success"] = true;

        return geom;
    }

    private static JObject GetElementsInRegion(Document doc, JObject p)
    {
        var minObj = p["min"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing 'min' point.");
        var maxObj = p["max"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing 'max' point.");

        var minMm = new XYZ(minObj["x"]?.Value<double>() ?? 0, minObj["y"]?.Value<double>() ?? 0, minObj["z"]?.Value<double>() ?? 0);
        var maxMm = new XYZ(maxObj["x"]?.Value<double>() ?? 0, maxObj["y"]?.Value<double>() ?? 0, maxObj["z"]?.Value<double>() ?? 0);

        var outline = new Outline(UnitConversions.MmToFeet(minMm), UnitConversions.MmToFeet(maxMm));
        var filter = new BoundingBoxIntersectsFilter(outline);

        var col = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WherePasses(filter);

        var catList = p["categories"] as JArray;
        var categories = catList?.Select(c => c.Value<string>()!).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();

        var elements = col.ToElements().Where(e =>
        {
            if (categories != null && categories.Count > 0)
            {
                return categories.Any(cat => CategoryResolver.Matches(e.Category, cat, doc));
            }
            return true;
        }).Select(e => new JObject
        {
            ["id"] = e.Id.Value,
            ["name"] = e.Name,
            ["category"] = e.Category?.Name,
            ["bbox"] = GeometryHelpers.ExtractBbox(e)
        });

        return new JObject
        {
            ["success"] = true,
            ["count"] = elements.Count(),
            ["elements"] = new JArray(elements)
        };
    }

    private static JObject MeasureDistance(Document doc, JObject p)
    {
        XYZ ptA;
        XYZ ptB;

        if (p["pointA"] != null && p["pointB"] != null)
        {
            var pA = p["pointA"] as JObject;
            var pB = p["pointB"] as JObject;
            ptA = UnitConversions.MmToFeet(new XYZ(pA!["x"]?.Value<double>() ?? 0, pA["y"]?.Value<double>() ?? 0, pA["z"]?.Value<double>() ?? 0));
            ptB = UnitConversions.MmToFeet(new XYZ(pB!["x"]?.Value<double>() ?? 0, pB["y"]?.Value<double>() ?? 0, pB["z"]?.Value<double>() ?? 0));
        }
        else if (p["elementIdA"] != null && p["elementIdB"] != null)
        {
            var elA = doc.GetElement(new ElementId(p["elementIdA"]!.Value<long>()))
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element A not found.");
            var elB = doc.GetElement(new ElementId(p["elementIdB"]!.Value<long>()))
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element B not found.");

            var bboxA = elA.get_BoundingBox(null) ?? throw new RevitMcpException("GEOMETRY_ERROR", "Element A has no bounding box.");
            var bboxB = elB.get_BoundingBox(null) ?? throw new RevitMcpException("GEOMETRY_ERROR", "Element B has no bounding box.");

            ptA = (bboxA.Min + bboxA.Max) * 0.5;
            ptB = (bboxB.Min + bboxB.Max) * 0.5;
        }
        else
        {
            throw new RevitMcpException("INVALID_ARGUMENT", "Specify either (elementIdA, elementIdB) or (pointA, pointB).");
        }

        var dist3dMm = UnitConversions.FeetToMm(ptA.DistanceTo(ptB));
        var ptAPlan = new XYZ(ptA.X, ptA.Y, 0);
        var ptBPlan = new XYZ(ptB.X, ptB.Y, 0);
        var distPlanMm = UnitConversions.FeetToMm(ptAPlan.DistanceTo(ptBPlan));

        var aMm = UnitConversions.FeetToMm(ptA);
        var bMm = UnitConversions.FeetToMm(ptB);

        return new JObject
        {
            ["success"] = true,
            ["distance3dMm"] = Math.Round(dist3dMm, 2),
            ["distancePlanMm"] = Math.Round(distPlanMm, 2),
            ["pointA"] = new JObject { ["x"] = Math.Round(aMm.X, 2), ["y"] = Math.Round(aMm.Y, 2), ["z"] = Math.Round(aMm.Z, 2) },
            ["pointB"] = new JObject { ["x"] = Math.Round(bMm.X, 2), ["y"] = Math.Round(bMm.Y, 2), ["z"] = Math.Round(bMm.Z, 2) }
        };
    }

    private static JObject GetHeightAtPoint(Document doc, JObject p)
    {
        var xMm = p["x"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing x coordinate.");
        var yMm = p["y"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing y coordinate.");
        var fromZMm = p["fromZ"]?.Value<double>() ?? 0;

        var startPtFeet = UnitConversions.MmToFeet(new XYZ(xMm, yMm, fromZMm));

        // Filter elements above point (Roofs, Floors, Ceilings)
        var categories = new[] { BuiltInCategory.OST_Roofs, BuiltInCategory.OST_Floors, BuiltInCategory.OST_Ceilings };
        var multiFilter = new ElementMulticategoryFilter(categories);

        var col = new FilteredElementCollector(doc)
            .WherePasses(multiFilter)
            .WhereElementIsNotElementType();

        double minClearanceMm = double.MaxValue;
        long targetElementId = -1;
        string targetElementName = "";

        // Check geometry intersection vertically at (x, y)
        foreach (var el in col)
        {
            var bbox = el.get_BoundingBox(null);
            if (bbox == null) continue;

            if (startPtFeet.X >= bbox.Min.X && startPtFeet.X <= bbox.Max.X &&
                startPtFeet.Y >= bbox.Min.Y && startPtFeet.Y <= bbox.Max.Y)
            {
                // Element is directly above or below
                var clearanceFeet = bbox.Min.Z - startPtFeet.Z;
                if (clearanceFeet > 0)
                {
                    var clearanceMm = UnitConversions.FeetToMm(clearanceFeet);
                    if (clearanceMm < minClearanceMm)
                    {
                        minClearanceMm = clearanceMm;
                        targetElementId = el.Id.Value;
                        targetElementName = el.Name;
                    }
                }
            }
        }

        return new JObject
        {
            ["success"] = true,
            ["point"] = new JObject { ["x"] = xMm, ["y"] = yMm, ["fromZ"] = fromZMm },
            ["clearanceMm"] = minClearanceMm < double.MaxValue ? Math.Round(minClearanceMm, 2) : (double?)null,
            ["targetElementId"] = targetElementId >= 0 ? targetElementId : (long?)null,
            ["targetElementName"] = targetElementName
        };
    }

    private static JObject CheckClearance(Document doc, JObject p)
    {
        var elementId = p["elementId"]?.Value<long>()
            ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementId.");
        var side = (p["side"]?.Value<string>() ?? "front").ToLowerInvariant();
        var depthMm = p["depth"]?.Value<double>() ?? 900.0; // 90 cm default

        var el = doc.GetElement(new ElementId(elementId))
            ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element {elementId} not found.");

        var bbox = el.get_BoundingBox(null)
            ?? throw new RevitMcpException("GEOMETRY_ERROR", "Element has no bounding box.");

        XYZ direction = XYZ.BasisY;
        if (el is FamilyInstance fi)
        {
            direction = side switch
            {
                "front" => fi.FacingOrientation,
                "back" => -fi.FacingOrientation,
                "left" => -fi.HandOrientation,
                "right" => fi.HandOrientation,
                _ => fi.FacingOrientation
            };
        }

        var depthFeet = UnitConversions.MmToFeet(depthMm);
        var centerFeet = (bbox.Min + bbox.Max) * 0.5;
        var checkPointFeet = centerFeet + direction.Normalize() * (depthFeet * 0.5);

        var halfExtent = new XYZ(depthFeet * 0.5, depthFeet * 0.5, (bbox.Max.Z - bbox.Min.Z) * 0.5);
        var outline = new Outline(checkPointFeet - halfExtent, checkPointFeet + halfExtent);
        var filter = new BoundingBoxIntersectsFilter(outline);

        var obstacles = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WherePasses(filter)
            .ToElements()
            .Where(o => o.Id.Value != el.Id.Value && o.Category != null && o.Category.CategoryType == CategoryType.Model)
            .Select(o => new JObject
            {
                ["id"] = o.Id.Value,
                ["name"] = o.Name,
                ["category"] = o.Category?.Name
            });

        return new JObject
        {
            ["success"] = true,
            ["elementId"] = elementId,
            ["side"] = side,
            ["depthMm"] = depthMm,
            ["isClear"] = !obstacles.Any(),
            ["obstacleCount"] = obstacles.Count(),
            ["obstacles"] = new JArray(obstacles)
        };
    }

    private static JObject GetWallOpenings(Document doc, JObject p)
    {
        var wallId = p["wallId"]?.Value<long>()
            ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing wallId.");

        var wall = doc.GetElement(new ElementId(wallId)) as Wall
            ?? throw new RevitMcpException("WALL_NOT_FOUND", $"Wall {wallId} not found.");

        var wallLocCurve = wall.Location as LocationCurve
            ?? throw new RevitMcpException("GEOMETRY_ERROR", "Wall has no location curve.");

        var curve = wallLocCurve.Curve;
        var wallStart = curve.GetEndPoint(0);
        var wallEnd = curve.GetEndPoint(1);
        var wallLengthFeet = curve.Length;
        var wallLengthMm = UnitConversions.FeetToMm(wallLengthFeet);

        // Find hosted instances (Doors, Windows, Openings)
        var hostedFilter = new ElementMulticategoryFilter(new[]
        {
            BuiltInCategory.OST_Doors,
            BuiltInCategory.OST_Windows
        });

        var hostedElements = new FilteredElementCollector(doc)
            .WherePasses(hostedFilter)
            .WhereElementIsNotElementType()
            .OfType<FamilyInstance>()
            .Where(fi => fi.Host != null && fi.Host.Id.Value == wall.Id.Value);

        var openings = hostedElements.Select(fi =>
        {
            var lp = fi.Location as LocationPoint;
            var ptFeet = lp?.Point ?? curve.Project(fi.get_BoundingBox(null)?.Min ?? wallStart).XYZPoint;
            var paramDistFeet = curve.Project(ptFeet).Parameter;
            var distFromStartFeet = curve.Length * paramDistFeet;
            var distFromEndFeet = curve.Length - distFromStartFeet;

            var widthParam = fi.Symbol.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM)
                          ?? fi.Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH)
                          ?? fi.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH);
            var heightParam = fi.Symbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM)
                           ?? fi.Symbol.get_Parameter(BuiltInParameter.WINDOW_HEIGHT)
                           ?? fi.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT);
            var sillParam = fi.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);

            return new JObject
            {
                ["id"] = fi.Id.Value,
                ["name"] = fi.Name,
                ["category"] = fi.Category?.Name,
                ["typeName"] = fi.Symbol.Name,
                ["widthMm"] = widthParam != null ? Math.Round(UnitConversions.FeetToMm(widthParam.AsDouble()), 1) : (double?)null,
                ["heightMm"] = heightParam != null ? Math.Round(UnitConversions.FeetToMm(heightParam.AsDouble()), 1) : (double?)null,
                ["sillHeightMm"] = sillParam != null ? Math.Round(UnitConversions.FeetToMm(sillParam.AsDouble()), 1) : (double?)null,
                ["distanceFromWallStartMm"] = Math.Round(UnitConversions.FeetToMm(distFromStartFeet), 1),
                ["distanceFromWallEndMm"] = Math.Round(UnitConversions.FeetToMm(distFromEndFeet), 1),
                ["isFlipped"] = fi.FacingFlipped || fi.HandFlipped,
                ["facingFlipped"] = fi.FacingFlipped,
                ["handFlipped"] = fi.HandFlipped
            };
        }).OrderBy(o => o["distanceFromWallStartMm"]?.Value<double>() ?? 0);

        return new JObject
        {
            ["success"] = true,
            ["wallId"] = wall.Id.Value,
            ["wallLengthMm"] = Math.Round(wallLengthMm, 1),
            ["openings"] = new JArray(openings)
        };
    }

    private static JObject GetRoomAtPoint(Document doc, JObject p)
    {
        var xMm = p["x"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing x coordinate.");
        var yMm = p["y"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing y coordinate.");
        var zMm = p["z"]?.Value<double>() ?? 0.0;

        var ptFeet = UnitConversions.MmToFeet(new XYZ(xMm, yMm, zMm));
        var room = doc.GetRoomAtPoint(ptFeet);

        if (room == null)
        {
            return new JObject
            {
                ["success"] = true,
                ["found"] = false,
                ["point"] = new JObject { ["x"] = xMm, ["y"] = yMm, ["z"] = zMm }
            };
        }

        return new JObject
        {
            ["success"] = true,
            ["found"] = true,
            ["roomId"] = room.Id.Value,
            ["name"] = room.Name,
            ["number"] = room.Number,
            ["level"] = room.Level?.Name,
            ["areaM2"] = Math.Round(UnitConversions.SqFtToM2(room.Area), 2)
        };
    }

    private static JObject CreateSchedule(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Schedule", p, () =>
        {
            var catName = p["category"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing category.");
            var name = p["name"]?.Value<string>() ?? $"{catName} Schedule";
            var fields = p["fields"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing fields array.");

            var bic = CategoryResolver.Resolve(catName, doc)
                ?? throw new RevitMcpException("INVALID_CATEGORY", $"Unknown category '{catName}'.");

            var sched = ViewSchedule.CreateSchedule(doc, new ElementId((long)bic));
            sched.Name = name;

            var schedDef = sched.Definition;
            var availableFields = schedDef.GetSchedulableFields();

            foreach (var fToken in fields)
            {
                var fName = fToken.Value<string>();
                var target = availableFields.FirstOrDefault(sf => string.Equals(sf.GetName(doc), fName, StringComparison.OrdinalIgnoreCase));
                if (target != null)
                {
                    schedDef.AddField(target);
                }
            }

            return new JObject
            {
                ["scheduleId"] = sched.Id.Value,
                ["name"] = sched.Name,
                ["success"] = true
            };
        });
    }

    private static JObject GetScheduleData(Document doc, JObject p)
    {
        var schedId = p["scheduleId"]?.Value<long>()
            ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing scheduleId.");

        var sched = doc.GetElement(new ElementId(schedId)) as ViewSchedule
            ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Schedule {schedId} not found.");

        var table = sched.GetTableData();
        var section = table.GetSectionData(SectionType.Body);

        var rows = new JArray();
        for (int r = 0; r < section.NumberOfRows; r++)
        {
            var row = new JArray();
            for (int c = 0; c < section.NumberOfColumns; c++)
            {
                row.Add(section.GetCellText(r, c));
            }
            rows.Add(row);
        }

        return new JObject
        {
            ["success"] = true,
            ["scheduleId"] = sched.Id.Value,
            ["name"] = sched.Name,
            ["rowCount"] = section.NumberOfRows,
            ["columnCount"] = section.NumberOfColumns,
            ["rows"] = rows
        };
    }
}
