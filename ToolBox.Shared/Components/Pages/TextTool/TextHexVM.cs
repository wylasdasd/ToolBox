using Blazing.Mvvm.ComponentModel;
using CommonHelp;
using ToolBox.Tools.Encoding;

namespace ToolBox.Components.Pages;

public sealed class TextHexVM : ViewModelBase
{
    private string _textInput = "Hello";
    private string _hexInput = string.Empty;
    private string _output = string.Empty;
    private string _encodingId = "utf-8";
    private string _displayModeId = "spaced";
    private bool _uppercaseHex = true;
    private bool _showUnicodeEscape;
    private bool _showCEscape;
    private string? _errorMessage;

    public static IReadOnlyList<EncodingHelp.EncodingOption> Encodings => EncodingHelp.TextHexEncodings;

    public sealed record DisplayMode(string Id, string Label);

    public static IReadOnlyList<DisplayMode> DisplayModes { get; } =
    [
        new("spaced", "空格分隔 Hex"),
        new("continuous", "连续 Hex"),
        new("c-escape", "\\xNN 转义"),
        new("unicode", "\\uXXXX 转义"),
    ];

    public string TextInput
    {
        get => _textInput;
        set => SetProperty(ref _textInput, value);
    }

    public string HexInput
    {
        get => _hexInput;
        set => SetProperty(ref _hexInput, value);
    }

    public string Output
    {
        get => _output;
        private set => SetProperty(ref _output, value);
    }

    public string EncodingId
    {
        get => _encodingId;
        set => SetProperty(ref _encodingId, value);
    }

    public string DisplayModeId
    {
        get => _displayModeId;
        set => SetProperty(ref _displayModeId, value);
    }

    public bool UppercaseHex
    {
        get => _uppercaseHex;
        set => SetProperty(ref _uppercaseHex, value);
    }

    public bool ShowUnicodeEscape
    {
        get => _showUnicodeEscape;
        set => SetProperty(ref _showUnicodeEscape, value);
    }

    public bool ShowCEscape
    {
        get => _showCEscape;
        set => SetProperty(ref _showCEscape, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void EncodeTextToHex()
    {
        ErrorMessage = null;
        var result = TextHexService.EncodeTextToHex(
            TextInput,
            EncodingId,
            DisplayModeId,
            UppercaseHex,
            ShowUnicodeEscape,
            ShowCEscape);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        var value = result.Value!;
        HexInput = value.HexInput;
        Output = value.Output;
    }

    public void DecodeHexToText()
    {
        ErrorMessage = null;
        var result = TextHexService.DecodeHexToText(HexInput, EncodingId);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        var value = result.Value!;
        TextInput = value.TextInput;
        Output = value.Output;
    }

    public void Clear()
    {
        TextInput = HexInput = Output = string.Empty;
        ErrorMessage = null;
    }
}
