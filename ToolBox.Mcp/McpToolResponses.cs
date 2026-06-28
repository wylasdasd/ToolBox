using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ToolBox.Tools.Common;

namespace ToolBox.Mcp;

internal static class McpToolResponses
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string From<T>(ToolResult<T> result)
    {
        if (!result.Success)
            return Error(result.Error ?? "Unknown error");

        return JsonSerializer.Serialize(result.Value, JsonOptions);
    }

    public static string FromValue<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    public static string Error(string message) => $"ERROR: {message}";
}
