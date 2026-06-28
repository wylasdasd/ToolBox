using System.Text.RegularExpressions;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Text;

public static class NamingStyleService
{
    public static ToolResult<NamingStyleResult> Convert(string? input)
    {
        var text = input ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return ToolResult<NamingStyleResult>.Ok(NamingStyleResult.Empty);

        try
        {
            var words = SplitWords(text);
            if (words.Count == 0)
                return ToolResult<NamingStyleResult>.Fail("无法识别有效单词。");

            var lower = words.Select(w => w.ToLowerInvariant()).ToArray();
            var result = new NamingStyleResult(
                CamelCase: lower[0] + string.Concat(lower.Skip(1).Select(Capitalize)),
                PascalCase: string.Concat(lower.Select(Capitalize)),
                SnakeCase: string.Join('_', lower),
                KebabCase: string.Join('-', lower),
                ScreamingSnake: string.Join('_', lower).ToUpperInvariant(),
                DotCase: string.Join('.', lower),
                TitleCase: string.Join(' ', lower.Select(Capitalize)),
                LowerCase: string.Join(string.Empty, lower));

            return ToolResult<NamingStyleResult>.Ok(result);
        }
        catch (Exception ex)
        {
            return ToolResult<NamingStyleResult>.Fail(ex.Message);
        }
    }

    private static string Capitalize(string word) =>
        word.Length switch
        {
            0 => string.Empty,
            1 => word.ToUpperInvariant(),
            _ => char.ToUpperInvariant(word[0]) + word[1..]
        };

    private static List<string> SplitWords(string input)
    {
        var normalized = input.Trim()
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Replace('.', ' ');

        normalized = Regex.Replace(normalized, @"([a-z0-9])([A-Z])", "$1 $2");
        normalized = Regex.Replace(normalized, @"([A-Z]+)([A-Z][a-z])", "$1 $2");

        return normalized.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .ToList();
    }
}
