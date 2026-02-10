using Blazing.Mvvm.ComponentModel;
using System.Text;
using System.Text.Json;

namespace ToolBox.Components.Pages.TextTool;

public sealed class JwtVM : ViewModelBase
{
    private string _token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxMDI0LCJ1c2VybmFtZSI6IkxhenlCdWdfQWRtaW4iLCJyb2xlIjoiYWRtaW4iLCJwZXJtaXNzaW9ucyI6WyJyZWFkIiwid3JpdGUiLCJkZWxldGUiXSwiaWF0IjoxNzM5MjcwNDAwLCJleHAiOjE3NzI3OTM2MDB9.v_8_W2V1N3Kx7Uf6p8Qk7X0O6f9g4L2m5n8p0q9r1s2";
    private string _headerJson = string.Empty;
    private string _payloadJson = string.Empty;
    private string? _errorMessage;
    private string _expiryUtc = string.Empty;
    private string _expiryLocal = string.Empty;

    public string Token
    {
        get => _token;
        set => SetProperty(ref _token, value);
    }

    public string HeaderJson
    {
        get => _headerJson;
        private set => SetProperty(ref _headerJson, value);
    }

    public string PayloadJson
    {
        get => _payloadJson;
        private set => SetProperty(ref _payloadJson, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string ExpiryUtc
    {
        get => _expiryUtc;
        private set => SetProperty(ref _expiryUtc, value);
    }

    public string ExpiryLocal
    {
        get => _expiryLocal;
        private set => SetProperty(ref _expiryLocal, value);
    }

    public void Parse()
    {
        ErrorMessage = null;
        HeaderJson = string.Empty;
        PayloadJson = string.Empty;
        ExpiryUtc = string.Empty;
        ExpiryLocal = string.Empty;

        var token = (Token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            ErrorMessage = "请输入 JWT。";
            return;
        }

        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            ErrorMessage = "JWT 格式不正确，需要至少包含 Header 和 Payload。";
            return;
        }

        try
        {
            HeaderJson = DecodePart(parts[0]);
            PayloadJson = DecodePart(parts[1]);

            if (TryReadExpiry(PayloadJson, out var expiry))
            {
                ExpiryUtc = expiry.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
                ExpiryLocal = expiry.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void Clear()
    {
        Token = string.Empty;
        HeaderJson = string.Empty;
        PayloadJson = string.Empty;
        ExpiryUtc = string.Empty;
        ExpiryLocal = string.Empty;
        ErrorMessage = null;
    }

    private static string DecodePart(string base64Url)
    {
        var jsonBytes = Convert.FromBase64String(FixBase64Padding(base64Url));
        var json = Encoding.UTF8.GetString(jsonBytes);
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string FixBase64Padding(string base64Url)
    {
        var restored = base64Url.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (restored.Length % 4);
        if (padding is > 0 and < 4)
        {
            restored = restored.PadRight(restored.Length + padding, '=');
        }

        return restored;
    }

    private static bool TryReadExpiry(string payloadJson, out DateTimeOffset expiry)
    {
        expiry = default;
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return false;
        }

        using var doc = JsonDocument.Parse(payloadJson);
        if (!doc.RootElement.TryGetProperty("exp", out var expElement))
        {
            return false;
        }

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
