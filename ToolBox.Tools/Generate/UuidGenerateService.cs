using System.Text;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Generate;

public static class UuidGenerateService
{
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
