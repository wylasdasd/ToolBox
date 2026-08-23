using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonHelp;

/// <summary>
/// 编码工具：常用文本编码表（UTF-8/GBK/Big5 等）的获取，以及十六进制字符串与字节数组的互转。
/// </summary>
public static class EncodingHelp
{
    public sealed record EncodingOption(string Id, string Label, string CodePageName);

    /// <summary>文本 ↔ Hex 等工具常用编码。</summary>
    public static IReadOnlyList<EncodingOption> TextHexEncodings { get; } =
    [
        new("utf-8", "UTF-8", "utf-8"),
        new("utf-16le", "UTF-16 LE", "utf-16"),
        new("utf-16be", "UTF-16 BE", "utf-16BE"),
        new("ascii", "ASCII", "us-ascii"),
        new("latin1", "Latin-1 (ISO-8859-1)", "iso-8859-1"),
        new("gb2312", "GB2312", "gb2312"),
        new("gbk", "GBK", "gbk"),
        new("gb18030", "GB18030", "gb18030"),
        new("big5", "Big5（繁体）", "big5"),
    ];

    static EncodingHelp()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// 按编码 Id（如 "gbk"）获取 Encoding；未知 Id 回退 UTF-8。
    /// </summary>
    public static Encoding GetEncoding(string id)
    {
        var option = TextHexEncodings.FirstOrDefault(o =>
            string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));

        return option is null
            ? Encoding.UTF8
            : Encoding.GetEncoding(option.CodePageName);
    }

    /// <summary>
    /// 将十六进制字符串解析为字节数组；容忍 0x/\x 前缀、空格、-、, 分隔符与 \xNN C 转义序列。
    /// expectedSize 大于 0 时校验字节数。
    /// </summary>
    public static byte[] ParseHexBytes(string input, int expectedSize = 0)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException("请输入 Hex 内容。");

        byte[] bytes;
        var matches = Regex.Matches(input, @"\\x([0-9a-fA-F]{2})");
        if (matches.Count > 0)
        {
            bytes = matches.Select(m => byte.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
        }
        else
        {
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

            bytes = new byte[cleaned.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = byte.Parse(cleaned.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        if (expectedSize > 0 && bytes.Length != expectedSize)
            throw new FormatException($"期望 {expectedSize} 字节，实际 {bytes.Length} 字节。");

        return bytes;
    }

    /// <summary>
    /// 将字节数组格式化为十六进制字符串，字节间以空格分隔。
    /// </summary>
    public static string FormatHexBytes(byte[] bytes, bool uppercaseHex = true)
    {
        var fmt = uppercaseHex ? "X2" : "x2";
        return string.Join(" ", bytes.Select(b => b.ToString(fmt, CultureInfo.InvariantCulture)));
    }
}
