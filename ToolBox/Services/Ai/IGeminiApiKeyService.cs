namespace ToolBox.Services.Ai;

public interface IGeminiApiKeyService
{
    Task<string?> GetApiKeyAsync();
    Task SaveApiKeyAsync(string apiKey);
    Task ClearApiKeyAsync();
}
