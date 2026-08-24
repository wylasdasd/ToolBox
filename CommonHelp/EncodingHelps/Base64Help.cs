using System.Text;

namespace CommonHelp;

/// <summary>
/// Base64 编解码工具：支持标准字母表与 URL-safe 字母表（-、_ 替换 +、/ 并去除 = 填充）。
/// </summary>
public static class Base64Help
{
    /// <summary>
    /// 将 UTF-8 文本编码为 Base64；useUrlSafe 时输出 URL-safe 字母表。
    /// </summary>
    public static Result<string> Encode(string? plainText, bool useUrlSafe)
    {
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(plainText ?? string.Empty);
            var base64 = Convert.ToBase64String(bytes);
            var result = useUrlSafe ? ToUrlSafe(base64) : base64;
            return Result<string>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 将 Base64 文本解码为 UTF-8 字符串；useUrlSafe 时先还原字母表与填充。
    /// </summary>
    public static Result<string> Decode(string? base64Text, bool useUrlSafe)
    {
        try
        {
            var input = base64Text ?? string.Empty;
            if (useUrlSafe)
                input = FromUrlSafe(input);

            var bytes = Convert.FromBase64String(input);
            return Result<string>.Ok(System.Text.Encoding.UTF8.GetString(bytes));
        }
        catch (FormatException ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 标准 Base64 转 URL-safe：+ → -、/ → _，并去除尾部 = 填充。
    /// </summary>
    private static string ToUrlSafe(string base64) =>
        base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>
    /// URL-safe 还原为标准 Base64：字符替换回退并按需补齐 = 填充。
    /// </summary>
    private static string FromUrlSafe(string base64)
    {
        var restored = base64.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (restored.Length % 4);
        if (padding is > 0 and < 4)
            restored = restored.PadRight(restored.Length + padding, '=');

        return restored;
    }
}
