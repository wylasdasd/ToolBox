using CommonHelp;

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
            if (MimeHelp.TryParseDataUrl(trimmed, out var dataUrl, out var base64Part, out var mimeType))
            {
                if (!MimeHelp.IsValidBase64(base64Part))
                    return ToolResult<DataUriImageParseResult>.Fail("无效的 Base64 字符串。");

                return ToolResult<DataUriImageParseResult>.Ok(new DataUriImageParseResult(
                    dataUrl,
                    mimeType,
                    null,
                    true));
            }

            if (!MimeHelp.IsValidBase64(trimmed))
                return ToolResult<DataUriImageParseResult>.Fail("无效的 Base64 字符串。");

            var imageType = MimeHelp.DetectImageMimeType(trimmed);
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
}
