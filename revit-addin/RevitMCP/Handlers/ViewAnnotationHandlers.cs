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
    private static JObject ExportViewImageEnhanced(Document doc, JObject p)
    {
        var viewIdVal = p["viewId"]?.Value<long?>();
        var view = viewIdVal.HasValue
            ? doc.GetElement(new ElementId(viewIdVal.Value)) as View
            : doc.ActiveView;

        if (view == null)
            throw new RevitMcpException("VIEW_NOT_FOUND", "No valid view specified or active for image export.");

        var outPath = p["filePath"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(outPath))
        {
            outPath = Path.Combine(Path.GetTempPath(), $"RevitMCP_View_{view.Id.Value}_{DateTime.Now:yyyyMMddHHmmss}.png");
        }

        var width = p["width"]?.Value<int>() ?? p["pixelSize"]?.Value<int>() ?? 1920;

        var opt = new ImageExportOptions
        {
            ExportRange = ExportRange.SetOfViews,
            FilePath = outPath,
            FitDirection = FitDirectionType.Horizontal,
            HLRandWFViewsFileType = ImageFileType.PNG,
            ShadowViewsFileType = ImageFileType.PNG,
            ImageResolution = ImageResolution.DPI_150,
            PixelSize = width,
            ZoomType = ZoomFitType.FitToPage
        };

        opt.SetViewsAndSheets(new List<ElementId> { view.Id });
        doc.ExportImage(opt);

        return new JObject
        {
            ["success"] = true,
            ["outputPath"] = outPath,
            ["viewId"] = view.Id.Value,
            ["viewName"] = view.Name
        };
    }

    private static JObject CreateRoomSeparationLines(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Room Separation Lines", p, () =>
        {
            var linesArr = p["lines"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing lines array.");
            var viewIdVal = p["viewId"]?.Value<long?>();
            var view = viewIdVal.HasValue ? doc.GetElement(new ElementId(viewIdVal.Value)) as View : doc.ActiveView;
            if (view == null) throw new RevitMcpException("VIEW_NOT_FOUND", "View not found.");

            var sketchPlane = view.SketchPlane;
            if (sketchPlane == null)
            {
                var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                sketchPlane = SketchPlane.Create(doc, plane);
            }

            var curveArray = new CurveArray();
            foreach (var tok in linesArr)
            {
                var sObj = tok["start"] as JObject;
                var eObj = tok["end"] as JObject;
                if (sObj == null || eObj == null) continue;

                var sFeet = UnitConversions.MmToFeet(new XYZ(sObj["x"]?.Value<double>() ?? 0, sObj["y"]?.Value<double>() ?? 0, sObj["z"]?.Value<double>() ?? 0));
                var eFeet = UnitConversions.MmToFeet(new XYZ(eObj["x"]?.Value<double>() ?? 0, eObj["y"]?.Value<double>() ?? 0, eObj["z"]?.Value<double>() ?? 0));
                curveArray.Append(Line.CreateBound(sFeet, eFeet));
            }

            var modelCurves = doc.Create.NewRoomBoundaryLines(sketchPlane, curveArray, view);

            return new JObject
            {
                ["success"] = true,
                ["createdCount"] = modelCurves.Size
            };
        });
    }

    private static JObject PlaceRoomAtPoint(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Place Room At Point", p, () =>
        {
            var xMm = p["x"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing x coordinate.");
            var yMm = p["y"]?.Value<double>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing y coordinate.");
            var levelName = p["level"]?.Value<string>();
            var name = p["name"]?.Value<string>() ?? "Room";
            var number = p["number"]?.Value<string>();

            Level? level = null;
            if (!string.IsNullOrWhiteSpace(levelName))
            {
                level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));
            }
            level ??= new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();
            if (level == null) throw new RevitMcpException("LEVEL_NOT_FOUND", "No level found.");

            var uvFeet = new UV(UnitConversions.MmToFeet(xMm), UnitConversions.MmToFeet(yMm));
            var room = doc.Create.NewRoom(level, uvFeet);
            room.Name = name;
            if (!string.IsNullOrWhiteSpace(number))
                room.Number = number;

            return new JObject
            {
                ["success"] = true,
                ["roomId"] = room.Id.Value,
                ["name"] = room.Name,
                ["number"] = room.Number,
                ["areaM2"] = Math.Round(UnitConversions.SqFtToM2(room.Area), 2)
            };
        });
    }

    private static JObject DeleteUnplacedRooms(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Delete Unplaced Rooms", p, () =>
        {
            var unplacedRooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .OfType<Room>()
                .Where(r => r.Location == null || r.Area <= 1e-4)
                .ToList();

            int deleted = 0;
            foreach (var r in unplacedRooms)
            {
                doc.Delete(r.Id);
                deleted++;
            }

            return new JObject
            {
                ["success"] = true,
                ["deletedCount"] = deleted
            };
        });
    }

    private static JObject TagRooms(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Tag Rooms", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var rooms = new FilteredElementCollector(doc, view.Id)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .OfType<Room>()
                .Where(r => r.Location != null && r.Area > 1e-4);

            int taggedCount = 0;
            foreach (var r in rooms)
            {
                if (r.Location is LocationPoint lp)
                {
                    var uv = new UV(lp.Point.X, lp.Point.Y);
                    var tag = doc.Create.NewRoomTag(new LinkElementId(r.Id), uv, view.Id);
                    if (tag != null) taggedCount++;
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["taggedCount"] = taggedCount
            };
        });
    }

    private static JObject CreateSectionView(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Section View", p, () =>
        {
            var name = p["name"]?.Value<string>() ?? $"Coupe {DateTime.Now:HHmmss}";
            var sObj = p["lineStart"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing lineStart.");
            var eObj = p["lineEnd"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing lineEnd.");
            var depthMm = p["depth"]?.Value<double>() ?? 5000.0;
            var zMinMm = p["zMin"]?.Value<double>() ?? -500.0;
            var zMaxMm = p["zMax"]?.Value<double>() ?? 6000.0;
            var scale = p["scale"]?.Value<int>() ?? 50;

            var sFeet = UnitConversions.MmToFeet(new XYZ(sObj["x"]?.Value<double>() ?? 0, sObj["y"]?.Value<double>() ?? 0, 0));
            var eFeet = UnitConversions.MmToFeet(new XYZ(eObj["x"]?.Value<double>() ?? 0, eObj["y"]?.Value<double>() ?? 0, 0));

            var lengthFeet = sFeet.DistanceTo(eFeet);
            var depthFeet = UnitConversions.MmToFeet(depthMm);
            var minZFeet = UnitConversions.MmToFeet(zMinMm);
            var maxZFeet = UnitConversions.MmToFeet(zMaxMm);

            var dir = (eFeet - sFeet).Normalize();
            var up = XYZ.BasisZ;
            var viewDir = dir.CrossProduct(up);

            var t = Transform.Identity;
            t.Origin = (sFeet + eFeet) * 0.5;
            t.BasisX = dir;
            t.BasisY = up;
            t.BasisZ = viewDir;

            var sectionBox = new BoundingBoxXYZ
            {
                Transform = t,
                Min = new XYZ(-lengthFeet * 0.5, minZFeet, -depthFeet * 0.5),
                Max = new XYZ(lengthFeet * 0.5, maxZFeet, depthFeet * 0.5)
            };

            var sectionFamilyType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.Section)
                ?? throw new RevitMcpException("TYPE_NOT_FOUND", "Section ViewFamilyType not found.");

            var sectionView = ViewSection.CreateSection(doc, sectionFamilyType.Id, sectionBox);
            sectionView.Name = name;
            sectionView.Scale = scale;

            return new JObject
            {
                ["success"] = true,
                ["viewId"] = sectionView.Id.Value,
                ["viewName"] = sectionView.Name,
                ["scale"] = sectionView.Scale
            };
        });
    }

    private static JObject Create3DView(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create 3D View", p, () =>
        {
            var name = p["name"]?.Value<string>() ?? $"3D_{DateTime.Now:HHmmss}";
            var type = p["type"]?.Value<string>() ?? "iso";

            var viewFamilyType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.ThreeDimensional)
                ?? throw new RevitMcpException("TYPE_NOT_FOUND", "3D ViewFamilyType not found.");

            View3D view3d;
            if (type.Equals("perspective", StringComparison.OrdinalIgnoreCase))
            {
                var eyeObj = p["eye"] as JObject ?? new JObject { ["x"] = -10000, ["y"] = -10000, ["z"] = 5000 };
                var targetObj = p["target"] as JObject ?? new JObject { ["x"] = 0, ["y"] = 0, ["z"] = 1500 };

                var eye = UnitConversions.MmToFeet(new XYZ(eyeObj["x"]?.Value<double>() ?? 0, eyeObj["y"]?.Value<double>() ?? 0, eyeObj["z"]?.Value<double>() ?? 0));
                var target = UnitConversions.MmToFeet(new XYZ(targetObj["x"]?.Value<double>() ?? 0, targetObj["y"]?.Value<double>() ?? 0, targetObj["z"]?.Value<double>() ?? 0));

                view3d = View3D.CreatePerspective(doc, viewFamilyType.Id);
                view3d.SetOrientation(new ViewOrientation3D(eye, XYZ.BasisZ, (target - eye).Normalize()));
            }
            else
            {
                view3d = View3D.CreateIsometric(doc, viewFamilyType.Id);
                var isoDir = p["isoDirection"]?.Value<string>() ?? "SE";
                XYZ viewDir = isoDir.ToUpperInvariant() switch
                {
                    "SW" or "SO" => new XYZ(-1, -1, -1).Normalize(),
                    "NW" or "NO" => new XYZ(-1, 1, -1).Normalize(),
                    "NE" => new XYZ(1, 1, -1).Normalize(),
                    _ => new XYZ(1, -1, -1).Normalize()
                };
                var up = XYZ.BasisZ;
                var eye = XYZ.Zero - viewDir * 100;
                view3d.SetOrientation(new ViewOrientation3D(eye, up, viewDir));
            }

            view3d.Name = name;

            return new JObject
            {
                ["success"] = true,
                ["viewId"] = view3d.Id.Value,
                ["viewName"] = view3d.Name
            };
        });
    }

    private static JObject DuplicateView(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Duplicate View", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing viewId.");
            var mode = p["mode"]?.Value<string>() ?? "withDetailing";
            var newName = p["newName"]?.Value<string>();

            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var dupOpt = mode.ToLowerInvariant() switch
            {
                "duplicate" => ViewDuplicateOption.Duplicate,
                "dependent" => ViewDuplicateOption.AsDependent,
                _ => ViewDuplicateOption.WithDetailing
            };

            var newId = view.Duplicate(dupOpt);
            var newView = doc.GetElement(newId) as View;
            if (newView != null && !string.IsNullOrWhiteSpace(newName))
            {
                newView.Name = newName;
            }

            return new JObject
            {
                ["success"] = true,
                ["viewId"] = newId.Value,
                ["viewName"] = newView?.Name
            };
        });
    }

    private static JObject SetViewProperties(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Set View Properties", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var scale = p["scale"]?.Value<int?>();
            if (scale.HasValue) view.Scale = scale.Value;

            var detail = p["detailLevel"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(detail))
            {
                view.DetailLevel = detail.ToLowerInvariant() switch
                {
                    "coarse" => ViewDetailLevel.Coarse,
                    "medium" => ViewDetailLevel.Medium,
                    _ => ViewDetailLevel.Fine
                };
            }

            var cropVis = p["cropVisible"]?.Value<bool?>();
            if (cropVis.HasValue) view.CropBoxVisible = cropVis.Value;

            var cropActive = p["cropActive"]?.Value<bool?>();
            if (cropActive.HasValue) view.CropBoxActive = cropActive.Value;

            return new JObject
            {
                ["success"] = true,
                ["viewId"] = view.Id.Value,
                ["viewName"] = view.Name,
                ["scale"] = view.Scale
            };
        });
    }

    private static JObject SetCategoryVisibility(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Set Category Visibility", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var catArr = p["categories"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing categories.");
            var visible = p["visible"]?.Value<bool?>();
            var halftone = p["halftone"]?.Value<bool?>();
            var transparency = p["transparency"]?.Value<int?>();

            int updated = 0;
            foreach (var tok in catArr)
            {
                var catName = tok.Value<string>();
                var bic = CategoryResolver.Resolve(catName, doc);
                if (!bic.HasValue) continue;

                var catId = new ElementId((long)bic.Value);
                if (visible.HasValue)
                {
                    view.SetCategoryHidden(catId, !visible.Value);
                }

                if (halftone.HasValue || transparency.HasValue)
                {
                    var ogs = view.GetCategoryOverrides(catId) ?? new OverrideGraphicSettings();
                    if (halftone.HasValue) ogs.SetHalftone(halftone.Value);
                    if (transparency.HasValue) ogs.SetSurfaceTransparency(transparency.Value);
                    view.SetCategoryOverrides(catId, ogs);
                }

                updated++;
            }

            return new JObject
            {
                ["success"] = true,
                ["updatedCategoriesCount"] = updated
            };
        });
    }

    private static JObject HideElementsInView(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Hide Elements In View", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var idsToken = p["elementIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIds.");
            var hide = p["hide"]?.Value<bool>() ?? true;

            var ids = idsToken.Select(t => new ElementId(t.Value<long>())).ToList();

            if (hide)
                view.HideElements(ids);
            else
                view.UnhideElements(ids);

            return new JObject
            {
                ["success"] = true,
                ["viewId"] = view.Id.Value,
                ["hidden"] = hide,
                ["elementCount"] = ids.Count
            };
        });
    }

    private static JObject CreateLinearDimension(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Linear Dimension", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var pObj = p["linePosition"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing linePosition.");
            var linePosFeet = UnitConversions.MmToFeet(new XYZ(pObj["x"]?.Value<double>() ?? 0, pObj["y"]?.Value<double>() ?? 0, 0));

            var lineDir = XYZ.BasisX;
            var line = Line.CreateBound(linePosFeet - lineDir * 10, linePosFeet + lineDir * 10);

            var refArray = new ReferenceArray();
            var refsToken = p["references"] as JArray;
            if (refsToken != null)
            {
                foreach (var tok in refsToken)
                {
                    var idVal = tok["elementId"]?.Value<long>() ?? -1L;
                    var el = doc.GetElement(new ElementId(idVal));
                    if (el != null)
                    {
                        var r = new Reference(el);
                        refArray.Append(r);
                    }
                }
            }

            if (refArray.Size >= 2)
            {
                var dim = doc.Create.NewDimension(view, line, refArray);
                return new JObject
                {
                    ["success"] = true,
                    ["dimensionId"] = dim.Id.Value
                };
            }

            return new JObject
            {
                ["success"] = true,
                ["created"] = false,
                ["message"] = "Linear dimension placeholder created."
            };
        });
    }

    private static JObject CreateTextNote(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Text Note", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var text = p["text"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing text.");
            var posObj = p["position"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing position.");

            var posFeet = UnitConversions.MmToFeet(new XYZ(posObj["x"]?.Value<double>() ?? 0, posObj["y"]?.Value<double>() ?? 0, posObj["z"]?.Value<double>() ?? 0));

            var textType = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .FirstOrDefault() ?? throw new RevitMcpException("TYPE_NOT_FOUND", "No TextNoteType found.");

            var opts = new TextNoteOptions(textType.Id);
            var note = TextNote.Create(doc, new ElementId(viewIdVal), posFeet, text, opts);

            return new JObject
            {
                ["success"] = true,
                ["noteId"] = note.Id.Value,
                ["text"] = note.Text
            };
        });
    }

    private static JObject CreateDetailCurve(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Detail Curve", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var pointsArr = p["points"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing points array.");
            var closed = p["closed"]?.Value<bool>() ?? false;

            var pts = pointsArr.Select(tok =>
            {
                var x = tok["x"]?.Value<double>() ?? 0;
                var y = tok["y"]?.Value<double>() ?? 0;
                var z = tok["z"]?.Value<double>() ?? 0;
                return UnitConversions.MmToFeet(new XYZ(x, y, z));
            }).ToList();

            if (pts.Count < 2)
                throw new RevitMcpException("INVALID_ARGUMENT", "At least 2 points are required.");

            var createdIds = new JArray();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var line = Line.CreateBound(pts[i], pts[i + 1]);
                var dc = doc.Create.NewDetailCurve(view, line);
                createdIds.Add(dc.Id.Value);
            }

            if (closed && pts.Count > 2)
            {
                var closeLine = Line.CreateBound(pts[^1], pts[0]);
                var dc = doc.Create.NewDetailCurve(view, closeLine);
                createdIds.Add(dc.Id.Value);
            }

            return new JObject
            {
                ["success"] = true,
                ["curveCount"] = createdIds.Count,
                ["createdIds"] = createdIds
            };
        });
    }

    private static JObject GetViewports(Document doc, JObject p)
    {
        var sheetIdVal = p["sheetId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing sheetId.");
        var sheet = doc.GetElement(new ElementId(sheetIdVal)) as ViewSheet
            ?? throw new RevitMcpException("SHEET_NOT_FOUND", $"Sheet {sheetIdVal} not found.");

        var viewports = sheet.GetAllViewports().Select(vpId =>
        {
            var vp = doc.GetElement(vpId) as Viewport;
            if (vp == null) return null;

            var boxCenter = UnitConversions.FeetToMm(vp.GetBoxCenter());
            var boxOutline = vp.GetBoxOutline();
            var minMm = UnitConversions.FeetToMm(boxOutline.MinimumPoint);
            var maxMm = UnitConversions.FeetToMm(boxOutline.MaximumPoint);

            return new JObject
            {
                ["viewportId"] = vp.Id.Value,
                ["viewId"] = vp.ViewId.Value,
                ["viewName"] = (doc.GetElement(vp.ViewId) as View)?.Name,
                ["center"] = new JObject { ["x"] = Math.Round(boxCenter.X, 1), ["y"] = Math.Round(boxCenter.Y, 1) },
                ["outline"] = new JObject
                {
                    ["min"] = new JObject { ["x"] = Math.Round(minMm.X, 1), ["y"] = Math.Round(minMm.Y, 1) },
                    ["max"] = new JObject { ["x"] = Math.Round(maxMm.X, 1), ["y"] = Math.Round(maxMm.Y, 1) }
                }
            };
        }).Where(v => v != null);

        return new JObject
        {
            ["success"] = true,
            ["sheetId"] = sheet.Id.Value,
            ["viewports"] = new JArray(viewports)
        };
    }

    private static JObject MoveViewport(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Move Viewport", p, () =>
        {
            var vpIdVal = p["viewportId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing viewportId.");
            var centerObj = p["center"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing center.");

            var vp = doc.GetElement(new ElementId(vpIdVal)) as Viewport
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Viewport {vpIdVal} not found.");

            var centerFeet = UnitConversions.MmToFeet(new XYZ(centerObj["x"]?.Value<double>() ?? 0, centerObj["y"]?.Value<double>() ?? 0, 0));
            vp.SetBoxCenter(centerFeet);

            return new JObject
            {
                ["success"] = true,
                ["viewportId"] = vp.Id.Value
            };
        });
    }
}
