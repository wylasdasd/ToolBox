using System.Net;

namespace CommonHelp;

/// <summary>
/// URL 百分号编解码工具：提供 Uri.EscapeDataString（严格 RFC 3986，保留更多字符）与 WebUtility.UrlEncode 两种模式。
/// </summary>
public static class UrlCodecHelp
{
    /// <summary>
    /// 将文本进行 URL 编码；useEscapeDataString 为 true 时使用 RFC 3986 严格编码，否则使用 WebUtility 宽松编码。
    /// </summary>
    public static ToolResult<string> Encode(string? plainText, bool useEscapeDataString)
    {
        try
        {
            var input = plainText ?? string.Empty;
            var encoded = useEscapeDataString
                ? Uri.EscapeDataString(input)
                : WebUtility.UrlEncode(input);
            return ToolResult<string>.Ok(encoded ?? string.Empty);
        }
        catch (Exception ex)
        {
            return ToolResult<string>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 将 URL 编码文本解码；模式需与编码时一致。
    /// </summary>
    public static ToolResult<string> Decode(string? encodedText, bool useEscapeDataString)
    {
        try
        {
            var input = encodedText ?? string.Empty;
            var decoded = useEscapeDataString
                ? Uri.UnescapeDataString(input)
                : WebUtility.UrlDecode(input);
            return ToolResult<string>.Ok(decoded ?? string.Empty);
        }
        catch (Exception ex)
        {
            return ToolResult<string>.Fail(ex.Message);
        }
    }
}
