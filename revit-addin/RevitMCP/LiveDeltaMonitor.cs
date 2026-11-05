using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RevitMCP;

/// <summary>
/// Moniteur de deltas en temps réel pour Autodesk Revit.
/// Capture les modifications géométriques du modèle à chaque clic de l'utilisateur
/// et les diffuse en flux continu vers le serveur Rust et le visualiseur Three.js 60 FPS.
/// </summary>
public static class LiveDeltaMonitor
{
    private static bool _isHooked;
    private static readonly ConcurrentQueue<JObject> _deltaQueue = new();
    private static long _sequenceId = 0;

    public static void Initialize(UIControlledApplication uiApp)
    {
        if (_isHooked) return;
        try
        {
            uiApp.ControlledApplication.DocumentChanged += OnDocumentChanged;
            _isHooked = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LiveDelta] Hook error: {ex.Message}");
        }
    }

    private static void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        try
        {
            var doc = e.GetDocument();
            var added = e.GetAddedElementIds();
            var modified = e.GetModifiedElementIds();
            var deleted = e.GetDeletedElementIds();

            if (added.Count == 0 && modified.Count == 0 && deleted.Count == 0) return;

            var seq = System.Threading.Interlocked.Increment(ref _sequenceId);
            var timestamp = DateTime.UtcNow.ToString("o");

            var deltaObj = new JObject
            {
                ["sequence"] = seq,
                ["timestamp"] = timestamp,
                ["documentTitle"] = doc.Title,
                ["addedCount"] = added.Count,
                ["modifiedCount"] = modified.Count,
                ["deletedCount"] = deleted.Count,
                ["addedIds"] = new JArray(added),
                ["modifiedIds"] = new JArray(modified),
                ["deletedIds"] = new JArray(deleted)
            };

            _deltaQueue.Enqueue(deltaObj);

            // Conserver au maximum les 500 derniers événements
            while (_deltaQueue.Count > 500)
            {
                _deltaQueue.TryDequeue(out _);
            }

            // Écriture du delta en mémoire partagée
            RevitSharedMemory.WriteUtf8String(2, deltaObj.ToString(Newtonsoft.Json.Formatting.None));
        }
        catch { }
    }

    public static JObject GetDeltas(long sinceSequence = 0)
    {
        var list = new JArray();
        foreach (var item in _deltaQueue)
        {
            var seq = item["sequence"]?.Value<long>() ?? 0;
            if (seq > sinceSequence)
            {
                list.Add(item);
            }
        }

        return new JObject
        {
            ["success"] = true,
            ["currentSequence"] = _sequenceId,
            ["isMonitoringActive"] = _isHooked,
            ["deltasCount"] = list.Count,
            ["deltas"] = list
        };
    }
}
