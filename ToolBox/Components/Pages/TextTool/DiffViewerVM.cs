using Blazing.Mvvm.ComponentModel;
using System.Text.RegularExpressions;

namespace ToolBox.Components.Pages;

public sealed class DiffViewerVM : ViewModelBase
{
    private string _text1 = "测试1";
    private string _text2 = "测试2";
    private bool _ignoreWhitespace;
    private bool _ignoreCase;
    private bool _collapseContent;
    private string _replacePattern = string.Empty;
    private string _replaceWith = string.Empty;
    private string _batchRules = string.Empty;
    private bool _useRegex = true;
    private bool _replaceIgnoreCase;
    private bool _replaceMultiline;
    private bool _replaceSingleline;
    private string _replaceMessage = string.Empty;
    private bool _replaceExpanded = true;

    public string Text1
    {
        get => _text1;
        set => SetProperty(ref _text1, value);
    }

    public string Text2
    {
        get => _text2;
        set => SetProperty(ref _text2, value);
    }

    public bool IgnoreWhitespace
    {
        get => _ignoreWhitespace;
        set => SetProperty(ref _ignoreWhitespace, value);
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set => SetProperty(ref _ignoreCase, value);
    }

    public bool CollapseContent
    {
        get => _collapseContent;
        set => SetProperty(ref _collapseContent, value);
    }

    public string ReplacePattern
    {
        get => _replacePattern;
        set => SetProperty(ref _replacePattern, value);
    }

    public string ReplaceWith
    {
        get => _replaceWith;
        set => SetProperty(ref _replaceWith, value);
    }

    public string BatchRules
    {
        get => _batchRules;
        set => SetProperty(ref _batchRules, value);
    }

    public bool UseRegex
    {
        get => _useRegex;
        set => SetProperty(ref _useRegex, value);
    }

    public bool ReplaceIgnoreCase
    {
        get => _replaceIgnoreCase;
        set => SetProperty(ref _replaceIgnoreCase, value);
    }

    public bool ReplaceMultiline
    {
        get => _replaceMultiline;
        set => SetProperty(ref _replaceMultiline, value);
    }

    public bool ReplaceSingleline
    {
        get => _replaceSingleline;
        set => SetProperty(ref _replaceSingleline, value);
    }

    public string ReplaceMessage
    {
        get => _replaceMessage;
        private set => SetProperty(ref _replaceMessage, value);
    }

    public bool ReplaceExpanded
    {
        get => _replaceExpanded;
        set => SetProperty(ref _replaceExpanded, value);
    }

    public void ReplaceInText1()
    {
        ReplaceMessage = ApplyReplaceToTarget(ref _text1);
        OnPropertyChanged(nameof(Text1));
    }

    public void ReplaceInText2()
    {
        ReplaceMessage = ApplyReplaceToTarget(ref _text2);
        OnPropertyChanged(nameof(Text2));
    }

    public void ReplaceInBoth()
    {
        var left = ApplyReplaceToTarget(ref _text1);
        var right = ApplyReplaceToTarget(ref _text2);
        ReplaceMessage = $"文本1：{left}  文本2：{right}";
        OnPropertyChanged(nameof(Text1));
        OnPropertyChanged(nameof(Text2));
    }

    public void BatchReplaceInText1()
    {
        ReplaceMessage = ApplyBatchReplace(ref _text1);
        OnPropertyChanged(nameof(Text1));
    }

    public void BatchReplaceInText2()
    {
        ReplaceMessage = ApplyBatchReplace(ref _text2);
        OnPropertyChanged(nameof(Text2));
    }

    public void BatchReplaceInBoth()
    {
        var left = ApplyBatchReplace(ref _text1);
        var right = ApplyBatchReplace(ref _text2);
        ReplaceMessage = $"文本1：{left}  文本2：{right}";
        OnPropertyChanged(nameof(Text1));
        OnPropertyChanged(nameof(Text2));
    }

    private string ApplyReplaceToTarget(ref string text)
    {
        if (string.IsNullOrEmpty(ReplacePattern))
        {
            return "替换规则为空。";
        }

        var count = 0;
        text = UseRegex
            ? RegexReplace(text ?? string.Empty, ReplacePattern, ReplaceWith ?? string.Empty, out count)
            : PlainReplace(text ?? string.Empty, ReplacePattern, ReplaceWith ?? string.Empty, out count);

        return $"已替换 {count} 处。";
    }

    private string ApplyBatchReplace(ref string text)
    {
        var rules = ParseBatchRules(BatchRules);
        if (rules.Count == 0)
        {
            return "批量规则为空。";
        }

        var total = 0;
        foreach (var (pattern, replacement) in rules)
        {
            var count = 0;
            text = UseRegex
                ? RegexReplace(text ?? string.Empty, pattern, replacement, out count)
                : PlainReplace(text ?? string.Empty, pattern, replacement, out count);
            total += count;
        }

        return $"已替换 {total} 处（{rules.Count} 条规则）。";
    }

    private string RegexReplace(string input, string pattern, string replacement, out int count)
    {
        var options = RegexOptions.None;
        if (ReplaceIgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (ReplaceMultiline)
        {
            options |= RegexOptions.Multiline;
        }

        if (ReplaceSingleline)
        {
            options |= RegexOptions.Singleline;
        }

        var regex = new Regex(pattern, options);
        count = regex.Matches(input).Count;
        return regex.Replace(input, replacement);
    }

    private string PlainReplace(string input, string pattern, string replacement, out int count)
    {
        var comparison = ReplaceIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        count = 0;
        var index = 0;
        while ((index = input.IndexOf(pattern, index, comparison)) >= 0)
        {
            count++;
            index += pattern.Length == 0 ? 1 : pattern.Length;
        }

        return input.Replace(pattern, replacement, comparison);
    }

    private static List<(string Pattern, string Replacement)> ParseBatchRules(string? rules)
    {
        var list = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(rules))
        {
            return list;
        }

        var lines = rules.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split(new[] { "=>" }, 2, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                continue;
            }

            var pattern = parts[0].Trim();
            var replacement = parts[1].Trim();
            if (pattern.Length == 0)
            {
                continue;
            }

            list.Add((pattern, replacement));
        }

        return list;
    }
}
