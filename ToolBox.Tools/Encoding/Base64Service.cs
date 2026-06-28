using System.Text;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Encoding;

public static class Base64Service
{
    public static ToolResult<string> Encode(string? plainText, bool useUrlSafe)
    {
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(plainText ?? string.Empty);
            var base64 = Convert.ToBase64String(bytes);
            var result = useUrlSafe ? ToUrlSafe(base64) : base64;
            return ToolResult<string>.Ok(result);
        }
        catch (Exception ex)
        {
            return ToolResult<string>.Fail(ex.Message);
        }
    }

    public static ToolResult<string> Decode(string? base64Text, bool useUrlSafe)
    {
        try
        {
            var input = base64Text ?? string.Empty;
            if (useUrlSafe)
                input = FromUrlSafe(input);

            var bytes = Convert.FromBase64String(input);
            return ToolResult<string>.Ok(System.Text.Encoding.UTF8.GetString(bytes));
        }
        catch (FormatException ex)
        {
            return ToolResult<string>.Fail(ex.Message);
        }
    }

    private static string ToUrlSafe(string base64) =>
        base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string FromUrlSafe(string base64)
    {
        var restored = base64.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (restored.Length % 4);
        if (padding is > 0 and < 4)
            restored = restored.PadRight(restored.Length + padding, '=');

        return restored;
    }
}

