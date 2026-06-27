namespace ToolBox.Services.Ocr;

public interface IImageOcrService
{
    Task<string> RecognizeTextAsync(
        Stream imageStream,
        string? languageTag = null,
        OcrCropOptions? cropOptions = null,
        CancellationToken cancellationToken = default);
}
