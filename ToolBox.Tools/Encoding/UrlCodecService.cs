using System.Net;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Encoding;

public static class UrlCodecService
{
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
