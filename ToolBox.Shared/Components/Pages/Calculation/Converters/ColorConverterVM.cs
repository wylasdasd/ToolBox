using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Calculation;

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

    public string HexDigits
    {
        get => _hexDigits;
        set
        {
            if (SetProperty(ref _hexDigits, ColorConvertService.NormalizeHexDigits(value)))
                UpdateFromHex();
        }
    }

    public int R
    {
        get => _r;
        set
        {
            if (SetProperty(ref _r, ColorConvertService.ClampByte(value)))
                UpdateFromRgba();
        }
    }

    public int G
    {
        get => _g;
        set
        {
            if (SetProperty(ref _g, ColorConvertService.ClampByte(value)))
                UpdateFromRgba();
        }
    }

    public int B
    {
        get => _b;
        set
        {
            if (SetProperty(ref _b, ColorConvertService.ClampByte(value)))
                UpdateFromRgba();
        }
    }

    public int A
    {
        get => _a;
        set
        {
            if (SetProperty(ref _a, ColorConvertService.ClampByte(value)))
            {
                UpdateFromRgba();
                OnPropertyChanged(nameof(AlphaPercent));
            }
        }
    }

    public int AlphaPercent
    {
        get => (int)Math.Round(_a * 100.0 / 255);
        set => A = ColorConvertService.AlphaPercentToByte(value);
    }

    public double H => _h;
    public double S => _s;
    public double L => _l;

    public string HexDisplay => _a >= 255 ? $"#{_hexDigits[..6]}" : $"#{_hexDigits}";

    public string RgbaCss =>
        $"rgba({_r}, {_g}, {_b}, {(_a / 255.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)})";

    public string PreviewCssColor => RgbaCss;

    private void ApplyState(ColorState state)
    {
        _hexDigits = state.HexDigits;
        _r = state.R;
        _g = state.G;
        _b = state.B;
        _a = state.A;
        _h = state.H;
        _s = state.S;
        _l = state.L;
    }

    private void UpdateFromHex()
    {
        if (_isUpdating)
            return;

        _isUpdating = true;
        try
        {
            var result = ColorConvertService.FromHex(_hexDigits);
            if (!result.Success)
                return;

            ApplyState(result.Value!);
            NotifyAllColorProperties();
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
            ApplyState(ColorConvertService.FromRgba(_r, _g, _b, _a));
            NotifyAllColorProperties();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void NotifyAllColorProperties()
    {
        OnPropertyChanged(nameof(HexDigits));
        OnPropertyChanged(nameof(R));
        OnPropertyChanged(nameof(G));
        OnPropertyChanged(nameof(B));
        OnPropertyChanged(nameof(A));
        OnPropertyChanged(nameof(AlphaPercent));
        OnPropertyChanged(nameof(H));
        OnPropertyChanged(nameof(S));
        OnPropertyChanged(nameof(L));
        OnPropertyChanged(nameof(HexDisplay));
        OnPropertyChanged(nameof(RgbaCss));
        OnPropertyChanged(nameof(PreviewCssColor));
    }
}
