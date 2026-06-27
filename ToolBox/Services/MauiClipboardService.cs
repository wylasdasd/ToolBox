using Microsoft.Maui.ApplicationModel.DataTransfer;
using ToolBox.Services;

namespace ToolBox.Services.MauiPlatform;

public sealed class MauiClipboardService : IClipboardService
{
    public async Task<string?> GetTextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Clipboard.Default.GetTextAsync();
    }

    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Clipboard.Default.SetTextAsync(text);
    }
}