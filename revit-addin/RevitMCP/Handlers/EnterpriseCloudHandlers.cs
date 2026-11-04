using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
    // ─────────────────────────────────────────────────────────────────────────────
    // 1. Tiers de Sécurité & Gouvernance (Read-Only vs Write vs Full)
    // ─────────────────────────────────────────────────────────────────────────────

    private static JObject GetSecurityMode()
    {
        return new JObject
        {
            ["success"] = true,
            ["securityMode"] = ConfigManager.SecurityMode,
            ["isReadOnly"] = ConfigManager.IsReadOnly,
            ["allowCodeExecution"] = ConfigManager.AllowCodeExecution,
            ["dialogPolicy"] = TransactionRunner.DialogPolicy
        };
    }

    private static JObject SetSecurityMode(JObject p)
    {
        var mode = p["mode"]?.Value<string>()?.ToLowerInvariant() ?? "write";
        if (mode != "read-only" && mode != "write" && mode != "full")
        {
            throw new RevitMcpException("INVALID_ARGUMENT", "Valid security modes are 'read-only', 'write', or 'full'.");
        }

        ConfigManager.SetSecurityMode(mode);
        ConfigManager.LogAudit("revit_set_security_mode", $"Security mode changed to '{mode}'.");

        return new JObject
        {
            ["success"] = true,
            ["securityMode"] = mode,
            ["isReadOnly"] = ConfigManager.IsReadOnly,
            ["allowCodeExecution"] = ConfigManager.AllowCodeExecution,
            ["message"] = $"Security tier updated to '{mode}'."
        };
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 2. Autodesk Cloud & Worksharing (ACC / BIM 360)
    // ─────────────────────────────────────────────────────────────────────────────

    private static JObject GetCloudInfo(Document doc)
    {
        var isCloud = doc.IsModelInCloud;
        string? cloudProjectId = null;
        string? cloudModelId = null;
        string? cloudUrn = null;
        string? userVisiblePath = doc.PathName;

        try
        {
            var modelPath = doc.GetCloudModelPath();
            if (modelPath != null)
            {
                userVisiblePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
                try { cloudProjectId = modelPath.GetProjectGUID().ToString(); } catch { }
                try { cloudModelId = modelPath.GetModelGUID().ToString(); } catch { }
            }
        }
        catch { }

        string? centralPath = null;
        int worksetsCount = 0;
        string? activeWorkset = null;

        if (doc.IsWorkshared)
        {
            try
            {
                var central = doc.GetWorksharingCentralModelPath();
                if (central != null)
                    centralPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(central);
            }
            catch { }

            try
            {
                var fwc = new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset);
                worksetsCount = fwc.ToWorksets().Count;
                var table = doc.GetWorksetTable();
                var activeId = table.GetActiveWorksetId();
                activeWorkset = table.GetWorkset(activeId)?.Name;
            }
            catch { }
        }

        return new JObject
        {
            ["success"] = true,
            ["title"] = doc.Title,
            ["path"] = doc.PathName,
            ["userVisiblePath"] = userVisiblePath,
            ["isModelInCloud"] = isCloud,
            ["cloudProjectId"] = cloudProjectId,
            ["cloudModelId"] = cloudModelId,
            ["cloudModelUrn"] = cloudUrn,
            ["isWorkshared"] = doc.IsWorkshared,
            ["centralModelPath"] = centralPath,
            ["worksetsCount"] = worksetsCount,
            ["activeWorksetName"] = activeWorkset
        };
    }

    private static JObject SyncWithCentral(Document doc, JObject p)
    {
        if (!doc.IsWorkshared)
        {
            throw new RevitMcpException("NOT_WORKSHARED", "The active document is not a workshared model (cannot synchronize with central).");
        }

        var comment = p["comment"]?.Value<string>() ?? "Synchronized via RevitMCP AI Copilot";
        var compact = p["compact"]?.Value<bool>() ?? false;
        var saveLocal = p["saveLocalAfter"]?.Value<bool>() ?? true;

        var transOpts = new TransactWithCentralOptions();
        var syncOpts = new SynchronizeWithCentralOptions
        {
            Comment = comment,
            Compact = compact,
            SaveLocalAfter = saveLocal
        };

        var relinq = new RelinquishOptions(true);
        syncOpts.SetRelinquishOptions(relinq);

        doc.SynchronizeWithCentral(transOpts, syncOpts);
        ConfigManager.LogAudit("revit_sync_with_central", $"Synchronized model '{doc.Title}' with comment: {comment}");

        return new JObject
        {
            ["success"] = true,
            ["title"] = doc.Title,
            ["synchronized"] = true,
            ["comment"] = comment
        };
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 3. Système de Snapshots IA & Checkpoints de Sauvegarde
    // ─────────────────────────────────────────────────────────────────────────────

    private static JObject CreateSnapshot(Document doc, JObject p)
    {
        if (string.IsNullOrEmpty(doc.PathName) || !File.Exists(doc.PathName))
        {
            throw new RevitMcpException("DOCUMENT_NOT_SAVED", "Document must be saved to disk before creating a snapshot.");
        }

        var snapshotName = p["name"]?.Value<string>() ?? $"Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}";
        var description = p["description"]?.Value<string>() ?? "Instant snapshot created prior to AI modifications";

        var projectDir = Path.GetDirectoryName(doc.PathName) ?? Path.GetTempPath();
        var snapshotDir = Path.Combine(projectDir, "_outils", ".snapshots");
        Directory.CreateDirectory(snapshotDir);

        var snapId = Guid.NewGuid().ToString("N")[..8];
        var safeDocTitle = Path.GetFileNameWithoutExtension(doc.PathName);
        var snapshotFileName = $"{safeDocTitle}_{DateTime.Now:yyyyMMdd_HHmmss}_{snapId}.rvt";
        var snapshotFilePath = Path.Combine(snapshotDir, snapshotFileName);

        File.Copy(doc.PathName, snapshotFilePath, overwrite: true);

        var fileInfo = new FileInfo(snapshotFilePath);
        var meta = new JObject
        {
            ["snapshotId"] = snapId,
            ["name"] = snapshotName,
            ["description"] = description,
            ["originalPath"] = doc.PathName,
            ["snapshotPath"] = snapshotFilePath,
            ["fileSizeBytes"] = fileInfo.Length,
            ["createdAtUtc"] = DateTime.UtcNow.ToString("O")
        };

        var metaPath = Path.Combine(snapshotDir, $"{Path.GetFileNameWithoutExtension(snapshotFileName)}.json");
        File.WriteAllText(metaPath, meta.ToString());

        ConfigManager.LogAudit("revit_create_snapshot", $"Created snapshot '{snapshotName}' ({snapId}) -> {snapshotFilePath}");

        return new JObject
        {
            ["success"] = true,
            ["snapshotId"] = snapId,
            ["name"] = snapshotName,
            ["snapshotPath"] = snapshotFilePath,
            ["fileSizeBytes"] = fileInfo.Length
        };
    }

    private static JObject ListSnapshots(Document doc)
    {
        var projectDir = !string.IsNullOrEmpty(doc.PathName) ? Path.GetDirectoryName(doc.PathName) : null;
        var snapshotDir = projectDir != null ? Path.Combine(projectDir, "_outils", ".snapshots") : null;

        var list = new JArray();
        if (snapshotDir != null && Directory.Exists(snapshotDir))
        {
            var metaFiles = Directory.GetFiles(snapshotDir, "*.json");
            foreach (var mf in metaFiles.OrderByDescending(f => File.GetCreationTimeUtc(f)))
            {
                try
                {
                    var content = File.ReadAllText(mf);
                    list.Add(JObject.Parse(content));
                }
                catch { }
            }
        }

        return new JObject
        {
            ["success"] = true,
            ["snapshotsCount"] = list.Count,
            ["snapshots"] = list
        };
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 4. Normalisation Sémantique BIM (IFC / OmniClass / UniFormat)
    // ─────────────────────────────────────────────────────────────────────────────

    private static JObject GetBimClassification(Document doc, JObject p)
    {
        var idsToken = p["elementIds"] as JArray ?? (p["elementId"] != null ? new JArray { p["elementId"]!.Value<long>() } : new JArray());
        if (idsToken.Count == 0)
        {
            throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementId or elementIds array.");
        }

        var results = new JArray();
        foreach (var tok in idsToken)
        {
            var elId = new ElementId(tok.Value<long>());
            var el = doc.GetElement(elId);
            if (el == null) continue;

            var cat = el.Category;
            var elemType = doc.GetElement(el.GetTypeId()) as ElementType;

            string? ifcExportAs = el.get_Parameter(BuiltInParameter.IFC_EXPORT_ELEMENT_AS)?.AsString()
                ?? elemType?.get_Parameter(BuiltInParameter.IFC_EXPORT_ELEMENT_AS)?.AsString();

            string? assemblyCode = el.get_Parameter(BuiltInParameter.ASSEMBLY_CODE)?.AsString()
                ?? elemType?.get_Parameter(BuiltInParameter.ASSEMBLY_CODE)?.AsString()
                ?? el.LookupParameter("Code d'assemblage")?.AsString()
                ?? elemType?.LookupParameter("Code d'assemblage")?.AsString();

            string? assemblyDesc = el.get_Parameter(BuiltInParameter.ASSEMBLY_DESCRIPTION)?.AsString()
                ?? elemType?.get_Parameter(BuiltInParameter.ASSEMBLY_DESCRIPTION)?.AsString()
                ?? el.LookupParameter("Description de l'assemblage")?.AsString()
                ?? elemType?.LookupParameter("Description de l'assemblage")?.AsString();

            string? omniCode = el.LookupParameter("OmniClass Number")?.AsString()
                ?? el.LookupParameter("Numéro OmniClass")?.AsString()
                ?? elemType?.LookupParameter("OmniClass Number")?.AsString()
                ?? elemType?.LookupParameter("Numéro OmniClass")?.AsString();

            string? omniTitle = el.LookupParameter("OmniClass Title")?.AsString()
                ?? el.LookupParameter("Titre OmniClass")?.AsString()
                ?? elemType?.LookupParameter("OmniClass Title")?.AsString()
                ?? elemType?.LookupParameter("Titre OmniClass")?.AsString();

            string? keynote = el.get_Parameter(BuiltInParameter.KEYNOTE_PARAM)?.AsString()
                ?? elemType?.get_Parameter(BuiltInParameter.KEYNOTE_PARAM)?.AsString();

            var defaultIfc = ResolveDefaultIfcType(cat);
            var levelName = (doc.GetElement(el.LevelId) as Level)?.Name;

            results.Add(new JObject
            {
                ["elementId"] = el.Id.Value,
                ["category"] = cat?.Name,
                ["builtInCategory"] = cat?.BuiltInCategory.ToString(),
                ["typeName"] = elemType?.Name,
                ["familyName"] = (elemType as FamilySymbol)?.FamilyName,
                ["level"] = levelName,
                ["ifcType"] = !string.IsNullOrWhiteSpace(ifcExportAs) ? ifcExportAs : defaultIfc,
                ["omniClass"] = new JObject
                {
                    ["code"] = omniCode,
                    ["title"] = omniTitle
                },
                ["assembly"] = new JObject
                {
                    ["code"] = assemblyCode,
                    ["description"] = assemblyDesc
                },
                ["keynote"] = keynote
            });
        }

        return new JObject
        {
            ["success"] = true,
            ["elements"] = results
        };
    }

    private static string ResolveDefaultIfcType(Category? cat)
    {
        if (cat == null) return "IfcBuildingElementProxy";
        return cat.BuiltInCategory switch
        {
            BuiltInCategory.OST_Walls => "IfcWallStandardCase",
            BuiltInCategory.OST_Doors => "IfcDoor",
            BuiltInCategory.OST_Windows => "IfcWindow",
            BuiltInCategory.OST_Floors => "IfcSlab",
            BuiltInCategory.OST_Roofs => "IfcRoof",
            BuiltInCategory.OST_Columns => "IfcColumn",
            BuiltInCategory.OST_StructuralColumns => "IfcColumn",
            BuiltInCategory.OST_StructuralFraming => "IfcBeam",
            BuiltInCategory.OST_Furniture => "IfcFurnishingElement",
            BuiltInCategory.OST_PlumbingFixtures => "IfcSanitaryTerminal",
            BuiltInCategory.OST_Stairs => "IfcStair",
            BuiltInCategory.OST_Rooms => "IfcSpace",
            BuiltInCategory.OST_Ceilings => "IfcCovering",
            _ => "IfcBuildingElementProxy"
        };
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 5. Diagnostics Système & Télémétrie Approfondie
    // ─────────────────────────────────────────────────────────────────────────────

    private static JObject GetDiagnostics(UIApplication app, Document doc)
    {
        var proc = Process.GetCurrentProcess();
        var links = new FilteredElementCollector(doc)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .Select(l =>
            {
                var type = doc.GetElement(l.GetTypeId()) as RevitLinkType;
                return new JObject
                {
                    ["linkName"] = l.Name,
                    ["isLoaded"] = type != null && RevitLinkType.IsLoaded(doc, type.Id)
                };
            })
            .ToList();

        var cadImports = new FilteredElementCollector(doc)
            .OfClass(typeof(ImportInstance))
            .Cast<ImportInstance>()
            .Select(i => i.IsLinked ? $"Linked CAD: {i.Category?.Name}" : $"Imported CAD: {i.Category?.Name}")
            .ToList();

        var warnings = doc.GetWarnings();
        var topWarnings = warnings
            .GroupBy(w => w.GetDescriptionText())
            .Select(g => new JObject
            {
                ["description"] = g.Key,
                ["count"] = g.Count(),
                ["elementIds"] = new JArray(g.SelectMany(w => w.GetFailingElements()).Select(id => id.Value).Take(10))
            })
            .Take(15)
            .ToList();

        return new JObject
        {
            ["success"] = true,
            ["revit"] = new JObject
            {
                ["versionName"] = app.Application.VersionName,
                ["versionNumber"] = app.Application.VersionNumber,
                ["versionBuild"] = app.Application.VersionBuild,
                ["subVersion"] = app.Application.SubVersionNumber,
                ["language"] = app.Application.Language.ToString()
            },
            ["document"] = new JObject
            {
                ["title"] = doc.Title,
                ["path"] = doc.PathName,
                ["isModified"] = doc.IsModified,
                ["isReadOnly"] = doc.IsReadOnly,
                ["isWorkshared"] = doc.IsWorkshared,
                ["isModelInCloud"] = doc.IsModelInCloud
            },
            ["memory"] = new JObject
            {
                ["workingSetMB"] = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1),
                ["privateMemoryMB"] = Math.Round(proc.PrivateMemorySize64 / (1024.0 * 1024.0), 1),
                ["managedHeapMB"] = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 1)
            },
            ["performance"] = new JObject
            {
                ["totalWarnings"] = warnings.Count,
                ["revitLinksCount"] = links.Count,
                ["cadImportsCount"] = cadImports.Count,
                ["topWarnings"] = new JArray(topWarnings)
            },
            ["links"] = new JArray(links),
            ["security"] = new JObject
            {
                ["mode"] = ConfigManager.SecurityMode,
                ["isReadOnly"] = ConfigManager.IsReadOnly,
                ["allowCodeExecution"] = ConfigManager.AllowCodeExecution,
                ["dialogPolicy"] = TransactionRunner.DialogPolicy
            }
        };
    }
}
