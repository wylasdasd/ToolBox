using ToolBox.Tools.Common;

namespace ToolBox.Tools.DirectorySync;

public sealed record DirectorySyncFileEntry(long Length, DateTime LastWriteTimeUtc, string FullName = "");

public sealed record DirectorySyncFilePlan(
    string RelativePath,
    string SourcePath,
    string TargetPath,
    long Length,
    DateTime LastWriteTimeUtc);

public sealed record DirectorySyncPlanResult(
    string SourcePath,
    string TargetPath,
    int SourceFileCount,
    int TargetFileCount,
    IReadOnlyList<DirectorySyncFilePlan> FilesToCopy,
    IReadOnlyList<DirectorySyncFilePlan> FilesToDelete,
    IReadOnlyList<string> DirectoriesToEnsure,
    IReadOnlyList<string> DirectoriesToDelete,
    int FilesToCopyCount,
    int FilesToDeleteCount,
    long EstimatedCopyBytes,
    bool DeleteExtraFiles);

public static class DirectorySyncPlanService
{
    public static ToolResult<DirectorySyncPlanResult> BuildPlan(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        IReadOnlyDictionary<string, DirectorySyncFileEntry> sourceFiles,
        IReadOnlyDictionary<string, DirectorySyncFileEntry> targetFiles)
    {
        try
        {
            var normalizedSource = NormalizePath(sourcePath);
            var normalizedTarget = NormalizePath(targetPath);
            ValidatePaths(normalizedSource, normalizedTarget);

            var filesToCopy = new List<DirectorySyncFilePlan>();
            var filesToDelete = new List<DirectorySyncFilePlan>();
            var directoriesToEnsure = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                normalizedTarget
            };

            foreach (var pair in sourceFiles.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
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
                        string.IsNullOrEmpty(sourceFile.FullName)
                            ? Path.Combine(normalizedSource, relativePath)
                            : sourceFile.FullName,
                        targetPathForFile,
                        sourceFile.Length,
                        sourceFile.LastWriteTimeUtc));
                }
            }

            if (deleteExtraFiles)
            {
                foreach (var pair in targetFiles.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (!sourceFiles.ContainsKey(pair.Key))
                    {
                        filesToDelete.Add(new DirectorySyncFilePlan(
                            pair.Key,
                            string.Empty,
                            string.IsNullOrEmpty(pair.Value.FullName)
                                ? Path.Combine(normalizedTarget, pair.Key)
                                : pair.Value.FullName,
                            pair.Value.Length,
                            pair.Value.LastWriteTimeUtc));
                    }
                }
            }

            var plan = new DirectorySyncPlanResult(
                normalizedSource,
                normalizedTarget,
                sourceFiles.Count,
                targetFiles.Count,
                filesToCopy,
                filesToDelete,
                directoriesToEnsure.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
                [],
                filesToCopy.Count,
                filesToDelete.Count,
                filesToCopy.Sum(x => x.Length),
                deleteExtraFiles && filesToDelete.Count > 0);

            return ToolResult<DirectorySyncPlanResult>.Ok(plan);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return ToolResult<DirectorySyncPlanResult>.Fail(ex.Message);
        }
    }

    public static IReadOnlyList<string> ComputeDirectoriesForDeletion(
        string normalizedTarget,
        IEnumerable<string> sourceRelativeFiles,
        IEnumerable<string> targetRelativeDirectories)
    {
        var sourceDirectories = BuildSourceDirectorySet(sourceRelativeFiles);

        return targetRelativeDirectories
            .Where(relativePath => !sourceDirectories.Contains(relativePath))
            .OrderByDescending(path => path.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar))
            .Select(relativePath => Path.Combine(normalizedTarget, relativePath))
            .ToList();
    }

    public static DirectorySyncPlanResult WithDirectoriesToDelete(
        DirectorySyncPlanResult plan,
        IReadOnlyList<string> directoriesToDelete) =>
        plan with { DirectoriesToDelete = directoriesToDelete };

    private static HashSet<string> BuildSourceDirectorySet(IEnumerable<string> sourceRelativeFiles)
    {
        var sourceDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { string.Empty };
        foreach (var relativeFile in sourceRelativeFiles)
        {
            var relativeDirectory = Path.GetDirectoryName(relativeFile) ?? string.Empty;
            while (true)
            {
                sourceDirectories.Add(relativeDirectory);
                if (string.IsNullOrEmpty(relativeDirectory))
                    break;

                relativeDirectory = Path.GetDirectoryName(relativeDirectory) ?? string.Empty;
            }
        }

        return sourceDirectories;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("目录路径不能为空。", nameof(path));

        return Path.GetFullPath(path.Trim());
    }

    private static void ValidatePaths(string sourcePath, string targetPath)
    {
        if (string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("源目录和目标目录不能相同。");

        if (IsSubPathOf(sourcePath, targetPath) || IsSubPathOf(targetPath, sourcePath))
            throw new InvalidOperationException("源目录和目标目录不能互相嵌套。");
    }

    private static bool IsSubPathOf(string candidateParent, string candidateChild)
    {
        var parent = EnsureTrailingSeparator(candidateParent);
        var child = EnsureTrailingSeparator(candidateChild);
        return child.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
