using ToolBox.Services.DirectorySync;

namespace ToolBox.Web.Client.Services;

public sealed class UnsupportedDirectorySyncService : IDirectorySyncService
{
    private const string Message = "目录同步仅支持 ToolBox 桌面版（MAUI），Web 端不可用。";

    public Task<DirectorySyncPreview> PreviewAsync(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        CancellationToken cancellationToken = default) =>
        throw new PlatformNotSupportedException(Message);

    public Task<DirectorySyncResult> SyncAsync(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        IProgress<DirectorySyncProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        throw new PlatformNotSupportedException(Message);
}
