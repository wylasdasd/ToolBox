using Blazing.Mvvm.ComponentModel;
using System.Text.RegularExpressions;

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
            {
                UpdateInputStats();
            }
        }
    }

    public string OutputText
    {
        get => _outputText;
        private set
        {
            if (SetProperty(ref _outputText, value))
            {
                UpdateOutputStats();
            }
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

    public int InputLineCount
    {
        get => _inputLineCount;
        private set => SetProperty(ref _inputLineCount, value);
    }

    public int InputNonEmptyLineCount
    {
        get => _inputNonEmptyLineCount;
        private set => SetProperty(ref _inputNonEmptyLineCount, value);
    }

    public int InputUniqueLineCount
    {
        get => _inputUniqueLineCount;
        private set => SetProperty(ref _inputUniqueLineCount, value);
    }

    public int InputWordCount
    {
        get => _inputWordCount;
        private set => SetProperty(ref _inputWordCount, value);
    }

    public int InputCharCount
    {
        get => _inputCharCount;
        private set => SetProperty(ref _inputCharCount, value);
    }

    public int InputCharNoWhitespaceCount
    {
        get => _inputCharNoWhitespaceCount;
        private set => SetProperty(ref _inputCharNoWhitespaceCount, value);
    }

    public int OutputLineCount
    {
        get => _outputLineCount;
        private set => SetProperty(ref _outputLineCount, value);
    }

    public int OutputNonEmptyLineCount
    {
        get => _outputNonEmptyLineCount;
        private set => SetProperty(ref _outputNonEmptyLineCount, value);
    }

    public int OutputUniqueLineCount
    {
        get => _outputUniqueLineCount;
        private set => SetProperty(ref _outputUniqueLineCount, value);
    }

    public int OutputWordCount
    {
        get => _outputWordCount;
        private set => SetProperty(ref _outputWordCount, value);
    }

    public int OutputCharCount
    {
        get => _outputCharCount;
        private set => SetProperty(ref _outputCharCount, value);
    }

    public int OutputCharNoWhitespaceCount
    {
        get => _outputCharNoWhitespaceCount;
        private set => SetProperty(ref _outputCharNoWhitespaceCount, value);
    }

    public void Deduplicate()
    {
        var lines = PrepareLines(InputText, TrimLines, IgnoreEmptyLines);
        var comparer = IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        List<string> result;
        if (KeepOrder)
        {
            result = new List<string>(lines.Count);
            var seen = new HashSet<string>(comparer);
            foreach (var line in lines)
            {
                if (seen.Add(line))
                {
                    result.Add(line);
                }
            }
        }
        else
        {
            result = lines.Distinct(comparer).ToList();
        }

        OutputText = JoinLines(result);
        StatusMessage = $"已去重 {lines.Count - result.Count} 行。";
    }

    public void SortAsc()
    {
        var lines = PrepareLines(InputText, TrimLines, IgnoreEmptyLines);
        var comparer = IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        lines.Sort(comparer);
        OutputText = JoinLines(lines);
        StatusMessage = "已按升序排序。";
    }

    public void SortDesc()
    {
        var lines = PrepareLines(InputText, TrimLines, IgnoreEmptyLines);
        var comparer = IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        lines.Sort(comparer);
        lines.Reverse();
        OutputText = JoinLines(lines);
        StatusMessage = "已按降序排序。";
    }

    public void RemoveEmptyLines()
    {
        var lines = PrepareLines(InputText, TrimLines, removeEmpty: true);
        OutputText = JoinLines(lines);
        StatusMessage = "已移除空行。";
    }

    public void TrimOnly()
    {
        var lines = PrepareLines(InputText, applyTrim: true, removeEmpty: false);
        OutputText = JoinLines(lines);
        StatusMessage = "已裁剪行首尾空白。";
    }

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

    private void UpdateInputStats()
    {
        var stats = ComputeStats(InputText, TrimLines, IgnoreEmptyLines, IgnoreCase);
        InputLineCount = stats.LineCount;
        InputNonEmptyLineCount = stats.NonEmptyLineCount;
        InputUniqueLineCount = stats.UniqueLineCount;
        InputWordCount = stats.WordCount;
        InputCharCount = stats.CharCount;
        InputCharNoWhitespaceCount = stats.CharNoWhitespaceCount;
    }

    private void UpdateOutputStats()
    {
        var stats = ComputeStats(OutputText, TrimLines, IgnoreEmptyLines, IgnoreCase);
        OutputLineCount = stats.LineCount;
        OutputNonEmptyLineCount = stats.NonEmptyLineCount;
        OutputUniqueLineCount = stats.UniqueLineCount;
        OutputWordCount = stats.WordCount;
        OutputCharCount = stats.CharCount;
        OutputCharNoWhitespaceCount = stats.CharNoWhitespaceCount;
    }

    private static List<string> PrepareLines(string text, bool applyTrim, bool removeEmpty)
    {
        var lines = SplitLines(text);
        var list = new List<string>(lines.Count);
        foreach (var line in lines)
        {
            var value = applyTrim ? line.Trim() : line;
            if (removeEmpty && string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            list.Add(value);
        }

        return list;
    }

    private static string JoinLines(IReadOnlyCollection<string> lines)
    {
        return lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);
    }

    private static List<string> SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<string>();
        }

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        return normalized.Split('\n', StringSplitOptions.None).ToList();
    }

    private static TextStats ComputeStats(string text, bool applyTrim, bool removeEmpty, bool ignoreCase)
    {
        var rawLines = SplitLines(text);
        var lineCount = rawLines.Count;
        var nonEmptyLineCount = rawLines.Count(line => !string.IsNullOrWhiteSpace(line));
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        var normalizedLines = rawLines.Select(line => applyTrim ? line.Trim() : line);
        if (removeEmpty)
        {
            normalizedLines = normalizedLines.Where(line => !string.IsNullOrWhiteSpace(line));
        }

        var uniqueLineCount = new HashSet<string>(normalizedLines, comparer).Count;
        var charCount = text?.Length ?? 0;
        var charNoWhitespaceCount = string.IsNullOrEmpty(text) ? 0 : text.Count(c => !char.IsWhiteSpace(c));
        var wordCount = CountWords(text ?? string.Empty);
        return new TextStats(lineCount, nonEmptyLineCount, uniqueLineCount, wordCount, charCount, charNoWhitespaceCount);
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var matches = Regex.Matches(text, @"\p{L}[\p{L}\p{M}]*|\p{N}+");
        return matches.Count;
    }

    private sealed record TextStats(
        int LineCount,
        int NonEmptyLineCount,
        int UniqueLineCount,
        int WordCount,
        int CharCount,
        int CharNoWhitespaceCount);
}
