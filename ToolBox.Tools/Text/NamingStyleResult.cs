namespace ToolBox.Tools.Text;

/// <summary>
/// NamingStyleResult：用于表示和处理相关的。
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
    public static NamingStyleResult Empty { get; } = new(
        string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty, string.Empty);
}
