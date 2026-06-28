using Blazing.Mvvm.ComponentModel;
using System.Globalization;

namespace ToolBox.Components.Pages.Converters;

public partial class ColorConverterVM : ViewModelBase
{
    private string _hexDigits = "FFFFFF";
    private int _r = 255;
    private int _g = 255;
    private int _b = 255;
    private int _a = 255;
    private double _h;
    private double _s;
    private double _l = 100;

    private bool _isUpdating;

    /// <summary>HEX 本体（不含 #），6 位 RGB 或 8 位 RGBA。</summary>
    public string HexDigits
    {
        get => _hexDigits;
        set
        {
            if (SetProperty(ref _hexDigits, NormalizeHexDigits(value)))
                UpdateFromHex();
        }
    }

    public int R
    {
        get => _r;
        set
        {
            if (SetProperty(ref _r, ClampByte(value)))
                UpdateFromRgba();
        }
    }

    public int G
    {
        get => _g;
        set
        {
            if (SetProperty(ref _g, ClampByte(value)))
                UpdateFromRgba();
        }
    }

    public int B
    {
        get => _b;
        set
        {
            if (SetProperty(ref _b, ClampByte(value)))
                UpdateFromRgba();
        }
    }

    public int A
    {
        get => _a;
        set
        {
            if (SetProperty(ref _a, ClampByte(value)))
            {
                UpdateFromRgba();
                OnPropertyChanged(nameof(AlphaPercent));
            }
        }
    }

    public int AlphaPercent
    {
        get => (int)Math.Round(_a * 100.0 / 255);
        set => A = (int)Math.Round(ClampByte(value) * 255.0 / 100);
    }

    public double H => _h;
    public double S => _s;
    public double L => _l;

    public string HexDisplay => _a >= 255 ? $"#{_hexDigits[..6]}" : $"#{_hexDigits}";

    public string RgbaCss =>
        $"rgba({_r}, {_g}, {_b}, {(_a / 255.0).ToString("0.##", CultureInfo.InvariantCulture)})";

    public string PreviewCssColor => RgbaCss;

    private void UpdateFromHex()
    {
        if (_isUpdating)
            return;

        _isUpdating = true;
        try
        {
            var hex = _hexDigits;
            if (hex.Length is not (6 or 8))
                return;

            _r = ParseHexByte(hex, 0);
            _g = ParseHexByte(hex, 2);
            _b = ParseHexByte(hex, 4);
            _a = hex.Length == 8 ? ParseHexByte(hex, 6) : 255;

            NotifyRgbChanged();
            UpdateHsl();
            SyncHexDigitsFromRgba();
        }
        catch
        {
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateFromRgba()
    {
        if (_isUpdating)
            return;

        _isUpdating = true;
        try
        {
            SyncHexDigitsFromRgba();
            UpdateHsl();
            OnPropertyChanged(nameof(HexDisplay));
            OnPropertyChanged(nameof(RgbaCss));
            OnPropertyChanged(nameof(PreviewCssColor));
            OnPropertyChanged(nameof(AlphaPercent));
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SyncHexDigitsFromRgba()
    {
        _hexDigits = _a >= 255
            ? $"{_r:X2}{_g:X2}{_b:X2}"
            : $"{_r:X2}{_g:X2}{_b:X2}{_a:X2}";

        OnPropertyChanged(nameof(HexDigits));
        OnPropertyChanged(nameof(HexDisplay));
        OnPropertyChanged(nameof(RgbaCss));
        OnPropertyChanged(nameof(PreviewCssColor));
    }

    private void NotifyRgbChanged()
    {
        OnPropertyChanged(nameof(R));
        OnPropertyChanged(nameof(G));
        OnPropertyChanged(nameof(B));
        OnPropertyChanged(nameof(A));
        OnPropertyChanged(nameof(AlphaPercent));
        OnPropertyChanged(nameof(HexDisplay));
        OnPropertyChanged(nameof(RgbaCss));
        OnPropertyChanged(nameof(PreviewCssColor));
    }

    private void UpdateHsl()
    {
        var r = _r / 255.0;
        var g = _g / 255.0;
        var b = _b / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));

        _l = (max + min) / 2.0;

        if (max == min)
        {
            _h = _s = 0;
        }
        else
        {
            var d = max - min;
            _s = _l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

            if (max == r)
                _h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g)
                _h = (b - r) / d + 2;
            else
                _h = (r - g) / d + 4;

            _h /= 6;
        }

        _h *= 360;
        _s *= 100;
        _l *= 100;

        OnPropertyChanged(nameof(H));
        OnPropertyChanged(nameof(S));
        OnPropertyChanged(nameof(L));
    }

    private static int ParseHexByte(string hex, int start) =>
        int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static int ClampByte(int value) => Math.Clamp(value, 0, 255);

    private static string NormalizeHexDigits(string? value)
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
}
