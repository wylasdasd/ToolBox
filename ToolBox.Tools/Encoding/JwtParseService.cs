using System.Text;
using System.Text.Json;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Encoding;

public static class JwtParseService
{
    public static ToolResult<JwtParseResult> Parse(string? token)
    {
        var trimmed = (token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return ToolResult<JwtParseResult>.Fail("请输入 JWT。");

        var parts = trimmed.Split('.');
        if (parts.Length < 2)
            return ToolResult<JwtParseResult>.Fail("JWT 格式不正确，需要至少包含 Header 和 Payload。");

        try
        {
            var headerJson = DecodePart(parts[0]);
            var payloadJson = DecodePart(parts[1]);

            var expiryUtc = string.Empty;
            var expiryLocal = string.Empty;
            if (TryReadExpiry(payloadJson, out var expiry))
            {
                expiryUtc = expiry.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
                expiryLocal = expiry.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            return ToolResult<JwtParseResult>.Ok(new JwtParseResult(
                headerJson,
                payloadJson,
                expiryUtc,
                expiryLocal));
        }
        catch (Exception ex)
        {
            return ToolResult<JwtParseResult>.Fail(ex.Message);
        }
    }

    private static string DecodePart(string base64Url)
    {
        var jsonBytes = Convert.FromBase64String(FixBase64Padding(base64Url));
        var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string FixBase64Padding(string base64Url)
    {
        var restored = base64Url.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (restored.Length % 4);
        if (padding is > 0 and < 4)
            restored = restored.PadRight(restored.Length + padding, '=');

        return restored;
    }

    private static bool TryReadExpiry(string payloadJson, out DateTimeOffset expiry)
    {
        expiry = default;
        if (string.IsNullOrWhiteSpace(payloadJson))
            return false;

        using var doc = JsonDocument.Parse(payloadJson);
        if (!doc.RootElement.TryGetProperty("exp", out var expElement))
            return false;

        if (expElement.ValueKind == JsonValueKind.Number && expElement.TryGetInt64(out var seconds))
        {
            expiry = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }

        if (expElement.ValueKind == JsonValueKind.String && long.TryParse(expElement.GetString(), out seconds))
        {
            expiry = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }

        return false;
    }
}

