using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace RevitMCP;

/// <summary>
/// Executes modifying operations inside a safe Revit Transaction conforming to Rules 6 and 7.
/// </summary>
public static class TransactionRunner
{
    public static JObject Run(Document doc, string transactionName, JObject? payload, Func<JObject> action)
    {
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
        var options = tx.GetFailureHandlingOptions();
        var failurePreprocessor = new SafeFailurePreprocessor();
        options.SetFailuresPreprocessor(failurePreprocessor);
        options.SetClearAfterRollback(true);
        options.SetForcedModalHandling(false);
        tx.SetFailureHandlingOptions(options);

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
                result["bimWarnings"] = new JArray(failurePreprocessor.Warnings);
                return result;
            }

            var status = tx.Commit();
            if (status != TransactionStatus.Committed)
            {
                throw new RevitMcpException("TRANSACTION_FAILED",
                    $"La transaction Revit '{txName}' n'a pas pu être validée (statut: {status}).");
            }

            if (!result.ContainsKey("success"))
                result["success"] = true;

            result["bimWarnings"] = new JArray(failurePreprocessor.Warnings);
            return result;
        }
        catch (Exception ex)
        {
            if (tx.HasStarted() && !tx.HasEnded())
                tx.RollBack();

            if (ex is RevitMcpException)
                throw;

            throw new RevitMcpException("REVIT_API_ERROR", ex.Message, new JObject
            {
                ["transaction"] = txName,
                ["exceptionType"] = ex.GetType().FullName
            }, ex);
        }
    }

    public class SafeFailurePreprocessor : IFailuresPreprocessor
    {
        public List<string> Warnings { get; } = new();

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var failures = failuresAccessor.GetFailureMessages();
            foreach (var f in failures)
            {
                var desc = f.GetDescriptionText();
                if (f.GetSeverity() == FailureSeverity.Warning)
                {
                    if (!string.IsNullOrWhiteSpace(desc))
                        Warnings.Add(desc);
                    failuresAccessor.DeleteWarning(f);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
