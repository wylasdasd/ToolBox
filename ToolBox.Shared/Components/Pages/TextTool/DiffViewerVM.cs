using Blazing.Mvvm.ComponentModel;

namespace ToolBox.Components.Pages;

public sealed class DiffViewerVM : ViewModelBase
{
    private string _text1 = "测试1";
    private string _text2 = "测试2";
    private bool _ignoreWhitespace;
    private bool _ignoreCase;
    private bool _collapseContent;

    public string Text1
    {
        get => _text1;
        set => SetProperty(ref _text1, value);
    }

    public string Text2
    {
        get => _text2;
        set => SetProperty(ref _text2, value);
    }

    public bool IgnoreWhitespace
    {
        get => _ignoreWhitespace;
        set => SetProperty(ref _ignoreWhitespace, value);
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set => SetProperty(ref _ignoreCase, value);
    }

    public bool CollapseContent
    {
        get => _collapseContent;
        set => SetProperty(ref _collapseContent, value);
    }
}