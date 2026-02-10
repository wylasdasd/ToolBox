using Blazing.Mvvm.ComponentModel;
using System.Text;

namespace ToolBox.Components.Pages.Converters;

public partial class NumberBaseConverterVM : ViewModelBase
{
    private string _binary = string.Empty;
    private string _octal = string.Empty;
    private string _decimal = string.Empty;
    private string _hex = string.Empty;
    private string? _errorMessage;

    public string Binary
    {
        get => _binary;
        set
        {
            if (SetProperty(ref _binary, value))
                ConvertFrom(value, 2);
        }
    }

    public string Octal
    {
        get => _octal;
        set
        {
            if (SetProperty(ref _octal, value))
                ConvertFrom(value, 8);
        }
    }

    public string Decimal
    {
        get => _decimal;
        set
        {
            if (SetProperty(ref _decimal, value))
                ConvertFrom(value, 10);
        }
    }

    public string Hex
    {
        get => _hex;
        set
        {
            if (SetProperty(ref _hex, value))
                ConvertFrom(value, 16);
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private bool _isUpdating;

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

            long longValue;
            try 
            {
                longValue = Convert.ToInt64(value, fromBase);
            }
            catch
            {
                // Handle potential overflow or format error more gracefully if needed
                // For now, just catch standard exceptions
                throw new FormatException($"Invalid number for base {fromBase}");
            }

            if (fromBase != 2) _binary = Convert.ToString(longValue, 2);
            if (fromBase != 8) _octal = Convert.ToString(longValue, 8);
            if (fromBase != 10) _decimal = Convert.ToString(longValue, 10);
            if (fromBase != 16) _hex = Convert.ToString(longValue, 16).ToUpperInvariant();
            
            OnPropertyChanged(nameof(Binary));
            OnPropertyChanged(nameof(Octal));
            OnPropertyChanged(nameof(Decimal));
            OnPropertyChanged(nameof(Hex));
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
        _binary = "";
        _octal = "";
        _decimal = "";
        _hex = "";
        OnPropertyChanged(nameof(Binary));
        OnPropertyChanged(nameof(Octal));
        OnPropertyChanged(nameof(Decimal));
        OnPropertyChanged(nameof(Hex));
        ErrorMessage = null;
        _isUpdating = false; // Reset lock just in case
    }
}
