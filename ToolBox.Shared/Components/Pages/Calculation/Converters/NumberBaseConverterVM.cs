using Blazing.Mvvm.ComponentModel;
using CommonHelp;

namespace ToolBox.Components.Pages.Converters;

public partial class NumberBaseConverterVM : ViewModelBase
{
    private readonly Dictionary<int, string> _values = RadixHelp.ProgrammerBases.ToDictionary(b => b, _ => string.Empty);
    private string? _errorMessage;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string DigitAlphabetHint =>
        $"32–128 进制数字符表（前 {Math.Min(48, RadixHelp.MaxRadix)} 个）：{RadixHelp.GetDigitAlphabet(Math.Min(48, RadixHelp.MaxRadix))}…";

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

    public IReadOnlyList<int> Bases => RadixHelp.ProgrammerBases;

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

            var big = RadixHelp.Parse(value, fromBase);

            foreach (var radix in RadixHelp.ProgrammerBases)
            {
                if (radix == fromBase) continue;
                _values[radix] = RadixHelp.Format(big, radix);
                OnPropertyChanged(GetPropertyName(radix));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    public void ClearAll()
    {
        foreach (var radix in RadixHelp.ProgrammerBases)
        {
            _values[radix] = string.Empty;
            OnPropertyChanged(GetPropertyName(radix));
        }

        ErrorMessage = null;
        _isUpdating = false;
    }
}
