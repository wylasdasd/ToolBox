using System.Text.Json;

namespace ToolBox.Components.Pages.AiExtract;

internal static class AiExtractBatchResults
{
    public static Dictionary<string, object?> Ok(string source, JsonElement data) =>
        new()
        {
            ["source"] = source,
            ["success"] = true,
            ["data"] = data
        };

    public static Dictionary<string, object?> Ok(string source, string normalizedJson) =>
        Ok(source, JsonSerializer.Deserialize<JsonElement>(normalizedJson));

    public static Dictionary<string, object?> Fail(string source, string error) =>
        new()
        {
            ["source"] = source,
            ["success"] = false,
            ["error"] = error
        };
}
