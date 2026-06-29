using CommonTool.FileHelps;
using ToolBox.Tools.DirectorySync;

namespace ToolBox.Services.DirectorySync;

public sealed class DirectorySyncService : IDirectorySyncService
{
    public Task<DirectorySyncPreview> PreviewAsync(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        CancellationToken cancellationToken = default)
    {
        EnsureWindowsSupported();
        var plan = BuildPlan(sourcePath, targetPath, deleteExtraFiles, cancellationToken);
        return Task.FromResult(plan.ToPreview());
    }

    public async Task<DirectorySyncResult> SyncAsync(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        IProgress<DirectorySyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EnsureWindowsSupported();
        var startedAt = DateTime.Now;
        var plan = BuildPlan(sourcePath, targetPath, deleteExtraFiles, cancellationToken);
        var copiedCount = 0;
        var deletedCount = 0;
        long copiedBytes = 0;

        foreach (var directory in plan.DirectoriesToEnsure)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(directory);
        }

        foreach (var file in plan.FilesToCopy)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FileHelp.EnsureDirectoryExists(file.TargetPath);

            await using var sourceStream = new FileStream(
                file.SourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var targetStream = new FileStream(
                file.TargetPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await sourceStream.CopyToAsync(targetStream, 81920, cancellationToken);
            await targetStream.FlushAsync(cancellationToken);

            File.SetLastWriteTimeUtc(file.TargetPath, file.LastWriteTimeUtc);
            copiedCount++;
            copiedBytes += file.Length;

            progress?.Report(new DirectorySyncProgress(
                copiedCount,
                plan.FilesToCopy.Count,
                deletedCount,
                plan.FilesToDelete.Count,
                file.RelativePath,
                copiedBytes));
        }

        foreach (var file in plan.FilesToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(file.TargetPath))
            {
                File.Delete(file.TargetPath);
                deletedCount++;
                progress?.Report(new DirectorySyncProgress(
                    copiedCount,
                    plan.FilesToCopy.Count,
                    deletedCount,
                    plan.FilesToDelete.Count,
                    file.RelativePath,
                    copiedBytes));
            }
        }

        foreach (var directory in plan.DirectoriesToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: false);
            }
        }

        return new DirectorySyncResult
        {
            SourcePath = plan.SourcePath,
            TargetPath = plan.TargetPath,
            DeleteExtraFiles = deleteExtraFiles,
            FilesCopied = copiedCount,
            FilesDeleted = deletedCount,
            BytesCopied = copiedBytes,
            StartedAt = startedAt,
            FinishedAt = DateTime.Now
        };
    }

    private static DirectorySyncPlan BuildPlan(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(NormalizePathForExistenceCheck(sourcePath)))
            throw new DirectoryNotFoundException($"源目录不存在：{Path.GetFullPath(sourcePath.Trim())}");

        Directory.CreateDirectory(NormalizePathForExistenceCheck(targetPath));

        var sourceFiles = EnumerateFileMap(sourcePath, cancellationToken);
        var targetFiles = EnumerateFileMap(targetPath, cancellationToken);

        var sourceEntries = ToFileEntries(sourceFiles);
        var targetEntries = ToFileEntries(targetFiles);

        var result = DirectorySyncPlanService.BuildPlan(
            sourcePath,
            targetPath,
            deleteExtraFiles,
            sourceEntries,
            targetEntries);

        if (!result.Success || result.Value is null)
            throw new InvalidOperationException(result.Error ?? "无法生成同步计划。");

        var plan = result.Value;
        if (!deleteExtraFiles)
            return DirectorySyncPlan.FromResult(plan);

        var normalizedTarget = plan.TargetPath;
        var targetRelativeDirs = Directory
            .EnumerateDirectories(normalizedTarget, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(normalizedTarget, path));

        cancellationToken.ThrowIfCancellationRequested();

        var directoriesToDelete = DirectorySyncPlanService.ComputeDirectoriesForDeletion(
            normalizedTarget,
            sourceFiles.Keys,
            targetRelativeDirs);

        return DirectorySyncPlan.FromResult(
            DirectorySyncPlanService.WithDirectoriesToDelete(plan, directoriesToDelete));
    }

    private static Dictionary<string, DirectorySyncFileEntry> ToFileEntries(Dictionary<string, FileInfo> files) =>
        files.ToDictionary(
            pair => pair.Key,
            pair => new DirectorySyncFileEntry(
                pair.Value.Length,
                pair.Value.LastWriteTimeUtc,
                pair.Value.FullName),
            StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, FileInfo> EnumerateFileMap(string rootPath, CancellationToken cancellationToken)
    {
        var normalizedRoot = NormalizePathForExistenceCheck(rootPath);
        var files = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in Directory.EnumerateFiles(normalizedRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(normalizedRoot, filePath);
            files[relativePath] = new FileInfo(filePath);
        }

        return files;
    }

    private static string NormalizePathForExistenceCheck(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("目录路径不能为空。", nameof(path));

        return Path.GetFullPath(path.Trim());
    }

    private static void EnsureWindowsSupported()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("目录同步当前仅支持 Windows。");
    }
}

internal sealed record DirectorySyncPlan(
    string SourcePath,
    string TargetPath,
    int SourceFileCount,
    int TargetFileCount,
    List<DirectorySyncFilePlan> FilesToCopy,
    List<DirectorySyncFilePlan> FilesToDelete,
    List<string> DirectoriesToEnsure,
    List<string> DirectoriesToDelete)
{
    public static DirectorySyncPlan FromResult(DirectorySyncPlanResult result) => new(
        result.SourcePath,
        result.TargetPath,
        result.SourceFileCount,
        result.TargetFileCount,
        result.FilesToCopy.ToList(),
        result.FilesToDelete.ToList(),
        result.DirectoriesToEnsure.ToList(),
        result.DirectoriesToDelete.ToList());

    public DirectorySyncPreview ToPreview() => new()
    {
        SourcePath = SourcePath,
        TargetPath = TargetPath,
        SourceFileCount = SourceFileCount,
        TargetFileCount = TargetFileCount,
        FilesToCopyCount = FilesToCopy.Count,
        FilesToDeleteCount = FilesToDelete.Count,
        EstimatedCopyBytes = FilesToCopy.Sum(x => x.Length),
        DeleteExtraFiles = FilesToDelete.Count > 0
    };
}
