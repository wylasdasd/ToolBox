using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ToolBox.Services.Ai;

public sealed class SemanticKernelAiAskService : IAiAskService
{
    private const int MaxAttempts = 4;
    private static readonly TimeSpan BaseBackoffDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxBackoffDelay = TimeSpan.FromSeconds(30);
    private static readonly Regex ModelIdRegex = new("^[a-zA-Z0-9][a-zA-Z0-9._:-]{0,127}$", RegexOptions.Compiled);

    public async Task<string> AskByOcrAsync(AiAskRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new InvalidOperationException($"{AiProviderCatalog.GetDisplayName(request.Provider)} API Key 未配置。");
        }

        if (string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            throw new InvalidOperationException("Prompt 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(request.OcrText))
        {
            throw new InvalidOperationException("OCR 文本为空，无法提问。");
        }

        var provider = request.Provider;
        var providerName = AiProviderCatalog.GetDisplayName(provider);
        var model = string.IsNullOrWhiteSpace(request.Model)
            ? AiProviderCatalog.GetDefaultModel(provider)
            : request.Model.Trim();
        ValidateModelId(model);

        var finalPrompt = $"""
你会收到一段 OCR 识别文本，请严格基于 OCR 文本回答。
如果 OCR 文本中没有证据，请明确说“未在 OCR 文本中找到”。

用户要求：
{request.UserPrompt.Trim()}

OCR 文本：
{request.OcrText.Trim()}
""";

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var result = await AskViaSemanticKernelAsync(
                    provider,
                    request.ApiKey.Trim(),
                    model,
                    finalPrompt,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(result))
                {
                    throw new InvalidOperationException($"{providerName} 返回文本为空。");
                }

                return result.Trim();
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                var context = ParseErrorContext(ex);
                var canRetry = ShouldRetry(context);
                if (canRetry && attempt < MaxAttempts)
                {
                    var delay = GetNextDelay(context.RetryDelay, attempt);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                throw new InvalidOperationException(
                    BuildFriendlyErrorMessage(providerName, model, context, attempt),
                    ex);
            }
        }

        throw new InvalidOperationException($"{providerName} 调用失败：达到最大重试次数。");
    }

    private static void ValidateModelId(string model)
    {
        if (!ModelIdRegex.IsMatch(model))
        {
            throw new InvalidOperationException(
                $"模型名不符合 OpenAI Chat Completion 规范：{model}。示例：gpt-4.1-mini、deepseek-chat、gemini-2.5-flash。");
        }
    }

    private static async Task<string> AskViaSemanticKernelAsync(
        AiProviderKind provider,
        string apiKey,
        string model,
        string prompt,
        CancellationToken cancellationToken)
    {
        var builder = Kernel.CreateBuilder();
        var endpoint = AiProviderCatalog.GetEndpoint(provider);
#pragma warning disable SKEXP0010
        if (endpoint is null)
        {
            builder.AddOpenAIChatCompletion(
                modelId: model,
                apiKey: apiKey);
        }
        else
        {
            builder.AddOpenAIChatCompletion(
                modelId: model,
                apiKey: apiKey,
                endpoint: endpoint);
        }
#pragma warning restore SKEXP0010
        var kernel = builder.Build();
        var chatService = kernel.Services.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }

    private static bool ShouldRetry(ApiErrorContext context)
    {
        if (context.IsQuotaZero)
        {
            return false;
        }

        return context.StatusCode is 408 or 425 or 429 or 500 or 502 or 503 or 504;
    }

