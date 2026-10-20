using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
    /// <summary>
    /// Récupère la liste des alertes et anomalies détectées en temps réel par le copilote proactif.
    /// </summary>
    private static JObject GetProactiveAlerts(Document doc, JObject p)
    {
        var maxCount = p["maxCount"]?.Value<int>() ?? 50;
        var clearAfterRead = p["clearAfterRead"]?.Value<bool>() ?? false;

        var alerts = ProactiveGuardian.GetAlerts(maxCount);

        var alertArray = new JArray();
        foreach (var a in alerts)
        {
            alertArray.Add(new JObject
            {
                ["id"] = a.Id,
                ["timestamp"] = a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                ["severity"] = a.Severity,
                ["category"] = a.Category,
                ["message"] = a.Message,
                ["recommendation"] = a.Recommendation,
                ["elementId"] = a.ElementId
            });
        }

        if (clearAfterRead)
        {
            ProactiveGuardian.ClearAlerts();
        }

        return new JObject
        {
            ["success"] = true,
            ["totalAlerts"] = alerts.Count,
            ["alerts"] = alertArray
        };
    }

    /// <summary>
    /// Vide le journal des alertes proactives.
    /// </summary>
    private static JObject ClearProactiveAlerts(Document doc, JObject p)
    {
        ProactiveGuardian.ClearAlerts();
        return new JObject
        {
            ["success"] = true,
            ["message"] = "Journal des alertes proactives effacé avec succès."
        };
    }
}
