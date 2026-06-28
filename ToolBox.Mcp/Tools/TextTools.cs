using System.ComponentModel;
using ModelContextProtocol.Server;
using ToolBox.Tools.Diff;
using ToolBox.Tools.Text;

namespace ToolBox.Mcp.Tools;

[McpServerToolType]
public static class TextTools
{
    [McpServerTool, Description("Convert identifiers between camelCase, PascalCase, snake_case, kebab-case, etc.")]
    public static string NamingStyleConvert(string input) =>
        McpToolResponses.From(NamingStyleService.Convert(input));

    [McpServerTool, Description("Match text with a .NET regular expression; optionally apply replacement.")]
    public static string RegexMatch(
        string pattern,
        string inputText,
        string? replacementText = null,
        bool ignoreCase = false,
        bool multiline = false,
        bool singleline = false,
        bool matchesOnly = false)
    {
        var options = new RegexMatchOptions(ignoreCase, multiline, singleline, matchesOnly);
        return McpToolResponses.From(RegexMatchService.Match(pattern, inputText, replacementText, options));
    }

    [McpServerTool, Description("Process multi-line text: deduplicate, sort_asc, sort_desc, remove_empty, or trim.")]
    public static string TextLinesProcess(
        string text,
        string operation,
        bool trimLines = true,
        bool ignoreEmptyLines = true,
        bool ignoreCase = false,
        bool keepOrder = true)
    {
        var result = operation.ToLowerInvariant() switch
        {
            "deduplicate" => TextLinesService.Deduplicate(text, trimLines, ignoreEmptyLines, ignoreCase, keepOrder),
            "sort_asc" => TextLinesService.SortAsc(text, trimLines, ignoreEmptyLines, ignoreCase),
            "sort_desc" => TextLinesService.SortDesc(text, trimLines, ignoreEmptyLines, ignoreCase),
            "remove_empty" => TextLinesService.RemoveEmptyLines(text, trimLines),
            "trim" => TextLinesService.TrimOnly(text),
            _ => ToolBox.Tools.Common.ToolResult<TextLinesOperationResult>.Fail(
                "operation must be: deduplicate, sort_asc, sort_desc, remove_empty, trim"),
        };
        return McpToolResponses.From(result);
    }

    [McpServerTool, Description("Compare two texts and return diff statistics.")]
    public static string TextDiffCompare(
        string text1,
        string text2,
        bool ignoreWhitespace = false,
        bool ignoreCase = false) =>
        McpToolResponses.From(TextDiffService.Compare(text1, text2, new TextDiffOptions(ignoreWhitespace, ignoreCase)));
}