    private static TimeSpan GetNextDelay(TimeSpan? serverRetryDelay, int attempt)
    {
        if (serverRetryDelay.HasValue && serverRetryDelay.Value > TimeSpan.Zero)
        {
            return serverRetryDelay.Value;
        }

        var exponential = TimeSpan.FromMilliseconds(
            BaseBackoffDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
        if (exponential > MaxBackoffDelay)
        {
            exponential = MaxBackoffDelay;
        }

        var jitterMs = Random.Shared.Next(0, 500);
        return exponential + TimeSpan.FromMilliseconds(jitterMs);
    }

    private static string BuildFriendlyErrorMessage(string providerName, string model, ApiErrorContext context, int attempts)
    {
        if (context.StatusCode == 429)
        {
            var retryText = TryFormatRetryDelay(context.RetryDelay);
            if (context.IsQuotaZero)
            {
                return $"{providerName} 配额不足：当前 Key 在模型 {model} 上可用额度可能为 0。请检查计费与配额设置。{retryText}";
            }

            return $"{providerName} 请求过于频繁（429）。已重试 {attempts} 次。{retryText}";
        }

        if (context.StatusCode == 401 || context.StatusCode == 403)
        {
            return $"{providerName} 鉴权失败（HTTP {context.StatusCode}）。请检查 API Key 与模型权限：{model}。";
        }

        if (context.StatusCode == 404)
        {
            return $"{providerName} 资源不存在（404）。请检查模型名是否可用：{model}。";
        }

        if (context.StatusCode == 400)
        {
            var detail = string.IsNullOrWhiteSpace(context.Message) ? string.Empty : $" 详情：{context.Message}";
            return $"{providerName} 请求参数错误（400）。请确认模型名符合 OpenAI Chat Completion 规范并且服务端可用。{detail}";
        }

        if (!string.IsNullOrWhiteSpace(context.Message))
        {
            return $"{providerName} 调用失败：{context.Message}";
        }

        if (context.StatusCode.HasValue)
        {
            return $"{providerName} 调用失败：HTTP {context.StatusCode.Value}";
        }

        return $"{providerName} 调用失败。";
    }

    private static string TryFormatRetryDelay(TimeSpan? retryDelay)
    {
        if (!retryDelay.HasValue)
        {
            return string.Empty;
        }

        var seconds = Math.Max(1, (int)Math.Ceiling(retryDelay.Value.TotalSeconds));
        return $"建议等待 {seconds} 秒后重试。";
    }

    private static ApiErrorContext ParseErrorContext(Exception ex)
    {
        var flattened = FlattenException(ex);
        var raw = flattened.Raw;
        var message = SelectBestMessage(flattened.Messages);

        var statusCode = ParseStatusCode(raw, message);
        var retryDelay = ParseRetryDelay(raw);
        var isQuotaZero = raw.Contains("limit: 0", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase);

        var shortMessage = ExtractFirstLine(message);
        return new ApiErrorContext
        {
            StatusCode = statusCode,
            RetryDelay = retryDelay,
            IsQuotaZero = isQuotaZero,
            Message = shortMessage
        };
    }

    private static (string Raw, IReadOnlyList<string> Messages) FlattenException(Exception ex)
    {
        var messages = new List<string>();
        var rawBuilder = new System.Text.StringBuilder();
        var current = ex;
        while (current is not null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                messages.Add(current.Message.Trim());
            }

            rawBuilder.AppendLine(current.ToString());
            current = current.InnerException;
        }

        return (rawBuilder.ToString(), messages);
    }

    private static string SelectBestMessage(IReadOnlyList<string> messages)
    {
        if (messages.Count == 0)
        {
            return string.Empty;
        }

        foreach (var message in messages)
        {
            if (message.Equals("Service request failed.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (message.Equals("Response status code does not indicate success.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return message;
        }

        return messages[0];
    }

    private static int? ParseStatusCode(string raw, string message)
    {
        if (Regex.IsMatch(raw, @"\b429\b") || message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
        {
            return 429;
        }

        var match = Regex.Match(raw, @"\b(4\d\d|5\d\d)\b");
        if (match.Success && int.TryParse(match.Value, out var code))
        {
            return code;
        }

        return null;
    }

    private static TimeSpan? ParseRetryDelay(string raw)
    {
        var match = Regex.Match(raw, @"retryDelay[""']?\s*:\s*[""'](?<sec>\d+(?:\.\d+)?)s[""']", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        if (!double.TryParse(match.Groups["sec"].Value, out var seconds))
        {
            return null;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static string ExtractFirstLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var index = text.IndexOf('\n');
        return index < 0 ? text.Trim() : text[..index].Trim();
    }

    private sealed class ApiErrorContext
    {
        public int? StatusCode { get; init; }
        public TimeSpan? RetryDelay { get; init; }
        public bool IsQuotaZero { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
