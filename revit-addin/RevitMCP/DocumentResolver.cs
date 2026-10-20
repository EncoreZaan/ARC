using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace RevitMCP;

/// <summary>
/// Resolves the targeted Document according to Rule 9 and manages open documents.
/// </summary>
public static class DocumentResolver
{
    public static Document ResolveDocument(UIApplication app, JObject? payload)
    {
        var docTitle = payload?["documentTitle"]?.Value<string>() ?? payload?["title"]?.Value<string>();
        var docPath = payload?["documentPath"]?.Value<string>() ?? payload?["path"]?.Value<string>();
        var target = !string.IsNullOrWhiteSpace(docTitle) ? docTitle : docPath;

        if (string.IsNullOrWhiteSpace(target))
        {
            var activeDoc = app.ActiveUIDocument?.Document;
            if (activeDoc == null)
            {
                // Fallback to first open document if UI document is null
                var first = app.Application.Documents.Cast<Document>().FirstOrDefault();
                if (first != null) return first;

                throw new RevitMcpException("NO_ACTIVE_DOCUMENT", "No active document open in Autodesk Revit.");
            }
            return activeDoc;
        }

        var normalizedTarget = target.Trim();
        var targetNameWithoutExt = Path.GetFileNameWithoutExtension(normalizedTarget);

        foreach (Document d in app.Application.Documents)
        {
            if (string.Equals(d.Title, normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileNameWithoutExtension(d.Title), targetNameWithoutExt, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.PathName, normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(d.PathName) && string.Equals(Path.GetFileName(d.PathName), Path.GetFileName(normalizedTarget), StringComparison.OrdinalIgnoreCase)))
            {
                return d;
            }
        }

        var openDocs = string.Join(", ", app.Application.Documents.Cast<Document>().Select(d => $"'{d.Title}'"));
        throw new RevitMcpException("DOCUMENT_NOT_FOUND",
            $"Document '{target}' not found. Open documents: [{openDocs}]",
            new JObject { ["target"] = target });
    }

    public static JArray GetOpenDocumentsList(UIApplication app)
    {
        var list = new JArray();
        var activeDoc = app.ActiveUIDocument?.Document;

        foreach (Document d in app.Application.Documents)
        {
            var isCurrentActive = activeDoc != null && (
                d.Equals(activeDoc) ||
                (!string.IsNullOrEmpty(d.PathName) && string.Equals(d.PathName, activeDoc.PathName, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(d.Title, activeDoc.Title, StringComparison.OrdinalIgnoreCase));

            list.Add(new JObject
            {
                ["title"] = d.Title,
                ["path"] = d.PathName ?? "",
                ["isActive"] = isCurrentActive,
                ["isModified"] = d.IsModified,
                ["isFamilyDocument"] = d.IsFamilyDocument,
                ["isReadOnly"] = d.IsReadOnly
            });
        }

        return list;
    }
}
