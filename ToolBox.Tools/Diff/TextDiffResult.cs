namespace ToolBox.Tools.Diff;

/// <summary>
/// TextDiffOptions：用于表示和处理相关的。
/// </summary>
public sealed record TextDiffOptions(bool IgnoreWhitespace, bool IgnoreCase);

/// <summary>
/// TextDiffResult：用于表示和处理相关的。
/// </summary>
public sealed record TextDiffResult(
    int LineAdditionCount,
    int LineDeletionCount,
    int LineModificationCount);
