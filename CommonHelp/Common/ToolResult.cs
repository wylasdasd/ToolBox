namespace CommonHelp;

/// <summary>
/// 工具操作的统一返回结果：成功时 <see cref="Value"/> 携带结果，失败时 <see cref="Error"/> 携带用户可读的错误说明。
/// </summary>
public sealed record ToolResult<T>(bool Success, T? Value, string? Error)
{
    /// <summary>
    /// 构建成功结果。
    /// </summary>
    public static ToolResult<T> Ok(T value) => new(true, value, null);

    /// <summary>
    /// 构建失败结果并携带错误说明。
    /// </summary>
    public static ToolResult<T> Fail(string error) => new(false, default, error);
}
