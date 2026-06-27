namespace ToolBox.Services.DirectorySync;

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
