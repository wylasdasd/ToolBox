using Blazing.Mvvm.ComponentModel;
using System.Text;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ToolBox.Components.Pages.Network;

public sealed class RequestToCurlVM : ViewModelBase
{
    private string _rawRequest = string.Empty;
    private string _curlCommand = string.Empty;
    private string? _errorMessage;
    private string _formMethod = "GET";
    private string _formUrl = string.Empty;
    private string _formHeaders = string.Empty;
    private string _formBody = string.Empty;
    private string _outputFormat = "curl";
    private bool _useSystemCurl;

    private HttpRequestData? _currentRequestData;
    private bool _isLoading;
    private string? _responseStatusCode;
    private string? _responseHeaders;
    private string? _responseBody;

    public IReadOnlyList<string> HttpMethods { get; } =
    [
        "GET",
        "POST",
        "PUT",
        "PATCH",
        "DELETE",
        "HEAD",
        "OPTIONS"
    ];

    public IReadOnlyList<OutputFormatOption> OutputFormats { get; } =
    [
        new OutputFormatOption("curl", "cURL（通用）"),
        new OutputFormatOption("powershell", "PowerShell（Invoke-WebRequest）")
    ];

    public string RawRequest
    {
        get => _rawRequest;
        set => SetProperty(ref _rawRequest, value);
    }

    public string CurlCommand
    {
        get => _curlCommand;
        private set => SetProperty(ref _curlCommand, value);
    }
    private bool _IsHttps;
    public bool IsHttps
    {
        get => _IsHttps;
        set => SetProperty(ref _IsHttps, value);
    }

    public string FormMethod
    {
        get => _formMethod;
        set => SetProperty(ref _formMethod, value);
    }

    public string FormUrl
    {
        get => _formUrl;
        set => SetProperty(ref _formUrl, value);
    }

    public string FormHeaders
    {
        get => _formHeaders;
        set => SetProperty(ref _formHeaders, value);
    }

    public string FormBody
    {
        get => _formBody;
        set => SetProperty(ref _formBody, value);
    }

    public string OutputFormat
    {
        get => _outputFormat;
        set => SetProperty(ref _outputFormat, value);
    }

