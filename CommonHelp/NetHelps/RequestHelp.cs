using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CommonHelp;

/// <summary>
/// HTTP 发送公共核心：组请求、超时发送、读响应。
/// 不负责拿 <see cref="HttpClient"/>；由子类提供客户端，可选提供日志。
/// 无重试（瞬时重试见 <see cref="HttpTransportHelp"/>）。
/// </summary>
public abstract class HttpExec
{
    /// <summary>本次请求使用的 <see cref="HttpClient"/>（注入实例或 Factory 创建）。</summary>
    protected abstract HttpClient GetClient();

    /// <summary>失败时写日志；无日志的实现返回 null，错误仍按约定返回 default 或抛出。</summary>
    protected virtual ILogger? ErrorLogger => null;

    /// <summary>
    /// 发送请求并反序列化响应：非 2xx、超时/取消、网络错误、JSON 解析失败返回 default。
    /// <typeparamref name="T"/> 为 string 时直接返回响应正文。
    /// </summary>
    protected async Task<T?> SendAndDecodeAsync<T>(HttpRequestMessage request, int timeoutSeconds, string operation)
        where T : class
    {
        using (request)
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            try
            {
                using var response = await GetClient().SendAsync(request, cts.Token);
                if (!response.IsSuccessStatusCode)
                    return default;

                var content = await response.Content.ReadAsStringAsync(cts.Token);
                return typeof(T) == typeof(string)
                    ? (T?)(object)content
                    : content.JsonDe<T>(JsonExtensions.LenientDeserializeOptions);
            }
            catch (OperationCanceledException)
            {
                ErrorLogger?.LogError("{Operation} timed out or was canceled.", operation);
                return default;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                ErrorLogger?.LogError(ex, "{Operation} failed.", operation);
                return default;
            }
        }
    }

    /// <summary>发送请求，只返回 HTTP 状态码；无超时。本方法会释放 <paramref name="request"/>。</summary>
    protected async Task<int> SendStatusAsync(HttpRequestMessage request)
    {
        using (request)
        using (var response = await GetClient().SendAsync(request))
            return (int)response.StatusCode;
    }

    /// <summary>
    /// 发送原始请求并返回响应（不反序列化、不吞异常）。
    /// 响应由调用方读取并 Dispose；请求在发送完成后释放。
    /// </summary>
    protected async Task<HttpResponseMessage> SendRawAsync(HttpRequestMessage request, int timeoutSeconds, CancellationToken cancellationToken)
    {
        using (request)
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            return await GetClient().SendAsync(request, cts.Token);
        }
    }

    /// <summary>组装字符串 JSON 请求；<paramref name="body"/> 为空则无请求体（GET/DELETE）。</summary>
    protected static HttpRequestMessage CreateStringRequest(
        HttpMethod method, string url, string? body, Action<HttpRequestHeaders>? action, string? mediaType = null)
    {
        var request = new HttpRequestMessage(method, url);
        action?.Invoke(request.Headers);
        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, mediaType ?? "application/json");
        return request;
    }

    /// <summary>组装强类型 JSON 请求：对象序列化为缩进、中文不转义的 JSON。</summary>
    protected static HttpRequestMessage CreateObjectRequest<T>(
        HttpMethod method, string url, T? body, Action<HttpRequestHeaders>? action, string? mediaType = null)
    {
        var request = new HttpRequestMessage(method, url);
        action?.Invoke(request.Headers);
        if (body is not null)
            request.Content = new StringContent(body.JsonSe(JsonExtensions.IndentedRelaxedOptions), Encoding.UTF8, mediaType ?? "application/json");
        return request;
    }

    /// <summary>组装 application/x-www-form-urlencoded POST。</summary>
    protected static HttpRequestMessage CreateFormRequest(
        string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        action?.Invoke(request.Headers);
        request.Content = new FormUrlEncodedContent(
            formData.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? ""));
        return request;
    }
}

/// <summary>
/// 字符串请求体的 <see cref="IHttpService"/> 实现：调用方自己把 JSON 字符串准备好再传入。
/// </summary>
public abstract class StringBodyHttpService : HttpExec, IHttpService
{
    /// <inheritdoc />
    public virtual Task<T?> PostRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where T : class =>
        SendAndDecodeAsync<T>(CreateStringRequest(HttpMethod.Post, url, body, action), timeoutSeconds, nameof(PostRequestAsync));

    /// <inheritdoc />
    public virtual Task<T?> PutRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where T : class =>
        SendAndDecodeAsync<T>(CreateStringRequest(HttpMethod.Put, url, body, action), timeoutSeconds, nameof(PutRequestAsync));

