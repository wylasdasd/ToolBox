using Microsoft.JSInterop;
using ToolBox.Services;

namespace ToolBox.Web.Client.Services;

public sealed class BrowserClipboardService(IJSRuntime jsRuntime) : IClipboardService
{
    public async Task<string?> GetTextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try { return await jsRuntime.InvokeAsync<string>("eval", "navigator.clipboard.readText()"); }
        catch { return null; }
    }

    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return jsRuntime.InvokeVoidAsync("eval", $"navigator.clipboard.writeText({System.Text.Json.JsonSerializer.Serialize(text)})").AsTask();
    }
}