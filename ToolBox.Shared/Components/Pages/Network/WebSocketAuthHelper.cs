namespace ToolBox.Components.Pages.Network;

internal static class WebSocketAuthHelper
{
    public static bool TryValidateBearer(string? authHeader, string expectedToken) =>
        authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
        && authHeader["Bearer ".Length..].Trim() == expectedToken;

    public static bool TryValidateApiKeyHeader(
        System.Collections.Specialized.NameValueCollection? headers,
        string headerName,
        string expectedToken)
    {
        var value = headers?[headerName];
        return !string.IsNullOrEmpty(value) && value == expectedToken;
    }

    public static bool TryValidateQueryToken(
        System.Collections.Specialized.NameValueCollection? query,
        string queryTokenName,
        string expectedToken)
    {
        var value = query?[queryTokenName];
        return !string.IsNullOrEmpty(value) && value == expectedToken;
    }

    public static string AppendQueryToken(string url, string queryTokenName, string token)
    {
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{Uri.EscapeDataString(queryTokenName)}={Uri.EscapeDataString(token)}";
    }
}
