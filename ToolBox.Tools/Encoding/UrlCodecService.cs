using System.Net;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Encoding;

/// <summary>
/// UrlCodecService：用于表示和处理相关的。
/// </summary>
public static class UrlCodecService
{
    /// <summary>
    /// 执行 Encode 操作，完成该方法对应的工具处理逻辑。
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
    /// 执行 Decode 操作，完成该方法对应的工具处理逻辑。
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
