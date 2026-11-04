using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace RevitMCP;

public class SafeFailurePreprocessor : IFailuresPreprocessor
{
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();

    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        var failures = failuresAccessor.GetFailureMessages();
        bool hasErrors = false;
        foreach (var f in failures)
        {
            var desc = f.GetDescriptionText();
            var severity = f.GetSeverity();

            if (severity == FailureSeverity.Warning)
            {
                if (!string.IsNullOrWhiteSpace(desc))
                    Warnings.Add(desc);
                // NE JAMAIS annuler la transaction sur un simple avertissement
                failuresAccessor.DeleteWarning(f);
            }
            else
            {
                hasErrors = true;
                if (!string.IsNullOrWhiteSpace(desc))
                    Errors.Add(desc);
            }
        }

        if (hasErrors)
        {
            failuresAccessor.RollBackPendingTransaction();
            return FailureProcessingResult.ProceedWithRollBack;
        }

        return FailureProcessingResult.Continue;
    }
}

/// <summary>
/// Executes modifying operations inside a safe Revit Transaction conforming to Rules 6 and 7.
/// </summary>
public static class TransactionRunner
{
    public static string DialogPolicy { get; set; } = "ok";

    public static JObject Run(Document doc, string transactionName, JObject? payload, Func<JObject> action)
    {
        if (ConfigManager.IsReadOnly)
        {
            throw new RevitMcpException("FORBIDDEN_READ_ONLY",
                $"Impossible d'exécuter la transaction '{transactionName}' : le mode sécurisé READ-ONLY est actif. Utilisez 'revit_set_security_mode' avec 'write' ou 'full' pour modifier le modèle.");
        }

        if (doc.IsModifiable)
        {
            throw new RevitMcpException("TRANSACTION_CONFLICT",
                $"Impossible de démarrer la transaction '{transactionName}' : le document est déjà en cours de modification dans une autre transaction.");
        }

        var isDryRun = payload?["dryRun"]?.Value<bool>() ?? false;
        var txName = transactionName.StartsWith("MCP:", StringComparison.OrdinalIgnoreCase)
            ? transactionName
            : $"MCP: {transactionName}";

        using var tx = new Transaction(doc, txName);
        SafeFailurePreprocessor? failurePreprocessor = null;

        if (!string.Equals(DialogPolicy, "off", StringComparison.OrdinalIgnoreCase))
        {
            var options = tx.GetFailureHandlingOptions();
            failurePreprocessor = new SafeFailurePreprocessor();
            options.SetFailuresPreprocessor(failurePreprocessor);
            options.SetClearAfterRollback(true);
            options.SetForcedModalHandling(false);
            tx.SetFailureHandlingOptions(options);
        }

        tx.Start();
        try
        {
            var result = action();

            if (isDryRun)
            {
                tx.RollBack();
                result["dryRun"] = true;
                if (!result.ContainsKey("success"))
                    result["success"] = true;
                if (failurePreprocessor != null)
                    result["bimWarnings"] = new JArray(failurePreprocessor.Warnings);
                return result;
            }

            var status = tx.Commit();
            if (status != TransactionStatus.Committed)
            {
                var errorMsg = (failurePreprocessor != null && failurePreprocessor.Errors.Count > 0)
                    ? string.Join("; ", failurePreprocessor.Errors)
                    : $"La transaction Revit '{txName}' n'a pas pu être validée (statut: {status}).";

                throw new RevitMcpException("TRANSACTION_FAILED", errorMsg, new JObject
                {
                    ["transaction"] = txName,
                    ["status"] = status.ToString(),
                    ["errors"] = failurePreprocessor != null ? new JArray(failurePreprocessor.Errors) : new JArray(),
                    ["warnings"] = failurePreprocessor != null ? new JArray(failurePreprocessor.Warnings) : new JArray()
                });
            }

            if (!result.ContainsKey("success"))
                result["success"] = true;

            if (failurePreprocessor != null)
                result["bimWarnings"] = new JArray(failurePreprocessor.Warnings);

            return result;
        }
        catch (Exception ex)
        {
            if (tx.GetStatus() == TransactionStatus.Started)
            {
                try { tx.RollBack(); } catch { }
            }

            if (ex is RevitMcpException)
                throw;

            var errorMsg = (failurePreprocessor != null && failurePreprocessor.Errors.Count > 0)
                ? string.Join("; ", failurePreprocessor.Errors)
                : ex.Message;

            throw new RevitMcpException("REVIT_API_ERROR", errorMsg, new JObject
            {
                ["transaction"] = txName,
                ["exceptionType"] = ex.GetType().FullName
            }, ex);
        }
    }
}
