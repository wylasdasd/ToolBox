namespace ToolBox.Tools.Text;

public sealed record TextLineStats(
    int LineCount,
    int NonEmptyLineCount,
    int UniqueLineCount,
    int WordCount,
    int CharCount,
    int CharNoWhitespaceCount);

public sealed record TextLinesOperationResult(string Output, string StatusMessage);