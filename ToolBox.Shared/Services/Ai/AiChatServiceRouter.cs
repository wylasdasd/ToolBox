namespace ToolBox.Services.Ai;

public sealed class AiChatServiceRouter : IAiChatService
{
    private readonly OpenAiCompatChatService _openAiCompatChatService;
    private readonly CursorCloudAgentChatService _cursorCloudAgentChatService;

    public AiChatServiceRouter(
        OpenAiCompatChatService openAiCompatChatService,
        CursorCloudAgentChatService cursorCloudAgentChatService)
    {
        _openAiCompatChatService = openAiCompatChatService;
        _cursorCloudAgentChatService = cursorCloudAgentChatService;
    }

    public Task<string> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default) =>
        request.Provider switch
        {
            AiProviderKind.Cursor => _cursorCloudAgentChatService.CompleteAsync(request, cancellationToken),
            _ when AiProviderCatalog.UsesOpenAiCompatChat(request.Provider) =>
                _openAiCompatChatService.CompleteAsync(request, cancellationToken),
            _ => throw new InvalidOperationException($"不支持的 AI 提供商：{request.Provider}")
        };
}
