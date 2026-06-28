namespace ToolBox.Tools.Text;

public sealed record RegexMatchResult(
    int MatchCount,
    string OptionsSummary,
    string ReplacementResult,
    IReadOnlyList<RegexTextSegment> Segments,
    IReadOnlyList<RegexGroupMatch> GroupMatches);

public sealed record RegexTextSegment(string Text, bool IsMatch);

public sealed record RegexGroupMatch(int MatchIndex, IReadOnlyList<RegexGroupInfo> Groups);

public sealed record RegexGroupInfo(string DisplayName, string Value);

public sealed record RegexMatchOptions(
    bool IgnoreCase,
    bool Multiline,
    bool Singleline,
    bool MatchesOnly);