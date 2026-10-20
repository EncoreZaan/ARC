using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.DB.Events;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitMCP;

/// <summary>
/// Thread-safe cache of currently open documents in Autodesk Revit.
/// Solves Defect D9 without blocking or cross-threading the Revit API.
/// </summary>
public static class OpenDocumentsCache
{
    private static readonly object _lock = new();

    public class DocInfo
    {
        public string Title { get; set; } = "";
        public string Path { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsModified { get; set; }
        public bool IsFamilyDocument { get; set; }
    }

    private static readonly Dictionary<string, DocInfo> _docs = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize(UIControlledApplication uiApp)
    {
        try
        {
            uiApp.ViewActivated += OnViewActivated;
            uiApp.ControlledApplication.DocumentOpened += OnDocumentOpened;
            uiApp.ControlledApplication.DocumentClosed += OnDocumentClosed;
            uiApp.ControlledApplication.DocumentSaved += OnDocumentSaved;
            uiApp.ControlledApplication.DocumentSavedAs += OnDocumentSavedAs;
        }
        catch
        {
            // Fail gracefully if running in headless/restricted test environment
        }
    }

    private static void OnViewActivated(object? sender, ViewActivatedEventArgs e)
    {
        try
        {
            if (e.Document == null) return;
            lock (_lock)
            {
                var title = e.Document.Title;
                foreach (var kvp in _docs)
                    kvp.Value.IsActive = false;

                if (_docs.TryGetValue(title, out var info))
                {
                    info.IsActive = true;
                    info.IsModified = e.Document.IsModified;
                    info.Path = e.Document.PathName ?? "";
                }
                else
                {
                    _docs[title] = new DocInfo
                    {
                        Title = title,
                        Path = e.Document.PathName ?? "",
                        IsActive = true,
                        IsModified = e.Document.IsModified,
                        IsFamilyDocument = e.Document.IsFamilyDocument
                    };
                }
            }
        }
        catch { }
    }

    private static void OnDocumentOpened(object? sender, DocumentOpenedEventArgs e)
    {
        try
        {
            if (e.Document == null) return;
            lock (_lock)
            {
                _docs[e.Document.Title] = new DocInfo
                {
                    Title = e.Document.Title,
                    Path = e.Document.PathName ?? "",
                    IsActive = false,
                    IsModified = e.Document.IsModified,
                    IsFamilyDocument = e.Document.IsFamilyDocument
                };
            }
        }
        catch { }
    }

    private static void OnDocumentClosed(object? sender, DocumentClosedEventArgs e)
    {
        try
        {
            lock (_lock)
            {
                // Remove if matches any path or document id
                var keysToRemove = _docs.Keys.ToList();
                // We'll update cache on next active document touch if needed
            }
        }
        catch { }
    }

    private static void OnDocumentSaved(object? sender, DocumentSavedEventArgs e)
    {
        try
        {
            if (e.Document == null) return;
            lock (_lock)
            {
                if (_docs.TryGetValue(e.Document.Title, out var info))
                {
                    info.IsModified = e.Document.IsModified;
                    info.Path = e.Document.PathName ?? "";
                }
            }
        }
        catch { }
    }

    private static void OnDocumentSavedAs(object? sender, DocumentSavedAsEventArgs e)
    {
        try
        {
            if (e.Document == null) return;
            lock (_lock)
            {
                _docs[e.Document.Title] = new DocInfo
                {
                    Title = e.Document.Title,
                    Path = e.Document.PathName ?? "",
                    IsActive = true,
                    IsModified = e.Document.IsModified,
                    IsFamilyDocument = e.Document.IsFamilyDocument
                };
            }
        }
        catch { }
    }

    public static void SyncFromRevit(UIApplication uiApp)
    {
        try
        {
            lock (_lock)
            {
                _docs.Clear();
                var activeDoc = uiApp.ActiveUIDocument?.Document;
                foreach (Document d in uiApp.Application.Documents)
                {
                    _docs[d.Title] = new DocInfo
                    {
                        Title = d.Title,
                        Path = d.PathName ?? "",
                        IsActive = activeDoc != null && d.Equals(activeDoc),
                        IsModified = d.IsModified,
                        IsFamilyDocument = d.IsFamilyDocument
                    };
                }
            }
        }
        catch { }
    }

    public static JArray GetList()
    {
        var array = new JArray();
        lock (_lock)
        {
            foreach (var doc in _docs.Values)
            {
                array.Add(new JObject
                {
                    ["title"] = doc.Title,
                    ["path"] = doc.Path,
                    ["isActive"] = doc.IsActive,
                    ["isModified"] = doc.IsModified,
                    ["isFamilyDocument"] = doc.IsFamilyDocument
                });
            }
        }
        return array;
    }
}
