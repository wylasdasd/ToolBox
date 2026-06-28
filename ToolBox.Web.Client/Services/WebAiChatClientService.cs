using System.Net.Http.Json;
using System.Text.Json;
using ToolBox.Services.Ai;

namespace ToolBox.Web.Client.Services;

public sealed class WebAiChatClientService(HttpClient httpClient) : IAiChatService
{
    public async Task<string> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/ai/complete", request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = AiHttpErrorHelp.TryExtractMessage(body);
            throw new InvalidOperationException(detail ?? $"AI 代理调用失败：HTTP {(int)response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            return content.GetString() ?? string.Empty;

        throw new InvalidOperationException("AI 代理返回格式无效。");
    }
}
