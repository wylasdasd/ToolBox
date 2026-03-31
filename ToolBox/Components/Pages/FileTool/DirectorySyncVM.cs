using Blazing.Mvvm.ComponentModel;
using ToolBox.Services.DirectorySync;
using ToolBox.Services.Picker;

namespace ToolBox.Components.Pages.FileTool;

public sealed class DirectorySyncVM : ViewModelBase
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly IDirectorySyncService _directorySyncService;

    private string _sourcePath = string.Empty;
    private string _targetPath = string.Empty;
    private bool _deleteExtraFiles = true;
    private bool _isBusy;
    private string _statusMessage = "选择源目录和目标目录后，可先预检查，再执行同步。";
    private string _logText = string.Empty;
    private double _progressPercent;
    private int _copiedFiles;
    private int _deletedFiles;
    private long _copiedBytes;
    private DirectorySyncPreview? _preview;

    public DirectorySyncVM(
        IFolderPickerService folderPickerService,
        IDirectorySyncService directorySyncService)
    {
        _folderPickerService = folderPickerService;
        _directorySyncService = directorySyncService;
    }

    public string SourcePath
    {
        get => _sourcePath;
        set => SetProperty(ref _sourcePath, value);
    }

    public string TargetPath
    {
        get => _targetPath;
        set => SetProperty(ref _targetPath, value);
    }

    public bool DeleteExtraFiles
    {
        get => _deleteExtraFiles;
        set => SetProperty(ref _deleteExtraFiles, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        private set => SetProperty(ref _progressPercent, value);
    }

    public int CopiedFiles
    {
        get => _copiedFiles;
        private set => SetProperty(ref _copiedFiles, value);
    }

    public int DeletedFiles
    {
        get => _deletedFiles;
        private set => SetProperty(ref _deletedFiles, value);
    }

    public long CopiedBytes
    {
        get => _copiedBytes;
        private set => SetProperty(ref _copiedBytes, value);
    }

    public DirectorySyncPreview? Preview
    {
        get => _preview;
        private set
        {
            if (SetProperty(ref _preview, value))
            {
                OnPropertyChanged(nameof(HasPreview));
                OnPropertyChanged(nameof(PreviewSummary));
            }
        }
    }

    public bool HasPreview => Preview is not null;

    public string PreviewSummary => Preview is null
        ? "尚未执行预检查。"
        : $"源文件 {Preview.SourceFileCount} 个，目标文件 {Preview.TargetFileCount} 个，待复制/覆盖 {Preview.FilesToCopyCount} 个，待删除 {Preview.FilesToDeleteCount} 个，预计写入 {FormatBytes(Preview.EstimatedCopyBytes)}。";

    public async Task PickSourceFolderAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await PickFolderAsync(path => SourcePath = path, "源目录");
    }

    public async Task PickTargetFolderAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await PickFolderAsync(path => TargetPath = path, "目标目录");
    }

    public async Task PreviewAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ResetRunStats();
        try
        {
            IsBusy = true;
            AppendLog("开始预检查目录差异。");
            Preview = await _directorySyncService.PreviewAsync(SourcePath, TargetPath, DeleteExtraFiles);
            StatusMessage = "预检查完成，可以开始同步。";
            AppendLog(PreviewSummary);
        }
        catch (Exception ex)
        {
            StatusMessage = $"预检查失败：{ex.Message}";
            AppendLog(StatusMessage);
            Preview = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SyncAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ResetRunStats();
        try
        {
            IsBusy = true;
            StatusMessage = "正在同步目录...";
            AppendLog("开始执行目录同步。");

            Preview ??= await _directorySyncService.PreviewAsync(SourcePath, TargetPath, DeleteExtraFiles);
            var totalOperations = Math.Max(1, Preview.FilesToCopyCount + Preview.FilesToDeleteCount);
            var progress = new Progress<DirectorySyncProgress>(value =>
            {
                CopiedFiles = value.CopiedFiles;
                DeletedFiles = value.DeletedFiles;
                CopiedBytes = value.CopiedBytes;
                ProgressPercent = (value.CopiedFiles + value.DeletedFiles) * 100d / totalOperations;
                if (!string.IsNullOrWhiteSpace(value.CurrentPath))
                {
                    AppendLog($"处理中：{value.CurrentPath}");
                }
            });

            var result = await _directorySyncService.SyncAsync(SourcePath, TargetPath, DeleteExtraFiles, progress);
            CopiedFiles = result.FilesCopied;
            DeletedFiles = result.FilesDeleted;
            CopiedBytes = result.BytesCopied;
            ProgressPercent = 100;
            Preview = await _directorySyncService.PreviewAsync(SourcePath, TargetPath, DeleteExtraFiles);
            StatusMessage = $"同步完成：复制/覆盖 {result.FilesCopied} 个，删除 {result.FilesDeleted} 个，写入 {FormatBytes(result.BytesCopied)}。";
            AppendLog(StatusMessage);
        }
        catch (Exception ex)
        {
            StatusMessage = $"同步失败：{ex.Message}";
            AppendLog(StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PickFolderAsync(Action<string> setter, string name)
    {
        try
        {
            var folder = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrWhiteSpace(folder))
            {
                setter(folder);
                Preview = null;
                StatusMessage = $"已选择{name}：{folder}";
                AppendLog(StatusMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择{name}失败：{ex.Message}";
            AppendLog(StatusMessage);
        }
    }

    private void ResetRunStats()
    {
        ProgressPercent = 0;
        CopiedFiles = 0;
        DeletedFiles = 0;
        CopiedBytes = 0;
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogText = string.IsNullOrWhiteSpace(LogText)
            ? line
            : $"{LogText}{Environment.NewLine}{line}";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024d;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
