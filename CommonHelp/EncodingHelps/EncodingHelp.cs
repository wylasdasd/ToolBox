using System.Text;

namespace CommonHelp;

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

    public static Encoding GetEncoding(string id)
    {
        var option = TextHexEncodings.FirstOrDefault(o =>
            string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));

        return option is null
            ? Encoding.UTF8
            : Encoding.GetEncoding(option.CodePageName);
    }
}
