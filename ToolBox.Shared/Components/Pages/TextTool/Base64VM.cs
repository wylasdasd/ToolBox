using Blazing.Mvvm.ComponentModel;
using System.Text;
using System.Security.Cryptography;

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
        var bytes = Encoding.UTF8.GetBytes(PlainText ?? string.Empty);
        var base64 = Convert.ToBase64String(bytes);
        Base64Text = UseUrlSafe ? ToUrlSafe(base64) : base64;
    }

    public void Decode()
    {
        ErrorMessage = null;
        var input = Base64Text ?? string.Empty;

        if (UseUrlSafe)
        {
            input = FromUrlSafe(input);
        }

        try
        {
            var bytes = Convert.FromBase64String(input);
            PlainText = Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void Clear()
    {
        PlainText = string.Empty;
        Base64Text = string.Empty;
        ErrorMessage = null;
    
    }



   

    private static string ToUrlSafe(string base64)
    {
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string FromUrlSafe(string base64)
    {
        var restored = base64.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (restored.Length % 4);
        if (padding is > 0 and < 4)
        {
            restored = restored.PadRight(restored.Length + padding, '=');
        }

        return restored;
    }
}
