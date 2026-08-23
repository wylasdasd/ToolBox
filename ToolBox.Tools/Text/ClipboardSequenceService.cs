namespace ToolBox.Tools.Text;

/// <summary>
/// ClipboardSequenceService：用于表示和处理相关的。
/// </summary>
public static class ClipboardSequenceService
{
    /// <summary>
    /// 执行 ParseNonEmptyLines 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static IReadOnlyList<string> ParseNonEmptyLines(string? text) =>
        (text ?? string.Empty)
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

    /// <summary>
    /// 执行 AppendDistinct 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static IReadOnlyList<string> AppendDistinct(
        IReadOnlyList<string> current,
        IEnumerable<string> values) =>
        current.Concat(values).Distinct().ToList();

    /// <summary>
    /// 执行 Remove 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static IReadOnlyList<string> Remove(
        IReadOnlyList<string> sequences,
        string selected) =>
        sequences.Where(x => x != selected).ToList();

    /// <summary>
    /// 执行 GetBatchItems 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static IReadOnlyList<string> GetBatchItems(IReadOnlyList<string> sequences) =>
        sequences.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

    /// <summary>
    /// 执行 SelectFirstOrKeep 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static string? SelectFirstOrKeep(
        IReadOnlyList<string> sequences,
        string? currentSelection) =>
        string.IsNullOrEmpty(currentSelection)
            ? sequences.FirstOrDefault()
            : currentSelection;
}
