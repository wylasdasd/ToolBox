using System.Text.Json;
using System.Text.RegularExpressions;

namespace CommonHelp;

/// <summary>
/// Markdown 围栏 JSON 提取工具：剥离 ```/```json 代码围栏并规范化 JSON 文本（LLM 输出清洗常用）。
/// </summary>
public static class MarkdownJsonHelp
{
    private static readonly Regex MarkdownFenceRegex = new(
        @"```(?:json)?\s*([\s\S]*?)```",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// 剥离 Markdown 代码围栏：命中围栏时返回围栏内文本（已 Trim），否则返回原文本 Trim。
    /// </summary>
    public static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();
        var match = MarkdownFenceRegex.Match(trimmed);
        return match.Success ? match.Groups[1].Value.Trim() : trimmed;
    }

    /// <summary>
    /// 清洗文本为规范 JSON：剥离围栏后解析并重新序列化为缩进、非 ASCII 不转义的展示格式；输入为空或解析失败时抛出异常。
    /// </summary>
    public static string NormalizeToJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("输入为空，无法解析为 JSON。");

        var text = StripCodeFence(raw);

        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.JsonSe(JsonExtensions.IndentedRelaxedOptions);
    }
}
