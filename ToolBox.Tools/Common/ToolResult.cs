namespace ToolBox.Tools.Common;

/// <summary>
/// ToolResult：用于表示和处理相关的。
/// </summary>
public sealed record ToolResult<T>(bool Success, T? Value, string? Error)
{
    /// <summary>
    /// 执行 Ok 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static ToolResult<T> Ok(T value) => new(true, value, null);
    /// <summary>
    /// 执行 Fail 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static ToolResult<T> Fail(string error) => new(false, default, error);
}
