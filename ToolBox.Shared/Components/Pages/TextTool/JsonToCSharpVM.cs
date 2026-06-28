using ToolBox.Services;
using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Format;

namespace ToolBox.Components.Pages;

public partial class JsonToCSharpVM : ViewModelBase
{
    private readonly IClipboardService _clipboardService;

    public JsonToCSharpVM(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
    }

    private string _jsonInput = string.Empty;
    private string _cSharpOutput = string.Empty;
    private bool _isLowerCase;
    private bool _isCSharpStyle = true;
    private bool _isNullable = true;
    private string _selectedNumberType = "double";
    private string? _errorMessage;
    private bool _detectTypes = true;
    private bool _isRecordType;
    private bool _mergeArrays = true;

    public string JsonInput
    {
        get => _jsonInput;
        set => SetProperty(ref _jsonInput, value);
    }

    public string CSharpOutput
    {
        get => _cSharpOutput;
        set => SetProperty(ref _cSharpOutput, value);
    }

    public bool IsLowerCase
    {
        get => _isLowerCase;
        set
        {
            if (SetProperty(ref _isLowerCase, value) && value)
                IsCSharpStyle = false;
        }
    }

    public bool IsCSharpStyle
    {
        get => _isCSharpStyle;
        set
        {
            if (SetProperty(ref _isCSharpStyle, value) && value)
                IsLowerCase = false;
        }
    }

    public bool IsNullable
    {
        get => _isNullable;
        set => SetProperty(ref _isNullable, value);
    }

    public string SelectedNumberType
    {
        get => _selectedNumberType;
        set => SetProperty(ref _selectedNumberType, value);
    }

    public bool DetectTypes
    {
        get => _detectTypes;
        set => SetProperty(ref _detectTypes, value);
    }

    public bool IsRecordType
    {
        get => _isRecordType;
        set => SetProperty(ref _isRecordType, value);
    }

    public bool MergeArrays
    {
        get => _mergeArrays;
        set => SetProperty(ref _mergeArrays, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public void ConvertJson()
    {
        ErrorMessage = null;
        var options = new JsonToCSharpOptions(
            IsLowerCase,
            IsCSharpStyle,
            IsNullable,
            SelectedNumberType,
            DetectTypes,
            IsRecordType,
            MergeArrays);

        var result = JsonToCSharpService.Convert(JsonInput, options);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            CSharpOutput = string.Empty;
            return;
        }

        CSharpOutput = result.Value!.Output;
    }

    public async Task CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(CSharpOutput))
            await _clipboardService.SetTextAsync(CSharpOutput);
    }
}