    /// <inheritdoc />
    public virtual Task<T?> DeleteRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where T : class =>
        SendAndDecodeAsync<T>(CreateStringRequest(HttpMethod.Delete, url, null, action), timeoutSeconds, nameof(DeleteRequestAsync));

    /// <inheritdoc />
    public virtual Task<T?> GetRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where T : class =>
        SendAndDecodeAsync<T>(CreateStringRequest(HttpMethod.Get, url, null, action), timeoutSeconds, nameof(GetRequestAsync));

    /// <inheritdoc />
    public virtual Task<T?> PostFormRequestAsync<T>(
        string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action = null, int timeoutSeconds = 60 * 3)
        where T : class =>
        SendAndDecodeAsync<T>(CreateFormRequest(url, formData, action), timeoutSeconds, nameof(PostFormRequestAsync));

    /// <inheritdoc />
    public virtual Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, string? body = null, string? mediaType = null, int timeoutSeconds = 30,
        Action<HttpRequestHeaders>? action = null, CancellationToken cancellationToken = default) =>
        SendRawAsync(CreateStringRequest(method, url, body, action, mediaType), timeoutSeconds, cancellationToken);

    /// <inheritdoc />
    public virtual Task<int> SendRequestAsync(HttpMethod method, string url, string body, Action<HttpRequestHeaders>? action = null) =>
        SendStatusAsync(CreateStringRequest(method, url, body, action));
}

