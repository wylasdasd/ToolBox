using System.Globalization;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Calculation;

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
    public string HexDisplay => A >= 255 ? $"#{HexDigits[..6]}" : $"#{HexDigits}";
    public string RgbaCss =>
        $"rgba({R}, {G}, {B}, {(A / 255.0).ToString("0.##", CultureInfo.InvariantCulture)})";
    public int AlphaPercent => (int)Math.Round(A * 100.0 / 255);
}

public static class ColorConvertService
{
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

    public static ToolResult<ColorState> FromHex(string hexDigits)
    {
        try
        {
            var hex = hexDigits;
            if (hex.Length is not (6 or 8))
                return ToolResult<ColorState>.Fail("invalid length");

            var r = ParseHexByte(hex, 0);
            var g = ParseHexByte(hex, 2);
            var b = ParseHexByte(hex, 4);
            var a = hex.Length == 8 ? ParseHexByte(hex, 6) : 255;
            return ToolResult<ColorState>.Ok(BuildFromRgba(r, g, b, a));
        }
        catch
        {
            return ToolResult<ColorState>.Fail("invalid hex");
        }
    }

    public static ColorState FromRgba(int r, int g, int b, int a) =>
        BuildFromRgba(ClampByte(r), ClampByte(g), ClampByte(b), ClampByte(a));

    public static int ClampByte(int value) => Math.Clamp(value, 0, 255);

    public static int AlphaPercentToByte(int alphaPercent) =>
        (int)Math.Round(ClampByte(alphaPercent) * 255.0 / 100);

    private static ColorState BuildFromRgba(int r, int g, int b, int a)
    {
        var hexDigits = a >= 255
            ? $"{r:X2}{g:X2}{b:X2}"
            : $"{r:X2}{g:X2}{b:X2}{a:X2}";

        ComputeHsl(r, g, b, out var h, out var s, out var l);
        return new ColorState(hexDigits, r, g, b, a, h, s, l);
    }

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

    private static int ParseHexByte(string hex, int start) =>
        int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
