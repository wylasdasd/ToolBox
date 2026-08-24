using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace CommonHelp;

/// <summary>
///  HttpClient 请求工具自己new：适用于无需 DI/日志的简单场景。
/// </summary>
public class RequestHelp : IHttpService
{
    private readonly HttpClient httpClient;

    /// <summary>使用调用方提供的 HttpClient；可通过 DI 或手动 new 注入。</summary>
    public RequestHelp(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// 使用 JSON 请求体发送指定 HTTP 方法并返回 HTTP 状态码；action 可在发送前修改请求头。
    /// </summary>
    public virtual async Task<int> SendRequestAsync(HttpMethod method, string url, string body, Action<HttpRequestHeaders>? action = null)
    {
        // 1. 创建请求消息对象，指明方法和地址
        using var request = new HttpRequestMessage(method, url);

        // 2. 在消息级别设置 Header（线程安全，仅对本次请求有效）
        action?.Invoke(request.Headers);

        // 3. 设置内容
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // 4. 发送这个特定的请求对象
        using var response = await httpClient.SendAsync(request);

        return (int)response.StatusCode;
    }

    /// <inheritdoc />
    public virtual Task<T?> PostRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class =>
        SendJsonAsync<T>(HttpMethod.Post, url, body, timeoutSeconds, action);

    /// <inheritdoc />
    public virtual Task<T?> PutRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class =>
        SendJsonAsync<T>(HttpMethod.Put, url, body, timeoutSeconds, action);

    /// <inheritdoc />
    public virtual Task<T?> DeleteRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class =>
        SendJsonAsync<T>(HttpMethod.Delete, url, null, timeoutSeconds, action);

    /// <inheritdoc />
    public virtual Task<T?> GetRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class =>
        SendJsonAsync<T>(HttpMethod.Get, url, null, timeoutSeconds, action);

    /// <inheritdoc />
    public virtual Task<T?> PostFormRequestAsync<T>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action = null, int timeoutSeconds = 60 * 3) where T : class =>
        SendFormAsync<T>(url, formData, timeoutSeconds, action);

    /// <inheritdoc />
    public virtual async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? body = null, string? mediaType = null, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, url);
        action?.Invoke(request.Headers);
        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, mediaType ?? "application/json");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        return await httpClient.SendAsync(request, cts.Token);
    }

    private async Task<T?> SendJsonAsync<T>(HttpMethod method, string url, string? body, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class
    {
        using var request = new HttpRequestMessage(method, url);
        action?.Invoke(request.Headers);
        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            using var response = await httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                return default;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            return typeof(T) == typeof(string) ? (T?)(object)content : content.JsonDe<T>();
        }
        catch
        {
            return default;
        }
    }

    private async Task<T?> SendFormAsync<T>(string url, Dictionary<string, object> formData, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        action?.Invoke(request.Headers);
        request.Content = new FormUrlEncodedContent(formData.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? ""));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            using var response = await httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                return default;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            return typeof(T) == typeof(string) ? (T?)(object)content : content.JsonDe<T>();
        }
        catch
        {
            return default;
        }
    }
}
/// <summary>
/// 基于 IHttpClientFactory 的 HTTP 服务（推荐通过 DI 注入使用）：
/// 封装 POST/PUT/DELETE/GET JSON 请求与表单 POST；超时或异常时返回 default 并记录日志。
/// </summary>
public class HttpClientFactoryService : IHttpService
{
    protected readonly IHttpClientFactory httpClientFactory;
    protected readonly ILogger<IHttpService> logger;

