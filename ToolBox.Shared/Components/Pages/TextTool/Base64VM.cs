using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Encoding;

namespace ToolBox.Components.Pages;

public sealed class Base64VM : ViewModelBase
{
    private string _plainText = string.Empty;
    private string _base64Text = string.Empty;
    private string? _errorMessage;
    private bool _useUrlSafe;


    public string PlainText
    {
        get => _plainText;
        set => SetProperty(ref _plainText, value);
    }

    public string Base64Text
    {
        get => _base64Text;
        set => SetProperty(ref _base64Text, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool UseUrlSafe
    {
        get => _useUrlSafe;
        set => SetProperty(ref _useUrlSafe, value);
    }

    public void Encode()
    {
        ErrorMessage = null;
        var result = Base64Service.Encode(PlainText, UseUrlSafe);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        Base64Text = result.Value!;
    }

    public void Decode()
    {
        ErrorMessage = null;
        var result = Base64Service.Decode(Base64Text, UseUrlSafe);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        PlainText = result.Value!;
    }

    public void Clear()
    {
        PlainText = string.Empty;
        Base64Text = string.Empty;
        ErrorMessage = null;
    }
}
