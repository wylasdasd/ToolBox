namespace ToolBox.Tools.Text;

public static class ClipboardSequenceService
{
    public static IReadOnlyList<string> ParseNonEmptyLines(string? text) =>
        (text ?? string.Empty)
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

    public static IReadOnlyList<string> AppendDistinct(
        IReadOnlyList<string> current,
        IEnumerable<string> values) =>
        current.Concat(values).Distinct().ToList();

    public static IReadOnlyList<string> Remove(
        IReadOnlyList<string> sequences,
        string selected) =>
        sequences.Where(x => x != selected).ToList();

    public static IReadOnlyList<string> GetBatchItems(IReadOnlyList<string> sequences) =>
        sequences.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

    public static string? SelectFirstOrKeep(
        IReadOnlyList<string> sequences,
        string? currentSelection) =>
        string.IsNullOrEmpty(currentSelection)
            ? sequences.FirstOrDefault()
            : currentSelection;
}
