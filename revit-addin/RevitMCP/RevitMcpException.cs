using Newtonsoft.Json.Linq;
using System;

namespace RevitMCP;

/// <summary>
/// Structured exception for RevitMCP conforming to Rule 8:
/// { "success": false, "error": { "code": "...", "message": "...", "details": { ... } } }
/// </summary>
public class RevitMcpException : Exception
{
    public string Code { get; }
    public JObject Details { get; }

    public RevitMcpException(string code, string message, JObject? details = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = string.IsNullOrWhiteSpace(code) ? "REVIT_API_ERROR" : code;
        Details = details ?? new JObject();
    }

    public JObject ToJson()
    {
        return new JObject
        {
            ["success"] = false,
            ["error"] = new JObject
            {
                ["code"] = Code,
                ["message"] = Message,
                ["details"] = Details
            }
        };
    }

    public static JObject CreateErrorResponse(string code, string message, JObject? details = null)
    {
        return new JObject
        {
            ["success"] = false,
            ["error"] = new JObject
            {
                ["code"] = code,
                ["message"] = message,
                ["details"] = details ?? new JObject()
            }
        };
    }

    public static JObject FromException(Exception ex)
    {
        if (ex is RevitMcpException rEx)
            return rEx.ToJson();

        return CreateErrorResponse("REVIT_API_ERROR", ex.Message, new JObject
        {
            ["exceptionType"] = ex.GetType().FullName
        });
    }
}
