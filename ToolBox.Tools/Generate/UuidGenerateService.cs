using System.Text;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Generate;

/// <summary>
/// UuidGenerateService：用于表示和处理相关的。
/// </summary>
public static class UuidGenerateService
{
    /// <summary>
    /// 执行 Generate 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static ToolResult<UuidGenerateResult> Generate(int count, bool includeHyphens, bool upperCase)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            var uuid = Guid.NewGuid();
            var uuidString = includeHyphens ? uuid.ToString() : uuid.ToString("N");
            if (upperCase)
                uuidString = uuidString.ToUpperInvariant();

            sb.AppendLine(uuidString);
        }

        return ToolResult<UuidGenerateResult>.Ok(new UuidGenerateResult(sb.ToString()));
    }
}
