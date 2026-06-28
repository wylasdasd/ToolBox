using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Format;

namespace ToolBox.Components.Pages.TextTool;

public sealed class JsonFormatVM : ViewModelBase
{
    private string _inputJson = string.Empty;
    private string _outputJson = string.Empty;
    private string? _errorMessage;
    private string? _infoMessage;
    private bool _sortKeys = true;
    private string _jsonPath = string.Empty;
    private string _jsonPathResult = string.Empty;
    private string? _jsonPathError;
    private string _selectedTemplateKey = "simple";

    public IReadOnlyList<JsonFormatService.JsonTemplateOption> TemplateOptions => JsonFormatService.TemplateOptions;

    public string InputJson
    {
        get => _inputJson;
        set => SetProperty(ref _inputJson, value);
    }

    public string OutputJson
    {
        get => _outputJson;
        private set => SetProperty(ref _outputJson, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string? InfoMessage
    {
        get => _infoMessage;
        private set => SetProperty(ref _infoMessage, value);
    }

    public bool SortKeys
    {
        get => _sortKeys;
        set => SetProperty(ref _sortKeys, value);
    }

    public string JsonPath
    {
        get => _jsonPath;
        set => SetProperty(ref _jsonPath, value);
    }

    public string JsonPathResult
    {
        get => _jsonPathResult;
        private set => SetProperty(ref _jsonPathResult, value);
    }

    public string? JsonPathError
    {
        get => _jsonPathError;
        private set => SetProperty(ref _jsonPathError, value);
    }

    public string SelectedTemplateKey
    {
        get => _selectedTemplateKey;
        set => SetProperty(ref _selectedTemplateKey, value);
    }

    public void Format() => ProcessJson(indent: true);

    public void Minify() => ProcessJson(indent: false);

    public void Validate()
    {
        ErrorMessage = null;
        InfoMessage = null;

        var result = JsonFormatService.Validate(InputJson);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        InfoMessage = result.Value!.Message;
    }

    public void Clear()
    {
        InputJson = string.Empty;
        OutputJson = string.Empty;
        ErrorMessage = null;
        InfoMessage = null;
        JsonPath = string.Empty;
        JsonPathResult = string.Empty;
        JsonPathError = null;
    }

    public void ApplyTemplate()
    {
        InputJson = JsonFormatService.GetTemplate(SelectedTemplateKey);
        OutputJson = string.Empty;
        ErrorMessage = null;
        InfoMessage = null;
        JsonPathError = null;
        JsonPathResult = string.Empty;
    }

    public void QueryJsonPath()
    {
        JsonPathError = null;
        JsonPathResult = string.Empty;
        ErrorMessage = null;
        InfoMessage = null;

        var result = JsonFormatService.QueryJsonPath(InputJson, JsonPath);
        if (!result.Success)
        {
            JsonPathError = result.Error;
            return;
        }

        JsonPathResult = result.Value!.Result;
    }

    public void ClearJsonPath()
    {
        JsonPathResult = string.Empty;
        JsonPathError = null;
    }

    private void ProcessJson(bool indent)
    {
        ErrorMessage = null;
        InfoMessage = null;

        var result = JsonFormatService.Process(InputJson, indent, SortKeys);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        OutputJson = result.Value!.Output;
    }
}
