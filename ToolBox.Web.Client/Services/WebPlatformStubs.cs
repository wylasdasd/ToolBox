using ToolBox.Services.Ai;
using ToolBox.Services.DirectorySync;
using ToolBox.Services.Ocr;
using ToolBox.Services.Picker;

namespace ToolBox.Web.Client.Services;

internal static class WebPlatformUnsupported
{
    private const string Message = "Web 版暂未开放此功能，请使用桌面版 ToolBox。";

    public static NotSupportedException Ex() => new(Message);
}

public sealed class UnsupportedImageOcrService : IImageOcrService
{
    public Task<string> RecognizeTextAsync(Stream imageStream, string? languageTag = null, OcrCropOptions? cropOptions = null, CancellationToken cancellationToken = default)
        => throw WebPlatformUnsupported.Ex();
}

public sealed class UnsupportedImagePickerService : IImagePickerService
{
    public Task<string?> PickImageAsync(CancellationToken cancellationToken = default)
        => throw WebPlatformUnsupported.Ex();
}

public sealed class UnsupportedFolderPickerService : IFolderPickerService
{
    public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
        => throw WebPlatformUnsupported.Ex();
}

public sealed class UnsupportedDirectorySyncService : IDirectorySyncService
{
    public Task<DirectorySyncPreview> PreviewAsync(string sourcePath, string targetPath, bool deleteExtraFiles, CancellationToken cancellationToken = default)
        => throw WebPlatformUnsupported.Ex();

    public Task<DirectorySyncResult> SyncAsync(string sourcePath, string targetPath, bool deleteExtraFiles, IProgress<DirectorySyncProgress>? progress = null, CancellationToken cancellationToken = default)
        => throw WebPlatformUnsupported.Ex();
}

public sealed class UnsupportedAiApiKeyService : IAiApiKeyService
{
    public Task<string?> GetApiKeyAsync(AiProviderKind provider) => throw WebPlatformUnsupported.Ex();
    public Task SaveApiKeyAsync(AiProviderKind provider, string apiKey) => throw WebPlatformUnsupported.Ex();
    public Task ClearApiKeyAsync(AiProviderKind provider) => throw WebPlatformUnsupported.Ex();
}

public sealed class UnsupportedAiAskService : IAiAskService
{
    public Task<string> AskByOcrAsync(AiAskRequest request, CancellationToken cancellationToken = default)
        => throw WebPlatformUnsupported.Ex();
}

public sealed class UnsupportedAiOcrService : IAiOcrService
{
    public Task<string> RecognizeImageTextAsync(AiOcrRequest request, CancellationToken cancellationToken = default)
        => throw WebPlatformUnsupported.Ex();
}