/// <summary>
/// 强类型请求体的 <see cref="ITypedHttpService"/> 实现：内部把对象序列化成 JSON 再发送。
/// </summary>
public abstract class TypedBodyHttpService : HttpExec, ITypedHttpService
{
    /// <inheritdoc />
    public virtual async Task<int> SendRequestAsync<TRequest>(
        HttpMethod method, string url, TRequest requestBody, Action<HttpRequestHeaders>? action = null)
    {
        try
        {
            return await SendStatusAsync(CreateObjectRequest(method, url, requestBody, action));
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogError(ex, "{Method} {Url} failed.", method, url);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual Task<TResponse?> PostRequestAsync<TRequest, TResponse>(
        string url, TRequest requestBody, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendAndDecodeAsync<TResponse>(CreateObjectRequest(HttpMethod.Post, url, requestBody, action), timeoutSeconds, $"{HttpMethod.Post} {url}");

    /// <inheritdoc />
    public virtual Task<TResponse?> PutRequestAsync<TRequest, TResponse>(
        string url, TRequest requestBody, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendAndDecodeAsync<TResponse>(CreateObjectRequest(HttpMethod.Put, url, requestBody, action), timeoutSeconds, $"{HttpMethod.Put} {url}");

    /// <inheritdoc />
    public virtual Task<TResponse?> DeleteRequestAsync<TResponse>(
        string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendAndDecodeAsync<TResponse>(CreateObjectRequest<object>(HttpMethod.Delete, url, null, action), timeoutSeconds, $"{HttpMethod.Delete} {url}");

    /// <inheritdoc />
    public virtual Task<TResponse?> GetRequestAsync<TResponse>(
        string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendAndDecodeAsync<TResponse>(CreateObjectRequest<object>(HttpMethod.Get, url, null, action), timeoutSeconds, $"{HttpMethod.Get} {url}");

    /// <inheritdoc />
    public virtual Task<TResponse?> PostFormRequestAsync<TResponse>(
        string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action = null, int timeoutSeconds = 60 * 3)
        where TResponse : class =>
        SendAndDecodeAsync<TResponse>(CreateFormRequest(url, formData, action), timeoutSeconds, $"POST form {url}");

    /// <inheritdoc />
    public virtual Task<HttpResponseMessage> SendAsync<TRequest>(
        HttpMethod method, string url, TRequest? requestBody = default, string? mediaType = null, int timeoutSeconds = 30,
        Action<HttpRequestHeaders>? action = null, CancellationToken cancellationToken = default) =>
        SendRawAsync(CreateObjectRequest(method, url, requestBody, action, mediaType), timeoutSeconds, cancellationToken);
}

/// <summary>
///  HttpClient 请求工具自己new：适用于无需 DI/日志的简单场景。
/// </summary>
public class RequestHelp : StringBodyHttpService
{
    private readonly HttpClient httpClient;

    /// <summary>使用调用方提供的 HttpClient；可通过 DI 或手动 new 注入。</summary>
    public RequestHelp(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    protected override HttpClient GetClient() => httpClient;
}

/// <summary>
/// 基于 IHttpClientFactory 的 HTTP 服务（推荐通过 DI 注入使用）：
/// 封装 POST/PUT/DELETE/GET JSON 请求与表单 POST；超时或异常时返回 default 并记录日志。
/// </summary>
public class HttpClientFactoryService : StringBodyHttpService
{
    protected readonly IHttpClientFactory httpClientFactory;
    protected readonly ILogger<IHttpService> logger;

    /// <summary>使用 IHttpClientFactory 和日志服务创建 HTTP 请求服务。</summary>
    public HttpClientFactoryService(IHttpClientFactory httpClientFactory, ILogger<IHttpService> logger)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override HttpClient GetClient() => httpClientFactory.CreateClient();

    /// <inheritdoc />
    protected override ILogger? ErrorLogger => logger;
}

/// <summary>
///  HttpClient 请求工具自己new：适用于无需 DI/日志的简单场景。
/// </summary>
public class TypedRequestHelp : TypedBodyHttpService
{
    private readonly HttpClient httpClient;

    /// <summary>使用调用方提供的 HttpClient；可通过 DI 或手动 new 注入。</summary>
    public TypedRequestHelp(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    protected override HttpClient GetClient() => httpClient;
}

/// <summary>
/// 使用强类型请求对象发送 JSON 的 HTTP 请求工具。
/// 请求对象由内部序列化，响应可反序列化为指定的响应类型。
/// </summary>
public class TypedHttpClientFactoryService : TypedBodyHttpService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<ITypedHttpService> logger;

    /// <summary>使用 IHttpClientFactory 和日志服务创建强类型请求服务。</summary>
    public TypedHttpClientFactoryService(IHttpClientFactory httpClientFactory, ILogger<ITypedHttpService> logger)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override HttpClient GetClient() => httpClientFactory.CreateClient();

    /// <inheritdoc />
    protected override ILogger? ErrorLogger => logger;
}

/// <summary>强类型 JSON 请求服务接口。</summary>
public interface ITypedHttpService
{
    /// <summary>使用强类型请求对象发送请求，只返回 HTTP 状态码。</summary>
    Task<int> SendRequestAsync<TRequest>(HttpMethod method, string url, TRequest requestBody, Action<HttpRequestHeaders>? action = null);

    /// <summary>使用强类型请求对象发送 JSON POST，并将响应反序列化为 TResponse。</summary>
    Task<TResponse?> PostRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class;

    /// <summary>使用强类型请求对象发送 JSON PUT，并将响应反序列化为 TResponse。</summary>
    Task<TResponse?> PutRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class;

    /// <summary>发送 DELETE 请求并将响应反序列化为 TResponse。</summary>
    Task<TResponse?> DeleteRequestAsync<TResponse>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class;

    /// <summary>发送 GET 请求并将响应反序列化为 TResponse。</summary>
    Task<TResponse?> GetRequestAsync<TResponse>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class;

    /// <summary>发送 urlencoded 表单并将响应反序列化为 TResponse。</summary>
    Task<TResponse?> PostFormRequestAsync<TResponse>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action = null, int timeoutSeconds = 60 * 3)
        where TResponse : class;

    /// <summary>使用强类型请求对象发送原始 HTTP 请求。</summary>
    Task<HttpResponseMessage> SendAsync<TRequest>(HttpMethod method, string url, TRequest? requestBody = default, string? mediaType = null, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// HTTP 请求服务接口：JSON/表单请求的统一入口，失败或超时返回 default；
/// 需要原始响应（错误体、状态码、流式读取）时使用 <see cref="SendAsync"/>。
/// </summary>
public interface IHttpService
{
    /// <summary>
    /// POST JSON 请求并把响应反序列化为 T；T 为 string 时直接返回响应文本。
    /// </summary>
    Task<T?> PostRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class;

    /// <summary>
    /// PUT JSON 请求并把响应反序列化为 T。
    /// </summary>
    Task<T?> PutRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class;

    /// <summary>
    /// DELETE 请求并把响应反序列化为 T。
    /// </summary>
    Task<T?> DeleteRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class;

    /// <summary>
    /// GET 请求并把响应反序列化为 T。
    /// </summary>
    Task<T?> GetRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class;

    /// <summary>
    /// POST 表单（urlencoded）请求并把响应反序列化为 T；实现方默认超时较长，适合耗时任务。
    /// </summary>
    Task<T?> PostFormRequestAsync<T>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action = null, int timeoutSeconds = 60 * 3) where T : class;

    /// <summary>
    /// 发送原始 HTTP 请求并返回未经处理的响应：不吞异常、不反序列化，超时/网络错误直接抛出；响应由调用方读取并释放。
    /// body 非空时作为请求体发送，mediaType 缺省为 application/json。
    /// </summary>
    Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? body = null, string? mediaType = null, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null, CancellationToken cancellationToken = default);

    /// <summary>使用 JSON 请求体发送指定 HTTP 方法并返回 HTTP 状态码。</summary>
    Task<int> SendRequestAsync(HttpMethod method, string url, string body, Action<HttpRequestHeaders>? action = null);
}
