namespace ToolBox.Services.Ai;

public interface IAiAskService
{
    Task<string> AskByOcrAsync(AiAskRequest request, CancellationToken cancellationToken = default);
}
