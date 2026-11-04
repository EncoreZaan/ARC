using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RevitMCP;

public partial class RevitEventHandler
{
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private static JObject LoadFamily(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Load Family", p, () =>
        {
            var path = p["path"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing path.");
            if (!File.Exists(path))
                throw new RevitMcpException("FILE_NOT_FOUND", $"Family file '{path}' not found.");

            var loaded = doc.LoadFamily(path, out var family);
            if (!loaded && family == null)
            {
                // Find existing family if already loaded
                var famName = Path.GetFileNameWithoutExtension(path);
                family = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .FirstOrDefault(f => string.Equals(f.Name, famName, StringComparison.OrdinalIgnoreCase));
            }

            var symbols = family?.GetFamilySymbolIds().Select(id => doc.GetElement(id) as FamilySymbol).Where(s => s != null);

            return new JObject
            {
                ["success"] = true,
                ["familyId"] = family?.Id.Value ?? -1L,
                ["familyName"] = family?.Name ?? Path.GetFileNameWithoutExtension(path),
                ["types"] = new JArray(symbols?.Select(s => new JObject { ["id"] = s!.Id.Value, ["name"] = s.Name }) ?? Enumerable.Empty<JObject>())
            };
        });
    }

    private static JObject ListFamilyLibrary(JObject p)
    {
        var defaultLib = @"C:\ProgramData\Autodesk\RVT 2027\Libraries\France";
        if (!Directory.Exists(defaultLib))
            defaultLib = @"C:\ProgramData\Autodesk\RVT 2027\Libraries\English";
        if (!Directory.Exists(defaultLib))
            defaultLib = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var folder = p["folder"]?.Value<string>() ?? defaultLib;
        var search = p["search"]?.Value<string>() ?? "*.rfa";

        var files = new JArray();
        if (Directory.Exists(folder))
        {
            var rfaFiles = Directory.GetFiles(folder, search.EndsWith(".rfa") ? search : $"*{search}*.rfa", SearchOption.AllDirectories);
            foreach (var f in rfaFiles.Take(100))
            {
                files.Add(new JObject
                {
                    ["name"] = Path.GetFileName(f),
                    ["path"] = f,
                    ["directory"] = Path.GetDirectoryName(f)
                });
            }
        }

        return new JObject
        {
            ["success"] = true,
            ["libraryFolder"] = folder,
            ["count"] = files.Count,
            ["families"] = files
        };
    }

    private static JObject CreateRoofFootprint(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Roof Footprint", p, () =>
        {
            var levelName = p["level"]?.Value<string>();
            var boundaryArr = p["boundary"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing boundary points.");
            var slopedEdgesArr = p["slopedEdges"] as JArray;
            var typeName = p["typeName"]?.Value<string>();

            Level? level = null;
            if (!string.IsNullOrWhiteSpace(levelName))
            {
                level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));
            }
            level ??= new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault()
                ?? throw new RevitMcpException("LEVEL_NOT_FOUND", "No level found.");

            RoofType? roofType = null;
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                roofType = new FilteredElementCollector(doc)
                    .OfClass(typeof(RoofType))
                    .Cast<RoofType>()
                    .FirstOrDefault(rt => string.Equals(rt.Name, typeName, StringComparison.OrdinalIgnoreCase));
            }
            roofType ??= new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>().FirstOrDefault()
                ?? throw new RevitMcpException("TYPE_NOT_FOUND", "No RoofType found.");

            var curveArray = new CurveArray();
            var pts = boundaryArr.Select(tok =>
            {
                var x = tok["x"]?.Value<double>() ?? 0;
                var y = tok["y"]?.Value<double>() ?? 0;
                var z = tok["z"]?.Value<double>() ?? 0;
                return UnitConversions.MmToFeet(new XYZ(x, y, z));
            }).ToList();

            for (int i = 0; i < pts.Count; i++)
            {
                var p1 = pts[i];
                var p2 = pts[(i + 1) % pts.Count];
                curveArray.Append(Line.CreateBound(p1, p2));
            }

            var mapping = new ModelCurveArray();
            var roof = doc.Create.NewFootPrintRoof(curveArray, level, roofType, out mapping);

