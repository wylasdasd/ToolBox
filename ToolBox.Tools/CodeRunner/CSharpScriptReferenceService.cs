namespace ToolBox.Tools.CodeRunner;

/// <summary>
/// CSharpScriptReferenceService：用于表示和处理相关的。
/// </summary>
public static class CSharpScriptReferenceService
{
    /// <summary>
    /// 执行 ParseReferenceNames 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static IReadOnlyList<string> ParseReferenceNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
    }

    /// <summary>
    /// 执行 ParseReferencePaths 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static IReadOnlyList<string> ParseReferencePaths(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
    }
}
