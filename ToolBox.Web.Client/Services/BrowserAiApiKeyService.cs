using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using ToolBox.Services.Ai;

namespace ToolBox.Web.Client.Services;

public sealed class BrowserAiApiKeyService(
    IJSRuntime jsRuntime,
    IWebAssemblyHostEnvironment hostEnvironment) : IAiApiKeyService
{
    private static string StorageKey(AiProviderKind provider) =>
        $"toolbox_{AiProviderCatalog.GetApiKeyStorageName(provider)}";

    private string OriginContext => hostEnvironment.BaseAddress;

    public async Task<string?> GetApiKeyAsync(AiProviderKind provider)
    {
        try
        {
            var stored = await jsRuntime.InvokeAsync<string?>("toolboxAiKeys.get", StorageKey(provider));
            if (stored == null)
                return null;

            if (!AiKeyLocalStorageCrypto.TryDecrypt(stored, OriginContext, out var plaintext) || plaintext == null)
                return null;

            return plaintext;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveApiKeyAsync(AiProviderKind provider, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API Key 不能为空。", nameof(apiKey));

        var encrypted = AiKeyLocalStorageCrypto.Encrypt(apiKey.Trim(), OriginContext);
        await jsRuntime.InvokeVoidAsync("toolboxAiKeys.set", StorageKey(provider), encrypted);
    }

    public async Task ClearApiKeyAsync(AiProviderKind provider)
    {
        await jsRuntime.InvokeVoidAsync("toolboxAiKeys.remove", StorageKey(provider));
    }
}
