namespace CommonHelp;

/// <summary>
/// 工具操作的统一返回结果：成功时 <see cref="Value"/> 携带结果，失败时 <see cref="Error"/> 携带用户可读的错误说明。
/// </summary>
public sealed record Result<T>(bool IsSuccess, T? Value, string? Error)
{
    /// <summary>
    /// 构建成功结果。
    /// </summary>
    public static Result<T> Ok(T value) => new(true, value, null);

    /// <summary>
    /// 构建失败结果并携带错误说明。
    /// </summary>
    public static Result<T> Fail(string error) => new(false, default, error);
}


public sealed record HttpResult<T>(int Code, T? Value, string? Error)
{
    // 根据 Code 自动判断是否成功（200-299 为成功）
    public bool IsSuccess => Code >= 200 && Code < 300;

    /// <summary>
    /// 构建成功结果，默认 HTTP 200
    /// </summary>
    public static HttpResult<T> Ok(T value, int code = 200)
        => new(code, value, null);

    /// <summary>
    /// 构建失败结果，默认 HTTP 500
    /// </summary>
    public static HttpResult<T> Fail(string error, int code = 500)
        => new(code, default, error);
}

/// <summary>
/// 只为兼容业务不建议使用，建议使用 <see cref="HttpResult{T}"/> 或 <see cref="Result{T}"/>，因为 Code 的语义不明确。
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Code"></param>
/// <param name="Value"></param>
/// <param name="Error"></param>
public sealed record BLLResult<T>(int Code, T? Value, string? Error)
{
    // 假设 0 表示成功，非0表示失败（类似 HTTP 或自定义码）
    public bool IsSuccess => Code == 0;

    // 成功时可以指定成功码，默认 0
    public static BLLResult<T> Ok(T value, int code = 0)
        => new(code, value, null);

    // 失败时可以指定错误码，默认 -1 或其他非0值
    public static BLLResult<T> Fail(string error, int code = -1)
        => new(code, default, error);
}
