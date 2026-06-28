using Blazing.Mvvm.ComponentModel;
using ToolBox.Services;
using ToolBox.Tools.Text;

namespace ToolBox.Components.Pages;

public sealed class NamingStyleVM : ViewModelBase
{
    private readonly IClipboardService _clipboardService;
    private string _input = "hello_world-example";
    private string _camelCase = string.Empty;
    private string _pascalCase = string.Empty;
    private string _snakeCase = string.Empty;
    private string _kebabCase = string.Empty;
    private string _screamingSnake = string.Empty;
    private string _dotCase = string.Empty;
    private string _titleCase = string.Empty;
    private string _lowerCase = string.Empty;
    private string? _errorMessage;

    public string Input
    {
        get => _input;
        set
        {
            if (SetProperty(ref _input, value))
                Convert();
        }
    }

    public string CamelCase
    {
        get => _camelCase;
        private set => SetProperty(ref _camelCase, value);
    }

    public string PascalCase
    {
        get => _pascalCase;
        private set => SetProperty(ref _pascalCase, value);
    }

    public string SnakeCase
    {
        get => _snakeCase;
        private set => SetProperty(ref _snakeCase, value);
    }

    public string KebabCase
    {
        get => _kebabCase;
        private set => SetProperty(ref _kebabCase, value);
    }

    public string ScreamingSnake
    {
        get => _screamingSnake;
        private set => SetProperty(ref _screamingSnake, value);
    }

    public string DotCase
    {
        get => _dotCase;
        private set => SetProperty(ref _dotCase, value);
    }

    public string TitleCase
    {
        get => _titleCase;
        private set => SetProperty(ref _titleCase, value);
    }

    public string LowerCase
    {
        get => _lowerCase;
        private set => SetProperty(ref _lowerCase, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public sealed record StyleRow(string EnglishName, string ChineseName, string Value);

    public IReadOnlyList<StyleRow> StyleRows =>
    [
        new("camelCase", "小驼峰", CamelCase),
        new("PascalCase", "大驼峰", PascalCase),
        new("snake_case", "下划线", SnakeCase),
        new("kebab-case", "短横线", KebabCase),
        new("SCREAMING_SNAKE", "全大写下划线", ScreamingSnake),
        new("dot.case", "点分隔", DotCase),
        new("Title Case", "标题格式", TitleCase),
        new("lowercase", "全小写", LowerCase),
    ];

    public NamingStyleVM(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
        Convert();
    }

    public void Convert()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Input))
        {
            CamelCase = PascalCase = SnakeCase = KebabCase = ScreamingSnake = DotCase = TitleCase = LowerCase = string.Empty;
            OnPropertyChanged(nameof(StyleRows));
            return;
        }

        var result = NamingStyleService.Convert(Input);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            CamelCase = PascalCase = SnakeCase = KebabCase = ScreamingSnake = DotCase = TitleCase = LowerCase = string.Empty;
            OnPropertyChanged(nameof(StyleRows));
            return;
        }

        var value = result.Value!;
        CamelCase = value.CamelCase;
        PascalCase = value.PascalCase;
        SnakeCase = value.SnakeCase;
        KebabCase = value.KebabCase;
        ScreamingSnake = value.ScreamingSnake;
        DotCase = value.DotCase;
        TitleCase = value.TitleCase;
        LowerCase = value.LowerCase;
        OnPropertyChanged(nameof(StyleRows));
    }

    public void Clear()
    {
        Input = string.Empty;
        ErrorMessage = null;
    }

    public async Task CopyToClipboardAsync(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        await _clipboardService.SetTextAsync(text);
    }
}