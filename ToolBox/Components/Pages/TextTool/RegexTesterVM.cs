using Blazing.Mvvm.ComponentModel;
using System.Text.RegularExpressions;

namespace ToolBox.Components.Pages;

public sealed class RegexTesterVM : ViewModelBase
{
    private const int RegexTimeoutMs = 200;

    private string _pattern = @"\b\w+\b";
    private string _inputText = "Type or paste text here.\r\nMatches will be highlighted in real time.";
    private string _replacementText = string.Empty;
    private string _replacementResult = string.Empty;
    private bool _ignoreCase = true;
    private bool _multiline;
    private bool _singleline;
    private bool _matchesOnly;
    private bool _showGroups;
    private string? _errorMessage;
    private int _matchCount;
    private IReadOnlyList<TextSegment> _segments = Array.Empty<TextSegment>();
    private IReadOnlyList<GroupMatch> _groupMatches = Array.Empty<GroupMatch>();

    public string Pattern
    {
        get => _pattern;
        set
        {
            if (SetProperty(ref _pattern, value))
            {
                UpdateMatches();
            }
        }
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                UpdateMatches();
            }
        }
    }

    public string ReplacementText
    {
        get => _replacementText;
        set
        {
            if (SetProperty(ref _replacementText, value))
            {
                UpdateMatches();
            }
        }
    }

    public string ReplacementResult
    {
        get => _replacementResult;
        private set => SetProperty(ref _replacementResult, value);
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set
        {
            if (SetProperty(ref _ignoreCase, value))
            {
                UpdateMatches();
            }
        }
    }

    public bool Multiline
    {
        get => _multiline;
        set
        {
            if (SetProperty(ref _multiline, value))
            {
                UpdateMatches();
            }
        }
    }

    public bool Singleline
    {
        get => _singleline;
        set
        {
            if (SetProperty(ref _singleline, value))
            {
                UpdateMatches();
            }
        }
    }

    public bool MatchesOnly
    {
        get => _matchesOnly;
        set
        {
            if (SetProperty(ref _matchesOnly, value))
            {
                UpdateMatches();
            }
        }
    }

    public bool ShowGroups
    {
        get => _showGroups;
        set
        {
            if (SetProperty(ref _showGroups, value))
            {
                UpdateMatches();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int MatchCount
    {
        get => _matchCount;
        private set => SetProperty(ref _matchCount, value);
    }

    public IReadOnlyList<TextSegment> Segments
    {
        get => _segments;
        private set => SetProperty(ref _segments, value);
    }

    public IReadOnlyList<GroupMatch> GroupMatches
    {
        get => _groupMatches;
        private set => SetProperty(ref _groupMatches, value);
    }

    public void ApplyReplacement()
    {
        if (!string.IsNullOrEmpty(ReplacementResult))
        {
            InputText = ReplacementResult;
        }
    }

    public override Task OnInitializedAsync()
    {
        UpdateMatches();
        return Task.CompletedTask;
    }

    private void UpdateMatches()
    {
        var text = InputText ?? string.Empty;
        var pattern = Pattern ?? string.Empty;

        if (string.IsNullOrWhiteSpace(pattern))
        {
            ErrorMessage = null;
            MatchCount = 0;
            Segments = BuildSegments(text, Array.Empty<(int Index, int Length)>());
            GroupMatches = Array.Empty<GroupMatch>();
            ReplacementResult = string.Empty;
            return;
        }

        try
        {
            var options = RegexOptions.None;
            if (IgnoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            if (Multiline)
            {
                options |= RegexOptions.Multiline;
            }

            if (Singleline)
            {
                options |= RegexOptions.Singleline;
            }

            var regex = new Regex(pattern, options, TimeSpan.FromMilliseconds(RegexTimeoutMs));
            var matches = regex.Matches(text);

            var spans = new List<(int Index, int Length)>(matches.Count);
            var groupMatches = new List<GroupMatch>(matches.Count);
            var groupNames = regex.GetGroupNames();
            var matchIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Length <= 0)
                {
                    continue;
                }

                spans.Add((match.Index, match.Length));
                groupMatches.Add(BuildGroupMatch(match, groupNames, matchIndex));
                matchIndex++;
            }

            ErrorMessage = null;
            MatchCount = spans.Count;
            GroupMatches = groupMatches;
            ReplacementResult = regex.Replace(text, ReplacementText ?? string.Empty);

            if (MatchesOnly)
            {
                if (spans.Count == 0)
                {
                    Segments = Array.Empty<TextSegment>();
                }
                else
                {
                    var filtered = FilterToMatchedLines(text, spans);
                    Segments = BuildSegments(filtered.FilteredText, filtered.FilteredSpans);
                }
            }
            else
            {
                Segments = BuildSegments(text, spans);
            }
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
            MatchCount = 0;
            Segments = BuildSegments(text, Array.Empty<(int Index, int Length)>());
            GroupMatches = Array.Empty<GroupMatch>();
            ReplacementResult = string.Empty;
        }
        catch (RegexMatchTimeoutException)
        {
            ErrorMessage = "Match timeout. Try simplifying the pattern or input.";
            MatchCount = 0;
            Segments = BuildSegments(text, Array.Empty<(int Index, int Length)>());
            GroupMatches = Array.Empty<GroupMatch>();
            ReplacementResult = string.Empty;
        }
    }

    private static IReadOnlyList<TextSegment> BuildSegments(string text, IReadOnlyList<(int Index, int Length)> matches)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<TextSegment>();
        }

        if (matches.Count == 0)
        {
            return new[] { new TextSegment(text, false) };
        }

        var segments = new List<TextSegment>(matches.Count * 2 + 1);
        var cursor = 0;

        foreach (var match in matches)
        {
            if (match.Index > cursor)
            {
                segments.Add(new TextSegment(text.Substring(cursor, match.Index - cursor), false));
            }

            segments.Add(new TextSegment(text.Substring(match.Index, match.Length), true));
            cursor = match.Index + match.Length;
        }

        if (cursor < text.Length)
        {
            segments.Add(new TextSegment(text.Substring(cursor), false));
        }

        return segments;
    }

    private static GroupMatch BuildGroupMatch(Match match, string[] groupNames, int matchIndex)
    {
        var groups = new List<GroupInfo>(match.Groups.Count);

        for (var i = 0; i < match.Groups.Count; i++)
        {
            var group = match.Groups[i];
            var name = i < groupNames.Length ? groupNames[i] : i.ToString();
            var display = string.IsNullOrWhiteSpace(name) || name == i.ToString()
                ? $"#{i}"
                : $"{name} (#{i})";
            groups.Add(new GroupInfo(display, group.Value));
        }

        return new GroupMatch(matchIndex + 1, groups);
    }

    private static FilteredResult FilterToMatchedLines(string text, IReadOnlyList<(int Index, int Length)> matches)
    {
        var lineRanges = GetLineRanges(text);
        var matchedLines = new bool[lineRanges.Count];

        foreach (var match in matches)
        {
            var lineIndex = FindLineIndex(lineRanges, match.Index);
            if (lineIndex >= 0)
            {
                matchedLines[lineIndex] = true;
            }
        }

        var builder = new System.Text.StringBuilder();
        var filteredSpans = new List<(int Index, int Length)>(matches.Count);
        var newCursor = 0;

        for (var i = 0; i < lineRanges.Count; i++)
        {
            if (!matchedLines[i])
            {
                continue;
            }

            var (lineStart, lineLength) = lineRanges[i];
            var lineEnd = lineStart + lineLength;
            var lineText = text.Substring(lineStart, lineLength);

            builder.Append(lineText);

            foreach (var match in matches)
            {
                if (match.Index < lineStart || match.Index >= lineEnd)
                {
                    continue;
                }

                var offset = match.Index - lineStart;
                filteredSpans.Add((newCursor + offset, match.Length));
            }

            newCursor = builder.Length;
        }

        return new FilteredResult(builder.ToString(), filteredSpans);
    }

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
        {
            ranges.Add((start, text.Length - start));
        }

        return ranges;
    }

    private static int FindLineIndex(IReadOnlyList<(int Start, int Length)> lineRanges, int index)
    {
        for (var i = 0; i < lineRanges.Count; i++)
        {
            var (start, length) = lineRanges[i];
            if (index >= start && index < start + length)
            {
                return i;
            }
        }

        return -1;
    }

    private sealed record FilteredResult(string FilteredText, IReadOnlyList<(int Index, int Length)> FilteredSpans);

    public sealed record TextSegment(string Text, bool IsMatch);

    public sealed record GroupMatch(int MatchIndex, IReadOnlyList<GroupInfo> Groups);

    public sealed record GroupInfo(string DisplayName, string Value);
}