            // Apply slope to specified edges
            if (slopedEdgesArr != null && mapping != null)
            {
                var curvesList = mapping.Cast<ModelCurve>().ToList();
                foreach (var sEdge in slopedEdgesArr)
                {
                    var edgeIdx = sEdge["edgeIndex"]?.Value<int>() ?? 0;
                    var slopeDeg = sEdge["slopeDeg"]?.Value<double?>() ?? 30.0;
                    if (edgeIdx >= 0 && edgeIdx < curvesList.Count)
                    {
                        var mc = curvesList[edgeIdx];
                        roof.set_DefinesSlope(mc, true);
                        roof.set_SlopeAngle(mc, UnitConversions.DegToRad(slopeDeg));
                    }
                }
            }

            var baseOffsetMm = p["baseOffset"]?.Value<double?>();
            if (baseOffsetMm.HasValue)
            {
                roof.get_Parameter(BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM)?.Set(UnitConversions.MmToFeet(baseOffsetMm.Value));
            }

            doc.Regenerate();
            return new JObject
            {
                ["success"] = true,
                ["roofId"] = roof.Id.Value,
                ["bbox"] = GeometryHelpers.ExtractBbox(roof)
            };
        });
    }

    private static JObject CreateCeiling(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Ceiling", p, () =>
        {
            var levelName = p["level"]?.Value<string>();
            var boundaryArr = p["boundary"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing boundary.");
            var offsetMm = p["heightOffset"]?.Value<double>() ?? 2500.0;

            var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                .FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase))
                ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault()
                ?? throw new RevitMcpException("LEVEL_NOT_FOUND", "No level found.");

            var pts = boundaryArr.Select(tok =>
            {
                var x = tok["x"]?.Value<double>() ?? 0;
                var y = tok["y"]?.Value<double>() ?? 0;
                var z = tok["z"]?.Value<double>() ?? 0;
                return UnitConversions.MmToFeet(new XYZ(x, y, z));
            }).ToList();

            var loop = new CurveLoop();
            for (int i = 0; i < pts.Count; i++)
            {
                loop.Append(Line.CreateBound(pts[i], pts[(i + 1) % pts.Count]));
            }

            var ceilingType = new FilteredElementCollector(doc)
                .OfClass(typeof(CeilingType))
                .Cast<CeilingType>()
                .FirstOrDefault() ?? throw new RevitMcpException("TYPE_NOT_FOUND", "No CeilingType found.");

            var ceiling = Ceiling.Create(doc, new List<CurveLoop> { loop }, ceilingType.Id, level.Id);
            ceiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM)?.Set(UnitConversions.MmToFeet(offsetMm));

            doc.Regenerate();
            return new JObject
            {
                ["success"] = true,
                ["ceilingId"] = ceiling.Id.Value,
                ["bbox"] = GeometryHelpers.ExtractBbox(ceiling)
            };
        });
    }

    private static JObject CreateRailing(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Railing", p, () =>
        {
            var pathArr = p["path"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing path.");
            var levelName = p["level"]?.Value<string>();

            var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                .FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase))
                ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault()
                ?? throw new RevitMcpException("LEVEL_NOT_FOUND", "No level found.");

            var pts = pathArr.Select(tok =>
            {
                var x = tok["x"]?.Value<double>() ?? 0;
                var y = tok["y"]?.Value<double>() ?? 0;
                var z = tok["z"]?.Value<double>() ?? 0;
                return UnitConversions.MmToFeet(new XYZ(x, y, z));
            }).ToList();

            var loop = new CurveLoop();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                loop.Append(Line.CreateBound(pts[i], pts[i + 1]));
            }

            var railingType = new FilteredElementCollector(doc)
                .OfClass(typeof(RailingType))
                .Cast<RailingType>()
                .FirstOrDefault() ?? throw new RevitMcpException("TYPE_NOT_FOUND", "No RailingType found.");

            var railing = Railing.Create(doc, loop, railingType.Id, level.Id);

            doc.Regenerate();
            return new JObject
            {
                ["success"] = true,
                ["railingId"] = railing.Id.Value,
                ["bbox"] = GeometryHelpers.ExtractBbox(railing)
            };
        });
    }

    private static JObject CreateOpening(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Opening", p, () =>
        {
            var hostIdVal = p["hostId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing hostId.");
            var host = doc.GetElement(new ElementId(hostIdVal))
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Host {hostIdVal} not found.");

            var boundaryArr = p["boundary"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing boundary points.");
            var pts = boundaryArr.Select(tok =>
            {
                var x = tok["x"]?.Value<double>() ?? 0;
                var y = tok["y"]?.Value<double>() ?? 0;
                var z = tok["z"]?.Value<double>() ?? 0;
                return UnitConversions.MmToFeet(new XYZ(x, y, z));
            }).ToList();

            var curveArray = new CurveArray();
            for (int i = 0; i < pts.Count; i++)
            {
                curveArray.Append(Line.CreateBound(pts[i], pts[(i + 1) % pts.Count]));
            }

            var opening = doc.Create.NewOpening(host, curveArray, true);

            return new JObject
            {
                ["success"] = true,
                ["openingId"] = opening.Id.Value
            };
        });
    }

    private static JObject CreateLadder(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Ladder", p, () =>
        {
            var baseObj = p["base"] as JObject ?? new JObject { ["x"] = 0, ["y"] = 0 };
            var widthMm = p["width"]?.Value<double>() ?? 500.0;
            var heightMm = p["height"]?.Value<double>() ?? 2600.0;
            var rungSpacingMm = p["rungSpacing"]?.Value<double>() ?? 250.0;

            var ds = DirectShape.CreateElement(doc, new ElementId((long)BuiltInCategory.OST_GenericModel));
            ds.Name = "Echelle meunier";

            var bX = UnitConversions.MmToFeet(baseObj["x"]?.Value<double>() ?? 0);
            var bY = UnitConversions.MmToFeet(baseObj["y"]?.Value<double>() ?? 0);
            var w = UnitConversions.MmToFeet(widthMm);
            var h = UnitConversions.MmToFeet(heightMm);
            var spacing = UnitConversions.MmToFeet(rungSpacingMm);

            var upright1 = GeometryCreationUtilities.CreateExtrusionGeometry(
                new List<CurveLoop> { CurveLoop.CreateViaThicken(Line.CreateBound(new XYZ(bX, bY, 0), new XYZ(bX, bY + 0.1, 0)), 0.1, XYZ.BasisZ) },
                XYZ.BasisZ, h);

            ds.SetShape(new List<GeometryObject> { upright1 });

            return new JObject
            {
                ["success"] = true,
                ["elementId"] = ds.Id.Value,
                ["name"] = ds.Name,
                ["heightMm"] = heightMm,
                ["widthMm"] = widthMm
            };
        });
    }

    private static JObject CreateSpotElevation(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Spot Elevation", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var elIdVal = p["elementId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementId.");
            var el = doc.GetElement(new ElementId(elIdVal)) ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element {elIdVal} not found.");

            var ptObj = p["point"] as JObject ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing point.");
            var ptFeet = UnitConversions.MmToFeet(new XYZ(ptObj["x"]?.Value<double>() ?? 0, ptObj["y"]?.Value<double>() ?? 0, ptObj["z"]?.Value<double>() ?? 0));

            var spot = doc.Create.NewSpotElevation(view, new Reference(el), ptFeet, ptFeet + XYZ.BasisX * 2, ptFeet + XYZ.BasisX * 3, ptFeet, true);

            return new JObject
            {
                ["success"] = true,
                ["spotElevationId"] = spot.Id.Value
            };
        });
    }

    private static JObject CreateTag(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Tag", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var elIdVal = p["elementId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementId.");
            var leader = p["leader"]?.Value<bool>() ?? false;

            var el = doc.GetElement(new ElementId(elIdVal))
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element {elIdVal} not found.");

            var tag = IndependentTag.Create(doc, new ElementId(viewIdVal), new Reference(el), leader, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, (el.Location as LocationPoint)?.Point ?? XYZ.Zero);

            return new JObject
            {
                ["success"] = true,
                ["tagId"] = tag.Id.Value
            };
        });
    }

    private static JObject TagAll(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Tag All", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var catArr = p["categories"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing categories.");

            int taggedCount = 0;
            foreach (var tok in catArr)
            {
                var bic = CategoryResolver.Resolve(tok.Value<string>(), doc);
                if (!bic.HasValue) continue;

                var elements = new FilteredElementCollector(doc, view.Id)
                    .OfCategory(bic.Value)
                    .WhereElementIsNotElementType();

                foreach (var el in elements)
                {
                    try
                    {
                        var tag = IndependentTag.Create(doc, view.Id, new Reference(el), false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, (el.Location as LocationPoint)?.Point ?? XYZ.Zero);
                        if (tag != null) taggedCount++;
                    }
                    catch { }
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["taggedCount"] = taggedCount
            };
        });
    }

    private static JObject CreateFilledRegion(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Filled Region", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var boundaryArr = p["boundary"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing boundary points.");

            var pts = boundaryArr.Select(tok =>
            {
                var x = tok["x"]?.Value<double>() ?? 0;
                var y = tok["y"]?.Value<double>() ?? 0;
                var z = tok["z"]?.Value<double>() ?? 0;
                return UnitConversions.MmToFeet(new XYZ(x, y, z));
            }).ToList();

            var loop = new CurveLoop();
            for (int i = 0; i < pts.Count; i++)
            {
                loop.Append(Line.CreateBound(pts[i], pts[(i + 1) % pts.Count]));
            }

            var frType = new FilteredElementCollector(doc)
                .OfClass(typeof(FilledRegionType))
                .Cast<FilledRegionType>()
                .FirstOrDefault() ?? throw new RevitMcpException("TYPE_NOT_FOUND", "No FilledRegionType found.");

            var fr = FilledRegion.Create(doc, frType.Id, new ElementId(viewIdVal), new List<CurveLoop> { loop });

            return new JObject
            {
                ["success"] = true,
                ["filledRegionId"] = fr.Id.Value
            };
        });
    }

    private static JObject CaptureUi(UIApplication app, JObject p)
    {
        var outPath = p["outputPath"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(outPath))
        {
            outPath = Path.Combine(Path.GetTempPath(), $"RevitMCP_UI_{DateTime.Now:yyyyMMddHHmmss}.png");
        }

        var hwnd = app.MainWindowHandle;
        if (hwnd == IntPtr.Zero)
            throw new RevitMcpException("UI_ERROR", "Could not obtain Revit main window handle.");

        GetWindowRect(hwnd, out var rect);
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
        {
            width = 1920;
            height = 1080;
        }

        using var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        bmp.Save(outPath, ImageFormat.Png);

        return new JObject
        {
            ["success"] = true,
            ["outputPath"] = outPath,
            ["width"] = width,
            ["height"] = height
        };
    }

    private static JObject GetLog(JObject p)
    {
        var linesCount = p["lines"]?.Value<int>() ?? 50;
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins", "2027");
        var auditPath = Path.Combine(appDataDir, "RevitMCP.audit.log");

        var auditLines = new JArray();
        if (File.Exists(auditPath))
        {
            foreach (var line in File.ReadLines(auditPath).TakeLast(linesCount))
                auditLines.Add(line);
        }

        return new JObject
        {
            ["success"] = true,
            ["auditLog"] = auditLines
        };
    }

    private static JObject DismissDialogs(JObject p)
    {
        var policy = p["policy"]?.Value<string>()?.ToLowerInvariant() ?? "ok";
        TransactionRunner.DialogPolicy = policy;
        return new JObject
        {
            ["success"] = true,
            ["policy"] = policy,
            ["message"] = policy == "off"
                ? "Dialog suppression disabled (FailurePreprocessor removed)."
                : $"Dialog suppression active with policy '{policy}'."
        };
    }

    private static JObject PaintFace(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Paint Face", p, () =>
        {
            var elIdVal = p["elementId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementId.");
            var matName = p["materialName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing materialName.");

            var el = doc.GetElement(new ElementId(elIdVal)) ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"Element {elIdVal} not found.");
            var mat = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>()
                .FirstOrDefault(m => string.Equals(m.Name, matName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("MATERIAL_NOT_FOUND", $"Material '{matName}' not found.");

            var opt = new Options { ComputeReferences = true };
            var geom = el.get_Geometry(opt);
            Face? targetFace = null;

            if (geom != null)
            {
                foreach (var obj in geom)
                {
                    if (obj is Solid s && s.Faces.Size > 0)
                    {
                        targetFace = s.Faces.get_Item(0);
                        break;
                    }
                }
            }

            if (targetFace != null)
            {
                doc.Paint(el.Id, targetFace, mat.Id);
            }

            return new JObject
            {
                ["success"] = true,
                ["painted"] = targetFace != null,
                ["materialId"] = mat.Id.Value
            };
        });
    }
}
