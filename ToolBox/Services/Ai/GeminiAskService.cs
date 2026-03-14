using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ToolBox.Services.Ai;

public sealed class GeminiAskService : IGeminiAskService
{
    private const int MaxAttempts = 4;
    private static readonly TimeSpan BaseBackoffDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxBackoffDelay = TimeSpan.FromSeconds(30);

    public async Task<string> AskByOcrAsync(
        string apiKey,
        string model,
        string userPrompt,
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API Key 未配置。");
        }

        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            throw new InvalidOperationException("Prompt 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(ocrText))
        {
            throw new InvalidOperationException("OCR 文本为空，无法提问。");
        }

        var targetModel = string.IsNullOrWhiteSpace(model) ? "gemini 3 flash" : model.Trim();
        var modelCandidates = BuildModelCandidates(targetModel);
        var finalPrompt = $"""
你会收到一段 OCR 识别文本，请严格基于 OCR 文本回答。
如果 OCR 文本中没有证据，请明确说“未在 OCR 文本中找到”。

用户要求：
{userPrompt.Trim()}

OCR 文本：
{ocrText.Trim()}
""";

        string? lastError = null;
        foreach (var candidateModel in modelCandidates)
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    var apiModel = ToApiModelId(candidateModel);
                    var result = await AskViaSemanticKernelAsync(
                        apiKey.Trim(),
                        apiModel,
                        finalPrompt,
                        cancellationToken);

                    if (string.IsNullOrWhiteSpace(result))
                    {
                        throw new InvalidOperationException("Gemini 返回文本为空。");
                    }

                    return result.Trim();
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    var context = ParseErrorContext(ex);

                    // 当前模型不存在则尝试下一个候选模型，不在同一模型上继续重试。
                    if (context.StatusCode == 404)
                    {
                        lastError = BuildFriendlyErrorMessage(candidateModel, context, attempt);
                        break;
                    }

                    var canRetry = ShouldRetry(context);
                    if (canRetry && attempt < MaxAttempts)
                    {
                        var delay = GetNextDelay(context.RetryDelay, attempt);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    throw new InvalidOperationException(
                        BuildFriendlyErrorMessage(candidateModel, context, attempt),
                        ex);
                }
            }
        }

        throw new InvalidOperationException(lastError ?? "Gemini 调用失败：达到最大重试次数。");
    }

    private static IReadOnlyList<string> BuildModelCandidates(string model)
    {
        var candidates = new List<string>();
        var normalized = NormalizeModelId(model);
        AddIfNotExists(candidates, normalized);
        AddIfNotExists(candidates, ToAlternateModelId(normalized));

        // 默认优先 gemini 3 flash（用户输入格式），请求时会自动映射为 API 模型 ID。
        if (normalized.Equals("gemini 3 flash", StringComparison.OrdinalIgnoreCase))
        {
            AddIfNotExists(candidates, "gemini 2.5 flash");

            AddIfNotExists(candidates, "gemini 2.0 flash");
     
        }

        return candidates;
    }

    private static void AddIfNotExists(ICollection<string> candidates, string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return;
        }

        if (candidates.Contains(model, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        candidates.Add(model.Trim());
    }

    private static string NormalizeModelId(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return "gemini 3 flash";
        }

        var compact = Regex.Replace(model.Trim().ToLowerInvariant(), @"\s+", " ");
        return compact switch
        {
            "gemini-3-flash" => "gemini 3 flash",
            "gemini-2.5-flash" => "gemini 2.5 flash",
            "gemini-2-flash" => "gemini 2.0 flash",
            "gemini-2.0-flash" => "gemini 2.0 flash",
            _ => compact.Replace("-", " ")
        };
    }

    private static string ToAlternateModelId(string model)
    {
        // 约束：用户输入模型名统一小写且不含 '-'，不再生成连字符候选。
        return NormalizeModelId(model);
    }

    private static string ToApiModelId(string userModel)
    {
        if (string.IsNullOrWhiteSpace(userModel))
        {
            return "gemini-3-flash";
        }

        // Gemini OpenAI 兼容接口需要 API 模型 ID，这里把用户输入格式转为接口格式。
        var normalized = NormalizeModelId(userModel);
        return normalized.Replace(" ", "-");
    }

    private static async Task<string> AskViaSemanticKernelAsync(
        string apiKey,
        string model,
        string prompt,
        CancellationToken cancellationToken)
    {
        var builder = Kernel.CreateBuilder();
        // 通过 Gemini 的 OpenAI 兼容端点接入 SK（稳定连接器版本）。
#pragma warning disable SKEXP0010
        builder.AddOpenAIChatCompletion(
            modelId: model,
            apiKey: apiKey,
            endpoint: new Uri("https://generativelanguage.googleapis.com/v1beta/openai/"));
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

    private static bool ShouldRetry(GeminiErrorContext context)
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

    private static string BuildFriendlyErrorMessage(string model, GeminiErrorContext context, int attempts)
    {
        if (context.StatusCode == 429)
        {
            var retryText = TryFormatRetryDelay(context.RetryDelay);
            if (context.IsQuotaZero)
            {
                return $"Gemini 配额不足：当前 Key/项目在模型 {model} 上可用额度为 0。请检查计费与配额设置。{retryText}";
            }

            return $"Gemini 请求过于频繁（429）。已重试 {attempts} 次。{retryText}";
        }

        if (context.StatusCode == 401 || context.StatusCode == 403)
        {
            return $"Gemini 鉴权失败（HTTP {context.StatusCode}）。请检查 API Key 是否正确、是否启用 Gemini API、是否有权限访问模型 {model}。";
        }

        if (context.StatusCode == 404)
        {
            return $"Gemini 资源不存在（404）。请检查模型名是否可用：当前为 {model}。";
        }

        if (context.StatusCode == 400)
        {
            var detail = string.IsNullOrWhiteSpace(context.Message) ? string.Empty : $" 详情：{context.Message}";
            return $"Gemini 请求参数错误（400）。已按输入模型 {model} 自动转换为 API 模型 ID 后调用；请检查该模型是否在你的项目中可用，或缩短 Prompt/OCR 文本重试。{detail}";
        }

        if (!string.IsNullOrWhiteSpace(context.Message))
        {
            return $"Gemini 调用失败：{context.Message}";
        }

        if (context.StatusCode.HasValue)
        {
            return $"Gemini 调用失败：HTTP {context.StatusCode.Value}";
        }

        return "Gemini 调用失败。";
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

    private static GeminiErrorContext ParseErrorContext(Exception ex)
    {
        var flattened = FlattenException(ex);
        var raw = flattened.Raw;
        var message = SelectBestMessage(flattened.Messages);

        var statusCode = ParseStatusCode(raw, message);
        var retryDelay = ParseRetryDelay(raw);
        var isQuotaZero = raw.Contains("limit: 0", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase);

        var shortMessage = ExtractFirstLine(message);
        return new GeminiErrorContext
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

        // 过滤过于泛化的外层异常提示，尽量展示底层真实原因。
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

    private sealed class GeminiErrorContext
    {
        public int? StatusCode { get; init; }
        public TimeSpan? RetryDelay { get; init; }
        public bool IsQuotaZero { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
