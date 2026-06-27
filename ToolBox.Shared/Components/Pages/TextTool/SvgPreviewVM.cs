using Blazing.Mvvm.ComponentModel;

namespace ToolBox.Components.Pages.TextTool;

public sealed class SvgPreviewVM : ViewModelBase
{
    private const string DefaultSvg = """
<svg width="260" height="120" viewBox="0 0 260 120" xmlns="http://www.w3.org/2000/svg">
  <rect x="10" y="10" width="240" height="100" rx="12" fill="#F6F7FB" stroke="#2F6DF6" stroke-width="2"/>
  <circle cx="52" cy="60" r="20" fill="#0E7490"/>
  <text x="88" y="67" font-size="20" fill="#0F172A">ToolBox SVG</text>
</svg>
""";

    private string _svgInput = DefaultSvg;
    private string _renderedSvg = DefaultSvg;
    private int _iconSizePercent = 100;
    private string? _errorMessage;

    public string SvgInput
    {
        get => _svgInput;
        set => SetProperty(ref _svgInput, value);
    }

    public string RenderedSvg
    {
        get => _renderedSvg;
        private set => SetProperty(ref _renderedSvg, value);
    }

    public int IconSizePercent
    {
        get => _iconSizePercent;
        set => SetProperty(ref _iconSizePercent, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void Render()
    {
        ErrorMessage = null;

        var input = SvgInput?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            ErrorMessage = "SVG 内容不能为空。";
            return;
        }

        if (!input.Contains("<svg", StringComparison.OrdinalIgnoreCase) ||
            !input.Contains("</svg>", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "请输入完整的 SVG 标签内容。";
            return;
        }

        RenderedSvg = input;
    }

    public void Clear()
    {
        SvgInput = string.Empty;
        RenderedSvg = string.Empty;
        IconSizePercent = 100;
        ErrorMessage = null;
    }
}
