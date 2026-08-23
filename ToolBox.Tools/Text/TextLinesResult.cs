namespace ToolBox.Tools.Text;

/// <summary>
/// TextLineStats：用于表示和处理相关的。
/// </summary>
public sealed record TextLineStats(
    int LineCount,
    int NonEmptyLineCount,
    int UniqueLineCount,
    int WordCount,
    int CharCount,
    int CharNoWhitespaceCount);

/// <summary>
/// TextLinesOperationResult：用于表示和处理相关的。
/// </summary>
public sealed record TextLinesOperationResult(string Output, string StatusMessage);
