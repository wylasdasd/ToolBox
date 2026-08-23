namespace ToolBox.Tools.Text;

/// <summary>
/// RegexMatchResult：用于表示和处理相关的。
/// </summary>
public sealed record RegexMatchResult(
    int MatchCount,
    string OptionsSummary,
    string ReplacementResult,
    IReadOnlyList<RegexTextSegment> Segments,
    IReadOnlyList<RegexGroupMatch> GroupMatches);

/// <summary>
/// RegexTextSegment：用于表示和处理相关的。
/// </summary>
public sealed record RegexTextSegment(string Text, bool IsMatch);

/// <summary>
/// RegexGroupMatch：用于表示和处理相关的。
/// </summary>
public sealed record RegexGroupMatch(int MatchIndex, IReadOnlyList<RegexGroupInfo> Groups);

/// <summary>
/// RegexGroupInfo：用于表示和处理相关的。
/// </summary>
public sealed record RegexGroupInfo(string DisplayName, string Value);

/// <summary>
/// RegexMatchOptions：用于表示和处理相关的。
/// </summary>
public sealed record RegexMatchOptions(
    bool IgnoreCase,
    bool Multiline,
    bool Singleline,
    bool MatchesOnly);
