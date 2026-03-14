namespace ToolBox.Services.Picker;

public interface IImagePickerService
{
    Task<string?> PickImageAsync(CancellationToken cancellationToken = default);
}
