using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitMCP;

/// <summary>
/// The "Git of BIM" - Architectural Branching, Alternatives & Comparative Exploration Engine.
/// Allows Claude / Cursor and architects to explore multiple design variants in parallel,
/// compute delta metrics (Loi Carrez, PMR score, cost, wall lengths), and merge the winning design.
/// </summary>
public partial class RevitEventHandler
{
    private static readonly string BranchesSubDir = Path.Combine("_outils", ".snapshots", "branches");

    public static JObject BranchCreate(Document doc, JObject p)
    {
        var branchName = p["name"]?.Value<string>() ?? p["branchName"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(branchName))
            throw new RevitMcpException("INVALID_ARGUMENT", "Missing branch 'name'.");

        var cleanName = string.Join("_", branchName.Split(Path.GetInvalidFileNameChars()));
        var description = p["description"]?.Value<string>() ?? "Branche d'exploration architecturale IA";

        var projectDir = Path.GetDirectoryName(doc.PathName);
        if (string.IsNullOrEmpty(projectDir))
            projectDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RevitMCP_Projects");

        var branchesDir = Path.Combine(projectDir, BranchesSubDir);
        Directory.CreateDirectory(branchesDir);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var branchFile = Path.Combine(branchesDir, $"{cleanName}_{timestamp}.rvt");
        var metaFile = Path.Combine(branchesDir, $"{cleanName}_{timestamp}.json");

        // Compute baseline architectural metrics for this branch
        var metrics = ComputeModelMetrics(doc);

        var meta = new JObject
        {
            ["branchName"] = cleanName,
            ["description"] = description,
            ["createdAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["sourceDocument"] = doc.PathName,
            ["branchPath"] = branchFile,
            ["metrics"] = metrics
        };

        // Save a standalone snapshot of the branch state
        var saveOpt = new SaveAsOptions { OverwriteExistingFile = true, Compact = true };
        doc.SaveAs(branchFile, saveOpt);
        File.WriteAllText(metaFile, meta.ToString(Formatting.Indented));

        return new JObject
        {
            ["success"] = true,
            ["branchName"] = cleanName,
            ["branchPath"] = branchFile,
            ["description"] = description,
            ["metrics"] = metrics,
            ["message"] = $"Branche architecturale '{cleanName}' créée avec succès."
        };
    }

    public static JObject BranchList(Document doc)
    {
        var projectDir = Path.GetDirectoryName(doc.PathName);
        if (string.IsNullOrEmpty(projectDir))
            projectDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RevitMCP_Projects");

        var branchesDir = Path.Combine(projectDir, BranchesSubDir);
        var branches = new JArray();

        if (Directory.Exists(branchesDir))
        {
            var metaFiles = Directory.GetFiles(branchesDir, "*.json");
            foreach (var mf in metaFiles)
            {
                try
                {
                    var text = File.ReadAllText(mf);
                    var obj = JObject.Parse(text);
                    branches.Add(obj);
                }
                catch { }
            }
        }

        return new JObject
        {
            ["success"] = true,
            ["branchesCount"] = branches.Count,
            ["branches"] = branches
        };
    }

    public static JObject BranchCompare(Document doc, JObject p)
    {
        var branchAName = p["branchA"]?.Value<string>();
        var branchBName = p["branchB"]?.Value<string>();

        if (string.IsNullOrWhiteSpace(branchAName) || string.IsNullOrWhiteSpace(branchBName))
            throw new RevitMcpException("INVALID_ARGUMENT", "Provide both 'branchA' and 'branchB' to compare.");

        var projectDir = Path.GetDirectoryName(doc.PathName);
        if (string.IsNullOrEmpty(projectDir))
            projectDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RevitMCP_Projects");

        var branchesDir = Path.Combine(projectDir, BranchesSubDir);
        if (!Directory.Exists(branchesDir))
            throw new RevitMcpException("NOT_FOUND", "Aucune branche trouvée dans le projet.");

        JObject? metaA = FindBranchMeta(branchesDir, branchAName);
        JObject? metaB = FindBranchMeta(branchesDir, branchBName);

        if (metaA == null)
            throw new RevitMcpException("NOT_FOUND", $"Branche '{branchAName}' introuvable.");
        if (metaB == null)
            throw new RevitMcpException("NOT_FOUND", $"Branche '{branchBName}' introuvable.");

        var metA = metaA["metrics"] as JObject ?? new JObject();
        var metB = metaB["metrics"] as JObject ?? new JObject();

        double areaA = metA["habitableAreaM2"]?.Value<double>() ?? 0.0;
        double areaB = metB["habitableAreaM2"]?.Value<double>() ?? 0.0;
        double deltaArea = Math.Round(areaB - areaA, 2);

        double wallLenA = metA["wallsLinearLengthM"]?.Value<double>() ?? 0.0;
        double wallLenB = metB["wallsLinearLengthM"]?.Value<double>() ?? 0.0;
        double deltaWalls = Math.Round(wallLenB - wallLenA, 2);

        int doorsA = metA["doorsCount"]?.Value<int>() ?? 0;
        int doorsB = metB["doorsCount"]?.Value<int>() ?? 0;

        int windowsA = metA["windowsCount"]?.Value<int>() ?? 0;
        int windowsB = metB["windowsCount"]?.Value<int>() ?? 0;

        double pmrA = metA["pmrComplianceScore"]?.Value<double>() ?? 100.0;
        double pmrB = metB["pmrComplianceScore"]?.Value<double>() ?? 100.0;

        // Estimated partition cost variation (standard ~85 € / linear meter)
        double estimatedCostDeltaEur = Math.Round(deltaWalls * 85.0 + (doorsB - doorsA) * 350.0 + (windowsB - windowsA) * 650.0, 2);

        return new JObject
        {
            ["success"] = true,
            ["comparison"] = new JObject
            {
                ["branchA"] = branchAName,
                ["branchB"] = branchBName,
                ["deltaHabitableAreaM2"] = deltaArea,
                ["deltaWallsLengthM"] = deltaWalls,
                ["deltaDoors"] = doorsB - doorsA,
                ["deltaWindows"] = windowsB - windowsA,
                ["deltaPmrComplianceScore"] = Math.Round(pmrB - pmrA, 1),
                ["estimatedCostDeltaEur"] = estimatedCostDeltaEur,
                ["recommendation"] = deltaArea >= 0 && pmrB >= pmrA
                    ? $"La variante '{branchBName}' optimise la surface habitable (+{deltaArea} m²) tout en conservant ou améliorant la conformité PMR."
                    : $"La variante '{branchAName}' offre un agencement plus compact ou économique (différence budgétaire estimée : {estimatedCostDeltaEur} €)."
            }
        };
    }

    private static JObject? FindBranchMeta(string dir, string branchName)
    {
        var files = Directory.GetFiles(dir, "*.json");
        foreach (var f in files)
        {
            try
            {
                var content = JObject.Parse(File.ReadAllText(f));
                if (string.Equals(content["branchName"]?.Value<string>(), branchName, StringComparison.OrdinalIgnoreCase))
                    return content;
            }
            catch { }
        }
        return null;
    }

    private static JObject ComputeModelMetrics(Document doc)
    {
        // 1. Rooms habitable area (Loi Carrez / surfaces habitables)
        double totalAreaM2 = 0.0;
        int roomsCount = 0;
        try
        {
            var roomCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Room r in roomCollector.Cast<Room>())
            {
                if (r.Area > 0.01)
                {
                    totalAreaM2 += UnitConversions.SqFtToM2(r.Area);
                    roomsCount++;
                }
            }
        }
        catch { }

        // 2. Linear wall length
        double totalWallLengthM = 0.0;
        int wallsCount = 0;
        try
        {
            var wallCollector = new FilteredElementCollector(doc).OfClass(typeof(Wall));
            foreach (Wall w in wallCollector.Cast<Wall>())
            {
                if (w.Location is LocationCurve lc)
                {
                    totalWallLengthM += (lc.Curve.Length * UnitConversions.MmPerFoot) / 1000.0;
                    wallsCount++;
                }
            }
        }
        catch { }

        // 3. Doors & Windows count
        int doorsCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().GetElementCount();
        int windowsCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().GetElementCount();

        // 4. PMR quick score (doors with clear width >= 830mm)
        double pmrScore = 100.0;
        if (doorsCount > 0)
        {
            int compliantDoors = 0;
            var doors = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType();
            foreach (FamilyInstance d in doors.Cast<FamilyInstance>())
            {
                var widthParam = d.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH) ?? d.get_Parameter(BuiltInParameter.DOOR_WIDTH);
                if (widthParam != null)
                {
                    var widthMm = widthParam.AsDouble() * UnitConversions.MmPerFoot;
                    if (widthMm >= 830.0) compliantDoors++;
                }
                else
                {
                    compliantDoors++;
                }
            }
            pmrScore = Math.Round((double)compliantDoors / doorsCount * 100.0, 1);
        }

        return new JObject
        {
            ["habitableAreaM2"] = Math.Round(totalAreaM2, 2),
            ["roomsCount"] = roomsCount,
            ["wallsLinearLengthM"] = Math.Round(totalWallLengthM, 2),
            ["wallsCount"] = wallsCount,
            ["doorsCount"] = doorsCount,
            ["windowsCount"] = windowsCount,
            ["pmrComplianceScore"] = pmrScore
        };
    }
}
