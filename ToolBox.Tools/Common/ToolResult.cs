namespace ToolBox.Tools.Common;

public sealed record ToolResult<T>(bool Success, T? Value, string? Error)
{
    public static ToolResult<T> Ok(T value) => new(true, value, null);
    public static ToolResult<T> Fail(string error) => new(false, default, error);
}
