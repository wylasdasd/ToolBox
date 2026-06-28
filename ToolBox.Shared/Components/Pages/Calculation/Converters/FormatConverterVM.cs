using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Format;

namespace ToolBox.Components.Pages.Converters;

public partial class FormatConverterVM : ViewModelBase
{
    public static IReadOnlyList<string> SupportedFormats => FormatConvertService.SupportedFormats;

    private string _input = string.Empty;
    private string _output = string.Empty;
    private string _sourceFormat = "JSON";
    private string _targetFormat = "YAML";
    private string? _errorMessage;

    public string Input
    {
        get => _input;
        set => SetProperty(ref _input, value);
    }

    public string Output
    {
        get => _output;
        set => SetProperty(ref _output, value);
    }

    public string SourceFormat
    {
        get => _sourceFormat;
        set => SetProperty(ref _sourceFormat, value);
    }

    public string TargetFormat
    {
        get => _targetFormat;
        set => SetProperty(ref _targetFormat, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public void ConvertFormat()
    {
        ErrorMessage = null;

        var result = FormatConvertService.Convert(Input, SourceFormat, TargetFormat);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        Output = result.Value!.Output;
    }
}