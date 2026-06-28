using System.Net;
using System.Text.Json;

namespace ToolBox.Services.Ai;

public static class AiHttpErrorHelp
{
    public static string FormatFailure(string prefix, HttpStatusCode code, string responseText)
    {
        var detail = TryExtractMessage(responseText);
        return string.IsNullOrWhiteSpace(detail)
            ? $"{prefix}：HTTP {(int)code}。"
            : $"{prefix}：HTTP {(int)code}，{detail}";
    }

    public static string? TryExtractMessage(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                    return error.GetString();

                if (error.ValueKind == JsonValueKind.Object &&
                    error.TryGetProperty("message", out var nestedMessage) &&
                    nestedMessage.ValueKind == JsonValueKind.String)
                {
                    return nestedMessage.GetString();
                }
            }

            if (root.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch
        {
        }

        return null;
    }
}
