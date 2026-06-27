namespace ToolBox.Services.DirectorySync;

public interface IDirectorySyncService
{
    Task<DirectorySyncPreview> PreviewAsync(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        CancellationToken cancellationToken = default);

    Task<DirectorySyncResult> SyncAsync(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        IProgress<DirectorySyncProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
