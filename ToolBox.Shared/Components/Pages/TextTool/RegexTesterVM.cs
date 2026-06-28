using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Text;

namespace ToolBox.Components.Pages;

public sealed class RegexTesterVM : ViewModelBase
{
    private string _pattern = @"\b\w+\b";
    private string _inputText = "Type or paste text here.\r\nMatches will be highlighted in real time.";
    private string _replacementText = string.Empty;
    private string _replacementResult = string.Empty;
    private bool _ignoreCase = true;
    private bool _multiline;
    private bool _singleline;
    private bool _matchesOnly;
    private bool _showGroups;
    private string? _errorMessage;
    private int _matchCount;
    private IReadOnlyList<RegexTextSegment> _segments = Array.Empty<RegexTextSegment>();
    private IReadOnlyList<RegexGroupMatch> _groupMatches = Array.Empty<RegexGroupMatch>();
    private string _optionsSummary = "(?i)";

    public string Pattern
    {
        get => _pattern;
        set
        {
            if (SetProperty(ref _pattern, value))
                UpdateMatches();
        }
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
                UpdateMatches();
        }
    }

    public string ReplacementText
    {
        get => _replacementText;
        set
        {
            if (SetProperty(ref _replacementText, value))
                UpdateMatches();
        }
    }

    public string ReplacementResult
    {
        get => _replacementResult;
        private set => SetProperty(ref _replacementResult, value);
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set
        {
            if (SetProperty(ref _ignoreCase, value))
                UpdateMatches();
        }
    }

    public bool Multiline
    {
        get => _multiline;
        set
        {
            if (SetProperty(ref _multiline, value))
                UpdateMatches();
        }
    }

    public bool Singleline
    {
        get => _singleline;
        set
        {
            if (SetProperty(ref _singleline, value))
                UpdateMatches();
        }
    }

    public bool MatchesOnly
    {
        get => _matchesOnly;
        set
        {
            if (SetProperty(ref _matchesOnly, value))
                UpdateMatches();
        }
    }

    public bool ShowGroups
    {
        get => _showGroups;
        set => SetProperty(ref _showGroups, value);
    }

    public string OptionsSummary
    {
        get => _optionsSummary;
        private set => SetProperty(ref _optionsSummary, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int MatchCount
    {
        get => _matchCount;
        private set => SetProperty(ref _matchCount, value);
    }

    public IReadOnlyList<RegexTextSegment> Segments
    {
        get => _segments;
        private set => SetProperty(ref _segments, value);
    }

    public IReadOnlyList<RegexGroupMatch> GroupMatches
    {
        get => _groupMatches;
        private set => SetProperty(ref _groupMatches, value);
    }

    public void ApplyReplacement()
    {
        if (!string.IsNullOrEmpty(ReplacementResult))
            InputText = ReplacementResult;
    }

    public override Task OnInitializedAsync()
    {
        UpdateMatches();
        return Task.CompletedTask;
    }

    private void UpdateMatches()
    {
        var options = new RegexMatchOptions(IgnoreCase, Multiline, Singleline, MatchesOnly);
        var result = RegexMatchService.Match(Pattern, InputText, ReplacementText, options);

        if (!result.Success)
        {
            ErrorMessage = result.Error;
            MatchCount = 0;
            OptionsSummary = "(?i)";
            Segments = BuildFallbackSegments(InputText ?? string.Empty);
            GroupMatches = Array.Empty<RegexGroupMatch>();
            ReplacementResult = string.Empty;
            return;
        }

        var value = result.Value!;
        ErrorMessage = null;
        MatchCount = value.MatchCount;
        OptionsSummary = value.OptionsSummary;
        Segments = value.Segments;
        GroupMatches = value.GroupMatches;
        ReplacementResult = value.ReplacementResult;
    }

    private static IReadOnlyList<RegexTextSegment> BuildFallbackSegments(string text) =>
        string.IsNullOrEmpty(text)
            ? Array.Empty<RegexTextSegment>()
            : new[] { new RegexTextSegment(text, false) };
}
