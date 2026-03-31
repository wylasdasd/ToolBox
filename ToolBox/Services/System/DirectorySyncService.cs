using CommonTool.FileHelps;

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
        var normalizedSource = NormalizePath(sourcePath);
        var normalizedTarget = NormalizePath(targetPath);
        ValidatePaths(normalizedSource, normalizedTarget);

        if (!Directory.Exists(normalizedSource))
        {
            throw new DirectoryNotFoundException($"源目录不存在：{normalizedSource}");
        }

        Directory.CreateDirectory(normalizedTarget);

        var sourceFiles = EnumerateFileMap(normalizedSource, cancellationToken);
        var targetFiles = EnumerateFileMap(normalizedTarget, cancellationToken);
        var filesToCopy = new List<DirectorySyncFilePlan>();
        var filesToDelete = new List<DirectorySyncFilePlan>();
        var directoriesToEnsure = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            normalizedTarget
        };

        foreach (var pair in sourceFiles.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = pair.Key;
            var sourceFile = pair.Value;
            var targetPathForFile = Path.Combine(normalizedTarget, relativePath);
            directoriesToEnsure.Add(Path.GetDirectoryName(targetPathForFile) ?? normalizedTarget);

            if (!targetFiles.TryGetValue(relativePath, out var targetFile)
                || sourceFile.Length != targetFile.Length
                || sourceFile.LastWriteTimeUtc != targetFile.LastWriteTimeUtc)
            {
                filesToCopy.Add(new DirectorySyncFilePlan(
                    relativePath,
                    sourceFile.FullName,
                    targetPathForFile,
                    sourceFile.Length,
                    sourceFile.LastWriteTimeUtc));
            }
        }

        if (deleteExtraFiles)
        {
            foreach (var pair in targetFiles.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!sourceFiles.ContainsKey(pair.Key))
                {
                    filesToDelete.Add(new DirectorySyncFilePlan(
                        pair.Key,
                        string.Empty,
                        pair.Value.FullName,
                        pair.Value.Length,
                        pair.Value.LastWriteTimeUtc));
                }
            }
        }

        var directoriesToDelete = deleteExtraFiles
            ? EnumerateDirectoriesForDeletion(normalizedTarget, sourceFiles.Keys, cancellationToken)
            : [];

        return new DirectorySyncPlan(
            normalizedSource,
            normalizedTarget,
            sourceFiles.Count,
            targetFiles.Count,
            filesToCopy,
            filesToDelete,
            directoriesToEnsure.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            directoriesToDelete);
    }

    private static Dictionary<string, FileInfo> EnumerateFileMap(string rootPath, CancellationToken cancellationToken)
    {
        var files = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(rootPath, filePath);
            files[relativePath] = new FileInfo(filePath);
        }

        return files;
    }

    private static List<string> EnumerateDirectoriesForDeletion(
        string targetPath,
        IEnumerable<string> sourceRelativeFiles,
        CancellationToken cancellationToken)
    {
        var sourceDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { string.Empty };
        foreach (var relativeFile in sourceRelativeFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativeDirectory = Path.GetDirectoryName(relativeFile) ?? string.Empty;
            while (true)
            {
                sourceDirectories.Add(relativeDirectory);
                if (string.IsNullOrEmpty(relativeDirectory))
                {
                    break;
                }

                relativeDirectory = Path.GetDirectoryName(relativeDirectory) ?? string.Empty;
            }
        }

        return Directory
            .EnumerateDirectories(targetPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(targetPath, path))
            .Where(relativePath => !sourceDirectories.Contains(relativePath))
            .OrderByDescending(path => path.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar))
            .Select(relativePath => Path.Combine(targetPath, relativePath))
            .ToList();
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("目录路径不能为空。", nameof(path));
        }

        return Path.GetFullPath(path.Trim());
    }

    private static void ValidatePaths(string sourcePath, string targetPath)
    {
        if (string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("源目录和目标目录不能相同。");
        }

        if (IsSubPathOf(sourcePath, targetPath) || IsSubPathOf(targetPath, sourcePath))
        {
            throw new InvalidOperationException("源目录和目标目录不能互相嵌套。");
        }
    }

    private static bool IsSubPathOf(string candidateParent, string candidateChild)
    {
        var parent = EnsureTrailingSeparator(candidateParent);
        var child = EnsureTrailingSeparator(candidateChild);
        return child.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static void EnsureWindowsSupported()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("目录同步当前仅支持 Windows。");
        }
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

internal sealed record DirectorySyncFilePlan(
    string RelativePath,
    string SourcePath,
    string TargetPath,
    long Length,
    DateTime LastWriteTimeUtc);

public sealed class DirectorySyncPreview
{
    public string SourcePath { get; init; } = string.Empty;
    public string TargetPath { get; init; } = string.Empty;
    public int SourceFileCount { get; init; }
    public int TargetFileCount { get; init; }
    public int FilesToCopyCount { get; init; }
    public int FilesToDeleteCount { get; init; }
    public long EstimatedCopyBytes { get; init; }
    public bool DeleteExtraFiles { get; init; }
}

public sealed class DirectorySyncResult
{
    public string SourcePath { get; init; } = string.Empty;
    public string TargetPath { get; init; } = string.Empty;
    public bool DeleteExtraFiles { get; init; }
    public int FilesCopied { get; init; }
    public int FilesDeleted { get; init; }
    public long BytesCopied { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime FinishedAt { get; init; }
}

public sealed record DirectorySyncProgress(
    int CopiedFiles,
    int TotalFilesToCopy,
    int DeletedFiles,
    int TotalFilesToDelete,
    string CurrentPath,
    long CopiedBytes);