    /// <summary>使用 IHttpClientFactory 和日志服务创建 HTTP 请求服务。</summary>
    public HttpClientFactoryService(IHttpClientFactory httpClientFactory, ILogger<IHttpService> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    /// <summary>JSON 请求公共核心：构建请求、超时控制、错误日志与字符串/反序列化分流。</summary>
    private async Task<T?> SendJsonAsync<T>(HttpMethod method, string url, string? body, int timeoutSeconds, Action<HttpRequestHeaders>? action, string operationName) where T : class
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(method, url);
        action?.Invoke(request.Headers);
        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            using var response = await client.SendAsync(request, cts.Token);

            if (!response.IsSuccessStatusCode)
                return default;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            return typeof(T) == typeof(string) ? (T?)(object)content : content.JsonDe<T>();
        }
        catch (OperationCanceledException)
        {
            logger.LogError("{Operation} timed out or was canceled.", operationName);
            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Operation} failed.", operationName);
            return default;
        }
    }

    /// <summary>表单 POST 公共核心：urlencoded 表单体，超时/日志/反序列化策略与 <see cref="SendJsonAsync{T}"/> 一致。</summary>
    private async Task<T?> SendFormAsync<T>(string url, Dictionary<string, object> formData, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        action?.Invoke(request.Headers);
        request.Content = new FormUrlEncodedContent(formData.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? ""));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            using var response = await client.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                return default;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            return typeof(T) == typeof(string) ? (T?)(object)content : content.JsonDe<T>();
        }
        catch (OperationCanceledException)
        {
            logger.LogError("PostFormRequestAsync OperationCanceledException");
            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PostFormRequestAsync Exception");
            return default;
        }
    }

    /// <inheritdoc />
    public virtual Task<T?> PostRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class =>
        SendJsonAsync<T>(HttpMethod.Post, url, body, timeoutSeconds, action, nameof(PostRequestAsync));

    /// <inheritdoc />
    public virtual Task<T?> PutRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class =>
        SendJsonAsync<T>(HttpMethod.Put, url, body, timeoutSeconds, action, nameof(PutRequestAsync));

    /// <inheritdoc />
    public virtual Task<T?> DeleteRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class =>
        SendJsonAsync<T>(HttpMethod.Delete, url, null, timeoutSeconds, action, nameof(DeleteRequestAsync));

    /// <inheritdoc />
    public virtual Task<T?> GetRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class =>
        SendJsonAsync<T>(HttpMethod.Get, url, null, timeoutSeconds, action, nameof(GetRequestAsync));

    /// <inheritdoc />
    public virtual Task<T?> PostFormRequestAsync<T>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action = null, int timeoutSeconds = 60 * 10) where T : class =>
        SendFormAsync<T>(url, formData, timeoutSeconds, action);

    /// <inheritdoc />
    public virtual async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? body = null, string? mediaType = null, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(method, url);
        action?.Invoke(request.Headers);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, mediaType ?? "application/json");
        }

        // 1. 关联外部取消令牌并附加超时
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        // 2. 不吞异常、不反序列化：原始响应交由调用方读取并在用完后释放
        return await client.SendAsync(request, cts.Token);
    }


    /// <summary>
    /// 使用 JSON 请求体发送指定 HTTP 方法并返回 HTTP 状态码；action 可在发送前修改请求头。
    /// </summary>
    public virtual async Task<int> SendRequestAsync(HttpMethod method, string url, string body, Action<HttpRequestHeaders>? action = null)
    {
        var client = httpClientFactory.CreateClient();
        // 1. 创建请求消息对象，指明方法和地址
        using var request = new HttpRequestMessage(method, url);

        // 2. 在消息级别设置 Header（线程安全，仅对本次请求有效）
        action?.Invoke(request.Headers);

        // 3. 设置内容
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // 4. 发送这个特定的请求对象
        using var response = await client.SendAsync(request);

        return (int)response.StatusCode;
    }
}




