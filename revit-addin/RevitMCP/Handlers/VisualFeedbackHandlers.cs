using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
    /// <summary>
    /// Capture visuelle instantanée de la vue active ou spécifiée dans Revit.
    /// Renvoie les métadonnées de la vue, le chemin du fichier et le contenu en Base64
    /// permettant à Claude / Cursor de l'analyser visuellement de façon multimodale.
    /// </summary>
    private static JObject ScreenshotActiveView(Document doc, JObject p)
    {
        var viewIdVal = p["viewId"]?.Value<long?>();
        var view = viewIdVal.HasValue
            ? doc.GetElement(new ElementId(viewIdVal.Value)) as View
            : doc.ActiveView;

        if (view == null)
            throw new RevitMcpException("VIEW_NOT_FOUND", "Aucune vue active ou valide n'a été trouvée pour la capture d'écran.");

        var width = p["widthPixels"]?.Value<int>() ?? p["pixelSize"]?.Value<int>() ?? 1920;
        width = Math.Clamp(width, 320, 4096);

        var formatStr = (p["format"]?.Value<string>() ?? "png").ToLowerInvariant();
        var isJpeg = formatStr.Contains("jpg") || formatStr.Contains("jpeg");
        var imageType = isJpeg ? ImageFileType.JPEGLossless : ImageFileType.PNG;
        var mimeType = isJpeg ? "image/jpeg" : "image/png";
        var extension = isJpeg ? ".jpg" : ".png";

        var requestedPath = p["saveFilePath"]?.Value<string>() ?? p["filePath"]?.Value<string>();
        string targetFilePath;
        string tempDir = Path.Combine(Path.GetTempPath(), "RevitMCP_Screenshots");
        Directory.CreateDirectory(tempDir);

        var baseFileName = $"Revit_Shot_{view.Id.Value}_{DateTime.Now:yyyyMMdd_HHmmss}";
        var exportBasePath = Path.Combine(tempDir, baseFileName);

        var opt = new ImageExportOptions
        {
            ExportRange = ExportRange.SetOfViews,
            FilePath = exportBasePath,
            FitDirection = FitDirectionType.Horizontal,
            HLRandWFViewsFileType = imageType,
            ShadowViewsFileType = imageType,
            ImageResolution = ImageResolution.DPI_150,
            PixelSize = width,
            ZoomType = ZoomFitType.FitToPage
        };

        opt.SetViewsAndSheets(new List<ElementId> { view.Id });

        // Exécution de l'export d'image Revit
        doc.ExportImage(opt);

        // Revit peut nommer le fichier exportBasePath + extension, ou exportBasePath + " - " + viewName + extension
        string? foundFile = null;
        var exactFile = exportBasePath + extension;
        if (File.Exists(exactFile))
        {
            foundFile = exactFile;
        }
        else
        {
            var candidates = Directory.GetFiles(tempDir, $"{baseFileName}*{extension}");
            if (candidates.Length > 0)
                foundFile = candidates.OrderByDescending(f => File.GetCreationTimeUtc(f)).First();
        }

        if (string.IsNullOrEmpty(foundFile) || !File.Exists(foundFile))
        {
            throw new RevitMcpException("EXPORT_FAILED", "Revit a exécuté l'export mais le fichier image résultant est introuvable sur le disque.");
        }

        if (!string.IsNullOrWhiteSpace(requestedPath))
        {
            var reqDir = Path.GetDirectoryName(requestedPath);
            if (!string.IsNullOrEmpty(reqDir)) Directory.CreateDirectory(reqDir);
            File.Copy(foundFile, requestedPath, overwrite: true);
            targetFilePath = requestedPath;
        }
        else
        {
            targetFilePath = foundFile;
        }

        var fileBytes = File.ReadAllBytes(foundFile);
        var includeBase64 = p["includeBase64"]?.Value<bool>() ?? false;
        string? base64Data = includeBase64 ? Convert.ToBase64String(fileBytes) : null;

        var result = new JObject
        {
            ["success"] = true,
            ["viewId"] = view.Id.Value,
            ["viewName"] = view.Name,
            ["viewType"] = view.ViewType.ToString(),
            ["widthPixels"] = width,
            ["filePath"] = targetFilePath,
            ["fileSizeBytes"] = fileBytes.Length,
            ["mimeType"] = mimeType,
            ["format"] = formatStr
        };

        if (base64Data != null)
        {
            result["imageBase64"] = base64Data;
        }

        return result;
    }
}
