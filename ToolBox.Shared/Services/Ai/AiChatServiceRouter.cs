namespace ToolBox.Services.Ai;

public sealed class AiChatServiceRouter(
    OpenAiCompatChatService openAiCompatChatService,
    CursorCloudAgentChatService cursorCloudAgentChatService) : IAiChatService
{
    public Task<string> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default) =>
        request.Provider switch
        {
            AiProviderKind.Cursor => cursorCloudAgentChatService.CompleteAsync(request, cancellationToken),
            _ when AiProviderCatalog.UsesOpenAiCompatChat(request.Provider) =>
                openAiCompatChatService.CompleteAsync(request, cancellationToken),
            _ => throw new InvalidOperationException($"不支持的 AI 提供商：{request.Provider}")
        };
}
