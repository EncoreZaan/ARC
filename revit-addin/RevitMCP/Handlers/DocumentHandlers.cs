using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
        private static JObject NewProject(UIApplication app, JObject p)
    {
        var templatePath = p["templatePath"]?.Value<string>() ?? p["template"]?.Value<string>();
        var savePath = p["savePath"]?.Value<string>() ?? p["path"]?.Value<string>()
            ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing savePath.");

        if (!string.IsNullOrWhiteSpace(templatePath) && !File.Exists(templatePath))
        {
            throw new RevitMcpException("FILE_NOT_FOUND", $"Template file '{templatePath}' does not exist.");
        }

        var saveDir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }

        Document newDoc;
        if (!string.IsNullOrWhiteSpace(templatePath))
        {
            newDoc = app.Application.NewProjectDocument(templatePath);
        }
        else
        {
            newDoc = app.Application.NewProjectDocument(UnitSystem.Metric);
        }

        var saveOpts = new SaveAsOptions
        {
            OverwriteExistingFile = p["overwrite"]?.Value<bool>() ?? true,
            MaximumBackups = p["maxBackups"]?.Value<int>() ?? 3
        };

        newDoc.SaveAs(savePath, saveOpts);

        UIDocument? uiDoc = null;
        try
        {
            uiDoc = app.OpenAndActivateDocument(savePath);
        }
        catch (Exception ex)
        {
            ConfigManager.LogAudit("NEW_PROJECT_ACTIVATE", $"OpenAndActivateDocument: {ex.Message}");
            try
            {
                newDoc.Close(false);
                uiDoc = app.OpenAndActivateDocument(savePath);
            }
            catch { }
        }

        OpenDocumentsCache.SyncFromRevit(app);

        return new JObject
        {
            ["success"] = true,
            ["title"] = uiDoc?.Document?.Title ?? newDoc.Title,
            ["path"] = uiDoc?.Document?.PathName ?? savePath
        };
    }

    private static JObject ListOpenDocuments(UIApplication app)
    {
        OpenDocumentsCache.SyncFromRevit(app);
        return new JObject
        {
            ["success"] = true,
            ["documents"] = DocumentResolver.GetOpenDocumentsList(app)
        };
    }

    private static JObject ActivateDocument(UIApplication app, JObject p)
    {
        var targetDoc = DocumentResolver.ResolveDocument(app, p);
        if (string.IsNullOrEmpty(targetDoc.PathName))
        {
            throw new RevitMcpException("CANNOT_ACTIVATE", "Document must be saved to disk before it can be activated via OpenAndActivateDocument.");
        }

        var uiDoc = app.OpenAndActivateDocument(targetDoc.PathName);
        OpenDocumentsCache.SyncFromRevit(app);

        return new JObject
        {
            ["success"] = true,
            ["activatedDocument"] = uiDoc.Document.Title,
            ["activeView"] = uiDoc.ActiveView?.Name
        };
    }

    private static JObject OpenDocument(UIApplication app, JObject p)
    {
        var filePath = p["path"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing path.");
        if (!File.Exists(filePath))
            throw new RevitMcpException("FILE_NOT_FOUND", $"Document file '{filePath}' does not exist.");

        var detach = p["detachFromCentral"]?.Value<bool>() ?? false;
        var audit = p["audit"]?.Value<bool>() ?? false;
        var activate = p["activate"]?.Value<bool>() ?? true;

        var openOpts = new OpenOptions
        {
            Audit = audit
        };

        if (detach)
        {
            openOpts.DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets;
        }

        var modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(filePath);
        Document doc;
        bool wasActivated = activate;
        if (activate)
        {
            try
            {
                var uiDoc = app.OpenAndActivateDocument(modelPath, openOpts, detach);
                doc = uiDoc.Document;
            }
            catch (Exception ex)
            {
                ConfigManager.LogAudit("OPEN_DOC_FALLBACK", $"OpenAndActivateDocument could not activate directly: {ex.Message}. Falling back to OpenDocumentFile.");
                doc = app.Application.OpenDocumentFile(modelPath, openOpts);
                wasActivated = false;
            }
        }
        else
        {
            doc = app.Application.OpenDocumentFile(modelPath, openOpts);
        }

        OpenDocumentsCache.SyncFromRevit(app);

        return new JObject
        {
            ["success"] = true,
            ["title"] = doc.Title,
            ["path"] = doc.PathName,
            ["isWorkshared"] = doc.IsWorkshared,
            ["isActive"] = wasActivated
        };
    }

    private static JObject CloseDocument(UIApplication app, JObject p)
    {
        var targetDoc = DocumentResolver.ResolveDocument(app, p);
        var save = p["save"]?.Value<bool>() ?? false;

        var title = targetDoc.Title;
        targetDoc.Close(save);
        OpenDocumentsCache.SyncFromRevit(app);

        return new JObject
        {
            ["success"] = true,
            ["closedDocument"] = title,
            ["saved"] = save
        };
    }

    private static JObject SaveAsDocument(Document doc, JObject p)
    {
        var filePath = p["path"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing path.");
        var overwrite = p["overwrite"]?.Value<bool>() ?? true;
        var maxBackups = p["maxBackups"]?.Value<int>() ?? 3;

        var saveOpts = new SaveAsOptions
        {
            OverwriteExistingFile = overwrite,
            MaximumBackups = maxBackups
        };

        doc.SaveAs(filePath, saveOpts);

        return new JObject
        {
            ["success"] = true,
            ["title"] = doc.Title,
            ["path"] = doc.PathName
        };
    }

    private static JObject PurgeUnusedEnhanced(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Purge Unused", p, () =>
        {
            var unusedFamilySymbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(s =>
                {
                    var filter = new FamilyInstanceFilter(doc, s.Id);
                    return !new FilteredElementCollector(doc).WherePasses(filter).Any();
                })
                .ToList();

            var purgedNames = new JArray();
            foreach (var s in unusedFamilySymbols)
            {
                purgedNames.Add($"{s.FamilyName} : {s.Name}");
                doc.Delete(s.Id);
            }

            return new JObject
            {
                ["success"] = true,
                ["purgedCount"] = unusedFamilySymbols.Count,
                ["purgedTypes"] = purgedNames
            };
        });
    }

    private static JObject SetProjectInfo(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Set Project Info", p, () =>
        {
            var info = doc.ProjectInformation;

            if (p["name"] != null) info.Name = p["name"]!.Value<string>();
            if (p["number"] != null) info.Number = p["number"]!.Value<string>();
            if (p["author"] != null) info.Author = p["author"]!.Value<string>();
            if (p["client"] != null) info.ClientName = p["client"]!.Value<string>();
            if (p["status"] != null) info.Status = p["status"]!.Value<string>();
            if (p["address"] != null) info.Address = p["address"]!.Value<string>();
            if (p["issueDate"] != null) info.IssueDate = p["issueDate"]!.Value<string>();

            if (p["custom"] is JObject customObj)
            {
                ApplyParameters(doc, info, customObj);
            }

            return new JObject
            {
                ["success"] = true,
                ["projectName"] = info.Name,
                ["projectNumber"] = info.Number,
                ["author"] = info.Author,
                ["client"] = info.ClientName,
                ["status"] = info.Status
            };
        });
    }

    private static JObject CopyElementsBetweenDocuments(UIApplication app, JObject p)
    {
        var srcTitle = p["sourceDocument"]?.Value<string>();
        var tgtTitle = p["targetDocument"]?.Value<string>();
        var idsToken = p["elementIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIds.");

        var srcDoc = app.Application.Documents.Cast<Document>().FirstOrDefault(d => string.Equals(d.Title, srcTitle, StringComparison.OrdinalIgnoreCase))
            ?? throw new RevitMcpException("DOCUMENT_NOT_FOUND", $"Source document '{srcTitle}' not found.");
        var tgtDoc = app.Application.Documents.Cast<Document>().FirstOrDefault(d => string.Equals(d.Title, tgtTitle, StringComparison.OrdinalIgnoreCase))
            ?? throw new RevitMcpException("DOCUMENT_NOT_FOUND", $"Target document '{tgtTitle}' not found.");

        return TransactionRunner.Run(tgtDoc, "Copy Elements Between Documents", p, () =>
        {
            var ids = idsToken.Select(t => new ElementId(t.Value<long>())).ToList();
            var copyOpts = new CopyPasteOptions();
            var pastedIds = ElementTransformUtils.CopyElements(srcDoc, ids, tgtDoc, Transform.Identity, copyOpts);

            return new JObject
            {
                ["success"] = true,
                ["copiedCount"] = pastedIds.Count,
                ["createdIds"] = new JArray(pastedIds.Select(id => id.Value))
            };
        });
    }

    private static JObject GetDesignOptions(Document doc)
    {
        var sets = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_DesignOptionSets)
            .Select(s => new JObject
            {
                ["id"] = s.Id.Value,
                ["name"] = s.Name
            });

        var options = new FilteredElementCollector(doc)
            .OfClass(typeof(DesignOption))
            .Cast<DesignOption>()
            .Select(opt => new JObject
            {
                ["id"] = opt.Id.Value,
                ["name"] = opt.Name,
                ["isPrimary"] = opt.IsPrimary
            });

        return new JObject
        {
            ["success"] = true,
            ["optionSets"] = new JArray(sets),
            ["options"] = new JArray(options)
        };
    }

    private static JObject CreateDesignOption(Document doc, JObject p)
    {
        return new JObject
        {
            ["success"] = false,
            ["error"] = new JObject
            {
                ["code"] = "NOT_SUPPORTED",
                ["message"] = "Revit API does not expose programmatic creation of Design Options. Please create option sets in Revit UI, then use revit_get_design_options and revit_set_view_design_option."
            }
        };
    }

    private static JObject MoveElementsToDesignOption(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Move Elements to Design Option", p, () =>
        {
            var idsToken = p["elementIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIds.");
            var optionName = p["optionName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing optionName.");

            var opt = new FilteredElementCollector(doc)
                .OfClass(typeof(DesignOption))
                .Cast<DesignOption>()
                .FirstOrDefault(o => string.Equals(o.Name, optionName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("OPTION_NOT_FOUND", $"Design option '{optionName}' not found.");

            int count = 0;
            foreach (var tok in idsToken)
            {
                var el = doc.GetElement(new ElementId(tok.Value<long>()));
                if (el != null)
                {
                    el.get_Parameter(BuiltInParameter.DESIGN_OPTION_PARAM)?.Set(opt.Id);
                    count++;
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["movedCount"] = count,
                ["optionName"] = opt.Name
            };
        });
    }

    private static JObject SetViewDesignOption(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Set View Design Option", p, () =>
        {
            var viewIdVal = p["viewId"]?.Value<long>() ?? doc.ActiveView.Id.Value;
            var optionName = p["optionName"]?.Value<string>() ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing optionName.");

            var view = doc.GetElement(new ElementId(viewIdVal)) as View
                ?? throw new RevitMcpException("VIEW_NOT_FOUND", $"View {viewIdVal} not found.");

            var opt = new FilteredElementCollector(doc)
                .OfClass(typeof(DesignOption))
                .Cast<DesignOption>()
                .FirstOrDefault(o => string.Equals(o.Name, optionName, StringComparison.OrdinalIgnoreCase))
                ?? throw new RevitMcpException("OPTION_NOT_FOUND", $"Design option '{optionName}' not found.");

            view.get_Parameter(BuiltInParameter.DESIGN_OPTION_PARAM)?.Set(opt.Id);

            return new JObject
            {
                ["success"] = true,
                ["viewId"] = view.Id.Value,
                ["optionName"] = opt.Name
            };
        });
    }

    private static JObject SetElementPhase(Document doc, JObject p)
    {
        return TransactionRunner.Run(doc, "Set Element Phase", p, () =>
        {
            var idsToken = p["elementIds"] as JArray ?? throw new RevitMcpException("INVALID_ARGUMENT", "Missing elementIds.");
            var createdPhaseName = p["createdPhase"]?.Value<string>();
            var demolishedPhaseName = p["demolishedPhase"]?.Value<string>();

            var phases = doc.Phases.Cast<Phase>().ToList();
            Phase? cPhase = !string.IsNullOrWhiteSpace(createdPhaseName)
                ? phases.FirstOrDefault(ph => string.Equals(ph.Name, createdPhaseName, StringComparison.OrdinalIgnoreCase))
                : null;
            Phase? dPhase = !string.IsNullOrWhiteSpace(demolishedPhaseName)
                ? phases.FirstOrDefault(ph => string.Equals(ph.Name, demolishedPhaseName, StringComparison.OrdinalIgnoreCase))
                : null;

            int count = 0;
            foreach (var tok in idsToken)
            {
                var el = doc.GetElement(new ElementId(tok.Value<long>()));
                if (el == null) continue;

                if (cPhase != null)
                    el.get_Parameter(BuiltInParameter.PHASE_CREATED)?.Set(cPhase.Id);

                if (dPhase != null)
                    el.get_Parameter(BuiltInParameter.PHASE_DEMOLISHED)?.Set(dPhase.Id);

                count++;
            }

            return new JObject
            {
                ["success"] = true,
                ["updatedCount"] = count
            };
        });
    }
}
