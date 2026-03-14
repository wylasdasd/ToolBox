using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Blazing.Mvvm.ComponentModel;
using CommonTool.FileHelps;
using CommonTool.StringHelp;
using ToolBox.Services.Ocr;
using ToolBox.Services.Picker;

namespace ToolBox.Components.Pages.OcrRegexBatch;

public sealed class OcrRegexBatchVM : ViewModelBase
{
    private readonly IImageOcrService _imageOcrService;
    private readonly IFolderPickerService _folderPickerService;
    private string _folderPath = string.Empty;
    private string _ocrLanguage = "zh-Hans";
    private bool _ignoreCase = true;
    private bool _removeSpaces;
    private bool _isBusy;
    private int _processedCount;
    private int _totalCount;
    private double _progressPercent;
    private string _statusMessage = "配置规则后，输入文件夹并开始批量处理。";
    private string _logText = string.Empty;
    private string _jsonResult = string.Empty;
    private List<RegexRuleItem> _rules =
    [
        new() { Key = "Address", Pattern = @"Address:([0-9A-F]{2}(:[0-9A-F]{2}){5})" },
        new() { Key = "Battery", Pattern = @"Battery:(\d+\.\d+)V" },
        new() { Key = "Value", Pattern = @"Value:(\d+\.\d+)" },
        new() { Key = "SensorTemp", Pattern = @"SensorTemp:(\d+\.\d+)" }
    ];
    private List<BatchExtractItem> _results = [];

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".webp"];

    public OcrRegexBatchVM(
        IImageOcrService imageOcrService,
        IFolderPickerService folderPickerService)
    {
        _imageOcrService = imageOcrService;
        _folderPickerService = folderPickerService;
    }

    public string FolderPath
    {
        get => _folderPath;
        set => SetProperty(ref _folderPath, value);
    }

    public string OcrLanguage
    {
        get => _ocrLanguage;
        set => SetProperty(ref _ocrLanguage, value);
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set => SetProperty(ref _ignoreCase, value);
    }

    public bool RemoveSpaces
    {
        get => _removeSpaces;
        set => SetProperty(ref _removeSpaces, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public int ProcessedCount
    {
        get => _processedCount;
        private set => SetProperty(ref _processedCount, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        private set => SetProperty(ref _progressPercent, value);
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

    public string JsonResult
    {
        get => _jsonResult;
        private set => SetProperty(ref _jsonResult, value);
    }

    public IReadOnlyList<RegexRuleItem> Rules => _rules;

    public bool HasResult => _results.Count > 0;

    public async Task PickFolderAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            var folder = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrWhiteSpace(folder))
            {
                FolderPath = folder;
                StatusMessage = $"已选择文件夹：{folder}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择文件夹失败：{ex.Message}";
        }
    }

    public void AddRule()
    {
        _rules.Add(new RegexRuleItem { Key = $"field_{_rules.Count + 1}", Pattern = string.Empty });
        OnPropertyChanged(nameof(Rules));
    }

    public void RemoveRule(RegexRuleItem rule)
    {
        if (_rules.Count <= 1)
        {
            StatusMessage = "至少保留 1 条规则。";
            return;
        }

        _rules.Remove(rule);
        OnPropertyChanged(nameof(Rules));
    }

    public async Task RunBatchAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            StatusMessage = "请先输入文件夹路径。";
            return;
        }

        if (!Directory.Exists(FolderPath))
        {
            StatusMessage = $"文件夹不存在：{FolderPath}";
            return;
        }

        var files = Directory
            .EnumerateFiles(FolderPath, "*.*", SearchOption.AllDirectories)
            .Where(path => ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (files.Count == 0)
        {
            StatusMessage = "未找到图片文件。";
            return;
        }

        try
        {
            IsBusy = true;
            _results = [];
            JsonResult = string.Empty;
            LogText = string.Empty;
            TotalCount = files.Count;
            ProcessedCount = 0;
            ProgressPercent = 0;

            AppendLog($"开始批量处理，共 {files.Count} 个文件。");

            foreach (var file in files)
            {
                var displayName = Path.GetFileName(file);
                try
                {
                    AppendLog($"[开始] {displayName}");
                    await using var fs = File.OpenRead(file);
                    var ocrRaw = await _imageOcrService.RecognizeTextAsync(fs, OcrLanguage);
                    var normalized = OcrTextNormalizeHelp.NormalizeSymbolsToAscii(ocrRaw, RemoveSpaces);
                    var extract = ExtractByRules(normalized, _rules, IgnoreCase);

                    _results.Add(new BatchExtractItem
                    {
                        FileName = displayName,
                        FilePath = file,
                        OcrText = normalized,
                        Extracted = extract.Extracted,
                        Errors = extract.Errors
                    });
                    AppendLog($"[完成] {displayName}，命中字段 {extract.Extracted.Count} 个。");
                }
                catch (Exception ex)
                {
                    _results.Add(new BatchExtractItem
                    {
                        FileName = displayName,
                        FilePath = file,
                        OcrText = string.Empty,
                        Extracted = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
                        Errors = [new RegexRuleError { Key = "system", Pattern = string.Empty, Error = ex.Message }]
                    });
                    AppendLog($"[失败] {displayName}：{ex.Message}");
                }

                ProcessedCount++;
                ProgressPercent = TotalCount == 0 ? 0 : ProcessedCount * 100d / TotalCount;
            }

            JsonResult = JsonSerializer.Serialize(_results, new JsonSerializerOptions { WriteIndented = true });
            OnPropertyChanged(nameof(HasResult));
            StatusMessage = $"批量处理完成：{ProcessedCount}/{TotalCount}";
            AppendLog("全部处理完成。");
        }
        catch (Exception ex)
        {
            StatusMessage = $"处理失败：{ex.Message}";
            AppendLog($"[错误] {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportJsonAsync()
    {
        if (_results.Count == 0)
        {
            StatusMessage = "暂无结果可导出。";
            return;
        }

        try
        {
            var fileName = $"ocr_regex_batch_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var outputPath = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            await FileHelp.WriteAllTextAsync(outputPath, JsonResult, Encoding.UTF8);
            StatusMessage = $"JSON 已导出：{outputPath}";
            AppendLog($"[导出] JSON -> {outputPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"JSON 导出失败：{ex.Message}";
            AppendLog($"[导出失败] JSON：{ex.Message}");
        }
    }

    public async Task ExportExcelAsync()
    {
        if (_results.Count == 0)
        {
            StatusMessage = "暂无结果可导出。";
            return;
        }

        try
        {
            var fileName = $"ocr_regex_batch_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var outputPath = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            var csv = BuildCsv(_results, _rules);
            // 写入 UTF8 BOM，Excel 打开中文更稳。
            var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            await FileHelp.WriteAllTextAsync(outputPath, csv, utf8Bom);
            StatusMessage = $"Excel(CSV) 已导出：{outputPath}";
            AppendLog($"[导出] Excel(CSV) -> {outputPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Excel 导出失败：{ex.Message}";
            AppendLog($"[导出失败] Excel：{ex.Message}");
        }
    }

    private static RegexExtractionResult ExtractByRules(string text, IReadOnlyList<RegexRuleItem> rules, bool ignoreCase)
    {
        var options = RegexOptions.Compiled;
        if (ignoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        var extracted = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<RegexRuleError>();

        foreach (var rule in rules)
        {
            var key = (rule.Key ?? string.Empty).Trim();
            var pattern = (rule.Pattern ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            try
            {
                var regex = new Regex(pattern, options, TimeSpan.FromMilliseconds(300));
                var matches = regex.Matches(text);
                var values = new List<string>(matches.Count);
                foreach (Match match in matches)
                {
                    if (!string.IsNullOrWhiteSpace(match.Value))
                    {
                        values.Add(match.Value.Trim());
                    }
                }

                extracted[key] = values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
            {
                errors.Add(new RegexRuleError
                {
                    Key = key,
                    Pattern = pattern,
                    Error = ex.Message
                });
            }
        }

        return new RegexExtractionResult
        {
            Source = "ocr",
            Extracted = extracted,
            Errors = errors
        };
    }

    private static string BuildCsv(IReadOnlyList<BatchExtractItem> data, IReadOnlyList<RegexRuleItem> rules)
    {
        var keys = rules
            .Select(rule => (rule.Key ?? string.Empty).Trim())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sb = new StringBuilder(4096);
        sb.Append("FileName,FilePath");
        foreach (var key in keys)
        {
            sb.Append(',');
            sb.Append(EscapeCsv(key));
        }
        sb.AppendLine();

        foreach (var item in data)
        {
            sb.Append(EscapeCsv(item.FileName));
            sb.Append(',');
            sb.Append(EscapeCsv(item.FilePath));

            foreach (var key in keys)
            {
                item.Extracted.TryGetValue(key, out var values);
                var joined = values is null ? string.Empty : string.Join(" | ", values);
                sb.Append(',');
                sb.Append(EscapeCsv(joined));
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needQuote)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogText = string.IsNullOrEmpty(LogText) ? line : $"{LogText}{Environment.NewLine}{line}";
    }
}

public sealed class BatchExtractItem
{
    public string FileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string OcrText { get; init; } = string.Empty;
    public Dictionary<string, List<string>> Extracted { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<RegexRuleError> Errors { get; init; } = [];
}

public sealed class RegexRuleItem
{
    public string Key { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
}

public sealed class RegexRuleError
{
    public string Key { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}

public sealed class RegexExtractionResult
{
    public string Source { get; init; } = string.Empty;
    public Dictionary<string, List<string>> Extracted { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<RegexRuleError> Errors { get; init; } = [];
}
