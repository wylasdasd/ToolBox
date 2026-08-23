using System.Text.RegularExpressions;

namespace CommonHelp;

/// <summary>
/// 命名风格转换结果：同一输入对应的 8 种常见命名风格输出。
/// </summary>
public sealed record NamingStyleResult(
    string CamelCase,
    string PascalCase,
    string SnakeCase,
    string KebabCase,
    string ScreamingSnake,
    string DotCase,
    string TitleCase,
    string LowerCase)
{
    /// <summary>
    /// 全部字段为空字符串的空结果（用于空输入）。
    /// </summary>
    public static NamingStyleResult Empty { get; } = new(
        string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty, string.Empty);
}

/// <summary>
/// 标识符命名风格转换工具：把任意形式的标识符拆词后一次性转换为 camelCase、PascalCase、snake_case 等 8 种风格。
/// </summary>
public static class NamingStyleHelp
{
    /// <summary>
    /// 将输入标识符转换为 8 种命名风格。支持 camelCase/PascalCase/snake_case/kebab-case/dot.case/空格等混合输入；空白输入返回空结果。
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
    /// 单词首字母转大写。
    /// </summary>
    private static string Capitalize(string word) =>
        word.Length switch
        {
            0 => string.Empty,
            1 => word.ToUpperInvariant(),
            _ => char.ToUpperInvariant(word[0]) + word[1..]
        };

    /// <summary>
    /// 按分隔符（- _ . 空白）与驼峰大小写边界将输入拆分为单词列表。
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