/// <summary>
/// 使用强类型请求对象发送 JSON 的 HTTP 请求工具。
/// 请求对象由内部序列化，响应可反序列化为指定的响应类型。
/// </summary>
public class TypedHttpClientFactoryService : ITypedHttpService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<ITypedHttpService> logger;

    /// <summary>使用 IHttpClientFactory 和日志服务创建强类型请求服务。</summary>
    public TypedHttpClientFactoryService(IHttpClientFactory httpClientFactory, ILogger<ITypedHttpService> logger)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>使用强类型请求对象发送请求，只返回 HTTP 状态码。</summary>
    public virtual async Task<int> SendRequestAsync<TRequest>(HttpMethod method, string url, TRequest requestBody, Action<HttpRequestHeaders>? action = null)
    {
        var httpClient = httpClientFactory.CreateClient();
        using var request = CreateRequest(method, url, requestBody, action);
        try
        {
            using var response = await httpClient.SendAsync(request);
            return (int)response.StatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Method} {Url} failed.", method, url);
            throw;
        }
    }

    /// <summary>使用强类型请求对象发送 JSON POST，并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> PostRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendJsonAsync<TRequest, TResponse>(HttpMethod.Post, url, requestBody, timeoutSeconds, action);

    /// <summary>使用强类型请求对象发送 JSON PUT，并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> PutRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendJsonAsync<TRequest, TResponse>(HttpMethod.Put, url, requestBody, timeoutSeconds, action);

    /// <summary>发送 DELETE 请求并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> DeleteRequestAsync<TResponse>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendJsonAsync<object, TResponse>(HttpMethod.Delete, url, null, timeoutSeconds, action);

    /// <summary>发送 GET 请求并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> GetRequestAsync<TResponse>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendJsonAsync<object, TResponse>(HttpMethod.Get, url, null, timeoutSeconds, action);

    /// <summary>发送 urlencoded 表单并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> PostFormRequestAsync<TResponse>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action = null, int timeoutSeconds = 60 * 3)
        where TResponse : class =>
        SendFormAsync<TResponse>(url, formData, timeoutSeconds, action);

    /// <summary>使用强类型请求对象发送原始 HTTP 请求。</summary>
    public virtual Task<HttpResponseMessage> SendAsync<TRequest>(HttpMethod method, string url, TRequest? requestBody = default, string? mediaType = null, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null, CancellationToken cancellationToken = default) =>
        SendRawAsync(method, url, requestBody, mediaType, timeoutSeconds, action, cancellationToken);

    private HttpRequestMessage CreateRequest<TRequest>(HttpMethod method, string url, TRequest? requestBody, Action<HttpRequestHeaders>? action, string? mediaType = null)
    {
        var request = new HttpRequestMessage(method, url);
        action?.Invoke(request.Headers);
        if (requestBody is not null)
            request.Content = new StringContent(requestBody.JsonSeNoLow(), Encoding.UTF8, mediaType ?? "application/json");
        return request;
    }

    private async Task<TResponse?> SendJsonAsync<TRequest, TResponse>(HttpMethod method, string url, TRequest? requestBody, int timeoutSeconds, Action<HttpRequestHeaders>? action)
        where TResponse : class
    {
        var httpClient = httpClientFactory.CreateClient();
        using var request = CreateRequest(method, url, requestBody, action);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            using var response = await httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                return default;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            return typeof(TResponse) == typeof(string) ? (TResponse?)(object)content : content.JsonDe<TResponse>();
        }
        catch (OperationCanceledException)
        {
            logger.LogError("{Method} {Url} timed out or was canceled.", method, url);
            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Method} {Url} failed.", method, url);
            return default;
        }
    }

    private async Task<TResponse?> SendFormAsync<TResponse>(string url, Dictionary<string, object> formData, int timeoutSeconds, Action<HttpRequestHeaders>? action)
        where TResponse : class
    {
        var httpClient = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        action?.Invoke(request.Headers);
        request.Content = new FormUrlEncodedContent(formData.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? ""));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            using var response = await httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                return default;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            return typeof(TResponse) == typeof(string) ? (TResponse?)(object)content : content.JsonDe<TResponse>();
        }
        catch (OperationCanceledException)
        {
            logger.LogError("POST form {Url} timed out or was canceled.", url);
            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "POST form {Url} failed.", url);
            return default;
        }
    }

    private async Task<HttpResponseMessage> SendRawAsync<TRequest>(HttpMethod method, string url, TRequest? requestBody, string? mediaType, int timeoutSeconds, Action<HttpRequestHeaders>? action, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();
        using var request = CreateRequest(method, url, requestBody, action, mediaType);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        return await httpClient.SendAsync(request, cts.Token);
    }
}


