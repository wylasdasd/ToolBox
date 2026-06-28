using ToolBox.Services.Picker;

namespace ToolBox.Web.Client.Services;

public sealed class UnsupportedFolderPickerService : IFolderPickerService
{
    public bool IsNativeSupported => false;

    public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
