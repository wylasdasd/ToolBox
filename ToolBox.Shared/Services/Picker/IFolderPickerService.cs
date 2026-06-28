namespace ToolBox.Services.Picker;

public interface IFolderPickerService
{
    bool IsNativeSupported { get; }

    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
}
