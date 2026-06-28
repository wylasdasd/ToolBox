using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Diff;

namespace ToolBox.Components.Pages;

public sealed class DiffViewerVM : ViewModelBase
{
    private string _text1 = "测试1";
    private string _text2 = "测试2";
    private bool _ignoreWhitespace;
    private bool _ignoreCase;
    private bool _collapseContent;
    private int _lineAdditionCount;
    private int _lineDeletionCount;
    private int _lineModificationCount;

    public string Text1
    {
        get => _text1;
        set
        {
            if (SetProperty(ref _text1, value))
                RefreshDiffStats();
        }
    }

    public string Text2
    {
        get => _text2;
        set
        {
            if (SetProperty(ref _text2, value))
                RefreshDiffStats();
        }
    }

    public bool IgnoreWhitespace
    {
        get => _ignoreWhitespace;
        set
        {
            if (SetProperty(ref _ignoreWhitespace, value))
                RefreshDiffStats();
        }
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set
        {
            if (SetProperty(ref _ignoreCase, value))
                RefreshDiffStats();
        }
    }

    public bool CollapseContent
    {
        get => _collapseContent;
        set => SetProperty(ref _collapseContent, value);
    }

    public int LineAdditionCount
    {
        get => _lineAdditionCount;
        private set => SetProperty(ref _lineAdditionCount, value);
    }

    public int LineDeletionCount
    {
        get => _lineDeletionCount;
        private set => SetProperty(ref _lineDeletionCount, value);
    }

    public int LineModificationCount
    {
        get => _lineModificationCount;
        private set => SetProperty(ref _lineModificationCount, value);
    }

    public override Task OnInitializedAsync()
    {
        RefreshDiffStats();
        return Task.CompletedTask;
    }

    private void RefreshDiffStats()
    {
        var result = TextDiffService.Compare(Text1, Text2, new TextDiffOptions(IgnoreWhitespace, IgnoreCase));
        if (!result.Success)
        {
            LineAdditionCount = 0;
            LineDeletionCount = 0;
            LineModificationCount = 0;
            return;
        }

        LineAdditionCount = result.Value!.LineAdditionCount;
        LineDeletionCount = result.Value.LineDeletionCount;
        LineModificationCount = result.Value.LineModificationCount;
    }
}
