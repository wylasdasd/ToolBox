using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ToolBox.Services.Ai;

public sealed class OpenAiCompatAiOcrService : IAiOcrService
{
    private static readonly Regex ModelIdRegex = new("^[a-zA-Z0-9][a-zA-Z0-9._:-]{0,127}$", RegexOptions.Compiled);
    private static readonly Uri OpenAiDefaultEndpoint = new("https://api.openai.com/v1/");

    public async Task<string> RecognizeImageTextAsync(AiOcrRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (!AiProviderCatalog.SupportsImageInChatCompletions(request.Provider))
        {
            var providerName = AiProviderCatalog.GetDisplayName(request.Provider);
            throw new InvalidOperationException(
                $"{providerName} 当前 OpenAI 兼容 Chat Completions 接口不支持图片输入（`image_url`）。" +
                "请改用 OpenAI/Gemini 的可视模型，或使用 Windows OCR + AI 分析页面。");
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new InvalidOperationException($"{AiProviderCatalog.GetDisplayName(request.Provider)} API Key 未配置。");
        }

        if (string.IsNullOrWhiteSpace(request.ImageDataUrl))
        {
            throw new InvalidOperationException("图片数据为空，无法进行 OCR。");
        }

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? AiProviderCatalog.GetDefaultModel(request.Provider)
            : request.Model.Trim();
        ValidateModelId(model);

        var prompt = string.IsNullOrWhiteSpace(request.Prompt)
            ? "请提取图片中的全部可读文本，保持原始换行。"
            : request.Prompt.Trim();

        var endpointRoot = AiProviderCatalog.GetEndpoint(request.Provider) ?? OpenAiDefaultEndpoint;
        var chatCompletionsUri = new Uri(endpointRoot, "chat/completions");
        var payload = new
        {
            model,
            temperature = 0,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = request.ImageDataUrl }
                        }
                    }
                }
            }
        };

        using var httpClient = new HttpClient();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, chatCompletionsUri);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey.Trim());
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(BuildHttpErrorMessage(response.StatusCode, responseText));
        }

        var content = TryExtractAssistantText(responseText);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("AI OCR 返回为空，请更换模型后重试。");
        }

        return content.Trim();
    }

    private static void ValidateModelId(string model)
    {
        if (!ModelIdRegex.IsMatch(model))
        {
            throw new InvalidOperationException(
                $"模型名不符合 OpenAI Chat Completion 规范：{model}。示例：gpt-4.1-mini、deepseek-chat、gemini-2.5-flash。");
        }
    }

    private static string BuildHttpErrorMessage(System.Net.HttpStatusCode code, string responseText)
    {
        var detail = TryExtractErrorMessage(responseText);
        if (string.IsNullOrWhiteSpace(detail))
        {
            return $"AI OCR 调用失败：HTTP {(int)code}。";
        }

        return $"AI OCR 调用失败：HTTP {(int)code}，{detail}";
    }

    private static string TryExtractErrorMessage(string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.Object &&
                error.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // ignore parse failure
        }

        return string.Empty;
    }

    private static string TryExtractAssistantText(string responseText)
    {
        using var doc = JsonDocument.Parse(responseText);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        if (choices.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var first = choices[0];
        if (!first.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        if (!message.TryGetProperty("content", out var contentNode))
        {
            return string.Empty;
        }

        if (contentNode.ValueKind == JsonValueKind.String)
        {
            return contentNode.GetString() ?? string.Empty;
        }

        if (contentNode.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var chunks = new List<string>();
        foreach (var item in contentNode.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (item.TryGetProperty("type", out var typeNode) &&
                typeNode.ValueKind == JsonValueKind.String &&
                string.Equals(typeNode.GetString(), "text", StringComparison.OrdinalIgnoreCase) &&
                item.TryGetProperty("text", out var textNode) &&
                textNode.ValueKind == JsonValueKind.String)
            {
                var text = textNode.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    chunks.Add(text);
                }
            }
        }

        return string.Join(Environment.NewLine, chunks);
    }
}
