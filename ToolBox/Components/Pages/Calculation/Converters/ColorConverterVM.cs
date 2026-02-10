using Blazing.Mvvm.ComponentModel;
using System.Globalization;

namespace ToolBox.Components.Pages.Converters;

public partial class ColorConverterVM : ViewModelBase
{
    private string _hex = "#FFFFFF";
    private int _r = 255;
    private int _g = 255;
    private int _b = 255;
    private double _h = 0;
    private double _s = 0;
    private double _l = 100;
    private double _c = 0;
    private double _m = 0;
    private double _y = 0;
    private double _k = 0;
    
    // For MudBlazor ColorPicker, usually takes MudColor or string.
    // We will sync with hex string.
    
    public string Hex
    {
        get => _hex;
        set
        {
            if (SetProperty(ref _hex, value))
            {
                UpdateFromHex();
            }
        }
    }

    public int R
    {
        get => _r;
        set
        {
            if (SetProperty(ref _r, value)) UpdateFromRgb();
        }
    }

    public int G
    {
        get => _g;
        set
        {
            if (SetProperty(ref _g, value)) UpdateFromRgb();
        }
    }

    public int B
    {
        get => _b;
        set
        {
            if (SetProperty(ref _b, value)) UpdateFromRgb();
        }
    }

    // HSL and CMYK properties... 
    // Implementing full sync logic can be verbose. 
    // Let's implement basic Hex <-> RGB first and maybe HSL.

    private bool _isUpdating;

    private void UpdateFromHex()
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            if (string.IsNullOrEmpty(_hex)) return;
            // Parse Hex
            string hex = _hex.TrimStart('#');
            if (hex.Length == 6)
            {
                _r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                _g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                _b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                OnPropertyChanged(nameof(R));
                OnPropertyChanged(nameof(G));
                OnPropertyChanged(nameof(B));
                UpdateHsl();
            }
        }
        catch { }
        finally { _isUpdating = false; }
    }

    private void UpdateFromRgb()
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            _hex = $"#{_r:X2}{_g:X2}{_b:X2}";
            OnPropertyChanged(nameof(Hex));
            UpdateHsl();
        }
        finally { _isUpdating = false; }
    }
    
    private void UpdateHsl()
    {
        // Simple RGB to HSL conversion
        double r = _r / 255.0;
        double g = _g / 255.0;
        double b = _b / 255.0;
        
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        
        _l = (max + min) / 2.0;

        if (max == min)
        {
            _h = _s = 0; // achromatic
        }
        else
        {
            double d = max - min;
            _s = _l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
            
            if (max == r) _h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) _h = (b - r) / d + 2;
            else if (max == b) _h = (r - g) / d + 4;
            
            _h /= 6;
        }

        _h *= 360;
        _s *= 100;
        _l *= 100;
        
        OnPropertyChanged(nameof(H));
        OnPropertyChanged(nameof(S));
        OnPropertyChanged(nameof(L));
    }

    public double H => _h;
    public double S => _s;
    public double L => _l;
}