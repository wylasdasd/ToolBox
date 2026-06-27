namespace ToolBox.Services.Ai;

public interface IAiOcrService
{
    Task<string> RecognizeImageTextAsync(AiOcrRequest request, CancellationToken cancellationToken = default);
}
