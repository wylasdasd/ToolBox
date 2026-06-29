using ToolBox.Tools.Common;

namespace ToolBox.Tools.Text;

public sealed record SvgPreviewValidateResult(string NormalizedSvg);

public static class SvgPreviewService
{
    public static ToolResult<SvgPreviewValidateResult> Validate(string? input)
    {
        var trimmed = input?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return ToolResult<SvgPreviewValidateResult>.Fail("SVG 内容不能为空。");

        if (!trimmed.Contains("<svg", StringComparison.OrdinalIgnoreCase) ||
            !trimmed.Contains("</svg>", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult<SvgPreviewValidateResult>.Fail("请输入完整的 SVG 标签内容。");
        }

        return ToolResult<SvgPreviewValidateResult>.Ok(new SvgPreviewValidateResult(trimmed));
    }
}
