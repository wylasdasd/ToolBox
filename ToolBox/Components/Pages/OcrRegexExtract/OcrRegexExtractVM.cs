using System.Text.Json;
using System.Text.RegularExpressions;
using Blazing.Mvvm.ComponentModel;
using CommonTool.StringHelp;
using ToolBox.Services.Ocr;
using ToolBox.Services.Picker;

namespace ToolBox.Components.Pages.OcrRegexExtract;

public sealed class OcrRegexExtractVM : ViewModelBase
{
    private readonly IImageOcrService _imageOcrService;
    private readonly IImagePickerService _imagePickerService;
    private string _sourceFileName = "111.jpg";
    private string _ocrLanguage = "zh-Hans";
    private string _imagePreviewDataUrl = string.Empty;
    private string _ocrText = string.Empty;
    private string _jsonResult = string.Empty;
    private string _statusMessage = "配置正则规则后，点击“OCR + 正则提取”。";
    private bool _isBusy;
    private bool _removeSpaces;
    private bool _ignoreCase = true;
    private bool _enableCropRegion;
    private int _cropX;
    private int _cropY;
    private int _cropWidth = 800;
    private int _cropHeight = 600;
    private List<RegexRuleItem> _rules =
    [
        new() { Key = "Address", Pattern = @"Address:([0-9A-F]{2}(:[0-9A-F]{2}){5})" },
        new() { Key = "Battery", Pattern = @"Battery:(\d+\.\d+)V" },
        new() { Key = "Value", Pattern = @"Value:(\d+\.\d+)" },
        new() { Key = "SensorTemp", Pattern = @"SensorTemp:(\d+\.\d+)" }
    ];

    public OcrRegexExtractVM(
        IImageOcrService imageOcrService,
        IImagePickerService imagePickerService)
    {
        _imageOcrService = imageOcrService;
        _imagePickerService = imagePickerService;
    }

    public string SourceFileName
    {
        get => _sourceFileName;
        set => SetProperty(ref _sourceFileName, value);
    }

    public string OcrText
    {
        get => _ocrText;
        private set => SetProperty(ref _ocrText, value);
    }

    public string ImagePreviewDataUrl
    {
        get => _imagePreviewDataUrl;
        private set => SetProperty(ref _imagePreviewDataUrl, value);
    }

    public string OcrLanguage
    {
        get => _ocrLanguage;
        set => SetProperty(ref _ocrLanguage, value);
    }

    public string JsonResult
    {
        get => _jsonResult;
        private set => SetProperty(ref _jsonResult, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool RemoveSpaces
    {
        get => _removeSpaces;
        set => SetProperty(ref _removeSpaces, value);
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set => SetProperty(ref _ignoreCase, value);
    }

    public bool EnableCropRegion
    {
        get => _enableCropRegion;
        set => SetProperty(ref _enableCropRegion, value);
    }

    public int CropX
    {
        get => _cropX;
        set
        {
            if (SetProperty(ref _cropX, value))
            {
                OnPropertyChanged(nameof(CropPreviewStyle));
            }
        }
    }

    public int CropY
    {
        get => _cropY;
        set
        {
            if (SetProperty(ref _cropY, value))
            {
                OnPropertyChanged(nameof(CropPreviewStyle));
            }
        }
    }

    public int CropWidth
    {
        get => _cropWidth;
        set
        {
            if (SetProperty(ref _cropWidth, value))
            {
                OnPropertyChanged(nameof(CropPreviewStyle));
            }
        }
    }

    public int CropHeight
    {
        get => _cropHeight;
        set
        {
            if (SetProperty(ref _cropHeight, value))
            {
                OnPropertyChanged(nameof(CropPreviewStyle));
            }
        }
    }

    public string CropPreviewStyle =>
        $"left:{Math.Max(0, CropX)}px;top:{Math.Max(0, CropY)}px;width:{Math.Max(1, CropWidth)}px;height:{Math.Max(1, CropHeight)}px;";

    public IReadOnlyList<RegexRuleItem> Rules => _rules;

    public async Task PickImageAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            var path = await _imagePickerService.PickImageAsync();
            if (!string.IsNullOrWhiteSpace(path))
            {
                SourceFileName = path;
                StatusMessage = $"已选择图片：{path}";
                await LoadPreviewAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择图片失败：{ex.Message}";
        }
    }

    public void AddRule()
    {
        _rules.Add(new RegexRuleItem { Key = $"field_{_rules.Count + 1}", Pattern = string.Empty });
        Notify(nameof(Rules));
    }

    public void RemoveRule(RegexRuleItem rule)
    {
        if (_rules.Count <= 1)
        {
            StatusMessage = "至少保留 1 条规则。";
            return;
        }

        _rules.Remove(rule);
        Notify(nameof(Rules));
    }

    public async Task RunAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "正在 OCR 识别...";
            JsonResult = string.Empty;

            await LoadPreviewAsync();
            await using var imageStream = await OpenSourceStreamAsync(SourceFileName);
            var recognized = await _imageOcrService.RecognizeTextAsync(imageStream, OcrLanguage, BuildCropOptions());
            OcrText = OcrTextNormalizeHelp.NormalizeSymbolsToAscii(recognized, RemoveSpaces);

            StatusMessage = "OCR 完成，正在执行正则提取...";
            var result = ExtractByRules(OcrText, _rules, IgnoreCase);
            JsonResult = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            StatusMessage = "提取完成。";
        }
        catch (FileNotFoundException)
        {
            OcrText = string.Empty;
            JsonResult = string.Empty;
            StatusMessage = $"未找到图片：{SourceFileName}。请先通过按钮选择图片，或输入已打包到 Resources/Raw 的文件名。";
        }
        catch (Exception ex)
        {
            JsonResult = string.Empty;
            StatusMessage = $"处理失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadPreviewAsync()
    {
        try
        {
            await using var imageStream = await OpenSourceStreamAsync(SourceFileName);
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var ext = Path.GetExtension(SourceFileName)?.ToLowerInvariant();
            var mime = ext switch
            {
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            // 使用 data url 让 Blazor 页面直接显示本地图片。
            ImagePreviewDataUrl = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            ImagePreviewDataUrl = string.Empty;
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

    private void Notify(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }

    private static async Task<Stream> OpenSourceStreamAsync(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new FileNotFoundException("source is empty", source);
        }

        if (Path.IsPathRooted(source))
        {
            if (!File.Exists(source))
            {
                throw new FileNotFoundException("file not found", source);
            }

            return File.OpenRead(source);
        }

        return await FileSystem.Current.OpenAppPackageFileAsync(source);
    }

    private OcrCropOptions? BuildCropOptions()
    {
        if (!EnableCropRegion)
        {
            return null;
        }

        return new OcrCropOptions
        {
            Enabled = true,
            X = CropX,
            Y = CropY,
            Width = CropWidth,
            Height = CropHeight
        };
    }
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
