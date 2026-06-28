namespace ToolBox.Tools.Text;

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
