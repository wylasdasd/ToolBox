using System.Text.RegularExpressions;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Text;

/// <summary>
/// NamingStyleService：用于表示和处理相关的。
/// </summary>
public static class NamingStyleService
{
    /// <summary>
    /// 执行 Convert 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
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

    /// <summary>
    /// 执行 Capitalize 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    private static string Capitalize(string word) =>
        word.Length switch
        {
            0 => string.Empty,
            1 => word.ToUpperInvariant(),
            _ => char.ToUpperInvariant(word[0]) + word[1..]
        };

    /// <summary>
    /// 执行 SplitWords 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
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
