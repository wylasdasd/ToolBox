using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Text;

namespace ToolBox.Components.Pages.TextTool;

public sealed class TextLinesVM : ViewModelBase
{
    private string _inputText = string.Empty;
    private string _outputText = string.Empty;
    private bool _trimLines = true;
    private bool _ignoreEmptyLines = true;
    private bool _ignoreCase;
    private bool _keepOrder = true;
    private string _statusMessage = string.Empty;

    private int _inputLineCount;
    private int _inputNonEmptyLineCount;
    private int _inputUniqueLineCount;
    private int _inputWordCount;
    private int _inputCharCount;
    private int _inputCharNoWhitespaceCount;

    private int _outputLineCount;
    private int _outputNonEmptyLineCount;
    private int _outputUniqueLineCount;
    private int _outputWordCount;
    private int _outputCharCount;
    private int _outputCharNoWhitespaceCount;

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
                UpdateInputStats();
        }
    }

    public string OutputText
    {
        get => _outputText;
        private set
        {
            if (SetProperty(ref _outputText, value))
                UpdateOutputStats();
        }
    }

    public bool TrimLines
    {
        get => _trimLines;
        set
        {
            if (SetProperty(ref _trimLines, value))
            {
                UpdateInputStats();
                UpdateOutputStats();
            }
        }
    }

    public bool IgnoreEmptyLines
    {
        get => _ignoreEmptyLines;
        set
        {
            if (SetProperty(ref _ignoreEmptyLines, value))
            {
                UpdateInputStats();
                UpdateOutputStats();
            }
        }
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set
        {
            if (SetProperty(ref _ignoreCase, value))
            {
                UpdateInputStats();
                UpdateOutputStats();
            }
        }
    }

    public bool KeepOrder
    {
        get => _keepOrder;
        set => SetProperty(ref _keepOrder, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int InputLineCount { get => _inputLineCount; private set => SetProperty(ref _inputLineCount, value); }
    public int InputNonEmptyLineCount { get => _inputNonEmptyLineCount; private set => SetProperty(ref _inputNonEmptyLineCount, value); }
    public int InputUniqueLineCount { get => _inputUniqueLineCount; private set => SetProperty(ref _inputUniqueLineCount, value); }
    public int InputWordCount { get => _inputWordCount; private set => SetProperty(ref _inputWordCount, value); }
    public int InputCharCount { get => _inputCharCount; private set => SetProperty(ref _inputCharCount, value); }
    public int InputCharNoWhitespaceCount { get => _inputCharNoWhitespaceCount; private set => SetProperty(ref _inputCharNoWhitespaceCount, value); }

    public int OutputLineCount { get => _outputLineCount; private set => SetProperty(ref _outputLineCount, value); }
    public int OutputNonEmptyLineCount { get => _outputNonEmptyLineCount; private set => SetProperty(ref _outputNonEmptyLineCount, value); }
    public int OutputUniqueLineCount { get => _outputUniqueLineCount; private set => SetProperty(ref _outputUniqueLineCount, value); }
    public int OutputWordCount { get => _outputWordCount; private set => SetProperty(ref _outputWordCount, value); }
    public int OutputCharCount { get => _outputCharCount; private set => SetProperty(ref _outputCharCount, value); }
    public int OutputCharNoWhitespaceCount { get => _outputCharNoWhitespaceCount; private set => SetProperty(ref _outputCharNoWhitespaceCount, value); }

    public void Deduplicate() => ApplyOperation(TextLinesService.Deduplicate(InputText, TrimLines, IgnoreEmptyLines, IgnoreCase, KeepOrder));

    public void SortAsc() => ApplyOperation(TextLinesService.SortAsc(InputText, TrimLines, IgnoreEmptyLines, IgnoreCase));

    public void SortDesc() => ApplyOperation(TextLinesService.SortDesc(InputText, TrimLines, IgnoreEmptyLines, IgnoreCase));

    public void RemoveEmptyLines() => ApplyOperation(TextLinesService.RemoveEmptyLines(InputText, TrimLines));

    public void TrimOnly() => ApplyOperation(TextLinesService.TrimOnly(InputText));

    public void UseOutputAsInput()
    {
        InputText = OutputText;
        StatusMessage = "已用输出覆盖输入。";
    }

    public void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        StatusMessage = string.Empty;
    }

    private void ApplyOperation(ToolBox.Tools.Common.ToolResult<TextLinesOperationResult> result)
    {
        if (!result.Success)
        {
            StatusMessage = result.Error ?? "操作失败。";
            return;
        }

        OutputText = result.Value!.Output;
        StatusMessage = result.Value.StatusMessage;
    }

    private void UpdateInputStats() => ApplyStats(TextLinesService.ComputeStats(InputText, TrimLines, IgnoreEmptyLines, IgnoreCase), input: true);

    private void UpdateOutputStats() => ApplyStats(TextLinesService.ComputeStats(OutputText, TrimLines, IgnoreEmptyLines, IgnoreCase), input: false);

    private void ApplyStats(TextLineStats stats, bool input)
    {
        if (input)
        {
            InputLineCount = stats.LineCount;
            InputNonEmptyLineCount = stats.NonEmptyLineCount;
            InputUniqueLineCount = stats.UniqueLineCount;
            InputWordCount = stats.WordCount;
            InputCharCount = stats.CharCount;
            InputCharNoWhitespaceCount = stats.CharNoWhitespaceCount;
        }
        else
        {
            OutputLineCount = stats.LineCount;
            OutputNonEmptyLineCount = stats.NonEmptyLineCount;
            OutputUniqueLineCount = stats.UniqueLineCount;
            OutputWordCount = stats.WordCount;
            OutputCharCount = stats.CharCount;
            OutputCharNoWhitespaceCount = stats.CharNoWhitespaceCount;
        }
    }
}
