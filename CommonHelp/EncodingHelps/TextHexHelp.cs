using System.Globalization;
using System.Text;

namespace CommonHelp;

/// <summary>
/// 文本转 Hex 结果：空格分隔的 Hex 串与按展示模式组装的完整输出。
/// </summary>
public sealed record TextHexEncodeResult(string HexInput, string Output);

/// <summary>
/// Hex 转文本结果：解码文本（两个字段内容一致，便于双向绑定）。
/// </summary>
public sealed record TextHexDecodeResult(string TextInput, string Output);

/// <summary>
/// 文本与十六进制互转工具：支持多种编码（见 <see cref="EncodingHelp.TextHexEncodings"/>）与多种 Hex 展示格式（空格分隔、连续、C 转义、Unicode 转义）。
/// </summary>
public static class TextHexHelp
{
    /// <summary>
    /// 将文本按指定编码转为字节，输出空格分隔 Hex 及附加展示块（连续 Hex、\xNN C 转义、\uXXXX Unicode 转义按需叠加）。
    /// </summary>
    public static ToolResult<TextHexEncodeResult> EncodeTextToHex(
        string? textInput,
        string encodingId,
        string displayModeId,
        bool uppercaseHex,
        bool showUnicodeEscape,
        bool showCEscape)
    {
        try
        {
            var encoding = EncodingHelp.GetEncoding(encodingId);
            var bytes = encoding.GetBytes(textInput ?? string.Empty);
            var hexInput = EncodingHelp.FormatHexBytes(bytes, uppercaseHex);
            var output = BuildDisplay(bytes, encoding, displayModeId, uppercaseHex, showUnicodeEscape, showCEscape);
            return ToolResult<TextHexEncodeResult>.Ok(new TextHexEncodeResult(hexInput, output));
        }
        catch (Exception ex)
        {
            return ToolResult<TextHexEncodeResult>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 将十六进制字符串（容忍 0x/\x 前缀、空格与 -、, 分隔符）按指定编码解码为文本。
    /// </summary>
    public static ToolResult<TextHexDecodeResult> DecodeHexToText(string? hexInput, string encodingId)
    {
        try
        {
            var bytes = EncodingHelp.ParseHexBytes(hexInput ?? string.Empty);
            var encoding = EncodingHelp.GetEncoding(encodingId);
            var textInput = encoding.GetString(bytes);
            return ToolResult<TextHexDecodeResult>.Ok(new TextHexDecodeResult(textInput, textInput));
        }
        catch (Exception ex)
        {
            return ToolResult<TextHexDecodeResult>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 按展示模式组装主输出与附加输出块，块之间以空行分隔。
    /// </summary>
    private static string BuildDisplay(
        byte[] bytes,
        System.Text.Encoding encoding,
        string displayModeId,
        bool uppercaseHex,
        bool showUnicodeEscape,
        bool showCEscape)
    {
        var parts = new List<string>();
        var primary = displayModeId switch
        {
            "continuous" => string.Concat(bytes.Select(b => b.ToString(uppercaseHex ? "X2" : "x2", CultureInfo.InvariantCulture))),
            "c-escape" => string.Concat(bytes.Select(b => $"\\x{b.ToString(uppercaseHex ? "X2" : "x2", CultureInfo.InvariantCulture)}")),
            "unicode" => BuildUnicodeEscape(bytes, encoding),
            _ => EncodingHelp.FormatHexBytes(bytes, uppercaseHex),
        };
        parts.Add(primary);

        if (showCEscape && displayModeId != "c-escape")
            parts.Add(string.Concat(bytes.Select(b => $"\\x{b.ToString(uppercaseHex ? "X2" : "x2", CultureInfo.InvariantCulture)}")));

        if (showUnicodeEscape && displayModeId != "unicode")
            parts.Add(BuildUnicodeEscape(bytes, encoding));

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    /// <summary>
    /// 把解码文本中的非 ASCII 与控制字符转为 \uXXXX 转义表示。
    /// </summary>
    private static string BuildUnicodeEscape(byte[] bytes, System.Text.Encoding encoding)
    {
        var text = encoding.GetString(bytes);
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (ch <= 0x7F && !char.IsControl(ch))
                sb.Append(ch);
            else
                sb.Append(CultureInfo.InvariantCulture, $"\\u{((int)ch):X4}");
        }

        return sb.ToString();
    }
}
