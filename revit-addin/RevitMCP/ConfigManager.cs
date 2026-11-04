using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace RevitMCP;

/// <summary>
/// Manages configuration settings for RevitMCP (Rule 12).
/// Looks for RevitMCP.config.json in the add-in directory or application directory.
/// </summary>
public static class ConfigManager
{
    private static readonly string ConfigFileName = "RevitMCP.config.json";
    private static readonly string LogFileName = "RevitMCP.audit.log";

    public static bool AllowCodeExecution { get; private set; } = true;
    public static string SecurityMode { get; private set; } = "full"; // "read-only", "write", "full"

    public static bool IsReadOnly => string.Equals(SecurityMode, "read-only", StringComparison.OrdinalIgnoreCase);

    static ConfigManager()
    {
        Reload();
    }

    public static void Reload()
    {
        try
        {
            var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins", "2027");
            var localDir = AppDomain.CurrentDomain.BaseDirectory;
            var paths = new[]
            {
                Path.Combine(appDataDir, ConfigFileName),
                Path.Combine(localDir, ConfigFileName),
                Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName)
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    var text = File.ReadAllText(path);
                    var json = JObject.Parse(text);
                    AllowCodeExecution = json["allowCodeExecution"]?.Value<bool>() ?? true;
                    SecurityMode = json["securityMode"]?.Value<string>()?.ToLowerInvariant() ?? "full";
                    return;
                }
            }

            AllowCodeExecution = true;
            SecurityMode = "full";
        }
        catch
        {
            AllowCodeExecution = true;
            SecurityMode = "full";
        }
    }

    public static void SetSecurityMode(string mode)
    {
        SecurityMode = mode.ToLowerInvariant();
        if (SecurityMode == "read-only")
        {
            AllowCodeExecution = false;
        }
        else if (SecurityMode == "full")
        {
            AllowCodeExecution = true;
        }

        try
        {
            var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins", "2027");
            var configPath = Path.Combine(appDataDir, ConfigFileName);
            var json = File.Exists(configPath) ? JObject.Parse(File.ReadAllText(configPath)) : new JObject();
            json["securityMode"] = SecurityMode;
            json["allowCodeExecution"] = AllowCodeExecution;
            File.WriteAllText(configPath, json.ToString());
        }
        catch { }
    }

    public static void LogAudit(string action, string details)
    {
        try
        {
            var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins", "2027");
            Directory.CreateDirectory(appDataDir);
            var logPath = Path.Combine(appDataDir, LogFileName);
            var entry = $"[{DateTime.UtcNow:O}] [AUDIT] {action}: {details}{Environment.NewLine}";
            File.AppendAllText(logPath, entry);
        }
        catch
        {
            // Logging failure should not crash execution
        }
    }
}
