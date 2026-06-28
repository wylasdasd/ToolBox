namespace ToolBox.Services.Ai;



public static class AiProviderCatalog

{

    public static IReadOnlyList<AiProviderKind> All { get; } =

        [AiProviderKind.OpenRouter, AiProviderKind.DeepSeek, AiProviderKind.Kimi, AiProviderKind.Cursor];



    public static string GetDisplayName(AiProviderKind provider) =>

        provider switch

        {

            AiProviderKind.DeepSeek => "DeepSeek",

            AiProviderKind.OpenRouter => "OpenRouter",

            AiProviderKind.Kimi => "Kimi (Moonshot)",

            AiProviderKind.Cursor => "Cursor",

            _ => provider.ToString()

        };



    public static string GetApiKeyStorageName(AiProviderKind provider) =>

        provider switch

        {

            AiProviderKind.DeepSeek => "deepseek_api_key",

            AiProviderKind.OpenRouter => "openrouter_api_key",

            AiProviderKind.Kimi => "kimi_api_key",

            AiProviderKind.Cursor => "cursor_api_key",

            _ => "ai_api_key"

        };



    public static string GetDefaultModel(AiProviderKind provider) =>

        provider switch

        {

            AiProviderKind.DeepSeek => "deepseek-v4-flash",

            AiProviderKind.OpenRouter => "moonshotai/kimi-k2.6",

            AiProviderKind.Kimi => "kimi-k2.6",

            AiProviderKind.Cursor => "composer-2.5",

            _ => "moonshotai/kimi-k2.6"

        };



    public static Uri GetChatCompletionsEndpoint(AiProviderKind provider) =>

        provider switch

        {

            AiProviderKind.DeepSeek => new Uri("https://api.deepseek.com/v1/chat/completions"),

            AiProviderKind.OpenRouter => new Uri("https://openrouter.ai/api/v1/chat/completions"),

            AiProviderKind.Kimi => new Uri("https://api.moonshot.cn/v1/chat/completions"),

            AiProviderKind.Cursor => new Uri("https://api.cursor.com/v1/agents"),

            _ => new Uri("https://openrouter.ai/api/v1/chat/completions")

        };



    public static bool SupportsVision(AiProviderKind provider) =>

        provider switch

        {

            AiProviderKind.DeepSeek => false,

            _ => true

        };



    public static bool UsesOpenAiCompatChat(AiProviderKind provider) =>

        provider is AiProviderKind.DeepSeek or AiProviderKind.OpenRouter or AiProviderKind.Kimi;

    public static int GetMaxMergeBatchSize(AiProviderKind provider) =>
        provider switch
        {
            AiProviderKind.Cursor => 5,
            _ => 10
        };

}


