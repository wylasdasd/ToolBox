namespace ToolBox.Services.Ai;

public sealed class AiApiKeyService : IAiApiKeyService
{
    public async Task<string?> GetApiKeyAsync(AiProviderKind provider)
    {
        var keyName = AiProviderCatalog.GetApiKeyStorageName(provider);
        try
        {
            return await SecureStorage.Default.GetAsync(keyName);
        }
        catch
        {
            return Preferences.Default.Get(keyName, string.Empty);
        }
    }

    public async Task SaveApiKeyAsync(AiProviderKind provider, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API Key 不能为空。", nameof(apiKey));
        }

        var keyName = AiProviderCatalog.GetApiKeyStorageName(provider);
        var value = apiKey.Trim();
        try
        {
            await SecureStorage.Default.SetAsync(keyName, value);
        }
        catch
        {
            Preferences.Default.Set(keyName, value);
        }
    }

    public async Task ClearApiKeyAsync(AiProviderKind provider)
    {
        var keyName = AiProviderCatalog.GetApiKeyStorageName(provider);
        try
        {
            SecureStorage.Default.Remove(keyName);
            await Task.CompletedTask;
        }
        catch
        {
            Preferences.Default.Remove(keyName);
        }
    }
}
