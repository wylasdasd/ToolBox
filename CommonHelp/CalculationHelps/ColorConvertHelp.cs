using System.Globalization;

namespace CommonHelp;

/// <summary>
/// 颜色状态：Hex 数字、RGBA 分量（0–255）与 HSL 分量（H 0–360，S/L 0–100）。
/// </summary>
public sealed record ColorState(
    string HexDigits,
    int R,
    int G,
    int B,
    int A,
    double H,
    double S,
    double L)
{
    /// <summary>带 # 前缀的展示用 Hex（Alpha 不透明时省略 Alpha 位）。</summary>
    public string HexDisplay => A >= 255 ? $"#{HexDigits[..6]}" : $"#{HexDigits}";
    /// <summary>CSS rgba() 字符串。</summary>
    public string RgbaCss =>
        $"rgba({R}, {G}, {B}, {(A / 255.0).ToString("0.##", CultureInfo.InvariantCulture)})";
    /// <summary>Alpha 的百分比表示（0–100）。</summary>
    public int AlphaPercent => (int)Math.Round(A * 100.0 / 255);
}

/// <summary>
/// 颜色转换工具：十六进制、RGBA 与 HSL 之间的互转。
/// </summary>
public static class ColorConvertHelp
{
    /// <summary>
    /// 归一化用户输入的 Hex 颜色：去 # 前缀、截断到最多 8 位、转大写。
    /// </summary>
    public static string NormalizeHexDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var hex = value.Trim().TrimStart('#');
        return hex.Length switch
        {
            <= 6 when hex.Length == 6 => hex.ToUpperInvariant(),
            >= 8 => hex[..8].ToUpperInvariant(),
            _ => hex.ToUpperInvariant()
        };
    }

    /// <summary>
    /// 解析 6 位（RGB，Alpha 视为 255）或 8 位（RGBA）Hex 为颜色状态（含 HSL 换算）。
    /// </summary>
    public static Result<ColorState> FromHex(string hexDigits)
    {
        try
        {
            var hex = hexDigits;
            if (hex.Length is not (6 or 8))
                return Result<ColorState>.Fail("invalid length");

            var r = ParseHexByte(hex, 0);
            var g = ParseHexByte(hex, 2);
            var b = ParseHexByte(hex, 4);
            var a = hex.Length == 8 ? ParseHexByte(hex, 6) : 255;
            return Result<ColorState>.Ok(BuildFromRgba(r, g, b, a));
        }
        catch
        {
            return Result<ColorState>.Fail("invalid hex");
        }
    }

    /// <summary>
    /// 由 RGBA 分量构造颜色状态；各分量自动钳制到 0–255。
    /// </summary>
    public static ColorState FromRgba(int r, int g, int b, int a) =>
        BuildFromRgba(ClampByte(r), ClampByte(g), ClampByte(b), ClampByte(a));

    /// <summary>
    /// 钳制数值到 0–255 字节范围。
    /// </summary>
    public static int ClampByte(int value) => Math.Clamp(value, 0, 255);

    /// <summary>
    /// 透明度百分比（0–100）转 Alpha 字节值（0–255）。
    /// </summary>
    public static int AlphaPercentToByte(int alphaPercent) =>
        (int)Math.Round(ClampByte(alphaPercent) * 255.0 / 100);

    /// <summary>
    /// 由 RGBA 分量构造 Hex 数字与 HSL，组装成颜色状态。
    /// </summary>
    private static ColorState BuildFromRgba(int r, int g, int b, int a)
    {
        var hexDigits = a >= 255
            ? $"{r:X2}{g:X2}{b:X2}"
            : $"{r:X2}{g:X2}{b:X2}{a:X2}";

        ComputeHsl(r, g, b, out var h, out var s, out var l);
        return new ColorState(hexDigits, r, g, b, a, h, s, l);
    }

    /// <summary>
    /// RGB 转 HSL（H 0–360，S/L 0–100）。
    /// </summary>
    private static void ComputeHsl(int r, int g, int b, out double h, out double s, out double l)
    {
        var rd = r / 255.0;
        var gd = g / 255.0;
        var bd = b / 255.0;

        var max = Math.Max(rd, Math.Max(gd, bd));
        var min = Math.Min(rd, Math.Min(gd, bd));

        l = (max + min) / 2.0;

        if (max == min)
        {
            h = s = 0;
        }
        else
        {
            var d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

            if (max == rd)
                h = (gd - bd) / d + (gd < bd ? 6 : 0);
            else if (max == gd)
                h = (bd - rd) / d + 2;
            else
                h = (rd - gd) / d + 4;

            h /= 6;
        }

        h *= 360;
        s *= 100;
        l *= 100;
    }

    /// <summary>
    /// 解析 hex 字符串中从 start 开始的两个字符为一个字节。
    /// </summary>
    private static int ParseHexByte(string hex, int start) =>
        int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
