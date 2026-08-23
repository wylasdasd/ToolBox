using ToolBox.Tools.Common;

namespace ToolBox.Tools.Encoding;

/// <summary>
/// DataUriImageParseResult：用于表示和处理相关的。
/// </summary>
public sealed record DataUriImageParseResult(
    string DataUrl,
    string? MimeType,
    string? DetectedMimeType,
    bool IsDataUrlInput);

/// <summary>
/// DataUriImageService：用于表示和处理相关的。
/// </summary>
public static class DataUriImageService
{
    /// <summary>
    /// 执行 Parse 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static ToolResult<DataUriImageParseResult> Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ToolResult<DataUriImageParseResult>.Fail("输入不能为空。");

        try
        {
            var trimmed = input.Trim();
            if (TryParseDataUrl(trimmed, out var dataUrl, out var base64Part, out var mimeType))
            {
                if (!IsValidBase64(base64Part))
                    return ToolResult<DataUriImageParseResult>.Fail("无效的 Base64 字符串。");

                return ToolResult<DataUriImageParseResult>.Ok(new DataUriImageParseResult(
                    dataUrl,
                    mimeType,
                    null,
                    true));
            }

            if (!IsValidBase64(trimmed))
                return ToolResult<DataUriImageParseResult>.Fail("无效的 Base64 字符串。");

            var imageType = DetectImageMimeType(trimmed);
            if (string.IsNullOrEmpty(imageType))
                return ToolResult<DataUriImageParseResult>.Fail("无法识别图片类型。");

            return ToolResult<DataUriImageParseResult>.Ok(new DataUriImageParseResult(
                $"data:{imageType};base64,{trimmed}",
                null,
                imageType,
                false));
        }
        catch (FormatException)
        {
            return ToolResult<DataUriImageParseResult>.Fail("无效的 Base64 字符串。");
        }
    }

    /// <summary>
    /// 执行 DetectImageMimeType 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static string DetectImageMimeType(string base64)
    {
        if (base64.StartsWith("iVBORw0KGgo")) return "image/png";
        if (base64.StartsWith("/9j/")) return "image/jpeg";
        if (base64.StartsWith("R0lGODlh")) return "image/gif";
        if (base64.StartsWith("PHN2Zy")) return "image/svg+xml";
        if (base64.StartsWith("Qk0")) return "image/bmp";
        if (base64.StartsWith("UklGR")) return "image/webp";
        return string.Empty;
    }

    /// <summary>
    /// 执行 IsValidBase64 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static bool IsValidBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var buffer = new Span<byte>(new byte[value.Length]);
        return Convert.TryFromBase64String(value, buffer, out _);
    }

    /// <summary>
    /// 执行 TryParseDataUrl 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static bool TryParseDataUrl(
        string input,
        out string dataUrl,
        out string base64Part,
        out string mimeType)
    {
        dataUrl = string.Empty;
        base64Part = string.Empty;
        mimeType = string.Empty;

        if (!input.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return false;

        var commaIndex = input.IndexOf(',');
        if (commaIndex <= 5)
            return false;

        var metadata = input.Substring(5, commaIndex - 5);
        if (!metadata.Contains(";base64", StringComparison.OrdinalIgnoreCase))
            return false;

        mimeType = metadata.Split(';')[0];
        if (string.IsNullOrWhiteSpace(mimeType))
            mimeType = "image/*";

        base64Part = input[(commaIndex + 1)..];
        dataUrl = $"data:{mimeType};base64,{base64Part}";
        return true;
    }
}