/// <summary>
///  HttpClient 请求工具自己new：适用于无需 DI/日志的简单场景。
/// </summary>
public class TypedRequestHelp : ITypedHttpService
{
    private readonly HttpClient httpClient;

    /// <summary>使用调用方提供的 HttpClient；可通过 DI 或手动 new 注入。</summary>
    public TypedRequestHelp(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>使用强类型请求对象发送请求，只返回 HTTP 状态码。</summary>
    public virtual async Task<int> SendRequestAsync<TRequest>(HttpMethod method, string url, TRequest requestBody, Action<HttpRequestHeaders>? action = null)
    {
        using var request = CreateRequest(method, url, requestBody, action);
        using var response = await httpClient.SendAsync(request);
        return (int)response.StatusCode;
    }

    /// <summary>使用强类型请求对象发送 JSON POST，并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> PostRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendJsonAsync<TRequest, TResponse>(HttpMethod.Post, url, requestBody, timeoutSeconds, action);

    /// <summary>使用强类型请求对象发送 JSON PUT，并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> PutRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendJsonAsync<TRequest, TResponse>(HttpMethod.Put, url, requestBody, timeoutSeconds, action);

    /// <summary>发送 DELETE 请求并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> DeleteRequestAsync<TResponse>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendJsonAsync<object, TResponse>(HttpMethod.Delete, url, null, timeoutSeconds, action);

    /// <summary>发送 GET 请求并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> GetRequestAsync<TResponse>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null)
        where TResponse : class =>
        SendJsonAsync<object, TResponse>(HttpMethod.Get, url, null, timeoutSeconds, action);

    /// <summary>发送 urlencoded 表单并将响应反序列化为 TResponse。</summary>
    public virtual Task<TResponse?> PostFormRequestAsync<TResponse>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action = null, int timeoutSeconds = 60 * 3)
        where TResponse : class =>
        SendFormAsync<TResponse>(url, formData, timeoutSeconds, action);

    /// <summary>使用强类型请求对象发送原始 HTTP 请求。</summary>
    public virtual Task<HttpResponseMessage> SendAsync<TRequest>(HttpMethod method, string url, TRequest? requestBody = default, string? mediaType = null, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null, CancellationToken cancellationToken = default) =>
        SendRawAsync(method, url, requestBody, mediaType, timeoutSeconds, action, cancellationToken);

    private HttpRequestMessage CreateRequest<TRequest>(HttpMethod method, string url, TRequest? requestBody, Action<HttpRequestHeaders>? action, string? mediaType = null)
    {
        var request = new HttpRequestMessage(method, url);
        action?.Invoke(request.Headers);
        if (requestBody is not null)
            request.Content = new StringContent(requestBody.JsonSeNoLow(), Encoding.UTF8, mediaType ?? "application/json");
        return request;
    }

    private async Task<TResponse?> SendJsonAsync<TRequest, TResponse>(HttpMethod method, string url, TRequest? requestBody, int timeoutSeconds, Action<HttpRequestHeaders>? action)
        where TResponse : class
    {
        using var request = CreateRequest(method, url, requestBody, action);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            using var response = await httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                return default;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            return typeof(TResponse) == typeof(string) ? (TResponse?)(object)content : content.JsonDe<TResponse>();
        }
        catch
        {
            return default;
        }
    }

    private async Task<TResponse?> SendFormAsync<TResponse>(string url, Dictionary<string, object> formData, int timeoutSeconds, Action<HttpRequestHeaders>? action)
        where TResponse : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        action?.Invoke(request.Headers);
        request.Content = new FormUrlEncodedContent(formData.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? ""));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            using var response = await httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                return default;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            return typeof(TResponse) == typeof(string) ? (TResponse?)(object)content : content.JsonDe<TResponse>();
        }
        catch
        {
            return default;
        }
    }

    private async Task<HttpResponseMessage> SendRawAsync<TRequest>(HttpMethod method, string url, TRequest? requestBody, string? mediaType, int timeoutSeconds, Action<HttpRequestHeaders>? action, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, url, requestBody, action, mediaType);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        return await httpClient.SendAsync(request, cts.Token);
    }
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
