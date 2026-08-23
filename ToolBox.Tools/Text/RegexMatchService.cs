using System.Text;
using System.Text.RegularExpressions;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Text;

/// <summary>
/// RegexMatchService：用于表示和处理相关的。
/// </summary>
public static class RegexMatchService
{
    private const int RegexTimeoutMs = 200;

    /// <summary>
    /// 执行 Match 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static ToolResult<RegexMatchResult> Match(
        string? pattern,
        string? inputText,
        string? replacementText,
        RegexMatchOptions options)
    {
        var text = inputText ?? string.Empty;
        var patternValue = pattern ?? string.Empty;

        if (string.IsNullOrWhiteSpace(patternValue))
        {
            var emptyOptions = BuildOptions(options);
            return ToolResult<RegexMatchResult>.Ok(new RegexMatchResult(
                0,
                FormatOptionsSummary(emptyOptions),
                string.Empty,
                BuildSegments(text, Array.Empty<(int Index, int Length)>()),
                Array.Empty<RegexGroupMatch>()));
        }

        try
        {
            var regexOptions = BuildOptions(options);
            var regex = new Regex(patternValue, regexOptions, TimeSpan.FromMilliseconds(RegexTimeoutMs));
            var matches = regex.Matches(text);

            var spans = new List<(int Index, int Length)>(matches.Count);
            var groupMatches = new List<RegexGroupMatch>(matches.Count);
            var groupNames = regex.GetGroupNames();
            var matchIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Length <= 0)
                    continue;

                spans.Add((match.Index, match.Length));
                groupMatches.Add(BuildGroupMatch(match, groupNames, matchIndex));
                matchIndex++;
            }

            var replacementResult = regex.Replace(text, replacementText ?? string.Empty);
            IReadOnlyList<RegexTextSegment> segments;

            if (options.MatchesOnly)
            {
                if (spans.Count == 0)
                    segments = Array.Empty<RegexTextSegment>();
                else
                {
                    var filtered = FilterToMatchedLines(text, spans);
                    segments = BuildSegments(filtered.FilteredText, filtered.FilteredSpans);
                }
            }
            else
            {
                segments = BuildSegments(text, spans);
            }

            return ToolResult<RegexMatchResult>.Ok(new RegexMatchResult(
                spans.Count,
                FormatOptionsSummary(regexOptions),
                replacementResult,
                segments,
                groupMatches));
        }
        catch (ArgumentException ex)
        {
            return ToolResult<RegexMatchResult>.Fail(ex.Message);
        }
        catch (RegexMatchTimeoutException)
        {
            return ToolResult<RegexMatchResult>.Fail("Match timeout. Try simplifying the pattern or input.");
        }
    }

    /// <summary>
    /// 执行 BuildOptions 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    private static RegexOptions BuildOptions(RegexMatchOptions options)
    {
        var result = RegexOptions.None;
        if (options.IgnoreCase)
            result |= RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        if (options.Multiline)
            result |= RegexOptions.Multiline;
        if (options.Singleline)
            result |= RegexOptions.Singleline;
        return result;
    }

    /// <summary>
    /// 执行 FormatOptionsSummary 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    private static string FormatOptionsSummary(RegexOptions options)
    {
        var flags = new List<string>(3);
        if (options.HasFlag(RegexOptions.IgnoreCase))
            flags.Add("i");
        if (options.HasFlag(RegexOptions.Multiline))
            flags.Add("m");
        if (options.HasFlag(RegexOptions.Singleline))
            flags.Add("s");
        return flags.Count == 0 ? "无修饰符" : $"(?{string.Join("", flags)})";
    }

    /// <summary>
    /// 执行 BuildSegments 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    private static IReadOnlyList<RegexTextSegment> BuildSegments(string text, IReadOnlyList<(int Index, int Length)> matches)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<RegexTextSegment>();

        if (matches.Count == 0)
            return new[] { new RegexTextSegment(text, false) };

        var segments = new List<RegexTextSegment>(matches.Count * 2 + 1);
        var cursor = 0;

        foreach (var match in matches)
        {
            if (match.Index > cursor)
                segments.Add(new RegexTextSegment(text.Substring(cursor, match.Index - cursor), false));

            segments.Add(new RegexTextSegment(text.Substring(match.Index, match.Length), true));
            cursor = match.Index + match.Length;
        }

        if (cursor < text.Length)
            segments.Add(new RegexTextSegment(text.Substring(cursor), false));

        return segments;
    }

    /// <summary>
    /// 执行 BuildGroupMatch 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    private static RegexGroupMatch BuildGroupMatch(Match match, string[] groupNames, int matchIndex)
    {
        var groups = new List<RegexGroupInfo>(match.Groups.Count);

        for (var i = 0; i < match.Groups.Count; i++)
        {
            var group = match.Groups[i];
            var name = i < groupNames.Length ? groupNames[i] : i.ToString();
            var display = string.IsNullOrWhiteSpace(name) || name == i.ToString()
                ? $"#{i}"
                : $"{name} (#{i})";
            groups.Add(new RegexGroupInfo(display, group.Value));
        }

        return new RegexGroupMatch(matchIndex + 1, groups);
    }

    /// <summary>
    /// FilteredResult：用于表示和处理相关的。
    /// </summary>
    private sealed record FilteredResult(string FilteredText, IReadOnlyList<(int Index, int Length)> FilteredSpans);

    /// <summary>
    /// 执行 FilterToMatchedLines 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    private static FilteredResult FilterToMatchedLines(string text, IReadOnlyList<(int Index, int Length)> matches)
    {
        var lineRanges = GetLineRanges(text);
        var matchedLines = new bool[lineRanges.Count];

        foreach (var match in matches)
        {
            var lineIndex = FindLineIndex(lineRanges, match.Index);
            if (lineIndex >= 0)
                matchedLines[lineIndex] = true;
        }

        var builder = new StringBuilder();
        var filteredSpans = new List<(int Index, int Length)>(matches.Count);
        var newCursor = 0;

        for (var i = 0; i < lineRanges.Count; i++)
        {
            if (!matchedLines[i])
                continue;

            var (lineStart, lineLength) = lineRanges[i];
            var lineEnd = lineStart + lineLength;
            var lineText = text.Substring(lineStart, lineLength);

            builder.Append(lineText);

            foreach (var match in matches)
            {
                if (match.Index < lineStart || match.Index >= lineEnd)
                    continue;

                var offset = match.Index - lineStart;
                filteredSpans.Add((newCursor + offset, match.Length));
            }

            newCursor = builder.Length;
        }

        return new FilteredResult(builder.ToString(), filteredSpans);
    }

    /// <summary>
    /// 计算文本各行的起始位置和长度范围。
    /// </summary>
    private static List<(int Start, int Length)> GetLineRanges(string text)
    {
        var ranges = new List<(int Start, int Length)>();
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '\r')
            {
                var length = i - start;
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    length += 2;
                    ranges.Add((start, length));
                    i++;
                }
                else
                {
                    ranges.Add((start, length + 1));
                }

                start = i + 1;
            }
            else if (ch == '\n')
            {
                ranges.Add((start, i - start + 1));
                start = i + 1;
            }
        }

        if (start < text.Length)
            ranges.Add((start, text.Length - start));

        return ranges;
    }

    /// <summary>
    /// 执行 FindLineIndex 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    private static int FindLineIndex(IReadOnlyList<(int Start, int Length)> lineRanges, int index)
    {
        for (var i = 0; i < lineRanges.Count; i++)
        {
            var (start, length) = lineRanges[i];
            if (index >= start && index < start + length)
                return i;
        }

        return -1;
    }
}
