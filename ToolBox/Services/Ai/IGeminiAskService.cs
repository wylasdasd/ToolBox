namespace ToolBox.Services.Ai;

public interface IGeminiAskService
{
    Task<string> AskByOcrAsync(string apiKey, string model, string userPrompt, string ocrText, CancellationToken cancellationToken = default);
}
