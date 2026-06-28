namespace ToolBox.Tools.Diff;

public sealed record TextDiffOptions(bool IgnoreWhitespace, bool IgnoreCase);

public sealed record TextDiffResult(
    int LineAdditionCount,
    int LineDeletionCount,
    int LineModificationCount);