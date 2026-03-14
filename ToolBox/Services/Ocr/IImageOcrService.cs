namespace ToolBox.Services.Ocr;

public interface IImageOcrService
{
    Task<string> RecognizeTextAsync(
        Stream imageStream,
        string? languageTag = null,
        CancellationToken cancellationToken = default);
}
