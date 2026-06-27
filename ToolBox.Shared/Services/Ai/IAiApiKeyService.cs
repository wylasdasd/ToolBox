namespace ToolBox.Services.Ai;

public interface IAiApiKeyService
{
    Task<string?> GetApiKeyAsync(AiProviderKind provider);
    Task SaveApiKeyAsync(AiProviderKind provider, string apiKey);
    Task ClearApiKeyAsync(AiProviderKind provider);
}
