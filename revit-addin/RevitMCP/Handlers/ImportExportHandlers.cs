using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
    private static JObject ExportPdfEnhanced(Document doc, JObject p)
    {
        var outputPath = p["outputPath"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing outputPath.");
        var sheetIds = p["sheetIds"]?.ToObject<long[]>();
        var viewIds = p["viewIds"]?.ToObject<long[]>();
        var paperSize = p["paperSize"]?.Value<string>() ?? "A4";
        var orientation = p["orientation"]?.Value<string>() ?? "Landscape";
        var zoomPercent = p["zoom"]?.Value<int>() ?? 100;
        var combine = p["combine"]?.Value<bool>() ?? true;
        var hideCrop = p["hideCropBoundaries"]?.Value<bool>() ?? true;
        var hideRef = p["hideRefPlanes"]?.Value<bool>() ?? true;
        var colorMode = p["colorMode"]?.Value<string>() ?? "Color";
        var rasterQuality = p["rasterQuality"]?.Value<string>() ?? "High";

        var outDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outDir))
            Directory.CreateDirectory(outDir);

        var opts = new PDFExportOptions
        {
            Combine = combine,
            FileName = Path.GetFileNameWithoutExtension(outputPath),
            HideCropBoundaries = hideCrop,
            HideReferencePlane = hideRef,
            HideScopeBoxes = true,
            HideUnreferencedViewTags = true,
            ZoomType = ZoomType.Zoom,
            ZoomPercentage = zoomPercent,
            PaperFormat = paperSize.ToUpperInvariant() switch
            {
                "A0" => ExportPaperFormat.ISO_A0,
                "A1" => ExportPaperFormat.ISO_A1,
                "A2" => ExportPaperFormat.ISO_A2,
                "A3" => ExportPaperFormat.ISO_A3,
                _ => ExportPaperFormat.ISO_A4
            },
            PaperOrientation = orientation.Equals("Portrait", StringComparison.OrdinalIgnoreCase)
                ? PageOrientationType.Portrait
                : PageOrientationType.Landscape,
            ColorDepth = colorMode.ToLowerInvariant() switch
            {
                "grayscale" => ColorDepthType.GrayScale,
                "blackline" or "bw" => ColorDepthType.BlackLine,
                _ => ColorDepthType.Color
            },
            RasterQuality = rasterQuality.ToLowerInvariant() switch
            {
                "presentation" => RasterQualityType.Presentation,
                "medium" => RasterQualityType.Medium,
                "low" => RasterQualityType.Low,
                _ => RasterQualityType.High
            }
        };

        var ids = new List<ElementId>();
        if (sheetIds != null) ids.AddRange(sheetIds.Select(i => new ElementId(i)));
        if (viewIds != null) ids.AddRange(viewIds.Select(i => new ElementId(i)));

        if (ids.Count == 0)
        {
            ids.AddRange(new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Select(e => e.Id));
        }

        if (ids.Count == 0)
        {
            ids.Add(doc.ActiveView.Id);
        }

        doc.Export(outDir ?? Path.GetTempPath(), ids, opts);

        return new JObject
        {
            ["success"] = true,
            ["outputPath"] = outputPath,
            ["exportedCount"] = ids.Count
        };
    }

    private static JObject ExportDwgEnhanced(Document doc, JObject p, bool isDxf = false)
    {
        var outputPath = p["outputPath"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing outputPath.");
        var sheetIds = p["sheetIds"]?.ToObject<long[]>();
        var viewIds = p["viewIds"]?.ToObject<long[]>();
        var version = p["version"]?.Value<string>() ?? "2018";
        var sharedCoords = p["sharedCoordinates"]?.Value<bool>() ?? false;
        var mergedViews = p["mergedViews"]?.Value<bool>() ?? true;
        var units = p["units"]?.Value<string>() ?? "mm";

        var outDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outDir))
            Directory.CreateDirectory(outDir);

        var acadVer = version switch
        {
            "2013" => ACADVersion.R2013,
            "2010" => ACADVersion.R2010,
            _ => ACADVersion.R2018
        };

        var ids = new List<ElementId>();
        if (sheetIds != null) ids.AddRange(sheetIds.Select(i => new ElementId(i)));
        if (viewIds != null) ids.AddRange(viewIds.Select(i => new ElementId(i)));

        if (ids.Count == 0)
            ids.Add(doc.ActiveView.Id);

        if (isDxf)
        {
            var dxfOpts = new DXFExportOptions
            {
                FileVersion = acadVer,
                TargetUnit = units.Equals("cm", StringComparison.OrdinalIgnoreCase) ? ExportUnit.Centimeter : ExportUnit.Millimeter
            };
            doc.Export(outDir ?? Path.GetTempPath(), Path.GetFileNameWithoutExtension(outputPath), ids, dxfOpts);
        }
        else
        {
            var dwgOpts = new DWGExportOptions
            {
                FileVersion = acadVer,
                SharedCoords = sharedCoords,
                MergedViews = mergedViews,
                TargetUnit = units.Equals("cm", StringComparison.OrdinalIgnoreCase) ? ExportUnit.Centimeter : ExportUnit.Millimeter
            };
            doc.Export(outDir ?? Path.GetTempPath(), Path.GetFileNameWithoutExtension(outputPath), ids, dwgOpts);
        }

        return new JObject
        {
            ["success"] = true,
            ["outputPath"] = outputPath,
            ["exportedCount"] = ids.Count
        };
    }

    private static JObject ImportCad(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Import CAD", p, () =>
        {
            var filePath = p["path"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing CAD file path.");
            if (!File.Exists(filePath))
                throw new RevitMcpException("FILE_NOT_FOUND", $"CAD file '{filePath}' not found.");

            var isLink = p["link"]?.Value<bool>() ?? true;
            var currentViewOnly = p["currentViewOnly"]?.Value<bool>() ?? true;
            var units = p["units"]?.Value<string>() ?? "mm";
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View ?? doc.ActiveView;

            var dwgOpt = new DWGImportOptions
            {
                ThisViewOnly = currentViewOnly,
                Unit = units.ToLowerInvariant() switch
                {
                    "m" or "meter" => ImportUnit.Meter,
                    "cm" => ImportUnit.Centimeter,
                    _ => ImportUnit.Millimeter
                },
                Placement = ImportPlacement.Origin
            };

            ElementId createdId;
            if (isLink)
            {
                doc.Link(filePath, dwgOpt, view, out createdId);
            }
            else
            {
                doc.Import(filePath, dwgOpt, view, out createdId);
            }

            return new JObject
            {
                ["success"] = true,
                ["importInstanceId"] = createdId.Value,
                ["isLink"] = isLink,
                ["filePath"] = filePath
            };
        });
    }

    private static JObject GetCadGeometry(Document doc, JObject p)
    {
        var importIdVal = p["importInstanceId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing importInstanceId.");
        var importInstance = doc.GetElement(new ElementId(importIdVal)) as ImportInstance
            ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"ImportInstance {importIdVal} not found.");

        var opt = new Options { DetailLevel = ViewDetailLevel.Fine };
        var geomElement = importInstance.get_Geometry(opt);

        var curves = new JArray();
        if (geomElement != null)
        {
            foreach (var obj in geomElement)
            {
                if (obj is GeometryInstance gi)
                {
                    var instGeom = gi.GetInstanceGeometry();
                    if (instGeom != null)
                    {
                        foreach (var inner in instGeom)
                        {
                            if (inner is Curve c)
                            {
                                var p0Mm = UnitConversions.FeetToMm(c.GetEndPoint(0));
                                var p1Mm = UnitConversions.FeetToMm(c.GetEndPoint(1));
                                curves.Add(new JObject
                                {
                                    ["type"] = c is Arc ? "arc" : "line",
                                    ["start"] = new JObject { ["x"] = Math.Round(p0Mm.X, 2), ["y"] = Math.Round(p0Mm.Y, 2), ["z"] = Math.Round(p0Mm.Z, 2) },
                                    ["end"] = new JObject { ["x"] = Math.Round(p1Mm.X, 2), ["y"] = Math.Round(p1Mm.Y, 2), ["z"] = Math.Round(p1Mm.Z, 2) },
                                    ["lengthMm"] = Math.Round(UnitConversions.FeetToMm(c.Length), 2)
                                });
                            }
                        }
                    }
                }
            }
        }

        return new JObject
        {
            ["success"] = true,
            ["importInstanceId"] = importInstance.Id.Value,
            ["curveCount"] = curves.Count,
            ["curves"] = curves
        };
    }

    private static JObject CreateWallsFromCad(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Create Walls From CAD", p, () =>
        {
            var importIdVal = p["importInstanceId"]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing importInstanceId.");
            var heightMm = p["height"]?.Value<double>() ?? 3000.0;
            var wallTypeName = p["wallType"]?.Value<string>();

            var importInstance = doc.GetElement(new ElementId(importIdVal)) as ImportInstance
                ?? throw new RevitMcpException("ELEMENT_NOT_FOUND", $"ImportInstance {importIdVal} not found.");

            WallType? wallType = null;
            if (!string.IsNullOrWhiteSpace(wallTypeName))
            {
                wallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(wt => string.Equals(wt.Name, wallTypeName, StringComparison.OrdinalIgnoreCase));
            }
            wallType ??= new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().FirstOrDefault();

            var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault()
                ?? throw new RevitMcpException("LEVEL_NOT_FOUND", "No level found.");

            var opt = new Options { DetailLevel = ViewDetailLevel.Fine };
            var geomElement = importInstance.get_Geometry(opt);

            var createdWallIds = new JArray();
            if (geomElement != null && wallType != null)
            {
                foreach (var obj in geomElement)
                {
                    if (obj is GeometryInstance gi)
                    {
                        var instGeom = gi.GetInstanceGeometry();
                        if (instGeom != null)
                        {
                            foreach (var inner in instGeom)
                            {
                                if (inner is Line line && line.Length > 0.5)
                                {
                                    var wall = Wall.Create(doc, line, wallType.Id, level.Id, UnitConversions.MmToFeet(heightMm), 0, false, false);
                                    createdWallIds.Add(wall.Id.Value);
                                }
                            }
                        }
                    }
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["wallsCreatedCount"] = createdWallIds.Count,
                ["wallIds"] = createdWallIds
            };
        });
    }

    private static JObject ImportImage(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Import Image", p, () =>
        {
            var filePath = p["path"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing image path.");
            if (!File.Exists(filePath))
                throw new RevitMcpException("FILE_NOT_FOUND", $"Image '{filePath}' not found.");

            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var view = doc.GetElement(new ElementId(viewIdVal)) as View ?? doc.ActiveView;

            var posObj = p["position"] as JObject ?? new JObject { ["x"] = 0, ["y"] = 0 };
            var posFeet = UnitConversions.MmToFeet(new XYZ(posObj["x"]?.Value<double>() ?? 0, posObj["y"]?.Value<double>() ?? 0, 0));

            var imgTypeOpts = new ImageTypeOptions(filePath, false, ImageTypeSource.Import);
            var imgType = ImageType.Create(doc, imgTypeOpts);

            var imgPlacementOpts = new ImagePlacementOptions(posFeet, BoxPlacement.Center);
            var imgInstance = ImageInstance.Create(doc, view, imgType.Id, imgPlacementOpts);

            var widthMm = p["widthMm"]?.Value<double?>();
            if (widthMm.HasValue)
            {
                imgInstance.Width = UnitConversions.MmToFeet(widthMm.Value);
            }

            return new JObject
            {
                ["success"] = true,
                ["imageInstanceId"] = imgInstance.Id.Value,
                ["typeId"] = imgType.Id.Value
            };
        });
    }
}
