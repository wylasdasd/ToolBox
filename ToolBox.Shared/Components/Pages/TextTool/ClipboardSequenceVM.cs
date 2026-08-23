using Blazing.Mvvm.ComponentModel;
using CommonHelp;
using ToolBox.Services;
using ToolBox.Tools.Text;

namespace ToolBox.Components.Pages;

public sealed class ClipboardSequenceVM(IClipboardService clipboardService) : ViewModelBase
{
    private CancellationTokenSource? _typingCts;
    private string _clipboardText = string.Empty;
    private IReadOnlyList<string> _sequences = Array.Empty<string>();
    private string _selectedSequence = string.Empty;
    private int _startDelayMs = 3000;
    private int _charIntervalMs = 30;
    private bool _enableBatchInput;
    private int _batchItemIntervalMs = 200;
    private bool _isTyping;
    private string _statusMessage = string.Empty;

    public string ClipboardText
    {
        get => _clipboardText;
        set => SetProperty(ref _clipboardText, value);
    }

    public IReadOnlyList<string> Sequences
    {
        get => _sequences;
        private set => SetProperty(ref _sequences, value);
    }

    public string SelectedSequence
    {
        get => _selectedSequence;
        set => SetProperty(ref _selectedSequence, value);
    }

    public int StartDelayMs
    {
        get => _startDelayMs;
        set => SetProperty(ref _startDelayMs, Math.Max(0, value));
    }

    public int CharIntervalMs
    {
        get => _charIntervalMs;
        set => SetProperty(ref _charIntervalMs, Math.Max(0, value));
    }

    public bool EnableBatchInput
    {
        get => _enableBatchInput;
        set => SetProperty(ref _enableBatchInput, value);
    }

    public int BatchItemIntervalMs
    {
        get => _batchItemIntervalMs;
        set => SetProperty(ref _batchItemIntervalMs, Math.Max(0, value));
    }

    public bool IsTyping
    {
        get => _isTyping;
        set => SetProperty(ref _isTyping, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public void SetClipboardText(string text)
    {
        ClipboardText = text ?? string.Empty;
    }

    public void ImportSequencesFromClipboardLines()
    {
        var lines = ClipboardSequenceService.ParseNonEmptyLines(ClipboardText);
        AppendSequences(lines);
        StatusMessage = lines.Count == 0 ? "剪贴板中没有可导入的非空行。" : $"已导入 {lines.Count} 条序列。";
    }

    public void AddClipboardAsOneSequence()
    {
        if (string.IsNullOrWhiteSpace(ClipboardText))
        {
            StatusMessage = "剪贴板内容为空。";
            return;
        }

        AppendSequences([ClipboardText]);
        StatusMessage = "已将整段剪贴板内容加入序列。";
    }

    public void RemoveSelectedSequence()
    {
        if (string.IsNullOrEmpty(SelectedSequence))
        {
            StatusMessage = "请先选择要删除的序列。";
            return;
        }

        var updated = ClipboardSequenceService.Remove(Sequences, SelectedSequence);
        Sequences = updated;
        SelectedSequence = updated.FirstOrDefault() ?? string.Empty;
        StatusMessage = "已删除所选序列。";
    }

    public void ClearSequences()
    {
        Sequences = Array.Empty<string>();
        SelectedSequence = string.Empty;
        StatusMessage = "已清空序列列表。";
    }

    private void AppendSequences(IEnumerable<string> values)
    {
        var merged = ClipboardSequenceService.AppendDistinct(Sequences, values);
        Sequences = merged;
        SelectedSequence = ClipboardSequenceService.SelectFirstOrKeep(merged, SelectedSequence) ?? string.Empty;
    }

    public IReadOnlyList<string> GetBatchItems() =>
        ClipboardSequenceService.GetBatchItems(Sequences);

    public async Task ReadClipboardAsync()
    {
        StatusMessage = "正在读取 Windows 系统剪贴板...";
        try
        {
            var text = await clipboardService.GetTextAsync();
            SetClipboardText(text ?? string.Empty);
            StatusMessage = string.IsNullOrEmpty(text)
                ? "已读取系统剪贴板，但当前没有文本内容。"
                : "已读取 Windows 系统剪贴板。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"读取剪贴板失败: {ex.Message}";
        }
    }

    public async Task TypeSelectedSequenceAsync()
    {
        if (IsTyping)
        {
            StatusMessage = "输入任务正在执行中。";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedSequence))
        {
            StatusMessage = "请先选择一个序列。";
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            StatusMessage = "当前平台不是 Windows，无法使用系统键盘模拟输入。";
            return;
        }

        try
        {
            _typingCts?.Dispose();
            _typingCts = new CancellationTokenSource();
            IsTyping = true;

            if (EnableBatchInput)
            {
                var items = GetBatchItems();
                if (items.Count == 0)
                {
                    StatusMessage = "序列列表为空，无法批量输入。";
                    return;
                }

                StatusMessage = $"将在 {StartDelayMs}ms 后开始批量输入 {items.Count} 条，请切换到目标窗口。";
                await KeyboardInputHelp.TypeBatchAsync(
                    items,
                    StartDelayMs,
                    CharIntervalMs,
                    BatchItemIntervalMs,
                    pressEnterAfterEach: true,
                    cancellationToken: _typingCts.Token);
                StatusMessage = "批量模拟输入完成。";
            }
            else
            {
                StatusMessage = $"将在 {StartDelayMs}ms 后开始输入，请切换到目标窗口。";
                await KeyboardInputHelp.TypeTextAsync(SelectedSequence, StartDelayMs, CharIntervalMs, _typingCts.Token);
                StatusMessage = "模拟输入完成。";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "输入已中断。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"模拟输入失败: {ex.Message}";
        }
        finally
        {
            IsTyping = false;
            _typingCts?.Dispose();
            _typingCts = null;
        }
    }

    public void CancelTyping()
    {
        if (!IsTyping || _typingCts is null)
        {
            StatusMessage = "当前没有正在执行的输入任务。";
            return;
        }

        _typingCts.Cancel();
        StatusMessage = "正在中断输入...";
    }
}
