using System.Text;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Network;

public static class RequestToCurlService
{
    public static ToolResult<RequestToCurlResult> ConvertFromRaw(string? raw, bool isHttps, string outputFormat)
    {
        try
        {
            var data = BuildFromRaw(raw, isHttps);
            if (data is null)
                return ToolResult<RequestToCurlResult>.Fail("未能解析请求。");

            var command = outputFormat == "powershell"
                ? GeneratePowerShell(data)
                : GenerateCurl(data);

            return ToolResult<RequestToCurlResult>.Ok(new RequestToCurlResult(command, data));
        }
        catch (Exception ex)
        {
            return ToolResult<RequestToCurlResult>.Fail(ex.Message);
        }
    }

    public static ToolResult<RequestToCurlResult> ConvertFromForm(
        string? method, string? url, string? headersText, string? body, bool isHttps, string outputFormat)
    {
        try
        {
            var data = BuildFromForm(method, url, headersText, body, isHttps);
            if (data is null)
                return ToolResult<RequestToCurlResult>.Fail("未能解析请求。");

            var command = outputFormat == "powershell"
                ? GeneratePowerShell(data)
                : GenerateCurl(data);

            return ToolResult<RequestToCurlResult>.Ok(new RequestToCurlResult(command, data));
        }
        catch (Exception ex)
        {
            return ToolResult<RequestToCurlResult>.Fail(ex.Message);
        }
    }

    private static HttpRequestData? BuildFromRaw(string? raw, bool isHttps)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var lines = NormalizeLines(raw);
        if (lines.Count == 0)
            return null;

        var requestLine = lines[0].Trim();
        var requestParts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestParts.Length < 2)
            return null;

        var method = requestParts[0].ToUpperInvariant();
        var target = requestParts[1];
        var headers = new List<(string Name, string Value)>();
        var bodyBuilder = new StringBuilder();
        var inBody = false;

        for (var i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (!inBody)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    inBody = true;
                    continue;
                }

                var colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                var name = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim();
                headers.Add((name, value));
            }
            else
            {
                if (bodyBuilder.Length > 0)
                    bodyBuilder.Append('\n');
                bodyBuilder.Append(line);
            }
        }

        var url = BuildUrl(target, headers, isHttps);
        return new HttpRequestData(method, url, headers, bodyBuilder.ToString());
    }

    private static HttpRequestData? BuildFromForm(
        string? method, string? url, string? headersText, string? body, bool isHttps)
    {
        var normalizedUrl = EnsureAbsoluteUrl(url ?? string.Empty, isHttps);
        if (string.IsNullOrWhiteSpace(normalizedUrl))
            return null;

        var normalizedMethod = string.IsNullOrWhiteSpace(method) ? "GET" : method.Trim().ToUpperInvariant();
        var headers = ParseHeaders(headersText ?? string.Empty);

        return new HttpRequestData(normalizedMethod, normalizedUrl.Trim(), headers, body ?? string.Empty);
    }

    private static string GenerateCurl(HttpRequestData data)
    {
        var sb = new StringBuilder();
        sb.Append("curl ");
        sb.Append("-X ");
        sb.Append(data.Method);
        sb.Append(' ');
        sb.Append(EscapeArg(data.Url));

        foreach (var (name, value) in data.Headers)
        {
            if (name.Equals("Host", StringComparison.OrdinalIgnoreCase))
                continue;

            sb.Append(' ');
            sb.Append("-H ");
            sb.Append(EscapeArg($"{name}: {value}"));
        }

        if (!string.IsNullOrWhiteSpace(data.Body) && data.Method != "GET" && data.Method != "HEAD")
        {
            sb.Append(' ');
            sb.Append("--data-raw ");
            sb.Append(EscapeArg(data.Body));
        }

        return sb.ToString();
    }

    private static string GeneratePowerShell(HttpRequestData data)
    {
        var sb = new StringBuilder();
        sb.Append("Invoke-WebRequest ");
        sb.Append("-Method ");
        sb.Append(data.Method);
        sb.Append(' ');
        sb.Append("-Uri ");
        sb.Append(EscapePowerShellArg(data.Url));

        if (data.Headers.Count > 0)
        {
            sb.Append(' ');
            sb.Append("-Headers ");
            sb.Append("@{");
            var first = true;
            foreach (var (name, value) in data.Headers)
            {
                if (name.Equals("Host", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!first)
                    sb.Append("; ");

                sb.Append(EscapePowerShellKey(name));
                sb.Append("=");
                sb.Append(EscapePowerShellArg(value));
                first = false;
            }
            sb.Append("}");
        }

        if (!string.IsNullOrWhiteSpace(data.Body) && data.Method != "GET" && data.Method != "HEAD")
        {
            sb.Append(' ');
            sb.Append("-Body ");
            sb.Append(EscapePowerShellArg(data.Body));
        }

        return sb.ToString();
    }

    private static string BuildUrl(string target, List<(string Name, string Value)> headers, bool isHttps)
    {
        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return target;

        var host = headers.FirstOrDefault(h => h.Name.Equals("Host", StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrWhiteSpace(host))
            return target;

        return isHttps ? $"https://{host}{target}" : $"http://{host}{target}";
    }

    private static List<string> NormalizeLines(string raw)
    {
        var normalized = raw.Replace("\r\n", "\n").Replace('\r', '\n');
        return normalized.Split('\n', StringSplitOptions.None).ToList();
    }

    private static List<(string Name, string Value)> ParseHeaders(string headersText)
    {
        var lines = NormalizeLines(headersText);
        var headers = new List<(string Name, string Value)>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            var name = line[..colonIndex].Trim();
            var value = line[(colonIndex + 1)..].Trim();
            if (name.Length == 0)
                continue;

            headers.Add((name, value));
        }

        return headers;
    }

    private static string EscapeArg(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private static string EscapePowerShellArg(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "''";

        return "'" + value.Replace("'", "''") + "'";
    }

    private static string EscapePowerShellKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "''";

        return "'" + value.Replace("'", "''") + "'";
    }

    private static string EnsureAbsoluteUrl(string url, bool https)
    {
        url = url.Trim();
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return url;

        return $"{(https ? "https" : "http")}://{url}";
    }
}
