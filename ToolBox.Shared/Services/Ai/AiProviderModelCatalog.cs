namespace ToolBox.Services.Ai;

public sealed record AiModelSuggestion(string Id, string Description);

public static class AiProviderModelCatalog
{
    private static readonly IReadOnlyDictionary<AiProviderKind, IReadOnlyList<AiModelSuggestion>> Catalog =
        new Dictionary<AiProviderKind, IReadOnlyList<AiModelSuggestion>>
        {
            [AiProviderKind.DeepSeek] =
            [
                new("deepseek-v4-flash", "默认，快速"),
                new("deepseek-v4-pro", "更强推理"),
            ],
            [AiProviderKind.OpenRouter] =
            [
                new("moonshotai/kimi-k2.6", "默认，Kimi 视觉+文本"),
                new("openai/gpt-4o-mini", "OpenAI 轻量视觉"),
                new("openai/gpt-4o", "OpenAI 视觉"),
                new("openai/gpt-4.1", "OpenAI 4.1"),
                new("openai/gpt-5-mini", "OpenAI GPT-5 mini"),
                new("openai/gpt-5.3-chat", "OpenAI GPT-5.3 Chat"),
                new("google/gemini-2.5-flash", "Gemini 2.5 Flash 视觉"),
                new("google/gemini-2.5-pro", "Gemini 2.5 Pro 视觉"),
                new("anthropic/claude-sonnet-4", "Claude Sonnet 4"),
                new("anthropic/claude-sonnet-4.6", "Claude Sonnet 4.6"),
                new("anthropic/claude-opus-4.6", "Claude Opus 4.6"),
                new("anthropic/claude-3.5-sonnet", "Claude 3.5 Sonnet"),
                new("deepseek/deepseek-v4-flash", "DeepSeek 仅文本"),
                new("deepseek/deepseek-v4-pro", "DeepSeek Pro 仅文本"),
                new("meta-llama/llama-4-maverick", "Llama 4 Maverick"),
                new("qwen/qwen3-vl-235b-a22b-instruct", "Qwen3 VL 视觉"),
            ],
            [AiProviderKind.Kimi] =
            [
                new("kimi-k2.6", "默认，支持视觉"),
                new("moonshot-v1-8k", "旧版纯文本"),
                new("moonshot-v1-32k", "旧版纯文本长上下文"),
            ],
            [AiProviderKind.Cursor] =
            [
                new("composer-2.5", "Cursor Composer 2.5"),
                new("composer-2", "Composer 2"),
                new("composer-latest", "Composer 最新别名"),
                new("gpt-5.3-codex", "GPT-5.3 Codex"),
                new("gpt-5.3-codex-high", "GPT-5.3 Codex High"),
                new("gpt-5.3-codex-fast", "GPT-5.3 Codex Fast"),
                new("gpt-5.3-codex-high-fast", "GPT-5.3 Codex High Fast"),
                new("sonnet-4.6", "Claude 4.6 Sonnet"),
                new("sonnet-4.6-thinking", "Claude 4.6 Sonnet 思考"),
                new("opus-4.6", "Claude 4.6 Opus"),
                new("opus-4.6-thinking", "Claude 4.6 Opus 思考"),
                new("claude-4-sonnet-thinking", "Claude Sonnet 思考（旧 ID）"),
                new("sonnet-4.5", "Claude 4.5 Sonnet"),
                new("opus-4.5", "Claude 4.5 Opus"),
            ],
        };

    public static IReadOnlyList<AiModelSuggestion> GetSuggestions(AiProviderKind provider) =>
        Catalog.TryGetValue(provider, out var list) ? list : [];

    public static IEnumerable<string> SearchModelIds(AiProviderKind provider, string? value)
    {
        var suggestions = GetSuggestions(provider).Select(x => x.Id);
        if (string.IsNullOrWhiteSpace(value))
            return suggestions;

        var query = value.Trim();
        var matched = suggestions.Where(id => id.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!matched.Contains(query, StringComparer.OrdinalIgnoreCase))
            matched.Insert(0, query);

        return matched;
    }

    public static string? GetDescription(AiProviderKind provider, string modelId)
    {
        foreach (var item in GetSuggestions(provider))
        {
            if (string.Equals(item.Id, modelId, StringComparison.OrdinalIgnoreCase))
                return item.Description;
        }

        return null;
    }
}
