using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Network;

namespace ToolBox.Components.Pages.Network;

public sealed class CronParserVM : ViewModelBase
{
    private string _expression = "0 0 * * *";
    private bool _includeSeconds;
    private string _description = string.Empty;
    private string _nextRuns = string.Empty;
    private string? _errorMessage;

    public string Expression
    {
        get => _expression;
        set => SetProperty(ref _expression, value);
    }

    public bool IncludeSeconds
    {
        get => _includeSeconds;
        set => SetProperty(ref _includeSeconds, value);
    }

    public string Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

    public string NextRuns
    {
        get => _nextRuns;
        private set => SetProperty(ref _nextRuns, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void Parse()
    {
        ErrorMessage = null;
        Description = string.Empty;
        NextRuns = string.Empty;

        var result = CronParseService.Parse(Expression, IncludeSeconds);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        var value = result.Value!;
        Description = value.Description;
        NextRuns = value.NextRuns;
    }

    public void Clear()
    {
        Expression = string.Empty;
        Description = string.Empty;
        NextRuns = string.Empty;
        ErrorMessage = null;
    }
}