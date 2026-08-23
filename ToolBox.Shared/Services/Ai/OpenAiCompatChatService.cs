using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommonHelp;

namespace ToolBox.Services.Ai;

public sealed class OpenAiCompatChatService(IHttpService httpService) : IAiChatService
{
    private static readonly Regex ModelIdRegex = new("^[a-zA-Z0-9][a-zA-Z0-9._:/-]{0,127}$", RegexOptions.Compiled);

    public async Task<string> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new InvalidOperationException($"{AiProviderCatalog.GetDisplayName(request.Provider)} API Key 未配置。");

        if (string.IsNullOrWhiteSpace(request.UserPrompt))
            throw new InvalidOperationException("用户提示不能为空。");

        if (!AiProviderCatalog.UsesOpenAiCompatChat(request.Provider))
            throw new InvalidOperationException($"{AiProviderCatalog.GetDisplayName(request.Provider)} 不支持 OpenAI 兼容调用。");

        var imageUrls = request.GetImageUrls();
        var hasImage = imageUrls.Count > 0;
        if (hasImage && !AiProviderCatalog.SupportsVision(request.Provider))
        {
            throw new InvalidOperationException(
                $"{AiProviderCatalog.GetDisplayName(request.Provider)} 不支持图片输入，请改用 Kimi / OpenRouter / Cursor，或上传文本文件。");
        }

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? AiProviderCatalog.GetDefaultModel(request.Provider)
            : request.Model.Trim();
        ValidateModelId(model);

        var messages = BuildMessages(request, imageUrls);
        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["temperature"] = 0,
            ["messages"] = messages
        };

        if (request.JsonOnly && !request.JsonArrayOutput && request.Provider != AiProviderKind.DeepSeek)
            payload["response_format"] = new { type = "json_object" };

        using var response = await httpService.SendAsync(
            HttpMethod.Post,
            AiProviderCatalog.GetChatCompletionsEndpoint(request.Provider).ToString(),
            JsonSerializer.Serialize(payload),
            timeoutSeconds: imageUrls.Count > 1 ? 5 * 60 : 3 * 60,
            action: headers =>
            {
                headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey.Trim());
                ApplyProviderHeaders(headers, request.Provider);
            },
            cancellationToken: cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(HttpErrorHelp.FormatFailure("AI 调用失败", response.StatusCode, responseText));

        var content = TryExtractAssistantText(responseText);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("AI 返回为空，请检查模型是否支持当前任务。");

        return content.Trim();
    }

    private static List<object> BuildMessages(AiChatRequest request, IReadOnlyList<string> imageUrls)
    {
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new { role = "system", content = request.SystemPrompt.Trim() });

        if (imageUrls.Count == 0)
        {
            messages.Add(new { role = "user", content = request.UserPrompt.Trim() });
            return messages;
        }

        var content = new List<object>
        {
            new { type = "text", text = request.UserPrompt.Trim() }
        };
        foreach (var imageUrl in imageUrls)
            content.Add(new { type = "image_url", image_url = new { url = imageUrl } });

        messages.Add(new { role = "user", content });
        return messages;
    }

    private static void ApplyProviderHeaders(HttpRequestHeaders headers, AiProviderKind provider)
    {
        if (provider != AiProviderKind.OpenRouter)
            return;

        headers.TryAddWithoutValidation("HTTP-Referer", "https://toolbox.local");
        headers.TryAddWithoutValidation("X-Title", "ToolBox");
    }

    private static void ValidateModelId(string model)
    {
        if (!ModelIdRegex.IsMatch(model))
        {
            throw new InvalidOperationException(
                $"模型名不符合规范：{model}。示例：gpt-4o-mini、deepseek-v4-flash、moonshotai/kimi-k2.6、kimi-k2.6");
        }
    }

    internal static string TryExtractAssistantText(string responseText)
    {
        using var doc = JsonDocument.Parse(responseText);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
            return string.Empty;

        if (choices.GetArrayLength() == 0)
            return string.Empty;

        var first = choices[0];
        if (!first.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object)
            return string.Empty;

        if (!message.TryGetProperty("content", out var contentNode))
            return string.Empty;

        if (contentNode.ValueKind == JsonValueKind.String)
            return contentNode.GetString() ?? string.Empty;

        if (contentNode.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var chunks = new List<string>();
        foreach (var item in contentNode.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            if (item.TryGetProperty("type", out var typeNode) &&
                typeNode.ValueKind == JsonValueKind.String &&
                string.Equals(typeNode.GetString(), "text", StringComparison.OrdinalIgnoreCase) &&
                item.TryGetProperty("text", out var textNode) &&
                textNode.ValueKind == JsonValueKind.String)
            {
                var text = textNode.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    chunks.Add(text);
            }
        }

        return string.Join(Environment.NewLine, chunks);
    }
}
