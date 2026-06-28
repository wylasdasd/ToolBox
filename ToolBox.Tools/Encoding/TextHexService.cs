using CommonHelp;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Encoding;

public static class TextHexService
{
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
            var hexInput = FormatBytes(bytes, uppercaseHex);
            var output = BuildDisplay(bytes, encoding, displayModeId, uppercaseHex, showUnicodeEscape, showCEscape);
            return ToolResult<TextHexEncodeResult>.Ok(new TextHexEncodeResult(hexInput, output));
        }
        catch (Exception ex)
        {
            return ToolResult<TextHexEncodeResult>.Fail(ex.Message);
        }
    }

    public static ToolResult<TextHexDecodeResult> DecodeHexToText(string? hexInput, string encodingId)
    {
        try
        {
            var bytes = ParseHexBytes(hexInput ?? string.Empty);
            var encoding = EncodingHelp.GetEncoding(encodingId);
            var textInput = encoding.GetString(bytes);
            return ToolResult<TextHexDecodeResult>.Ok(new TextHexDecodeResult(textInput, textInput));
        }
        catch (Exception ex)
        {
            return ToolResult<TextHexDecodeResult>.Fail(ex.Message);
        }
    }

    private static string FormatBytes(byte[] bytes, bool uppercaseHex)
    {
        var fmt = uppercaseHex ? "X2" : "x2";
        return string.Join(" ", bytes.Select(b => b.ToString(fmt, CultureInfo.InvariantCulture)));
    }

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
            _ => FormatBytes(bytes, uppercaseHex),
        };
        parts.Add(primary);

        if (showCEscape && displayModeId != "c-escape")
            parts.Add(string.Concat(bytes.Select(b => $"\\x{b.ToString(uppercaseHex ? "X2" : "x2", CultureInfo.InvariantCulture)}")));

        if (showUnicodeEscape && displayModeId != "unicode")
            parts.Add(BuildUnicodeEscape(bytes, encoding));

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

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

    private static byte[] ParseHexBytes(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException("请输入 Hex 内容。");

        var matches = Regex.Matches(input, @"\\x([0-9a-fA-F]{2})");
        if (matches.Count > 0)
        {
            return matches.Select(m => byte.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
        }

        var cleaned = input.Trim()
            .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
            .Replace("\\x", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(",", "")
            .Replace(Environment.NewLine, "");

        if (cleaned.Length == 0)
            throw new FormatException("未找到有效 Hex 字节。");

        if (cleaned.Length % 2 != 0)
            cleaned = "0" + cleaned;

        var bytes = new byte[cleaned.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = byte.Parse(cleaned.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return bytes;
    }
}