    public bool UseSystemCurl
    {
        get => _useSystemCurl;
        set => SetProperty(ref _useSystemCurl, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? ResponseStatusCode
    {
        get => _responseStatusCode;
        private set => SetProperty(ref _responseStatusCode, value);
    }

    public string? ResponseHeaders
    {
        get => _responseHeaders;
        private set => SetProperty(ref _responseHeaders, value);
    }

    public string? ResponseBody
    {
        get => _responseBody;
        private set => SetProperty(ref _responseBody, value);
    }

    public void ConvertRaw()
    {
        ErrorMessage = null;
        try
        {
            _currentRequestData = BuildFromRaw(RawRequest);
            if (_currentRequestData == null)
            {
                ErrorMessage = "未能解析请求。";
                CurlCommand = string.Empty;
                return;
            }

            CurlCommand = OutputFormat == "powershell"
                ? GeneratePowerShell(_currentRequestData)
                : GenerateCurl(_currentRequestData);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            CurlCommand = string.Empty;
            _currentRequestData = null;
        }
    }

    public void ConvertForm()
    {
        ErrorMessage = null;
        try
        {
            if (!FormUrl.Contains("http"))
            {
                if(_IsHttps)
                    FormUrl = $"https://{FormUrl}";
                else
                    FormUrl = $"http://{FormUrl}";
            }
            _currentRequestData = BuildFromForm(FormMethod, FormUrl, FormHeaders, FormBody);
            if (_currentRequestData == null)
            {
                ErrorMessage = "未能解析请求。";
                CurlCommand = string.Empty;
                return;
            }

            CurlCommand = OutputFormat == "powershell"
                ? GeneratePowerShell(_currentRequestData)
                : GenerateCurl(_currentRequestData);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            CurlCommand = string.Empty;
            _currentRequestData = null;
        }
    }

    public bool IsSystemCurlSupported => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

    public async Task ExecuteRequest()
    {
        if (_currentRequestData == null && string.IsNullOrWhiteSpace(CurlCommand))
        {
            ErrorMessage = "请先转换请求。";
            return;
        }

        IsLoading = true;
        ResponseStatusCode = null;
        ResponseHeaders = null;
        ResponseBody = null;
        ErrorMessage = null;

        try
        {
            if (UseSystemCurl)
            {
                if (IsSystemCurlSupported)
                {
                    await ExecuteWithCurl();
                }
                else
                {
                    ErrorMessage = "当前平台不支持系统 cURL 执行。";
                }
            }
            else
            {
                await ExecuteWithHttpClient();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"请求执行失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteWithCurl()
    {
        if (string.IsNullOrWhiteSpace(CurlCommand))
        {
            ErrorMessage = "没有可执行的 cURL 命令。";
            return;
        }

        if (OutputFormat == "powershell")
        {
            ErrorMessage = "系统执行仅支持 cURL 格式命令。请将输出格式切换为 cURL。";
            return;
        }

        var cmd = CurlCommand + " -i -s";
        ProcessStartInfo? psi = null;

        if (OperatingSystem.IsWindows())
        {
            psi = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/c {cmd}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{cmd.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
        }
        else
        {
            ErrorMessage = "当前平台不支持执行系统命令。";
            return;
        }

        if (psi == null) return;

        using var process = new Process { StartInfo = psi };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) outputBuilder.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            ErrorMessage = $"cURL 执行出错 (ExitCode {process.ExitCode}):\n{errorBuilder}";
            if (outputBuilder.Length > 0)
            {
                ResponseBody = outputBuilder.ToString();
            }
        }
        else
        {
            var fullOutput = outputBuilder.ToString();
            fullOutput = fullOutput.Replace("\r\n", "\n");

            var splitIndex = fullOutput.IndexOf("\n\n");
            if (splitIndex >= 0)
            {
                ResponseHeaders = fullOutput.Substring(0, splitIndex).Trim();
                ResponseBody = fullOutput.Substring(splitIndex + 2);

                var firstLine = ResponseHeaders.Split('\n').FirstOrDefault();
                ResponseStatusCode = firstLine ?? "Unknown";
            }
            else
            {
                ResponseHeaders = fullOutput;
                ResponseBody = string.Empty;
                var firstLine = fullOutput.Split('\n').FirstOrDefault();
                ResponseStatusCode = firstLine ?? "Unknown";
            }
        }
    }

    private async Task ExecuteWithHttpClient()
    {
        if (_currentRequestData == null)
        {
            ErrorMessage = "请先转换请求（Internal Data Missing）。";
            return;
        }

        using var client = new HttpClient();
        var request = new HttpRequestMessage(new HttpMethod(_currentRequestData.Method), _currentRequestData.Url);

        foreach (var (name, value) in _currentRequestData.Headers)
        {
            if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Handled in Content
            }

            try
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }
            catch
            {
                // Ignore invalid headers for now
            }
        }

        if (!string.IsNullOrEmpty(_currentRequestData.Body) &&
            _currentRequestData.Method != "GET" &&
            _currentRequestData.Method != "HEAD")
        {
            var content = new StringContent(_currentRequestData.Body);
            var contentType = _currentRequestData.Headers.FirstOrDefault(h => string.Equals(h.Name, "Content-Type", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(contentType.Value))
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType.Value);
            }
            request.Content = content;
        }

        var response = await client.SendAsync(request);

        ResponseStatusCode = $"{(int)response.StatusCode} {response.ReasonPhrase}";

        var sbHeaders = new StringBuilder();
        foreach (var header in response.Headers)
        {
            sbHeaders.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
        foreach (var header in response.Content.Headers)
        {
            sbHeaders.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
        ResponseHeaders = sbHeaders.ToString();

        ResponseBody = await response.Content.ReadAsStringAsync();
    }

    public void ClearRaw()
    {
        RawRequest = string.Empty;
        CurlCommand = string.Empty;
        ErrorMessage = null;
        _currentRequestData = null;
        ClearResponse();
    }

    public void ClearForm()
    {
        FormMethod = "GET";
        FormUrl = string.Empty;
        FormHeaders = string.Empty;
        FormBody = string.Empty;
        CurlCommand = string.Empty;
        ErrorMessage = null;
        _currentRequestData = null;
        ClearResponse();
    }

    private void ClearResponse()
    {
        ResponseStatusCode = null;
        ResponseHeaders = null;
        ResponseBody = null;
    }

    private HttpRequestData? BuildFromRaw(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var lines = NormalizeLines(raw);
        if (lines.Count == 0)
        {
            return null;
        }

        var requestLine = lines[0].Trim();
        var requestParts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestParts.Length < 2)
        {
            return null;
        }

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
                {
                    continue;
                }

                var name = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim();
                headers.Add((name, value));
            }
            else
            {
                if (bodyBuilder.Length > 0)
                {
                    bodyBuilder.Append('\n');
                }
                bodyBuilder.Append(line);
            }
        }

        var url = BuildUrl(target, headers);
        return new HttpRequestData(method, url, headers, bodyBuilder.ToString());
    }

    private HttpRequestData? BuildFromForm(string method, string url, string headersText, string body)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var normalizedMethod = string.IsNullOrWhiteSpace(method) ? "GET" : method.Trim().ToUpperInvariant();
        var headers = ParseHeaders(headersText);
        var normalizedUrl = url.Trim();

        return new HttpRequestData(normalizedMethod, normalizedUrl, headers, body);
    }

    private string GenerateCurl(HttpRequestData data)
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
            {
                continue;
            }

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

    private string GeneratePowerShell(HttpRequestData data)
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
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append("; ");
                }

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

    private string BuildUrl(string target, List<(string Name, string Value)> headers)
    {
        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return target;
        }

        var host = headers.FirstOrDefault(h => h.Name.Equals("Host", StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrWhiteSpace(host))
        {
            return target;
        }
        if (_IsHttps)
        {
            return $"https://{host}{target}";

        }

        return $"http://{host}{target}";

    }

    private List<string> NormalizeLines(string raw)
    {
        var normalized = raw.Replace("\r\n", "\n").Replace('\r', '\n');
        return normalized.Split('\n', StringSplitOptions.None).ToList();
    }

    private List<(string Name, string Value)> ParseHeaders(string headersText)
    {
        var lines = NormalizeLines(headersText);
        var headers = new List<(string Name, string Value)>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            var name = line[..colonIndex].Trim();
            var value = line[(colonIndex + 1)..].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            headers.Add((name, value));
        }

        return headers;
    }

    private string EscapeArg(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private string EscapePowerShellArg(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "''";
        }

        return "'" + value.Replace("'", "''") + "'";
    }

    private string EscapePowerShellKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "''";
        }

        return "'" + value.Replace("'", "''") + "'";
    }

    public sealed record OutputFormatOption(string Key, string Label);

    private sealed record HttpRequestData(string Method, string Url, List<(string Name, string Value)> Headers, string Body);
}
