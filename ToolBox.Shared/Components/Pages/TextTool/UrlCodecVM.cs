using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Encoding;

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
        var result = UrlCodecService.Encode(PlainText, UseEscapeDataString);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        EncodedText = result.Value!;
    }

    public void Decode()
    {
        ErrorMessage = null;
        var result = UrlCodecService.Decode(EncodedText, UseEscapeDataString);
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
        EncodedText = string.Empty;
        ErrorMessage = null;
    }
}
