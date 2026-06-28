using Blazing.Mvvm.ComponentModel;
using CommonHelp;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ToolBox.Components.Pages;

public sealed class TextHexVM : ViewModelBase
{
    private string _textInput = "Hello";
    private string _hexInput = string.Empty;
    private string _output = string.Empty;
    private string _encodingId = "utf-8";
    private string _displayModeId = "spaced";
    private bool _uppercaseHex = true;
    private bool _showUnicodeEscape;
    private bool _showCEscape;
    private string? _errorMessage;

    public static IReadOnlyList<EncodingHelp.EncodingOption> Encodings => EncodingHelp.TextHexEncodings;

    public sealed record DisplayMode(string Id, string Label);

    public static IReadOnlyList<DisplayMode> DisplayModes { get; } =
    [
        new("spaced", "空格分隔 Hex"),
        new("continuous", "连续 Hex"),
        new("c-escape", "\\xNN 转义"),
        new("unicode", "\\uXXXX 转义"),
    ];

    public string TextInput
    {
        get => _textInput;
        set => SetProperty(ref _textInput, value);
    }

    public string HexInput
    {
        get => _hexInput;
        set => SetProperty(ref _hexInput, value);
    }

    public string Output
    {
        get => _output;
        private set => SetProperty(ref _output, value);
    }

    public string EncodingId
    {
        get => _encodingId;
        set => SetProperty(ref _encodingId, value);
    }

    public string DisplayModeId
    {
        get => _displayModeId;
        set => SetProperty(ref _displayModeId, value);
    }

    public bool UppercaseHex
    {
        get => _uppercaseHex;
        set => SetProperty(ref _uppercaseHex, value);
    }

    public bool ShowUnicodeEscape
    {
        get => _showUnicodeEscape;
        set => SetProperty(ref _showUnicodeEscape, value);
    }

    public bool ShowCEscape
    {
        get => _showCEscape;
        set => SetProperty(ref _showCEscape, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void EncodeTextToHex()
    {
        ErrorMessage = null;
        try
        {
            var encoding = EncodingHelp.GetEncoding(EncodingId);
            var bytes = encoding.GetBytes(TextInput ?? string.Empty);
            HexInput = FormatBytes(bytes);
            Output = BuildDisplay(bytes, encoding);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void DecodeHexToText()
    {
        ErrorMessage = null;
        try
        {
            var bytes = ParseHexBytes(HexInput);
            var encoding = EncodingHelp.GetEncoding(EncodingId);
            TextInput = encoding.GetString(bytes);
            Output = TextInput;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void Clear()
    {
        TextInput = HexInput = Output = string.Empty;
        ErrorMessage = null;
    }

    private string FormatBytes(byte[] bytes)
    {
        var fmt = UppercaseHex ? "X2" : "x2";
        return string.Join(" ", bytes.Select(b => b.ToString(fmt, CultureInfo.InvariantCulture)));
    }

    private string BuildDisplay(byte[] bytes, Encoding encoding)
    {
        var parts = new List<string>();
        var primary = DisplayModeId switch
        {
            "continuous" => string.Concat(bytes.Select(b => b.ToString(UppercaseHex ? "X2" : "x2", CultureInfo.InvariantCulture))),
            "c-escape" => string.Concat(bytes.Select(b => $"\\x{b.ToString(UppercaseHex ? "X2" : "x2", CultureInfo.InvariantCulture)}")),
            "unicode" => BuildUnicodeEscape(bytes, encoding),
            _ => FormatBytes(bytes),
        };
        parts.Add(primary);

        if (ShowCEscape && DisplayModeId != "c-escape")
            parts.Add(string.Concat(bytes.Select(b => $"\\x{b.ToString(UppercaseHex ? "X2" : "x2", CultureInfo.InvariantCulture)}")));

        if (ShowUnicodeEscape && DisplayModeId != "unicode")
            parts.Add(BuildUnicodeEscape(bytes, encoding));

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    private static string BuildUnicodeEscape(byte[] bytes, Encoding encoding)
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
