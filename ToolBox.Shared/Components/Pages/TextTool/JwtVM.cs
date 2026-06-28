using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Encoding;

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

        var result = JwtParseService.Parse(Token);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        var value = result.Value!;
        HeaderJson = value.HeaderJson;
        PayloadJson = value.PayloadJson;
        ExpiryUtc = value.ExpiryUtc;
        ExpiryLocal = value.ExpiryLocal;
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
}
