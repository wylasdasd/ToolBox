using CommonHelp;

namespace ToolBox.Tools.Text;

/// <summary>
/// SvgPreviewValidateResult：用于表示和处理相关的。
/// </summary>
public sealed record SvgPreviewValidateResult(string NormalizedSvg);

/// <summary>
/// SvgPreviewService：用于表示和处理相关的。
/// </summary>
public static class SvgPreviewService
{
    /// <summary>
    /// 执行 Validate 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
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
