using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ToolBox.Services.Ai;

public sealed class CursorCloudAgentChatService
{
    private static readonly Regex DataUrlRegex = new(
        @"^data:(?<mime>[^;]+);base64,(?<data>.+)$",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "FINISHED", "ERROR", "CANCELLED", "EXPIRED"
    };

    public async Task<string> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new InvalidOperationException("Cursor API Key 未配置。");

        if (string.IsNullOrWhiteSpace(request.UserPrompt))
            throw new InvalidOperationException("用户提示不能为空。");

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? AiProviderCatalog.GetDefaultModel(AiProviderKind.Cursor)
            : request.Model.Trim();

        var promptText = BuildPromptText(request);
        var payload = new Dictionary<string, object?>
        {
            ["prompt"] = BuildPromptPayload(request, promptText),
            ["model"] = new { id = model }
        };

        using var httpClient = CreateHttpClient(request.ApiKey);
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.cursor.com/v1/agents");
        createRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var createResponse = await httpClient.SendAsync(createRequest, cancellationToken);
        var createText = await createResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
            throw new InvalidOperationException(BuildHttpErrorMessage(createResponse.StatusCode, createText));

        var (agentId, runId) = ParseCreateResponse(createText);
        return await PollRunResultAsync(httpClient, agentId, runId, cancellationToken);
    }

    private static HttpClient CreateHttpClient(string apiKey)
    {
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        return httpClient;
    }

    private static string BuildPromptText(AiChatRequest request)
    {
        var chunks = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            chunks.Add(request.SystemPrompt.Trim());

        if (request.JsonOnly)
        {
            chunks.Add(request.JsonArrayOutput
                ? "只输出一个合法 JSON 数组，不要 Markdown 代码块，不要额外说明。"
                : "只输出一个合法 JSON 对象，不要 Markdown 代码块，不要额外说明。");
        }

        chunks.Add(request.UserPrompt.Trim());
        return string.Join("\n\n", chunks);
    }

    private static object BuildPromptPayload(AiChatRequest request, string promptText)
    {
        var imageUrls = request.ImageDataUrls is { Count: > 0 }
            ? request.ImageDataUrls.Where(url => !string.IsNullOrWhiteSpace(url)).ToList()
            : string.IsNullOrWhiteSpace(request.ImageDataUrl)
                ? []
                : new List<string> { request.ImageDataUrl };

        if (imageUrls.Count == 0)
            return new { text = promptText };

        var images = new List<object>();
        foreach (var imageUrl in imageUrls)
        {
            var match = DataUrlRegex.Match(imageUrl.Trim());
            if (!match.Success)
                throw new InvalidOperationException("图片格式无效，无法发送给 Cursor。");

            images.Add(new
            {
                data = match.Groups["data"].Value,
                mimeType = match.Groups["mime"].Value
            });
        }

        return new { text = promptText, images };
    }

    private static (string AgentId, string RunId) ParseCreateResponse(string responseText)
    {
        using var doc = JsonDocument.Parse(responseText);
        var root = doc.RootElement;

        if (!root.TryGetProperty("agent", out var agent) ||
            !agent.TryGetProperty("id", out var agentIdNode) ||
            agentIdNode.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("Cursor 创建 Agent 失败：响应缺少 agent.id。");
        }

        string? runId = null;
        if (root.TryGetProperty("run", out var run) &&
            run.TryGetProperty("id", out var runIdNode) &&
            runIdNode.ValueKind == JsonValueKind.String)
        {
            runId = runIdNode.GetString();
        }
        else if (agent.TryGetProperty("latestRunId", out var latestRunIdNode) &&
                 latestRunIdNode.ValueKind == JsonValueKind.String)
        {
            runId = latestRunIdNode.GetString();
        }

        if (string.IsNullOrWhiteSpace(runId))
            throw new InvalidOperationException("Cursor 创建 Agent 失败：响应缺少 run.id。");

        return (agentIdNode.GetString()!, runId);
    }

    private static async Task<string> PollRunResultAsync(
        HttpClient httpClient,
        string agentId,
        string runId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddMinutes(4);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var getRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.cursor.com/v1/agents/{agentId}/runs/{runId}");

            using var getResponse = await httpClient.SendAsync(getRequest, cancellationToken);
            var getText = await getResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!getResponse.IsSuccessStatusCode)
                throw new InvalidOperationException(BuildHttpErrorMessage(getResponse.StatusCode, getText));

            using var doc = JsonDocument.Parse(getText);
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var statusNode) && statusNode.ValueKind == JsonValueKind.String
                ? statusNode.GetString() ?? string.Empty
                : string.Empty;

            if (!TerminalStatuses.Contains(status))
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                continue;
            }

            if (string.Equals(status, "FINISHED", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("result", out var resultNode) &&
                    resultNode.ValueKind == JsonValueKind.String)
                {
                    var result = resultNode.GetString();
                    if (!string.IsNullOrWhiteSpace(result))
                        return result.Trim();
                }

                throw new InvalidOperationException("Cursor 任务完成但返回为空。");
            }

            throw new InvalidOperationException($"Cursor 任务失败：{status}。");
        }

        throw new InvalidOperationException("Cursor 任务超时，请稍后重试。");
    }

    private static string BuildHttpErrorMessage(System.Net.HttpStatusCode code, string responseText)
    {
        var detail = TryExtractErrorMessage(responseText);
        return string.IsNullOrWhiteSpace(detail)
            ? $"Cursor 调用失败：HTTP {(int)code}。"
            : $"Cursor 调用失败：HTTP {(int)code}，{detail}";
    }

    private static string TryExtractErrorMessage(string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString() ?? string.Empty;
            }

            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                    return error.GetString() ?? string.Empty;

                if (error.ValueKind == JsonValueKind.Object &&
                    error.TryGetProperty("message", out var nestedMessage) &&
                    nestedMessage.ValueKind == JsonValueKind.String)
                {
                    return nestedMessage.GetString() ?? string.Empty;
                }
            }
        }
        catch
        {
        }

        return string.Empty;
    }
}
