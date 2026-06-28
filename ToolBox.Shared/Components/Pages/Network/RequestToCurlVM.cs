using Blazing.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using ToolBox.Tools.Network;

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

    private bool _isHttps;
    public bool IsHttps
    {
        get => _isHttps;
        set => SetProperty(ref _isHttps, value);
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
        private set
        {
            if (SetProperty(ref _responseStatusCode, value))
                OnPropertyChanged(nameof(IsResponseSuccessful));
        }
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

    public void ConvertRaw() =>
        ApplyConvertResult(RequestToCurlService.ConvertFromRaw(RawRequest, IsHttps, OutputFormat));

    public void ConvertForm() =>
        ApplyConvertResult(RequestToCurlService.ConvertFromForm(FormMethod, FormUrl, FormHeaders, FormBody, IsHttps, OutputFormat));

    private void ApplyConvertResult(ToolBox.Tools.Common.ToolResult<RequestToCurlResult> result)
    {
        ErrorMessage = null;
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            CurlCommand = string.Empty;
            _currentRequestData = null;
            return;
        }

        CurlCommand = result.Value!.Command;
        _currentRequestData = result.Value.Request;
    }

    public bool IsSystemCurlSupported => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

    public bool IsResponseSuccessful => TryGetHttpStatusCode(ResponseStatusCode, out var code) && code is >= 200 and < 300;

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
                    await ExecuteWithCurl();
                else
                    ErrorMessage = "当前平台不支持系统 cURL 执行。";
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
        if (_currentRequestData == null)
        {
            ErrorMessage = "请先转换请求后再执行。";
            return;
        }

        if (OutputFormat == "powershell")
        {
            ErrorMessage = "系统执行仅支持 cURL 格式命令。请将输出格式切换为 cURL。";
            return;
        }

        using var process = new Process { StartInfo = CreateCurlProcessStartInfo() };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) outputBuilder.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
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
                ResponseBody = outputBuilder.ToString();
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

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(100) };
        var request = new HttpRequestMessage(new HttpMethod(_currentRequestData.Method), _currentRequestData.Url);

        foreach (var (name, value) in _currentRequestData.Headers)
        {
            if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
            {
                continue;
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
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType.Value);
            request.Content = content;
        }

        var response = await client.SendAsync(request);

        ResponseStatusCode = $"{(int)response.StatusCode} {response.ReasonPhrase}";

        var sbHeaders = new StringBuilder();
        foreach (var header in response.Headers)
            sbHeaders.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        foreach (var header in response.Content.Headers)
            sbHeaders.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
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

    private ProcessStartInfo CreateCurlProcessStartInfo()
    {
        var data = _currentRequestData!;
        var psi = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "curl.exe" : "curl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add("-s");
        psi.ArgumentList.Add("-X");
        psi.ArgumentList.Add(data.Method);
        psi.ArgumentList.Add(data.Url);

        foreach (var (name, value) in data.Headers)
        {
            if (name.Equals("Host", StringComparison.OrdinalIgnoreCase))
                continue;

            psi.ArgumentList.Add("-H");
            psi.ArgumentList.Add($"{name}: {value}");
        }

        if (!string.IsNullOrWhiteSpace(data.Body) && data.Method is not "GET" and not "HEAD")
        {
            psi.ArgumentList.Add("--data-binary");
            psi.ArgumentList.Add(data.Body);
        }

        return psi;
    }

    private static bool TryGetHttpStatusCode(string? statusLine, out int code)
    {
        code = 0;
        if (string.IsNullOrWhiteSpace(statusLine))
            return false;

        foreach (Match match in Regex.Matches(statusLine, @"\b(\d{3})\b"))
        {
            if (int.TryParse(match.Groups[1].Value, out code))
                return true;
        }

        return false;
    }

    public sealed record OutputFormatOption(string Key, string Label);
}
