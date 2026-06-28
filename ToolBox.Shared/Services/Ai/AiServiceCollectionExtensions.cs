using Microsoft.Extensions.DependencyInjection;

namespace ToolBox.Services.Ai;

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// 注册 AI 聊天后端（OpenAI 兼容 + Cursor Agent + 路由）。Web 宿主在 AddToolBoxWeb 之后调用以覆盖 WASM 客户端代理。
    /// </summary>
    public static IServiceCollection AddToolBoxAiChatBackend(this IServiceCollection services)
    {
        services.AddScoped<OpenAiCompatChatService>();
        services.AddScoped<CursorCloudAgentChatService>();
        services.AddScoped<IAiChatService, AiChatServiceRouter>();
        return services;
    }
}
