using System.Text.RegularExpressions;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Text;

public static class TextLinesService
{
    public static TextLineStats ComputeStats(string? text, bool applyTrim, bool removeEmpty, bool ignoreCase)
    {
        var rawLines = SplitLines(text ?? string.Empty);
        var lineCount = rawLines.Count;
        var nonEmptyLineCount = rawLines.Count(line => !string.IsNullOrWhiteSpace(line));
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        var normalizedLines = rawLines.Select(line => applyTrim ? line.Trim() : line);
        if (removeEmpty)
            normalizedLines = normalizedLines.Where(line => !string.IsNullOrWhiteSpace(line));

        var uniqueLineCount = new HashSet<string>(normalizedLines, comparer).Count;
        var charCount = text?.Length ?? 0;
        var charNoWhitespaceCount = string.IsNullOrEmpty(text) ? 0 : text.Count(c => !char.IsWhiteSpace(c));
        var wordCount = CountWords(text ?? string.Empty);
        return new TextLineStats(lineCount, nonEmptyLineCount, uniqueLineCount, wordCount, charCount, charNoWhitespaceCount);
    }

    public static ToolResult<TextLinesOperationResult> Deduplicate(
        string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase, bool keepOrder)
    {
        var lines = PrepareLines(text ?? string.Empty, trimLines, ignoreEmptyLines);
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        List<string> result;
        if (keepOrder)
        {
            result = new List<string>(lines.Count);
            var seen = new HashSet<string>(comparer);
            foreach (var line in lines)
            {
                if (seen.Add(line))
                    result.Add(line);
            }
        }
        else
        {
            result = lines.Distinct(comparer).ToList();
        }

        return ToolResult<TextLinesOperationResult>.Ok(new TextLinesOperationResult(
            JoinLines(result),
            $"已去重 {lines.Count - result.Count} 行。"));
    }

    public static ToolResult<TextLinesOperationResult> SortAsc(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase)
    {
        var lines = PrepareLines(text ?? string.Empty, trimLines, ignoreEmptyLines);
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        lines.Sort(comparer);
        return ToolResult<TextLinesOperationResult>.Ok(new TextLinesOperationResult(JoinLines(lines), "已按升序排序。"));
    }

    public static ToolResult<TextLinesOperationResult> SortDesc(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase)
    {
        var lines = PrepareLines(text ?? string.Empty, trimLines, ignoreEmptyLines);
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        lines.Sort(comparer);
        lines.Reverse();
        return ToolResult<TextLinesOperationResult>.Ok(new TextLinesOperationResult(JoinLines(lines), "已按降序排序。"));
    }

    public static ToolResult<TextLinesOperationResult> RemoveEmptyLines(string? text, bool trimLines)
    {
        var lines = PrepareLines(text ?? string.Empty, trimLines, removeEmpty: true);
        return ToolResult<TextLinesOperationResult>.Ok(new TextLinesOperationResult(JoinLines(lines), "已移除空行。"));
    }

    public static ToolResult<TextLinesOperationResult> TrimOnly(string? text)
    {
        var lines = PrepareLines(text ?? string.Empty, applyTrim: true, removeEmpty: false);
        return ToolResult<TextLinesOperationResult>.Ok(new TextLinesOperationResult(JoinLines(lines), "已裁剪行首尾空白。"));
    }

    private static List<string> PrepareLines(string text, bool applyTrim, bool removeEmpty)
    {
        var lines = SplitLines(text);
        var list = new List<string>(lines.Count);
        foreach (var line in lines)
        {
            var value = applyTrim ? line.Trim() : line;
            if (removeEmpty && string.IsNullOrWhiteSpace(value))
                continue;

            list.Add(value);
        }

        return list;
    }

    private static string JoinLines(IReadOnlyCollection<string> lines) =>
        lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);

    private static List<string> SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        return normalized.Split('\n', StringSplitOptions.None).ToList();
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return Regex.Matches(text, @"\p{L}[\p{L}\p{M}]*|\p{N}+").Count;
    }
}