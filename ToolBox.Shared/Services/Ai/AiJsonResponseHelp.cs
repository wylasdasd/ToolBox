using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ToolBox.Services.Ai;

public static class AiJsonResponseHelp
{
    public static readonly JsonSerializerOptions DisplayOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly Regex MarkdownFenceRegex = new(
        @"```(?:json)?\s*([\s\S]*?)```",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string NormalizeToJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("AI 返回为空。");

        var text = raw.Trim();
        var fenceMatch = MarkdownFenceRegex.Match(text);
        if (fenceMatch.Success)
            text = fenceMatch.Groups[1].Value.Trim();

        using var doc = JsonDocument.Parse(text);
        return JsonSerializer.Serialize(doc.RootElement, DisplayOptions);
    }

    public static string SerializeForDisplay<T>(T value) =>
        JsonSerializer.Serialize(value, DisplayOptions);
}
