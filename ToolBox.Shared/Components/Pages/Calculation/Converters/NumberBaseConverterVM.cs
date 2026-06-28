using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Calculation;

namespace ToolBox.Components.Pages.Converters;

public partial class NumberBaseConverterVM : ViewModelBase
{
    private readonly Dictionary<int, string> _values = RadixConvertService.ProgrammerBases.ToDictionary(b => b, _ => string.Empty);
    private string? _errorMessage;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string DigitAlphabetHint => RadixConvertService.DigitAlphabetHint;

    public string Binary
    {
        get => Get(2);
        set => Set(2, value);
    }

    public string Octal
    {
        get => Get(8);
        set => Set(8, value);
    }

    public string Decimal
    {
        get => Get(10);
        set => Set(10, value);
    }

    public string Hex
    {
        get => Get(16);
        set => Set(16, value);
    }

    public string Base32
    {
        get => Get(32);
        set => Set(32, value);
    }

    public string Base64
    {
        get => Get(64);
        set => Set(64, value);
    }

    public string Base128
    {
        get => Get(128);
        set => Set(128, value);
    }

    public IReadOnlyList<int> Bases => RadixConvertService.ProgrammerBases;

    private bool _isUpdating;

    private string Get(int radix) => _values.GetValueOrDefault(radix, string.Empty);

    private void Set(int radix, string value)
    {
        if (_isUpdating) return;
        _values[radix] = value;
        OnPropertyChanged(GetPropertyName(radix));
        ConvertFrom(value, radix);
    }

    private static string GetPropertyName(int radix) => radix switch
    {
        2 => nameof(Binary),
        8 => nameof(Octal),
        10 => nameof(Decimal),
        16 => nameof(Hex),
        32 => nameof(Base32),
        64 => nameof(Base64),
        128 => nameof(Base128),
        _ => nameof(Decimal),
    };

    private void ConvertFrom(string value, int fromBase)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        ErrorMessage = null;

        try
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ClearAll();
                return;
            }

            var result = RadixConvertService.ConvertFrom(value, fromBase);
            if (!result.Success)
            {
                ErrorMessage = result.Error;
                return;
            }

            foreach (var (radix, text) in result.Value!.ValuesByRadix)
            {
                if (radix == fromBase) continue;
                _values[radix] = text;
                OnPropertyChanged(GetPropertyName(radix));
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    public void ClearAll()
    {
        foreach (var (radix, text) in RadixConvertService.Clear().ValuesByRadix)
        {
            _values[radix] = text;
            OnPropertyChanged(GetPropertyName(radix));
        }

        ErrorMessage = null;
        _isUpdating = false;
    }
}
