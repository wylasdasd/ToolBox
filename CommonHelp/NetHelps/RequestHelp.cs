using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace CommonHelp;

/// <summary>
/// 静态 HttpClient 快捷请求工具：适用于无需 DI/日志的简单场景。
/// </summary>
public class RequestHelp
{
    private static readonly HttpClient _httpClient = new HttpClient();

    /// <summary>
    /// 发送 JSON POST 请求并返回 HTTP 状态码；action 可在发送前修改请求头。
    /// </summary>
    public static async Task<int> SendRequestAsync(string url, string body, Action<HttpRequestHeaders>? action = null)
    {
        // 1. 创建请求消息对象，指明方法和地址
        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        // 2. 在消息级别设置 Header（线程安全，仅对本次请求有效）
        action?.Invoke(request.Headers);

        // 3. 设置内容
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // 4. 发送这个特定的请求对象
        using var response = await _httpClient.SendAsync(request);

        return (int)response.StatusCode;
    }
}

/// <summary>
/// 基于 IHttpClientFactory 的 HTTP 服务（推荐通过 DI 注入使用）：
/// 封装 POST/PUT/DELETE/GET JSON 请求与表单 POST；超时或异常时返回 default 并记录日志。
/// </summary>
public class HttpService : IHttpService
{
    protected readonly IHttpClientFactory httpClientFactory;
    protected readonly ILogger<IHttpService> logger;

    public HttpService(IHttpClientFactory httpClientFactory, ILogger<IHttpService> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    /// <inheritdoc />
    public virtual async Task<T?> PostRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        action?.Invoke(request.Headers);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // 1. 创建超时取消令牌
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            // 2. 发送请求时传入令牌
            using var response = await client.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                if (typeof(T) == typeof(string))
                {
                    return (T?)(object)content;
                }

                return content.JsonDe<T>();
            }
            return default;
        }
        catch (OperationCanceledException)
        {
            logger.LogError("PostRequestAsync timed out or was canceled.");
            // 3. 专门捕获超时异常

            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PostRequestAsync failed.");
            return default;
        }
    }

    /// <inheritdoc />
    public virtual async Task<T?> PutRequestAsync<T>(string url, string body, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        action?.Invoke(request.Headers);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // 1. 创建超时取消令牌
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            // 2. 发送请求时传入令牌
            using var response = await client.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                if (typeof(T) == typeof(string))
                {
                    return (T?)(object)content;
                }

                return content.JsonDe<T>();
            }
            return default;
        }
        catch (OperationCanceledException)
        {
            logger.LogError("PutRequestAsync timed out or was canceled.");
            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PutRequestAsync failed.");
            return default;
        }
    }

    /// <inheritdoc />
    public virtual async Task<T?> DeleteRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        action?.Invoke(request.Headers);

        // 1. 创建超时取消令牌
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            // 2. 发送请求时传入令牌
            using var response = await client.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                if (typeof(T) == typeof(string))
                {
                    return (T?)(object)content;
                }

                return content.JsonDe<T>();
            }
            return default;
        }
        catch (OperationCanceledException)
        {
            logger.LogError("DeleteRequestAsync timed out or was canceled.");
            // 3. 专门捕获超时异常

            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeleteRequestAsync failed.");
            return default;
        }
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetRequestAsync<T>(string url, int timeoutSeconds = 30, Action<HttpRequestHeaders>? action = null) where T : class
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        action?.Invoke(request.Headers);

        // 1. 创建超时取消令牌
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            // 2. 发送请求时传入令牌
            using var response = await client.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                if (typeof(T) == typeof(string))
                {
                    return (T?)(object)content;
                }

                return content.JsonDe<T>();
            }
            return default;
        }
        catch (OperationCanceledException)
        {
            logger.LogError("GetRequestAsync timed out or was canceled.");
            // 3. 专门捕获超时异常

            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetRequestAsync failed.");
            return default;
        }
    }

    /// <inheritdoc />
    public virtual async Task<T?> PostFormRequestAsync<T>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action = null, int timeoutSeconds = 60 * 10) where T : class
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        action?.Invoke(request.Headers);
        request.Content = new FormUrlEncodedContent(formData.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? ""));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            using var response = await client.SendAsync(request, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (typeof(T) == typeof(string))
                {
                    return (T?)(object)content;
                }
                return content.JsonDe<T>();
            }
            return default;
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
}
