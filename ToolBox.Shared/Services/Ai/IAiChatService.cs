namespace ToolBox.Services.Ai;

public interface IAiChatService
{
    Task<string> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}
