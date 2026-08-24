using System.Text.RegularExpressions;

namespace CommonHelp;

/// <summary>
/// 文本统计信息：总行数、非空行数、去重后行数、单词数、字符数与去除空白后的字符数。
/// </summary>
public sealed record TextLineStats(
    int LineCount,
    int NonEmptyLineCount,
    int UniqueLineCount,
    int WordCount,
    int CharCount,
    int CharNoWhitespaceCount);

/// <summary>
/// 多行文本操作结果：处理后的文本与状态提示。
/// </summary>
public sealed record TextLinesOperationResult(string Output, string StatusMessage);

/// <summary>
/// 多行文本处理工具：去重、排序、去空行、裁剪空白与行级统计。
/// </summary>
public static class TextLinesHelp
{
    /// <summary>
    /// 统计文本的行数、非空行数、去重行数、单词数与字符数；可选按行 trim、忽略空行、忽略大小写。
    /// </summary>
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

    /// <summary>
    /// 多行文本去重；keepOrder 为 true 时保留首次出现顺序，否则只保证去重不保证顺序。
    /// </summary>
    public static Result<TextLinesOperationResult> Deduplicate(
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

        return Result<TextLinesOperationResult>.Ok(new TextLinesOperationResult(
            JoinLines(result),
            $"已去重 {lines.Count - result.Count} 行。"));
    }

    /// <summary>
    /// 多行文本按字典序升序排序。
    /// </summary>
    public static Result<TextLinesOperationResult> SortAsc(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase)
    {
        var lines = PrepareLines(text ?? string.Empty, trimLines, ignoreEmptyLines);
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        lines.Sort(comparer);
        return Result<TextLinesOperationResult>.Ok(new TextLinesOperationResult(JoinLines(lines), "已按升序排序。"));
    }

    /// <summary>
    /// 多行文本按字典序降序排序。
    /// </summary>
    public static Result<TextLinesOperationResult> SortDesc(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase)
    {
        var lines = PrepareLines(text ?? string.Empty, trimLines, ignoreEmptyLines);
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        lines.Sort(comparer);
        lines.Reverse();
        return Result<TextLinesOperationResult>.Ok(new TextLinesOperationResult(JoinLines(lines), "已按降序排序。"));
    }

    /// <summary>
    /// 移除所有空白行。
    /// </summary>
    public static Result<TextLinesOperationResult> RemoveEmptyLines(string? text, bool trimLines)
    {
        var lines = PrepareLines(text ?? string.Empty, trimLines, removeEmpty: true);
        return Result<TextLinesOperationResult>.Ok(new TextLinesOperationResult(JoinLines(lines), "已移除空行。"));
    }

    /// <summary>
    /// 裁剪每行首尾空白（保留空行）。
    /// </summary>
    public static Result<TextLinesOperationResult> TrimOnly(string? text)
    {
        var lines = PrepareLines(text ?? string.Empty, applyTrim: true, removeEmpty: false);
        return Result<TextLinesOperationResult>.Ok(new TextLinesOperationResult(JoinLines(lines), "已裁剪行首尾空白。"));
    }

    /// <summary>
    /// 按选项预处理行列表（可选 trim 与去空行）。
    /// </summary>
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

    /// <summary>
    /// 用系统换行符连接行列表；空列表返回空字符串。
    /// </summary>
    private static string JoinLines(IReadOnlyCollection<string> lines) =>
        lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);

    /// <summary>
    /// 统一换行符后按行拆分为列表。
    /// </summary>
    private static List<string> SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        return normalized.Split('\n', StringSplitOptions.None).ToList();
    }

    /// <summary>
    /// 按 Unicode 字母/数字类别统计单词数（支持中英文与数字混排）。
    /// </summary>
    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return Regex.Matches(text, @"\p{L}[\p{L}\p{M}]*|\p{N}+").Count;
    }
}
