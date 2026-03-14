namespace ToolBox.Services.Ai;

public sealed class GeminiApiKeyService : IGeminiApiKeyService
{
    private const string GeminiApiKeyName = "gemini_api_key";

    public async Task<string?> GetApiKeyAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(GeminiApiKeyName);
        }
        catch
        {
            // 某些平台 SecureStorage 不可用时，降级到 Preferences。
            return Preferences.Default.Get(GeminiApiKeyName, string.Empty);
        }
    }

    public async Task SaveApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API Key 不能为空。", nameof(apiKey));
        }

        var value = apiKey.Trim();
        try
        {
            await SecureStorage.Default.SetAsync(GeminiApiKeyName, value);
        }
        catch
        {
            Preferences.Default.Set(GeminiApiKeyName, value);
        }
    }

    public async Task ClearApiKeyAsync()
    {
        try
        {
            SecureStorage.Default.Remove(GeminiApiKeyName);
            await Task.CompletedTask;
        }
        catch
        {
            Preferences.Default.Remove(GeminiApiKeyName);
        }
    }
}
