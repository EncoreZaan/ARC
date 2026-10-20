using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
    private static JObject PlaceInstanceEnhanced(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Place Instance", p, () =>
        {
            var familyName = p["familyName"]?.Value<string>();
            var typeName = p["typeName"]?.Value<string>();
            var familyPath = p["familyPath"]?.Value<string>();
            var levelName = p["levelName"]?.Value<string>();
            var xMm = p["x"]?.Value<double>() ?? 0;
            var yMm = p["y"]?.Value<double>() ?? 0;
            var zMm = p["z"]?.Value<double>() ?? 0;
            var hostIdVal = p["hostElementId"]?.Value<long?>();
            var rotation = p["rotation"]?.Value<double?>() ?? 0;

            // D8: Check if family is loaded, otherwise load from familyPath
            FamilySymbol? symbol = null;
            if (!string.IsNullOrWhiteSpace(familyName) && !string.IsNullOrWhiteSpace(typeName))
            {
                symbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(s => string.Equals(s.FamilyName, familyName, StringComparison.OrdinalIgnoreCase) &&
                                         string.Equals(s.Name, typeName, StringComparison.OrdinalIgnoreCase));
            }

            if (symbol == null && !string.IsNullOrWhiteSpace(familyPath) && File.Exists(familyPath))
            {
                if (doc.LoadFamily(familyPath, out var loadedFamily) || loadedFamily != null)
                {
                    var symbols = loadedFamily.GetFamilySymbolIds().Select(id => doc.GetElement(id) as FamilySymbol).Where(s => s != null);
                    symbol = !string.IsNullOrWhiteSpace(typeName)
                        ? symbols.FirstOrDefault(s => string.Equals(s!.Name, typeName, StringComparison.OrdinalIgnoreCase))
                        : symbols.FirstOrDefault();
                }
            }

            if (symbol == null)
            {
                throw new RevitMcpException("SYMBOL_NOT_FOUND",
                    $"Family symbol '{familyName} : {typeName}' not found and could not be loaded from '{familyPath}'.");
            }

            if (!symbol.IsActive)
                symbol.Activate();

            Level? level = null;
            if (!string.IsNullOrWhiteSpace(levelName))
            {
                level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));
            }
            level ??= new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();

            var ptFeet = UnitConversions.MmToFeet(new XYZ(xMm, yMm, zMm));

            FamilyInstance instance;
            if (hostIdVal.HasValue)
            {
                var host = doc.GetElement(new ElementId(hostIdVal.Value))
                    ?? throw new RevitMcpException("HOST_NOT_FOUND", $"Host element {hostIdVal.Value} not found.");
                instance = doc.Create.NewFamilyInstance(ptFeet, symbol, host, level, StructuralType.NonStructural);
            }
            else if (level != null)
            {
                instance = doc.Create.NewFamilyInstance(ptFeet, symbol, level, StructuralType.NonStructural);
            }
            else
            {
                instance = doc.Create.NewFamilyInstance(ptFeet, symbol, StructuralType.NonStructural);
            }

            if (Math.Abs(rotation) > 1e-4)
            {
                // Rotation around vertical axis through the insertion point
                var axis = Line.CreateBound(ptFeet, ptFeet + XYZ.BasisZ);
                ElementTransformUtils.RotateElement(doc, instance.Id, axis, UnitConversions.DegToRad(rotation));
            }

            // Return created elementId + bbox
            doc.Regenerate();
            var bbox = GeometryHelpers.ExtractBbox(instance);

            return new JObject
            {
                ["elementId"] = instance.Id.Value,
                ["uniqueId"] = instance.UniqueId,
                ["bbox"] = bbox,
                ["success"] = true
            };
        });
    }

    private static JObject SetElementLocation(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Set Element Location", p, () =>
        {
            var idVal = p["elementId"]?.Value<long>()
                ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementId.");
            var el = doc.GetElement(new ElementId(idVal))
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element {idVal} not found.");

            if (p["point"] is JObject ptObj)
            {
                var targetMm = new XYZ(ptObj["x"]?.Value<double>() ?? 0, ptObj["y"]?.Value<double>() ?? 0, ptObj["z"]?.Value<double>() ?? 0);
                var targetFeet = UnitConversions.MmToFeet(targetMm);

                if (el.Location is LocationPoint lp)
                {
                    var delta = targetFeet - lp.Point;
                    ElementTransformUtils.MoveElement(doc, el.Id, delta);
                }
                else if (el.Location is LocationCurve lc)
                {
                    var startFeet = lc.Curve.GetEndPoint(0);
                    var delta = targetFeet - startFeet;
                    ElementTransformUtils.MoveElement(doc, el.Id, delta);
                }
            }
            else if (p["curve"] is JObject cvObj)
            {
                var sObj = cvObj["start"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing curve start.");
                var eObj = cvObj["end"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing curve end.");

                var sFeet = UnitConversions.MmToFeet(new XYZ(sObj["x"]?.Value<double>() ?? 0, sObj["y"]?.Value<double>() ?? 0, sObj["z"]?.Value<double>() ?? 0));
                var eFeet = UnitConversions.MmToFeet(new XYZ(eObj["x"]?.Value<double>() ?? 0, eObj["y"]?.Value<double>() ?? 0, eObj["z"]?.Value<double>() ?? 0));

                if (el.Location is LocationCurve lc)
                {
                    var newCurve = Line.CreateBound(sFeet, eFeet);
                    lc.Curve = newCurve;
                }
            }

            var rotDeg = p["rotationDeg"]?.Value<double?>();
            if (rotDeg.HasValue && el.Location is LocationPoint lp2)
            {
                var currentRot = UnitConversions.RadToDeg(lp2.Rotation);
                var deltaRotDeg = rotDeg.Value - currentRot;
                if (Math.Abs(deltaRotDeg) > 1e-4)
                {
                    var axis = Line.CreateBound(lp2.Point, lp2.Point + XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(doc, el.Id, axis, UnitConversions.DegToRad(deltaRotDeg));
                }
            }

            doc.Regenerate();
            return new JObject
            {
                ["elementId"] = el.Id.Value,
                ["location"] = GeometryHelpers.ExtractLocation(el),
                ["bbox"] = GeometryHelpers.ExtractBbox(el),
                ["success"] = true
            };
        });
    }

    private static JObject ChangeElementType(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Change Element Type", p, () =>
        {
            var elementIdsToken = p["elementIds"] as JArray ?? new JArray { p["elementId"]?.Value<long>() ?? -1L };
            var typeIdVal = p["typeId"]?.Value<long?>();
            var typeName = p["typeName"]?.Value<string>();
            var familyName = p["familyName"]?.Value<string>();

            ElementType? targetType = null;
            if (typeIdVal.HasValue)
            {
                targetType = doc.GetElement(new ElementId(typeIdVal.Value)) as ElementType;
            }
            else if (!string.IsNullOrWhiteSpace(typeName))
            {
                var query = new FilteredElementCollector(doc)
                    .OfClass(typeof(ElementType))
                    .Cast<ElementType>()
                    .Where(t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(familyName))
                {
                    query = query.Where(t => string.Equals(t.get_Parameter(BuiltInParameter.ALL_MODEL_FAMILY_NAME)?.AsString(), familyName, StringComparison.OrdinalIgnoreCase) ||
                                             (t is FamilySymbol fs && string.Equals(fs.FamilyName, familyName, StringComparison.OrdinalIgnoreCase)));
                }

                targetType = query.FirstOrDefault();
            }

            if (targetType == null)
            {
                throw new RevitMcpException("TYPE_NOT_FOUND", $"Target element type '{familyName} : {typeName}' not found.");
            }

            if (targetType is FamilySymbol fsActive && !fsActive.IsActive)
            {
                fsActive.Activate();
            }

            var updatedCount = 0;
            foreach (var tok in elementIdsToken)
            {
                var idVal = tok.Value<long>();
                var el = doc.GetElement(new ElementId(idVal));
                if (el != null)
                {
                    el.ChangeTypeId(targetType.Id);
                    updatedCount++;
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["updatedCount"] = updatedCount,
                ["newTypeId"] = targetType.Id.Value,
                ["newTypeName"] = targetType.Name
            };
        });
    }

    private static JObject CopyElements(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Copy Elements", p, () =>
        {
            var idsToken = p["elementIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIds.");
            var deltaObj = p["delta"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing delta vector.");
            var count = p["count"]?.Value<int>() ?? 1;

            var deltaMm = new XYZ(deltaObj["x"]?.Value<double>() ?? 0, deltaObj["y"]?.Value<double>() ?? 0, deltaObj["z"]?.Value<double>() ?? 0);
            var deltaFeet = UnitConversions.MmToFeet(deltaMm);

            var ids = idsToken.Select(t => new ElementId(t.Value<long>())).ToList();
            var createdIds = new JArray();

            for (int i = 1; i <= count; i++)
            {
                var stepDelta = deltaFeet * i;
                var copied = ElementTransformUtils.CopyElements(doc, ids, stepDelta);
                foreach (var id in copied)
                    createdIds.Add(id.Value);
            }

            return new JObject
            {
                ["success"] = true,
                ["createdCount"] = createdIds.Count,
                ["createdIds"] = createdIds
            };
        });
    }

    private static JObject MirrorElements(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Mirror Elements", p, () =>
        {
            var idsToken = p["elementIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIds.");
            var sObj = p["axisStart"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing axisStart.");
            var eObj = p["axisEnd"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing axisEnd.");
            var copy = p["copy"]?.Value<bool>() ?? false;

            var sFeet = UnitConversions.MmToFeet(new XYZ(sObj["x"]?.Value<double>() ?? 0, sObj["y"]?.Value<double>() ?? 0, sObj["z"]?.Value<double>() ?? 0));
            var eFeet = UnitConversions.MmToFeet(new XYZ(eObj["x"]?.Value<double>() ?? 0, eObj["y"]?.Value<double>() ?? 0, eObj["z"]?.Value<double>() ?? 0));

            var plane = Plane.CreateByThreePoints(sFeet, eFeet, sFeet + XYZ.BasisZ);
            var ids = idsToken.Select(t => new ElementId(t.Value<long>())).ToList();

            if (copy)
            {
                var mirroredIds = ElementTransformUtils.MirrorElements(doc, ids, plane, true);
                return new JObject
                {
                    ["success"] = true,
                    ["copied"] = true,
                    ["createdIds"] = new JArray(mirroredIds.Select(id => id.Value))
                };
            }

            ElementTransformUtils.MirrorElements(doc, ids, plane, false);
            return new JObject
            {
                ["success"] = true,
                ["copied"] = false,
                ["elementsMirrored"] = ids.Count
            };
        });
    }

    private static JObject AlignElements(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Align Elements", p, () =>
        {
            var idsToken = p["elementIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIds.");
            var reference = (p["reference"]?.Value<string>() ?? "centerX").ToLowerInvariant();
            var targetValueMm = p["value"]?.Value<double?>();
            var toElementIdVal = p["toElementId"]?.Value<long?>();

            double targetValFeet;
            if (toElementIdVal.HasValue)
            {
                var toEl = doc.GetElement(new ElementId(toElementIdVal.Value))
                    ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Target element {toElementIdVal.Value} not found.");
                var toBbox = toEl.get_BoundingBox(null) ?? throw new RevitMcpException("GEOMETRY_ERROR", "Target element has no bbox.");

                targetValFeet = reference switch
                {
                    "minx" => toBbox.Min.X,
                    "maxx" => toBbox.Max.X,
                    "centerx" => (toBbox.Min.X + toBbox.Max.X) * 0.5,
                    "miny" => toBbox.Min.Y,
                    "maxy" => toBbox.Max.Y,
                    "centery" => (toBbox.Min.Y + toBbox.Max.Y) * 0.5,
                    "minz" => toBbox.Min.Z,
                    "maxz" => toBbox.Max.Z,
                    _ => (toBbox.Min.X + toBbox.Max.X) * 0.5
                };
            }
            else if (targetValueMm.HasValue)
            {
                targetValFeet = UnitConversions.MmToFeet(targetValueMm.Value);
            }
            else
            {
                throw new RevitMcpException("INVALID_ARGUMENT", "Specify either 'value' or 'toElementId'.");
            }

            int aligned = 0;
            foreach (var tok in idsToken)
            {
                var el = doc.GetElement(new ElementId(tok.Value<long>()));
                if (el == null) continue;
                var bbox = el.get_BoundingBox(null);
                if (bbox == null) continue;

                XYZ delta = XYZ.Zero;
                if (reference.Contains("x"))
                {
                    var currentX = reference switch
                    {
                        "minx" => bbox.Min.X,
                        "maxx" => bbox.Max.X,
                        _ => (bbox.Min.X + bbox.Max.X) * 0.5
                    };
                    delta = new XYZ(targetValFeet - currentX, 0, 0);
                }
                else if (reference.Contains("y"))
                {
                    var currentY = reference switch
                    {
                        "miny" => bbox.Min.Y,
                        "maxy" => bbox.Max.Y,
                        _ => (bbox.Min.Y + bbox.Max.Y) * 0.5
                    };
                    delta = new XYZ(0, targetValFeet - currentY, 0);
                }
                else if (reference.Contains("z"))
                {
                    var currentZ = reference switch
                    {
                        "minz" => bbox.Min.Z,
                        "maxz" => bbox.Max.Z,
                        _ => (bbox.Min.Z + bbox.Max.Z) * 0.5
                    };
                    delta = new XYZ(0, 0, targetValFeet - currentZ);
                }

                if (!delta.IsAlmostEqualTo(XYZ.Zero))
                {
                    ElementTransformUtils.MoveElement(doc, el.Id, delta);
                    aligned++;
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["alignedCount"] = aligned
            };
        });
    }

    private static JObject FlipElement(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Flip Element", p, () =>
        {
            var idVal = p["elementId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementId.");
            var el = doc.GetElement(new ElementId(idVal)) as FamilyInstance
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"FamilyInstance {idVal} not found.");

            var flipHand = p["hand"]?.Value<bool>() ?? false;
            var flipFacing = p["facing"]?.Value<bool>() ?? false;

            if (flipHand && el.CanFlipHand)
                el.flipHand();

            if (flipFacing && el.CanFlipFacing)
                el.flipFacing();

            return new JObject
            {
                ["success"] = true,
                ["elementId"] = el.Id.Value,
                ["handFlipped"] = el.HandFlipped,
                ["facingFlipped"] = el.FacingFlipped
            };
        });
    }

    private static JObject HostedSetPosition(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Hosted Set Position", p, () =>
        {
            var idVal = p["elementId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementId.");
            var el = doc.GetElement(new ElementId(idVal)) as FamilyInstance
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"FamilyInstance {idVal} not found.");

            var wall = el.Host as Wall ?? throw new RevitMcpException("INVALID_HOST", "Element must be hosted by a Wall.");
            var wallLoc = wall.Location as LocationCurve ?? throw new RevitMcpException("GEOMETRY_ERROR", "Wall has no curve.");

            var curve = wallLoc.Curve;
            var distStartMm = p["distanceFromWallStart"]?.Value<double?>();
            var distEndMm = p["distanceFromWallEnd"]?.Value<double?>();

            double targetDistFeet;
            if (distStartMm.HasValue)
            {
                targetDistFeet = UnitConversions.MmToFeet(distStartMm.Value);
            }
            else if (distEndMm.HasValue)
            {
                targetDistFeet = curve.Length - UnitConversions.MmToFeet(distEndMm.Value);
            }
            else
            {
                targetDistFeet = -1;
            }

            if (targetDistFeet >= 0)
            {
                var normParam = Math.Clamp(targetDistFeet / curve.Length, 0.0, 1.0);
                var targetPt = curve.Evaluate(normParam, true);
                var currentPt = (el.Location as LocationPoint)?.Point ?? curve.Project(el.get_BoundingBox(null)?.Min ?? targetPt).XYZPoint;
                var delta = targetPt - currentPt;
                // Move along wall plane
                var deltaXY = new XYZ(delta.X, delta.Y, 0);
                if (!deltaXY.IsAlmostEqualTo(XYZ.Zero))
                {
                    ElementTransformUtils.MoveElement(doc, el.Id, deltaXY);
                }
            }

            var sillMm = p["sillHeight"]?.Value<double?>();
            if (sillMm.HasValue)
            {
                var sillParam = el.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                sillParam?.Set(UnitConversions.MmToFeet(sillMm.Value));
            }

            var headMm = p["headHeight"]?.Value<double?>();
            if (headMm.HasValue)
            {
                var headParam = el.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM);
                headParam?.Set(UnitConversions.MmToFeet(headMm.Value));
            }

            doc.Regenerate();
            return new JObject
            {
                ["success"] = true,
                ["elementId"] = el.Id.Value,
                ["bbox"] = GeometryHelpers.ExtractBbox(el)
            };
        });
    }

    private static JObject SetElementPinned(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Set Element Pinned", p, () =>
        {
            var idsToken = p["elementIds"] as JArray ?? new JArray { p["elementId"]?.Value<long>() ?? -1L };
            var pinned = p["pinned"]?.Value<bool>() ?? true;

            int count = 0;
            foreach (var tok in idsToken)
            {
                var el = doc.GetElement(new ElementId(tok.Value<long>()));
                if (el != null)
                {
                    el.Pinned = pinned;
                    count++;
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["pinnedCount"] = count,
                ["pinnedState"] = pinned
            };
        });
    }

    private static JObject JoinGeometry(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Join Geometry", p, () =>
        {
            var idA = p["elementIdA"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIdA.");
            var idB = p["elementIdB"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIdB.");
            var join = p["join"]?.Value<bool>() ?? true;

            var elA = doc.GetElement(new ElementId(idA)) ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element A {idA} not found.");
            var elB = doc.GetElement(new ElementId(idB)) ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element B {idB} not found.");

            bool areJoined = JoinGeometryUtils.AreElementsJoined(doc, elA, elB);

            if (join && !areJoined)
            {
                JoinGeometryUtils.JoinGeometry(doc, elA, elB);
            }
            else if (!join && areJoined)
            {
                JoinGeometryUtils.UnjoinGeometry(doc, elA, elB);
            }

            return new JObject
            {
                ["success"] = true,
                ["areJoined"] = join
            };
        });
    }

    private static JObject GroupElements(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Group Elements", p, () =>
        {
            var idsToken = p["elementIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIds.");
            var groupName = p["groupName"]?.Value<string>();

            var ids = idsToken.Select(t => new ElementId(t.Value<long>())).ToList();
            var group = doc.Create.NewGroup(ids);

            if (!string.IsNullOrWhiteSpace(groupName))
            {
                group.GroupType.Name = groupName;
            }

            return new JObject
            {
                ["success"] = true,
                ["groupId"] = group.Id.Value,
                ["groupName"] = group.GroupType.Name
            };
        });
    }

    private static JObject UngroupElements(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Ungroup Elements", p, () =>
        {
            var groupIdVal = p["groupId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing groupId.");
            var group = doc.GetElement(new ElementId(groupIdVal)) as Group
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Group {groupIdVal} not found.");

            var memberIds = group.UngroupMembers();

            return new JObject
            {
                ["success"] = true,
                ["releasedCount"] = memberIds.Count,
                ["releasedIds"] = new JArray(memberIds.Select(id => id.Value))
            };
        });
    }

    private static JObject DuplicateType(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Duplicate Type", p, () =>
        {
            var typeIdVal = p["typeId"]?.Value<long?>();
            var familyName = p["familyName"]?.Value<string>();
            var typeName = p["typeName"]?.Value<string>();
            var newName = p["newName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing newName.");

            ElementType? srcType = null;
            if (typeIdVal.HasValue)
            {
                srcType = doc.GetElement(new ElementId(typeIdVal.Value)) as ElementType;
            }
            else if (!string.IsNullOrWhiteSpace(familyName) && !string.IsNullOrWhiteSpace(typeName))
            {
                srcType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(s => string.Equals(s.FamilyName, familyName, StringComparison.OrdinalIgnoreCase) &&
                                         string.Equals(s.Name, typeName, StringComparison.OrdinalIgnoreCase));
            }

            if (srcType == null)
            {
                throw new RevitMcpException("TYPE_NOT_FOUND", $"Source type '{familyName} : {typeName}' not found.");
            }

            var duplicated = srcType.Duplicate(newName);

            // Apply parameters if provided
            var parametersObj = p["parameters"] as JObject;
            if (parametersObj != null)
            {
                ApplyParameters(doc, duplicated, parametersObj);
            }

            return new JObject
            {
                ["success"] = true,
                ["typeId"] = duplicated.Id.Value,
                ["typeName"] = duplicated.Name
            };
        });
    }

    private static JObject SetTypeParameters(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Set Type Parameters", p, () =>
        {
            var typeIdVal = p["typeId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing typeId.");
            var parametersObj = p["parameters"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing parameters.");

            var type = doc.GetElement(new ElementId(typeIdVal)) as ElementType
                ?? throw new RevitMcpException("TYPE_NOT_FOUND", $"Type {typeIdVal} not found.");

            ApplyParameters(doc, type, parametersObj);

            return new JObject
            {
                ["success"] = true,
                ["typeId"] = type.Id.Value,
                ["typeName"] = type.Name
            };
        });
    }

    private static JObject GetTypeParameters(Document doc, JObject p)
    {
        var typeIdVal = p["typeId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing typeId.");
        var type = doc.GetElement(new ElementId(typeIdVal)) as ElementType
            ?? throw new RevitMcpException("TYPE_NOT_FOUND", $"Type {typeIdVal} not found.");

        return new JObject
        {
            ["success"] = true,
            ["typeId"] = type.Id.Value,
            ["typeName"] = type.Name,
            ["parameters"] = GeometryHelpers.ExtractParameters(type, doc)
        };
    }

    private static JObject CreateWindowOrDoorType(Document doc, JObject p, bool isDoor)
    {
        return TransactionRunner.Run(doc, isDoor ? "Create Door Type" : "Create Window Type", p, () =>
        {
            var familyName = p["familyName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing familyName.");
            var newName = p["newName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing newName.");
            var widthMm = p["width"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing width.");
            var heightMm = p["height"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing height.");

            var bic = isDoor ? BuiltInCategory.OST_Doors : BuiltInCategory.OST_Windows;

            var symbol = new FilteredElementCollector(doc)
                .OfCategory(bic)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => string.Equals(s.FamilyName, familyName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("FAMILY_NOT_FOUND", $"Family '{familyName}' not found.");

            var duplicated = symbol.Duplicate(newName) as FamilySymbol
                ?? throw new RevitMcpException("REVIT_API_ERROR", "Failed to duplicate family symbol.");

            var widthParam = duplicated.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM)
                          ?? duplicated.get_Parameter(BuiltInParameter.WINDOW_WIDTH)
                          ?? duplicated.get_Parameter(BuiltInParameter.DOOR_WIDTH);
            widthParam?.Set(UnitConversions.MmToFeet(widthMm));

            var heightParam = duplicated.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM)
                           ?? duplicated.get_Parameter(BuiltInParameter.WINDOW_HEIGHT)
                           ?? duplicated.get_Parameter(BuiltInParameter.DOOR_HEIGHT);
            heightParam?.Set(UnitConversions.MmToFeet(heightMm));

            var sillMm = p["sillHeight"]?.Value<double?>();
            if (sillMm.HasValue)
            {
                var sillParam = duplicated.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                sillParam?.Set(UnitConversions.MmToFeet(sillMm.Value));
            }

            return new JObject
            {
                ["success"] = true,
                ["typeId"] = duplicated.Id.Value,
                ["typeName"] = duplicated.Name,
                ["widthMm"] = widthMm,
                ["heightMm"] = heightMm
            };
        });
    }

    private static JObject CreateDirectShape(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create DirectShape", p, () =>
        {
            var catName = p["category"]?.Value<string>() ?? "Generic Models";
            var name = p["name"]?.Value<string>() ?? "DirectShape";
            var shapeObj = p["shape"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing shape definition.");

            var bic = CategoryResolver.Resolve(catName, doc) ?? BuiltInCategory.OST_GenericModel;
            var ds = DirectShape.CreateElement(doc, new ElementId((long)bic));
            ds.Name = name;

            var shapeType = shapeObj["type"]?.Value<string>() ?? "box";
            var geometries = new List<GeometryObject>();

            if (shapeType.Equals("box", StringComparison.OrdinalIgnoreCase))
            {
                var wMm = shapeObj["width"]?.Value<double>() ?? 1000;
                var lMm = shapeObj["length"]?.Value<double>() ?? 1000;
                var hMm = shapeObj["height"]?.Value<double>() ?? 1000;
                var xMm = shapeObj["x"]?.Value<double>() ?? 0;
                var yMm = shapeObj["y"]?.Value<double>() ?? 0;
                var zMm = shapeObj["z"]?.Value<double>() ?? 0;

                var w = UnitConversions.MmToFeet(wMm);
                var l = UnitConversions.MmToFeet(lMm);
                var h = UnitConversions.MmToFeet(hMm);
                var o = UnitConversions.MmToFeet(new XYZ(xMm, yMm, zMm));

                var pts = new List<XYZ>
                {
                    o,
                    o + new XYZ(l, 0, 0),
                    o + new XYZ(l, w, 0),
                    o + new XYZ(0, w, 0)
                };

                var loop = new CurveLoop();
                for (int i = 0; i < 4; i++)
                    loop.Append(Line.CreateBound(pts[i], pts[(i + 1) % 4]));

                var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { loop }, XYZ.BasisZ, h);
                geometries.Add(solid);
            }

            ds.SetShape(geometries);

            return new JObject
            {
                ["success"] = true,
                ["elementId"] = ds.Id.Value,
                ["name"] = ds.Name,
                ["bbox"] = GeometryHelpers.ExtractBbox(ds)
            };
        });
    }

    private static JObject ModifyWall(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Modify Wall", p, () =>
        {
            var wallIdVal = p["wallId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing wallId.");
            var wall = doc.GetElement(new ElementId(wallIdVal)) as Wall
                ?? throw new RevitMcpException("WALL_NOT_FOUND", $"Wall {wallIdVal} not found.");

            var sObj = p["start"] as JObject;
            var eObj = p["end"] as JObject;
            if (sObj != null && eObj != null && wall.Location is LocationCurve lc)
            {
                var sFeet = UnitConversions.MmToFeet(new XYZ(sObj["x"]?.Value<double>() ?? 0, sObj["y"]?.Value<double>() ?? 0, sObj["z"]?.Value<double>() ?? 0));
                var eFeet = UnitConversions.MmToFeet(new XYZ(eObj["x"]?.Value<double>() ?? 0, eObj["y"]?.Value<double>() ?? 0, eObj["z"]?.Value<double>() ?? 0));
                lc.Curve = Line.CreateBound(sFeet, eFeet);
            }

            var hMm = p["height"]?.Value<double?>();
            if (hMm.HasValue)
            {
                var hParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                hParam?.Set(UnitConversions.MmToFeet(hMm.Value));
            }

            var baseOffsetMm = p["baseOffset"]?.Value<double?>();
            if (baseOffsetMm.HasValue)
            {
                var boParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                boParam?.Set(UnitConversions.MmToFeet(baseOffsetMm.Value));
            }

            var topOffsetMm = p["topOffset"]?.Value<double?>();
            if (topOffsetMm.HasValue)
            {
                var toParam = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
                toParam?.Set(UnitConversions.MmToFeet(topOffsetMm.Value));
            }

            var topConstraintName = p["topConstraintLevel"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(topConstraintName))
            {
                var lvl = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => string.Equals(l.Name, topConstraintName, StringComparison.OrdinalIgnoreCase));
                if (lvl != null)
                {
                    wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.Set(lvl.Id);
                }
            }

            var typeName = p["typeName"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                var wt = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));
                if (wt != null)
                {
                    wall.ChangeTypeId(wt.Id);
                }
            }

            doc.Regenerate();
            return new JObject
            {
                ["success"] = true,
                ["wallId"] = wall.Id.Value,
                ["bbox"] = GeometryHelpers.ExtractBbox(wall)
            };
        });
    }

    private static JObject SplitWall(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Split Wall", p, () =>
        {
            var wallIdVal = p["wallId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing wallId.");
            var distMm = p["distanceFromStart"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing distanceFromStart.");

            var wall = doc.GetElement(new ElementId(wallIdVal)) as Wall
                ?? throw new RevitMcpException("WALL_NOT_FOUND", $"Wall {wallIdVal} not found.");
            var lc = wall.Location as LocationCurve ?? throw new RevitMcpException("GEOMETRY_ERROR", "Wall has no curve.");

            var curve = lc.Curve;
            var distFeet = UnitConversions.MmToFeet(distMm);
            var param = distFeet / curve.Length;
            var splitPt = curve.Evaluate(param, true);

            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);
            lc.Curve = Line.CreateBound(p0, splitPt);

            var heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 10.0;
            var newWall = Wall.Create(doc, Line.CreateBound(splitPt, p1), wall.WallType.Id, wall.LevelId, heightParam, 0, false, false);

            return new JObject
            {
                ["success"] = true,
                ["originalWallId"] = wall.Id.Value,
                ["newWallId"] = newWall.Id.Value
            };
        });
    }

    private static JObject AttachWall(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Attach Wall", p, () =>
        {
            var wallIdsToken = p["wallIds"] as JArray ?? new JArray { p["wallId"]?.Value<long>() ?? -1L };
            var targetIdVal = p["targetId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing targetId.");
            var side = (p["side"]?.Value<string>() ?? "top").ToLowerInvariant();
            var detach = p["detach"]?.Value<bool>() ?? false;

            var target = doc.GetElement(new ElementId(targetIdVal))
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Target {targetIdVal} not found.");

            int count = 0;
            foreach (var tok in wallIdsToken)
            {
                var wall = doc.GetElement(new ElementId(tok.Value<long>())) as Wall;
                if (wall == null) continue;

                if (detach)
                {
                    wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET)?.Set(0);
                    count++;
                }
                else
                {
                    // Attach top or base to target
                    count++;
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["affectedCount"] = count
            };
        });
    }

    private static JObject CreateLevel(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Level", p, () =>
        {
            var name = p["name"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing level name.");
            var elevMm = p["elevation"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elevation.");

            var elevFeet = UnitConversions.MmToFeet(elevMm);
            var lvl = Level.Create(doc, elevFeet);
            lvl.Name = name;

            return new JObject
            {
                ["success"] = true,
                ["levelId"] = lvl.Id.Value,
                ["name"] = lvl.Name,
                ["elevationMm"] = elevMm
            };
        });
    }

    private static JObject ModifyLevel(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Modify Level", p, () =>
        {
            var levelIdVal = p["levelId"]?.Value<long?>();
            var name = p["name"]?.Value<string>();

            Level? lvl = null;
            if (levelIdVal.HasValue)
            {
                lvl = doc.GetElement(new ElementId(levelIdVal.Value)) as Level;
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                lvl = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
            }

            if (lvl == null) throw new RevitMcpException("LEVEL_NOT_FOUND", "Level not found.");

            var newName = p["newName"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(newName))
                lvl.Name = newName;

            var elevMm = p["elevation"]?.Value<double?>();
            if (elevMm.HasValue)
                lvl.Elevation = UnitConversions.MmToFeet(elevMm.Value);

            return new JObject
            {
                ["success"] = true,
                ["levelId"] = lvl.Id.Value,
                ["name"] = lvl.Name,
                ["elevationMm"] = Math.Round(UnitConversions.FeetToMm(lvl.Elevation), 1)
            };
        });
    }

    private static void ApplyParameters(Document doc, Element el, JObject parametersObj)
    {
        foreach (var prop in parametersObj.Properties())
        {
            var pName = prop.Name;
            var param = el.LookupParameter(pName);
            if (param == null || param.IsReadOnly) continue;

            if (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer)
            {
                var num = prop.Value.Value<double>();
                // Distinguish length vs integer vs double
                if (param.StorageType == StorageType.Double)
                {
                    // Convert assuming mm if length parameter
                    param.Set(UnitConversions.MmToFeet(num));
                }
                else if (param.StorageType == StorageType.Integer)
                {
                    param.Set((int)num);
                }
            }
            else if (prop.Value.Type == JTokenType.String)
            {
                param.Set(prop.Value.Value<string>());
            }
        }
    }
}
