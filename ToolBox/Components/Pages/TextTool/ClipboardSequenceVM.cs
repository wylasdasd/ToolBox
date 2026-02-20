using Blazing.Mvvm.ComponentModel;

namespace ToolBox.Components.Pages;

public sealed class ClipboardSequenceVM : ViewModelBase
{
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
        var lines = (ClipboardText ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        AppendSequences(lines);
        StatusMessage = lines.Length == 0 ? "剪贴板中没有可导入的非空行。" : $"已导入 {lines.Length} 条序列。";
    }

    public void AddClipboardAsOneSequence()
    {
        if (string.IsNullOrWhiteSpace(ClipboardText))
        {
            StatusMessage = "剪贴板内容为空。";
            return;
        }

        AppendSequences(new[] { ClipboardText });
        StatusMessage = "已将整段剪贴板内容加入序列。";
    }

    public void RemoveSelectedSequence()
    {
        if (string.IsNullOrEmpty(SelectedSequence))
        {
            StatusMessage = "请先选择要删除的序列。";
            return;
        }

        var updated = Sequences.Where(x => x != SelectedSequence).ToList();
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
        var merged = Sequences.Concat(values).Distinct().ToList();
        Sequences = merged;
        if (string.IsNullOrEmpty(SelectedSequence))
        {
            SelectedSequence = merged.FirstOrDefault() ?? string.Empty;
        }
    }

    public IReadOnlyList<string> GetBatchItems()
    {
        return Sequences.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }
}
