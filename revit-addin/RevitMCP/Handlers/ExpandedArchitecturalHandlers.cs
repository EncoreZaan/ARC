using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    // TOITURES, SOLS ET ESCALIERS
    // ─────────────────────────────────────────────────────────────────────────

    private static JObject CreateRoofExtrusion(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Roof Extrusion", p, () =>
        {
            var levelName = p["levelName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "levelName requis.");
            var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Niveau '{levelName}' introuvable.");

            var startExtMm = p["startExtrusionMm"]?.Value<double>() ?? 0;
            var endExtMm = p["endExtrusionMm"]?.Value<double>() ?? 5000;
            var startExtFt = UnitConversions.MmToFeet(startExtMm);
            var endExtFt = UnitConversions.MmToFeet(endExtMm);

            var roofType = new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>().FirstOrDefault()
                ?? throw new RevitMcpException("NOT_FOUND", "Aucun RoofType disponible.");

            var profileArr = p["profile"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "profile (tableau de segments) requis.");
            var curves = new CurveArray();
            foreach (var seg in profileArr)
            {
                var sx = UnitConversions.MmToFeet(seg["startX"]?.Value<double>() ?? 0);
                var sy = UnitConversions.MmToFeet(seg["startY"]?.Value<double>() ?? 0);
                var sz = UnitConversions.MmToFeet(seg["startZ"]?.Value<double>() ?? 0);
                var ex = UnitConversions.MmToFeet(seg["endX"]?.Value<double>() ?? 0);
                var ey = UnitConversions.MmToFeet(seg["endY"]?.Value<double>() ?? 0);
                var ez = UnitConversions.MmToFeet(seg["endZ"]?.Value<double>() ?? 0);
                curves.Append(Line.CreateBound(new XYZ(sx, sy, sz), new XYZ(ex, ey, ez)));
            }

            var p0 = curves.get_Item(0).GetEndPoint(0);
            var refPlane = doc.Create.NewReferencePlane(p0, p0 + XYZ.BasisX * 10, XYZ.BasisZ, doc.ActiveView);

            var roof = doc.Create.NewExtrusionRoof(curves, refPlane, level, roofType, startExtFt, endExtFt);
            return new JObject
            {
                ["roofId"] = roof.Id.Value,
                ["roofName"] = roof.Name,
                ["typeId"] = roof.GetTypeId().Value
            };
        });
    }

    private static JObject ModifyRoof(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Modify Roof", p, () =>
        {
            var roofIdVal = p["roofId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "roofId requis.");
            var roof = doc.GetElement(new ElementId(roofIdVal)) as RoofBase
                ?? throw new RevitMcpException("NOT_FOUND", $"Toiture ID {roofIdVal} introuvable.");

            if (p["typeName"] != null)
            {
                var typeName = p["typeName"]!.Value<string>();
                var targetType = new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>()
                    .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new RevitMcpException("NOT_FOUND", $"RoofType '{typeName}' introuvable.");
                roof.ChangeTypeId(targetType.Id);
            }

            if (p["baseLevelName"] != null)
            {
                var lvlName = p["baseLevelName"]!.Value<string>();
                var lvl = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                    .FirstOrDefault(l => l.Name.Equals(lvlName, StringComparison.OrdinalIgnoreCase));
                if (lvl != null)
                {
                    var param = roof.get_Parameter(BuiltInParameter.ROOF_BASE_LEVEL_PARAM);
                    if (param != null && !param.IsReadOnly) param.Set(lvl.Id);
                }
            }

            return new JObject
            {
                ["roofId"] = roof.Id.Value,
                ["roofName"] = roof.Name,
                ["updated"] = true
            };
        });
    }

    private static JObject ModifyFloorBoundary(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Modify Floor Boundary", p, () =>
        {
            var floorIdVal = p["floorId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "floorId requis.");
            var floor = doc.GetElement(new ElementId(floorIdVal)) as Floor
                ?? throw new RevitMcpException("NOT_FOUND", $"Sol ID {floorIdVal} introuvable.");

            var boundaryArr = p["boundary"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "boundary (liste de points {x, y}) requis.");
            var curveLoop = new CurveLoop();
            for (int i = 0; i < boundaryArr.Count; i++)
            {
                var ptA = boundaryArr[i];
                var ptB = boundaryArr[(i + 1) % boundaryArr.Count];
                var p1 = new XYZ(UnitConversions.MmToFeet(ptA["x"]?.Value<double>() ?? 0), UnitConversions.MmToFeet(ptA["y"]?.Value<double>() ?? 0), 0);
                var p2 = new XYZ(UnitConversions.MmToFeet(ptB["x"]?.Value<double>() ?? 0), UnitConversions.MmToFeet(ptB["y"]?.Value<double>() ?? 0), 0);
                curveLoop.Append(Line.CreateBound(p1, p2));
            }

            var levelId = floor.LevelId;
            var floorTypeId = floor.GetTypeId();

            doc.Delete(floor.Id);
            var newFloor = Floor.Create(doc, new List<CurveLoop> { curveLoop }, floorTypeId, levelId);
            return new JObject
            {
                ["oldFloorId"] = floorIdVal,
                ["newFloorId"] = newFloor.Id.Value,
                ["levelId"] = levelId.Value
            };
        });
    }

    private static JObject CreateStair(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Stair", p, () =>
        {
            var baseLevelName = p["baseLevelName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "baseLevelName requis.");
            var topLevelName = p["topLevelName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "topLevelName requis.");

            var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
            var baseLvl = levels.FirstOrDefault(l => l.Name.Equals(baseLevelName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Niveau de base '{baseLevelName}' introuvable.");
            var topLvl = levels.FirstOrDefault(l => l.Name.Equals(topLevelName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Niveau supérieur '{topLevelName}' introuvable.");

            var sx = UnitConversions.MmToFeet(p["startX"]?.Value<double>() ?? 0);
            var sy = UnitConversions.MmToFeet(p["startY"]?.Value<double>() ?? 0);
            var ex = UnitConversions.MmToFeet(p["endX"]?.Value<double>() ?? 2500);
            var ey = UnitConversions.MmToFeet(p["endY"]?.Value<double>() ?? 0);

            var runLine = Line.CreateBound(new XYZ(sx, sy, baseLvl.Elevation), new XYZ(ex, ey, baseLvl.Elevation));

            var stairsId = ElementId.InvalidElementId;

            using (var scope = new StairsEditScope(doc, "MCP: Create Straight Stair"))
            {
                stairsId = scope.Start(baseLvl.Id, topLvl.Id);
                using (var tx = new Transaction(doc, "MCP: Add Stair Run"))
                {
                    tx.Start();
                    StairsRun.CreateStraightRun(doc, stairsId, runLine, StairsRunJustification.Center);
                    tx.Commit();
                }
                scope.Commit(new SafeFailurePreprocessor());
            }

            return new JObject
            {
                ["stairId"] = stairsId.Value,
                ["baseLevel"] = baseLvl.Name,
                ["topLevel"] = topLvl.Name
            };
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FAMILLES : CRÉATION ET ÉDITION
    // ─────────────────────────────────────────────────────────────────────────

    private static JObject CreateSimpleFamily(UIApplication app, Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Simple Family", p, () =>
        {
            var familyName = p["familyName"]?.Value<string>() ?? $"CustomFamily_{DateTime.Now:yyyyMMddHHmmss}";
            var widthMm = p["widthMm"]?.Value<double>() ?? 1000;
            var depthMm = p["depthMm"]?.Value<double>() ?? 600;
            var heightMm = p["heightMm"]?.Value<double>() ?? 800;
            var categoryName = p["category"]?.Value<string>() ?? "Mobilier";

            var category = CategoryResolver.Resolve(categoryName, doc) ?? BuiltInCategory.OST_Furniture;

            var ds = DirectShape.CreateElement(doc, new ElementId(category));
            ds.ApplicationId = "RevitMCP";
            ds.ApplicationDataId = Guid.NewGuid().ToString();

            var wFt = UnitConversions.MmToFeet(widthMm);
            var dFt = UnitConversions.MmToFeet(depthMm);
            var hFt = UnitConversions.MmToFeet(heightMm);

            var p0 = new XYZ(-wFt / 2, -dFt / 2, 0);
            var p1 = new XYZ(wFt / 2, -dFt / 2, 0);
            var p2 = new XYZ(wFt / 2, dFt / 2, 0);
            var p3 = new XYZ(-wFt / 2, dFt / 2, 0);

            var profile = new CurveLoop();
            profile.Append(Line.CreateBound(p0, p1));
            profile.Append(Line.CreateBound(p1, p2));
            profile.Append(Line.CreateBound(p2, p3));
            profile.Append(Line.CreateBound(p3, p0));

            var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { profile }, XYZ.BasisZ, hFt);
            ds.SetShape(new GeometryObject[] { solid });
            ds.Name = familyName;

            return new JObject
            {
                ["elementId"] = ds.Id.Value,
                ["familyName"] = familyName,
                ["category"] = category.ToString(),
                ["widthMm"] = widthMm,
                ["depthMm"] = depthMm,
                ["heightMm"] = heightMm
            };
        });
    }

    private static JObject EditFamilyParameters(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Edit Family Parameters", p, () =>
        {
            var familyName = p["familyName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "familyName requis.");
            var family = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>()
                .FirstOrDefault(f => f.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Famille '{familyName}' introuvable.");

            if (!family.IsEditable)
                throw new RevitMcpException("NOT_EDITABLE", $"La famille '{familyName}' n'est pas modifiable.");

            var paramsObj = p["parameters"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "parameters (clé-valeur) requis.");
            var familyDoc = doc.EditFamily(family);
            var fm = familyDoc.FamilyManager;

            var updatedCount = 0;
            using (var tx = new Transaction(familyDoc, "MCP: Update Family Params"))
            {
                tx.Start();
                foreach (var prop in paramsObj.Properties())
                {
                    var param = fm.get_Parameter(prop.Name);
                    if (param != null)
                    {
                        if (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer)
                            fm.Set(param, UnitConversions.MmToFeet(prop.Value.Value<double>()));
                        else if (prop.Value.Type == JTokenType.String)
                            fm.Set(param, prop.Value.Value<string>());
                        updatedCount++;
                    }
                }
                tx.Commit();
            }

            familyDoc.LoadFamily(doc, new SimpleFamilyLoadOptions());
            familyDoc.Close(false);

            return new JObject
            {
                ["familyName"] = familyName,
                ["updatedParametersCount"] = updatedCount
            };
        });
    }

    private class SimpleFamilyLoadOptions : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }
        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = true;
            return true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VUES, ÉLÉVATIONS, DRAFTING ET FILTRES
    // ─────────────────────────────────────────────────────────────────────────

    private static JObject CreateElevationViews(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Elevation Views", p, () =>
        {
            var planViewIdVal = p["planViewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var planView = doc.GetElement(new ElementId(planViewIdVal)) as ViewPlan
                ?? throw new RevitMcpException("INVALID_ARGUMENT", "Une vue en plan est requise pour créer un marqueur d'élévation.");

            var x = UnitConversions.MmToFeet(p["x"]?.Value<double>() ?? 0);
            var y = UnitConversions.MmToFeet(p["y"]?.Value<double>() ?? 0);
            var center = new XYZ(x, y, 0);

            var viewFamilyType = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.Elevation)
                ?? throw new RevitMcpException("NOT_FOUND", "Type d'élévation introuvable.");

            var marker = ElevationMarker.CreateElevationMarker(doc, viewFamilyType.Id, center, 100);
            var createdViews = new JArray();

            var directions = new[] { "Nord", "Est", "Sud", "Ouest" };
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    var view = marker.CreateElevation(doc, planView.Id, i);
                    view.Name = $"Élévation {directions[i]} ({DateTime.Now:HHmmss})";
                    createdViews.Add(new JObject
                    {
                        ["viewId"] = view.Id.Value,
                        ["viewName"] = view.Name,
                        ["direction"] = directions[i]
                    });
                }
                catch { }
            }

            return new JObject
            {
                ["markerId"] = marker.Id.Value,
                ["elevations"] = createdViews
            };
        });
    }

    private static JObject CreateDraftingView(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Drafting View", p, () =>
        {
            var viewName = p["name"]?.Value<string>() ?? $"Dessin 2D {DateTime.Now:HHmmss}";
            var scale = p["scale"]?.Value<int>() ?? 50;

            var vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                .FirstOrDefault(t => t.ViewFamily == ViewFamily.Drafting)
                ?? throw new RevitMcpException("NOT_FOUND", "Type de vue de dessin (Drafting) introuvable.");

            var draftingView = ViewDrafting.Create(doc, vft.Id);
            draftingView.Name = viewName;
            draftingView.Scale = scale;

            return new JObject
            {
                ["viewId"] = draftingView.Id.Value,
                ["viewName"] = draftingView.Name,
                ["scale"] = scale
            };
        });
    }

    private static JObject CreateViewFilter(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create View Filter", p, () =>
        {
            var filterName = p["filterName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "filterName requis.");
            var categoriesArr = p["categories"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "categories requis.");

            var catIds = new List<ElementId>();
            foreach (var c in categoriesArr)
            {
                var bic = CategoryResolver.Resolve(c.Value<string>(), doc);
                if (bic.HasValue) catIds.Add(new ElementId(bic.Value));
            }

            var filter = ParameterFilterElement.Create(doc, filterName, catIds);
            return new JObject
            {
                ["filterId"] = filter.Id.Value,
                ["filterName"] = filter.Name,
                ["categoriesCount"] = catIds.Count
            };
        });
    }

    private static JObject ApplyViewFilter(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Apply View Filter", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("NOT_FOUND", $"Vue ID {viewIdVal} introuvable.");

            var filterIdVal = p["filterId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "filterId requis.");
            var filterId = new ElementId(filterIdVal);

            if (!view.IsFilterApplied(filterId))
            {
                view.AddFilter(filterId);
            }

            if (p["visible"] != null)
            {
                view.SetFilterVisibility(filterId, p["visible"]!.Value<bool>());
            }

            return new JObject
            {
                ["viewId"] = view.Id.Value,
                ["filterId"] = filterIdVal,
                ["applied"] = true
            };
        });
    }

    private static JObject CreateViewTemplateFromView(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create View Template", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("NOT_FOUND", $"Vue ID {viewIdVal} introuvable.");

            var templateName = p["templateName"]?.Value<string>() ?? $"Gabarit_{view.Name}";
            var template = view.CreateViewTemplate();
            template.Name = templateName;

            return new JObject
            {
                ["templateId"] = template.Id.Value,
                ["templateName"] = template.Name,
                ["sourceViewId"] = view.Id.Value
            };
        });
    }

    private static JObject DeleteViews(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Delete Views", p, () =>
        {
            var viewIdsArr = p["viewIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "viewIds requis.");
            var ids = viewIdsArr.Select(id => new ElementId(id.Value<long>())).ToList();
            var deleted = doc.Delete(ids);

            return new JObject
            {
                ["deletedViewsCount"] = deleted.Count,
                ["deletedIds"] = new JArray(deleted.Select(d => d.Value))
            };
        });
    }

    private static JObject CloseInactiveViews(UIApplication app)
    {
        var uiDoc = app.ActiveUIDocument;
        if (uiDoc == null) return new JObject { ["success"] = true, ["message"] = "Aucun document actif." };

        var count = 0;
        var openUiViews = uiDoc.GetOpenUIViews();
        var activeViewId = uiDoc.ActiveView.Id;

        foreach (var uv in openUiViews)
        {
            if (uv.ViewId != activeViewId)
            {
                try
                {
                    uv.Close();
                    count++;
                }
                catch { }
            }
        }

        return new JObject
        {
            ["success"] = true,
            ["closedViewsCount"] = count
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COTATIONS, ÉTIQUETTES ET DÉTAILS
    // ─────────────────────────────────────────────────────────────────────────

    private static JObject AutoDimensionWalls(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Auto Dimension Walls", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("NOT_FOUND", $"Vue ID {viewIdVal} introuvable.");

            var walls = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Wall)).Cast<Wall>().ToList();

            var createdDims = new JArray();
            foreach (var w in walls)
            {
                if (w.Location is LocationCurve lc && lc.Curve is Line l)
                {
                    var refs = new ReferenceArray();
                    var opt = new Options { ComputeReferences = true, View = view };
                    var geom = w.get_Geometry(opt);
                    if (geom != null)
                    {
                        foreach (var obj in geom)
                        {
                            if (obj is Solid solid)
                            {
                                foreach (Face face in solid.Faces)
                                {
                                    if (face.Reference != null)
                                    {
                                        refs.Append(face.Reference);
                                        if (refs.Size >= 2) break;
                                    }
                                }
                            }
                            if (refs.Size >= 2) break;
                        }
                    }

                    if (refs.Size >= 2)
                    {
                        try
                        {
                            var offsetVec = XYZ.BasisZ.CrossProduct(l.Direction).Normalize() * 1.5;
                            var dimLine = Line.CreateBound(l.GetEndPoint(0) + offsetVec, l.GetEndPoint(1) + offsetVec);
                            var dim = doc.Create.NewDimension(view, dimLine, refs);
                            createdDims.Add(dim.Id.Value);
                        }
                        catch { }
                    }
                }
            }

            return new JObject
            {
                ["dimensionsCreatedCount"] = createdDims.Count,
                ["dimensionIds"] = createdDims
            };
        });
    }

    private static JObject DimensionOpenings(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Dimension Openings", p, () =>
        {
            var wallIdVal = p["wallId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "wallId requis.");
            var wall = doc.GetElement(new ElementId(wallIdVal)) as Wall
                ?? throw new RevitMcpException("NOT_FOUND", $"Mur ID {wallIdVal} introuvable.");

            if (wall.Location is not LocationCurve lc || lc.Curve is not Line l)
                throw new RevitMcpException("INVALID_GEOMETRY", "Le mur doit être linéaire.");

            // Resolve 2D floor plan view (dimensions on 3D views are disallowed)
            View? planView = null;
            if (p["viewId"] != null)
            {
                var candidate = doc.GetElement(new ElementId(p["viewId"]!.Value<long>())) as View;
                if (candidate != null && candidate.ViewType != ViewType.ThreeD && !candidate.IsTemplate)
                {
                    planView = candidate;
                }
            }

            if (planView == null)
            {
                var wallLevelId = wall.LevelId;
                planView = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewPlan))
                    .Cast<ViewPlan>()
                    .FirstOrDefault(vp => !vp.IsTemplate && vp.ViewType == ViewType.FloorPlan && vp.GenLevel != null && vp.GenLevel.Id == wallLevelId);

                planView ??= new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewPlan))
                    .Cast<ViewPlan>()
                    .FirstOrDefault(vp => !vp.IsTemplate && vp.ViewType == ViewType.FloorPlan);
            }

            if (planView == null)
                throw new RevitMcpException("NO_SUITABLE_VIEW", "Aucune vue en plan d'étage 2D disponible pour placer les cotes.");

            var inserts = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(fi => fi.Host != null && fi.Host.Id.Value == wall.Id.Value)
                .OrderBy(fi => (fi.Location as LocationPoint)?.Point.DistanceTo(l.GetEndPoint(0)) ?? 0)
                .ToList();

            if (inserts.Count == 0)
            {
                throw new RevitMcpException("NO_OPENINGS", $"Le mur {wall.Id.Value} ne contient aucune ouverture à coter.");
            }

            var refs = new ReferenceArray();
            var opt = new Options { ComputeReferences = true, View = planView, IncludeNonVisibleObjects = true };

            Reference? wallStartRef = null;
            Reference? wallEndRef = null;

            var wallGeom = wall.get_Geometry(opt);
            if (wallGeom != null)
            {
                foreach (var obj in wallGeom)
                {
                    if (obj is Solid solid)
                    {
                        foreach (Face f in solid.Faces)
                        {
                            if (f is PlanarFace pf && f.Reference != null)
                            {
                                var dot = pf.FaceNormal.DotProduct(l.Direction);
                                if (dot < -0.8 && wallStartRef == null)
                                    wallStartRef = f.Reference;
                                else if (dot > 0.8 && wallEndRef == null)
                                    wallEndRef = f.Reference;
                            }
                        }
                    }
                }
            }

            if (wallStartRef != null)
                refs.Append(wallStartRef);

            foreach (var inst in inserts)
            {
                var leftRefs = inst.GetReferences(FamilyInstanceReferenceType.Left);
                var rightRefs = inst.GetReferences(FamilyInstanceReferenceType.Right);

                if (leftRefs.Count > 0)
                    refs.Append(leftRefs[0]);
                else
                {
                    var centerRefs = inst.GetReferences(FamilyInstanceReferenceType.CenterLeftRight);
                    if (centerRefs.Count > 0)
                        refs.Append(centerRefs[0]);
                }

                if (rightRefs.Count > 0)
                    refs.Append(rightRefs[0]);
            }

            if (wallEndRef != null)
                refs.Append(wallEndRef);

            if (refs.Size < 2)
            {
                throw new RevitMcpException("INSUFFICIENT_REFERENCES",
                    $"Références insuffisantes ({refs.Size} trouvées, minimum 2 requises). Vérifiez la géométrie du mur et des baies.");
            }

            var wallDir = l.Direction.Normalize();
            var normal2D = new XYZ(-wallDir.Y, wallDir.X, 0).Normalize();
            var offsetDistFeet = UnitConversions.MmToFeet(600.0);
            var dimLine = Line.CreateBound(l.GetEndPoint(0) + normal2D * offsetDistFeet, l.GetEndPoint(1) + normal2D * offsetDistFeet);

            Dimension dim;
            try
            {
                dim = doc.Create.NewDimension(planView, dimLine, refs);
            }
            catch (Exception ex)
            {
                throw new RevitMcpException("DIMENSION_CREATION_FAILED", $"Échec de création de la cote : {ex.Message}");
            }

            if (dim == null || dim.Id.Value < 0)
            {
                throw new RevitMcpException("DIMENSION_CREATION_FAILED", "Revit a échoué à générer l'objet Dimension.");
            }

            return new JObject
            {
                ["success"] = true,
                ["wallId"] = wall.Id.Value,
                ["dimensionId"] = dim.Id.Value,
                ["viewId"] = planView.Id.Value,
                ["viewName"] = planView.Name,
                ["openingsCount"] = inserts.Count,
                ["referencesCount"] = refs.Size
            };
        });
    }

    private static JObject CreateLineStyle(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Line Style", p, () =>
        {
            var styleName = p["name"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "name requis.");
            var weight = p["weight"]?.Value<int>() ?? 2;
            var r = p["red"]?.Value<byte>() ?? 0;
            var g = p["green"]?.Value<byte>() ?? 0;
            var b = p["blue"]?.Value<byte>() ?? 0;

            var linesCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            var subCats = linesCat.SubCategories;

            Category subCat;
            if (subCats.Contains(styleName))
            {
                subCat = subCats.get_Item(styleName);
            }
            else
            {
                subCat = doc.Settings.Categories.NewSubcategory(linesCat, styleName);
            }

            subCat.LineColor = new Color(r, g, b);
            subCat.SetLineWeight(weight, GraphicsStyleType.Projection);

            return new JObject
            {
                ["name"] = styleName,
                ["weight"] = weight,
                ["color"] = $"#{r:X2}{g:X2}{b:X2}"
            };
        });
    }

    private static JObject PlaceAnnotationSymbol(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Place Annotation Symbol", p, () =>
        {
            var symbolName = p["symbolName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "symbolName requis.");
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("NOT_FOUND", $"Vue ID {viewIdVal} introuvable.");

            var x = UnitConversions.MmToFeet(p["x"]?.Value<double>() ?? 0);
            var y = UnitConversions.MmToFeet(p["y"]?.Value<double>() ?? 0);

            var symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.Name.Equals(symbolName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Symbole d'annotation '{symbolName}' introuvable.");

            if (!symbol.IsActive) symbol.Activate();

            var inst = doc.Create.NewFamilyInstance(new XYZ(x, y, 0), symbol, view);
            return new JObject
            {
                ["symbolId"] = inst.Id.Value,
                ["symbolName"] = symbol.Name,
                ["viewId"] = view.Id.Value
            };
        });
    }

    private static JObject PlaceDetailComponent(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Place Detail Component", p, () =>
        {
            var componentName = p["componentName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "componentName requis.");
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("NOT_FOUND", $"Vue ID {viewIdVal} introuvable.");

            var x = UnitConversions.MmToFeet(p["x"]?.Value<double>() ?? 0);
            var y = UnitConversions.MmToFeet(p["y"]?.Value<double>() ?? 0);

            var fs = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_DetailComponents)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.Name.Equals(componentName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Composant de détail '{componentName}' introuvable.");

            if (!fs.IsActive) fs.Activate();

            var inst = doc.Create.NewFamilyInstance(new XYZ(x, y, 0), fs, view);
            return new JObject
            {
                ["componentId"] = inst.Id.Value,
                ["componentName"] = fs.Name,
                ["viewId"] = view.Id.Value
            };
        });
    }

    private static JObject CreateLegendView(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Legend View", p, () =>
        {
            var name = p["name"]?.Value<string>() ?? $"Légende {DateTime.Now:HHmmss}";
            var scale = p["scale"]?.Value<int>() ?? 50;

            var existingLegend = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                .FirstOrDefault(v => v.ViewType == ViewType.Legend);

            View legend;
            if (existingLegend != null)
            {
                var newId = existingLegend.Duplicate(ViewDuplicateOption.Duplicate);
                legend = (View)doc.GetElement(newId);
                legend.Name = name;
            }
            else
            {
                var vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                    .FirstOrDefault(t => t.ViewFamily == ViewFamily.Drafting)
                    ?? throw new RevitMcpException("NOT_FOUND", "Type de vue 2D introuvable.");
                legend = ViewDrafting.Create(doc, vft.Id);
                legend.Name = name;
            }

            legend.Scale = scale;
            return new JObject
            {
                ["legendId"] = legend.Id.Value,
                ["name"] = legend.Name,
                ["scale"] = scale
            };
        });
    }

    private static JObject PlaceLegendComponent(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Place Legend Component", p, () =>
        {
            var legendIdVal = p["legendId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "legendId requis.");
            var legend = doc.GetElement(new ElementId(legendIdVal)) as View
                ?? throw new RevitMcpException("NOT_FOUND", $"Légende ID {legendIdVal} introuvable.");

            var familyTypeName = p["familyTypeName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "familyTypeName requis.");
            var x = UnitConversions.MmToFeet(p["x"]?.Value<double>() ?? 0);
            var y = UnitConversions.MmToFeet(p["y"]?.Value<double>() ?? 0);

            var fs = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                .FirstOrDefault(s => s.Name.Equals(familyTypeName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Type de famille '{familyTypeName}' introuvable.");

            if (!fs.IsActive) fs.Activate();
            var inst = doc.Create.NewFamilyInstance(new XYZ(x, y, 0), fs, legend);

            return new JObject
            {
                ["instanceId"] = inst.Id.Value,
                ["familyTypeName"] = fs.Name,
                ["legendId"] = legend.Id.Value
            };
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COULEURS DE PIÈCES & SURFACES
    // ─────────────────────────────────────────────────────────────────────────

    private static JObject CreateColorFillScheme(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Color Fill Scheme", p, () =>
        {
            var name = p["name"]?.Value<string>() ?? "Schéma Pièces";
            var categoryName = p["category"]?.Value<string>() ?? "Pièces";
            var bic = CategoryResolver.Resolve(categoryName, doc) ?? BuiltInCategory.OST_Rooms;

            var schemes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_ColorFillSchema)
                .Cast<ColorFillScheme>()
                .ToList();

            var scheme = schemes.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? schemes.FirstOrDefault();

            return new JObject
            {
                ["schemeId"] = scheme?.Id.Value ?? -1,
                ["schemeName"] = scheme?.Name ?? name,
                ["category"] = bic.ToString()
            };
        });
    }

    private static JObject ApplyColorFill(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Apply Color Fill", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("NOT_FOUND", $"Vue ID {viewIdVal} introuvable.");

            var x = UnitConversions.MmToFeet(p["legendX"]?.Value<double>() ?? 5000);
            var y = UnitConversions.MmToFeet(p["legendY"]?.Value<double>() ?? 5000);

            var roomCatId = new ElementId(BuiltInCategory.OST_Rooms);
            ColorFillLegend? legend = null;
            try
            {
                legend = ColorFillLegend.Create(doc, view.Id, roomCatId, new XYZ(x, y, 0));
            }
            catch { }

            return new JObject
            {
                ["viewId"] = view.Id.Value,
                ["legendId"] = legend?.Id.Value ?? -1,
                ["applied"] = true
            };
        });
    }

    private static JObject CreateAreaPlan(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Area Plan", p, () =>
        {
            var levelName = p["levelName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "levelName requis.");
            var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Niveau '{levelName}' introuvable.");

            var areaScheme = new FilteredElementCollector(doc).OfClass(typeof(AreaScheme)).Cast<AreaScheme>().FirstOrDefault()
                ?? throw new RevitMcpException("NOT_FOUND", "Aucun AreaScheme disponible.");

            var areaPlan = ViewPlan.CreateAreaPlan(doc, areaScheme.Id, level.Id);
            return new JObject
            {
                ["areaPlanId"] = areaPlan.Id.Value,
                ["areaPlanName"] = areaPlan.Name,
                ["schemeName"] = areaScheme.Name,
                ["levelName"] = level.Name
            };
        });
    }

    private static JObject CreateArea(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Create Area", p, () =>
        {
            var viewIdVal = p["areaPlanId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var viewPlan = doc.GetElement(new ElementId(viewIdVal)) as ViewPlan
                ?? throw new RevitMcpException("INVALID_ARGUMENT", "La vue spécifiée doit être un plan de surface.");

            var x = UnitConversions.MmToFeet(p["x"]?.Value<double>() ?? 0);
            var y = UnitConversions.MmToFeet(p["y"]?.Value<double>() ?? 0);

            var area = doc.Create.NewArea(viewPlan, new UV(x, y));
            return new JObject
            {
                ["areaId"] = area.Id.Value,
                ["number"] = area.Number,
                ["areaSqm"] = UnitConversions.SqFtToM2(area.Area)
            };
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FEUILLES, CARTOUCHES ET VIEWPORTS
    // ─────────────────────────────────────────────────────────────────────────

    private static JObject ChangeTitleBlock(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Change Title Block", p, () =>
        {
            var sheetIdVal = p["sheetId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "sheetId requis.");
            var sheet = doc.GetElement(new ElementId(sheetIdVal)) as ViewSheet
                ?? throw new RevitMcpException("NOT_FOUND", $"Feuille ID {sheetIdVal} introuvable.");

            var newTitleBlockTypeName = p["titleBlockName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "titleBlockName requis.");
            var targetTb = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(tb => tb.Name.Equals(newTitleBlockTypeName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Cartouche '{newTitleBlockTypeName}' introuvable.");

            var existingTb = new FilteredElementCollector(doc, sheet.Id)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .FirstOrDefault();

            if (existingTb != null)
            {
                existingTb.ChangeTypeId(targetTb.Id);
            }
            else
            {
                doc.Create.NewFamilyInstance(XYZ.Zero, targetTb, sheet);
            }

            return new JObject
            {
                ["sheetId"] = sheet.Id.Value,
                ["titleBlockName"] = targetTb.Name,
                ["changed"] = true
            };
        });
    }

    private static JObject SetSheetParameters(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Set Sheet Parameters", p, () =>
        {
            var sheetIdVal = p["sheetId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "sheetId requis.");
            var sheet = doc.GetElement(new ElementId(sheetIdVal)) as ViewSheet
                ?? throw new RevitMcpException("NOT_FOUND", $"Feuille ID {sheetIdVal} introuvable.");

            if (p["sheetNumber"] != null) sheet.SheetNumber = p["sheetNumber"]!.Value<string>();
            if (p["sheetName"] != null) sheet.Name = p["sheetName"]!.Value<string>();

            var paramMap = new Dictionary<string, BuiltInParameter>
            {
                ["drawnBy"] = BuiltInParameter.SHEET_DRAWN_BY,
                ["checkedBy"] = BuiltInParameter.SHEET_CHECKED_BY,
                ["approvedBy"] = BuiltInParameter.SHEET_APPROVED_BY,
                ["issueDate"] = BuiltInParameter.SHEET_ISSUE_DATE
            };

            foreach (var kvp in paramMap)
            {
                if (p[kvp.Key] != null)
                {
                    var param = sheet.get_Parameter(kvp.Value);
                    if (param != null && !param.IsReadOnly) param.Set(p[kvp.Key]!.Value<string>());
                }
            }

            return new JObject
            {
                ["sheetId"] = sheet.Id.Value,
                ["sheetNumber"] = sheet.SheetNumber,
                ["sheetName"] = sheet.Name,
                ["updated"] = true
            };
        });
    }

    private static JObject SetViewportType(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Set Viewport Type", p, () =>
        {
            var viewportIdVal = p["viewportId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "viewportId requis.");
            var vp = doc.GetElement(new ElementId(viewportIdVal)) as Viewport
                ?? throw new RevitMcpException("NOT_FOUND", $"Viewport ID {viewportIdVal} introuvable.");

            var typeName = p["typeName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "typeName requis.");
            var targetType = new FilteredElementCollector(doc).OfClass(typeof(ElementType)).Cast<ElementType>()
                .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("NOT_FOUND", $"Type de viewport '{typeName}' introuvable.");

            vp.ChangeTypeId(targetType.Id);
            return new JObject
            {
                ["viewportId"] = vp.Id.Value,
                ["typeName"] = targetType.Name,
                ["changed"] = true
            };
        });
    }

    private static JObject PlaceScheduleOnSheet(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Place Schedule On Sheet", p, () =>
        {
            var sheetIdVal = p["sheetId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "sheetId requis.");
            var sheet = doc.GetElement(new ElementId(sheetIdVal)) as ViewSheet
                ?? throw new RevitMcpException("NOT_FOUND", $"Feuille ID {sheetIdVal} introuvable.");

            var scheduleIdVal = p["scheduleId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "scheduleId requis.");
            var schedule = doc.GetElement(new ElementId(scheduleIdVal)) as ViewSchedule
                ?? throw new RevitMcpException("NOT_FOUND", $"Nomenclature ID {scheduleIdVal} introuvable.");

            var x = UnitConversions.MmToFeet(p["x"]?.Value<double>() ?? 100);
            var y = UnitConversions.MmToFeet(p["y"]?.Value<double>() ?? 100);

            var ssi = ScheduleSheetInstance.Create(doc, sheet.Id, schedule.Id, new XYZ(x, y, 0));
            return new JObject
            {
                ["instanceId"] = ssi.Id.Value,
                ["sheetId"] = sheet.Id.Value,
                ["scheduleId"] = schedule.Id.Value
            };
        });
    }

    private static JObject AlignViewports(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "MCP: Align Viewports", p, () =>
        {
            var viewportIdsArr = p["viewportIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "viewportIds requis.");
            var alignMode = p["mode"]?.Value<string>() ?? "horizontal";

            var vps = viewportIdsArr.Select(id => doc.GetElement(new ElementId(id.Value<long>())) as Viewport)
                .Where(vp => vp != null).Cast<Viewport>().ToList();

            if (vps.Count < 2) throw new RevitMcpException("INVALID_ARGUMENT", "Au moins 2 viewports sont requis pour l'alignement.");

            var refCenter = vps[0].GetBoxCenter();
            var movedCount = 0;

            for (int i = 1; i < vps.Count; i++)
            {
                var curCenter = vps[i].GetBoxCenter();
                var newCenter = alignMode.Equals("horizontal", StringComparison.OrdinalIgnoreCase)
                    ? new XYZ(curCenter.X, refCenter.Y, curCenter.Z)
                    : new XYZ(refCenter.X, curCenter.Y, curCenter.Z);

                vps[i].SetBoxCenter(newCenter);
                movedCount++;
            }

            return new JObject
            {
                ["alignedCount"] = movedCount + 1,
                ["alignMode"] = alignMode
            };
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RENDU & DYNAMO INLINE
    // ─────────────────────────────────────────────────────────────────────────

    private static JObject RenderView(Document doc, JObject p)
    {
        var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
        var view = doc.GetElement(new ElementId(viewIdVal)) as View
            ?? throw new RevitMcpException("NOT_FOUND", $"Vue ID {viewIdVal} introuvable.");

        var outputPath = p["outputPath"]?.Value<string>() ?? Path.Combine(Path.GetTempPath(), $"Render_{view.Id}_{DateTime.Now:yyyyMMddHHmmss}.png");
        var width = p["width"]?.Value<int>() ?? 1920;

        var opt = new ImageExportOptions
        {
            ExportRange = ExportRange.SetOfViews,
            FilePath = outputPath,
            FitDirection = FitDirectionType.Horizontal,
            HLRandWFViewsFileType = ImageFileType.PNG,
            ShadowViewsFileType = ImageFileType.PNG,
            ImageResolution = ImageResolution.DPI_300,
            PixelSize = width,
            ZoomType = ZoomFitType.FitToPage
        };
        opt.SetViewsAndSheets(new List<ElementId> { view.Id });

        doc.ExportImage(opt);

        return new JObject
        {
            ["viewId"] = view.Id.Value,
            ["outputPath"] = outputPath,
            ["width"] = width,
            ["rendered"] = true
        };
    }

    private static JObject RunDynamoInline(UIApplication app, Document doc, JObject p)
    {
        return ExecutePython(app, doc, p);
    }
}
