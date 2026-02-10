using Blazing.Mvvm.ComponentModel;
using System.Net;

namespace ToolBox.Components.Pages.TextTool;

public sealed class UrlCodecVM : ViewModelBase
{
    private string _plainText = string.Empty;
    private string _encodedText = string.Empty;
    private string? _errorMessage;
    private bool _useEscapeDataString;

    public string PlainText
    {
        get => _plainText;
        set => SetProperty(ref _plainText, value);
    }

    public string EncodedText
    {
        get => _encodedText;
        set => SetProperty(ref _encodedText, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool UseEscapeDataString
    {
        get => _useEscapeDataString;
        set => SetProperty(ref _useEscapeDataString, value);
    }

    public void Encode()
    {
        ErrorMessage = null;
        try
        {
            var input = PlainText ?? string.Empty;
            EncodedText = UseEscapeDataString
                ? Uri.EscapeDataString(input)
                : WebUtility.UrlEncode(input);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void Decode()
    {
        ErrorMessage = null;
        try
        {
            var input = EncodedText ?? string.Empty;
            PlainText = UseEscapeDataString
                ? Uri.UnescapeDataString(input)
                : WebUtility.UrlDecode(input);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void Clear()
    {
        PlainText = string.Empty;
        EncodedText = string.Empty;
        ErrorMessage = null;
    }
}
