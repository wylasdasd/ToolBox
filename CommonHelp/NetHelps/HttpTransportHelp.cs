using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CommonHelp;

/// <summary>
/// HTTP JSON 传输工具：共享连接池的 <see cref="HttpClient"/> 工厂、JSON POST 请求构建、瞬时失败自动重试与响应体读取。
/// <para>适用于无 <c>IHttpClientFactory</c> 场景下的高频 API 调用（如 LLM/OpenAI 风格接口）。</para>
/// </summary>
public class HttpTransportHelp
{
    private const int MaxSendAttempts = 3;
    private readonly HttpClient httpClient;

    public HttpTransportHelp(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// 将对象序列化为 UTF-8 JSON 并构建 <c>application/json; charset=utf-8</c> 内容。
    /// </summary>
    public static ByteArrayContent CreateJsonContent<T>(T payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8"
        };
        return content;
    }

    /// <summary>
    /// 构建 JSON POST 请求（HTTP/1.1 优先，允许降级）。
    /// </summary>
    public static HttpRequestMessage CreateJsonPost(Uri uri, byte[] jsonBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new ByteArrayContent(jsonBody)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8"
        };
        request.Version = HttpVersion.Version11;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        return request;
    }

    /// <summary>
    /// 发送 JSON POST 请求，遇到瞬时网络失败（连接中断、IO/超时异常）时自动重试（默认最多 3 次，退避 400ms×次数）。
    /// </summary>
    /// <param name="httpClient">目标客户端。</param>
    /// <param name="uri">请求地址。</param>
    /// <param name="jsonBody">UTF-8 JSON 字节体。</param>
    /// <param name="configure">发送前的请求自定义（如追加 Authorization 头）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <exception cref="HttpRequestException">全部尝试均失败。</exception>
    public virtual async Task<HttpResponseMessage> SendJsonPostWithRetryAsync(
        Uri uri,
        byte[] jsonBody,
        Action<HttpRequestMessage>? configure,
        CancellationToken cancellationToken)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= MaxSendAttempts; attempt++)
        {
            using var request = CreateJsonPost(uri, jsonBody);
            configure?.Invoke(request);

            try
            {
                return await SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < MaxSendAttempts && IsTransientSendFailure(ex))
            {
                last = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        throw last ?? new HttpRequestException("JSON POST 请求发送失败。");
    }

    /// <summary>
    /// 以流式方式读取响应体为文本。
    /// </summary>
    public virtual async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>发送原始 HTTP 请求；可在派生类中重写以替换网络传输。</summary>
    public virtual Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default) =>
        httpClient.SendAsync(request, cancellationToken);

    /// <summary>
    /// 发送请求并按 <paramref name="completionOption"/> 返回。
    /// 流式读 body 时请用 <see cref="HttpCompletionOption.ResponseHeadersRead"/>，避免等整包下完。
    /// </summary>
    public virtual Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken = default) =>
        httpClient.SendAsync(request, completionOption, cancellationToken);

    /// <summary>判断异常链中是否包含瞬时发送失败（可重试）。</summary>
    private static bool IsTransientSendFailure(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException or IOException or TaskCanceledException)
                return true;

            if (current.Message.Contains("copying content to a stream", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("An error occurred while sending the request", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("Unable to read data from the transport connection", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
