using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Diff;

/// <summary>
/// TextDiffService：用于表示和处理相关的。
/// </summary>
public static class TextDiffService
{
    /// <summary>
    /// 执行 Compare 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static ToolResult<TextDiffResult> Compare(string? text1, string? text2, TextDiffOptions options)
    {
        try
        {
            var oldText = PrepareText(text1, options);
            var newText = PrepareText(text2, options);

            var builder = new SideBySideDiffBuilder(new Differ());
            var model = builder.BuildDiffModel(oldText, newText);

            var additions = model.NewText.Lines.Count(l => l.Type == ChangeType.Inserted);
            var deletions = model.OldText.Lines.Count(l => l.Type == ChangeType.Deleted);
            var modifications = model.NewText.Lines.Count(l => l.Type == ChangeType.Modified);

            return ToolResult<TextDiffResult>.Ok(new TextDiffResult(additions, deletions, modifications));
        }
        catch (Exception ex)
        {
            return ToolResult<TextDiffResult>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 执行 PrepareText 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    private static string PrepareText(string? text, TextDiffOptions options)
    {
        var value = text ?? string.Empty;
        if (options.IgnoreCase)
            value = value.ToLowerInvariant();

        if (!options.IgnoreWhitespace)
            return value;

        var lines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        return string.Join('\n', lines.Select(NormalizeLineWhitespace));
    }

    /// <summary>
    /// 执行 NormalizeLineWhitespace 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    private static string NormalizeLineWhitespace(string line)
    {
        if (string.IsNullOrEmpty(line))
            return string.Empty;

        return string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
