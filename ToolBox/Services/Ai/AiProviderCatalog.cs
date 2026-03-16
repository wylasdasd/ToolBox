namespace ToolBox.Services.Ai;

public static class AiProviderCatalog
{
    public static string GetDisplayName(AiProviderKind provider)
    {
        return provider switch
        {
            AiProviderKind.OpenAI => "OpenAI",
            AiProviderKind.DeepSeek => "DeepSeek",
            AiProviderKind.Gemini => "Gemini",
            _ => provider.ToString()
        };
    }

    public static string GetApiKeyStorageName(AiProviderKind provider)
    {
        return provider switch
        {
            AiProviderKind.OpenAI => "openai_api_key",
            AiProviderKind.DeepSeek => "deepseek_api_key",
            AiProviderKind.Gemini => "gemini_api_key",
            _ => "ai_api_key"
        };
    }

    public static string GetDefaultModel(AiProviderKind provider)
    {
        return provider switch
        {
            AiProviderKind.OpenAI => "gpt-4.1-mini",
            AiProviderKind.DeepSeek => "deepseek-chat",
            AiProviderKind.Gemini => "gemini-2.5-flash",
            _ => "gpt-4.1-mini"
        };
    }

    public static Uri? GetEndpoint(AiProviderKind provider)
    {
        return provider switch
        {
            AiProviderKind.OpenAI => null,
            AiProviderKind.DeepSeek => new Uri("https://api.deepseek.com/v1"),
            AiProviderKind.Gemini => new Uri("https://generativelanguage.googleapis.com/v1beta/openai/"),
            _ => null
        };
    }
}
