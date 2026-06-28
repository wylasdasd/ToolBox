using Blazing.Mvvm.ComponentModel;
using CommonTool.FileHelps;
using System.Text;
using System.Text.Json;
using ToolBox.Services.Ai;
using ToolBox.Services.Picker;

namespace ToolBox.Components.Pages.AiExtract;

public sealed class AiExtractVM(
    IAiChatService aiChatService,
    IAiApiKeyService apiKeyService,
    IFolderPickerService folderPickerService) : ViewModelBase
{
    private const int MaxBatchFileCount = 500;
    private const long MaxBatchFileBytes = 20 * 1024 * 1024;
    private const string VisionUnsupportedMessage = "当前提供商不支持图片，请切换 Kimi / OpenRouter / Cursor。";

    private AiProviderKind _selectedProvider = AiProviderKind.OpenRouter;
    private string _model = AiProviderCatalog.GetDefaultModel(AiProviderKind.OpenRouter);
    private string _apiKey = string.Empty;
    private string _selectedTemplateId = AiExtractTemplates.Presets[0].Id;
    private string _extractTemplate = AiExtractTemplates.Presets[0].Template;
    private string _systemPrompt = AiExtractTemplates.DefaultSystemPrompt;
    private string _singleInputText = string.Empty;
    private string _outputJson = string.Empty;
    private string _logText = string.Empty;
    private bool _isBusy;
    private int _processedCount;
    private int _totalCount;
    private double _progressPercent;
    private string _statusMessage = "配置 API Key 与提取模板，上传文件或粘贴文本后开始。";
    private bool _mergeBatchSend = true;

    private readonly List<BatchInputItem> _batchItems = [];

    public IReadOnlyList<AiProviderKind> Providers => AiProviderCatalog.All;
    public IReadOnlyList<AiExtractTemplate> TemplatePresets => AiExtractTemplates.Presets;

    public Task<IEnumerable<string>> SearchModelsAsync(string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AiProviderModelCatalog.SearchModelIds(SelectedProvider, value));
    }

    public AiProviderKind SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (SetProperty(ref _selectedProvider, value))
            {
                Model = AiProviderCatalog.GetDefaultModel(value);
                OnPropertyChanged(nameof(MergeBatchHint));
                _ = LoadApiKeyAsync();
            }
        }
    }

    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    public string SelectedTemplateId
    {
        get => _selectedTemplateId;
        set
        {
            if (SetProperty(ref _selectedTemplateId, value))
                ApplyTemplatePreset(value);
        }
    }

    public string ExtractTemplate
    {
        get => _extractTemplate;
        set => SetProperty(ref _extractTemplate, value);
    }

    public string SystemPrompt
    {
        get => _systemPrompt;
        set => SetProperty(ref _systemPrompt, value);
    }

    public string SingleInputText
    {
        get => _singleInputText;
        set => SetProperty(ref _singleInputText, value);
    }

    public string OutputJson
    {
        get => _outputJson;
        private set => SetProperty(ref _outputJson, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
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

    public bool MergeBatchSend
    {
        get => _mergeBatchSend;
        set => SetProperty(ref _mergeBatchSend, value);
    }

    public string MergeBatchHint =>
        $"合并发送：每请求最多 {AiProviderCatalog.GetMaxMergeBatchSize(SelectedProvider)} 个（Cursor 限 5 张图）";

    public IReadOnlyList<BatchInputItem> BatchItems => _batchItems;
    public bool SupportsVision => AiProviderCatalog.SupportsVision(SelectedProvider);
    public bool SupportsNativeFolderPicker => folderPickerService.IsNativeSupported;
    public bool HasBatchItems => _batchItems.Count > 0;

    private string EffectiveExtractTemplate =>
        string.IsNullOrWhiteSpace(ExtractTemplate)
            ? AiExtractTemplates.Presets[0].Template
            : ExtractTemplate.Trim();

    public override async Task OnInitializedAsync()
    {
        await LoadApiKeyAsync();
    }

    public async Task LoadApiKeyAsync()
    {
        ApiKey = await apiKeyService.GetApiKeyAsync(SelectedProvider) ?? string.Empty;
    }

    public async Task SaveApiKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "API Key 不能为空。";
            return;
        }

        await apiKeyService.SaveApiKeyAsync(SelectedProvider, ApiKey);
        StatusMessage = $"{AiProviderCatalog.GetDisplayName(SelectedProvider)} API Key 已保存。";
    }

    public async Task ClearApiKeyAsync()
    {
        await apiKeyService.ClearApiKeyAsync(SelectedProvider);
        ApiKey = string.Empty;
        StatusMessage = "API Key 已清除。";
    }

    public void ClearBatchItems()
    {
        _batchItems.Clear();
        OnPropertyChanged(nameof(BatchItems));
        OnPropertyChanged(nameof(HasBatchItems));
        StatusMessage = "已清空批量文件列表。";
    }

    public async Task AddBatchFileAsync(
        string fileName,
        Stream content,
        string contentType,
        bool updateStatus = true,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedBatchFile(fileName))
            throw new InvalidOperationException($"不支持的文件类型：{fileName}");

        var bytes = await ReadAllBytesAsync(content, cancellationToken);
        if (bytes.Length > MaxBatchFileBytes)
            throw new InvalidOperationException($"文件超过 20MB：{fileName}");

        var isImage = AiBatchFileHelp.IsImage(fileName, contentType);
        string? dataUrl = null;
        string? text = null;

        if (isImage)
        {
            var mime = AiBatchFileHelp.GuessImageMime(fileName, contentType);
            dataUrl = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
        }
        else
        {
            text = Encoding.UTF8.GetString(bytes);
        }

        _batchItems.Add(new BatchInputItem(fileName, isImage, dataUrl, text));
        OnPropertyChanged(nameof(BatchItems));
        OnPropertyChanged(nameof(HasBatchItems));
        if (updateStatus)
            StatusMessage = $"已添加 {fileName}（共 {_batchItems.Count} 个）。";
    }

    public async Task PickBatchFolderAsync()
    {
        if (IsBusy)
            return;

        if (!folderPickerService.IsNativeSupported)
        {
            StatusMessage = "当前环境请使用「选择文件夹」按钮（浏览器目录选择）。";
            return;
        }

        string? folder;
        try
        {
            folder = await folderPickerService.PickFolderAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择文件夹失败：{ex.Message}";
            return;
        }

        if (string.IsNullOrWhiteSpace(folder))
            return;

        var paths = FileHelp.GetFilesRecursively(folder)
            .Where(IsSupportedBatchFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(MaxBatchFileCount)
            .ToList();

        if (paths.Count == 0)
        {
            StatusMessage = "文件夹内没有支持的文件（.txt/.log/.csv/.json/.md/.png/.jpg 等）。";
            return;
        }

        var added = 0;
        var skipped = 0;
        foreach (var path in paths)
        {
            if (_batchItems.Count >= MaxBatchFileCount)
            {
                skipped++;
                continue;
            }

            try
            {
                var info = new FileInfo(path);
                if (info.Length > MaxBatchFileBytes)
                {
                    skipped++;
                    AppendLog($"跳过（超过 20MB）：{path}");
                    continue;
                }

                await using var stream = info.OpenRead();
                var displayName = Path.GetRelativePath(folder, path);
                await AddBatchFileAsync(displayName, stream, AiBatchFileHelp.GuessContentType(path), updateStatus: false);
                added++;
            }
            catch (Exception ex)
            {
                skipped++;
                AppendLog($"跳过 {path}：{ex.Message}");
            }
        }

        NotifyBatchImportFinished(added, skipped);
    }

    public void NotifyBatchImportFinished(int added, int skipped)
    {
        StatusMessage = skipped > 0
            ? $"已添加 {added} 个文件，跳过 {skipped} 个（类型不符或超过 20MB）。当前共 {_batchItems.Count} 个。"
            : $"已添加 {added} 个文件（共 {_batchItems.Count} 个）。";
    }

    public static bool IsSupportedBatchFile(string pathOrName) => AiBatchFileHelp.IsSupported(pathOrName);

    public async Task RunSingleAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "请先配置并保存 API Key。";
            return;
        }

        if (string.IsNullOrWhiteSpace(SingleInputText))
        {
            StatusMessage = "请粘贴文本内容，或使用批量模式上传文件。";
            return;
        }

        IsBusy = true;
        AppendLog("开始单条提取…");
        try
        {
            var raw = await ExtractOneAsync("inline-text", text: SingleInputText, imageDataUrl: null);
            OutputJson = raw;
            StatusMessage = "单条提取完成。";
            AppendLog("单条提取成功。");
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            AppendLog($"失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RunBatchAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "请先配置并保存 API Key。";
            return;
        }

        if (_batchItems.Count == 0)
        {
            StatusMessage = "请先添加批量文件。";
            return;
        }

        IsBusy = true;
        ProcessedCount = 0;
        TotalCount = _batchItems.Count;
        ProgressPercent = 0;
        AppendLog($"开始批量提取，共 {TotalCount} 个文件…");

        var results = new List<Dictionary<string, object?>>();
        try
        {
            if (MergeBatchSend)
                await RunBatchMergedAsync(results);
            else
                await RunBatchSequentialAsync(results);

            OutputJson = AiJsonResponseHelp.SerializeForDisplay(results);
            StatusMessage = $"批量完成：{ProcessedCount}/{TotalCount}。";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunBatchSequentialAsync(List<Dictionary<string, object?>> results)
    {
        foreach (var item in _batchItems)
        {
            try
            {
                if (item.IsImage && !SupportsVision)
                    throw new InvalidOperationException(VisionUnsupportedMessage);

                var json = await ExtractOneAsync(item.FileName, item.Text, item.ImageDataUrl);
                results.Add(AiExtractBatchResults.Ok(item.FileName, json));
                AppendLog($"✓ {item.FileName}");
            }
            catch (Exception ex)
            {
                results.Add(AiExtractBatchResults.Fail(item.FileName, ex.Message));
                AppendLog($"✗ {item.FileName}: {ex.Message}");
            }

            BumpProgress();
        }
    }

    private async Task RunBatchMergedAsync(List<Dictionary<string, object?>> results)
    {
        var chunkSize = AiProviderCatalog.GetMaxMergeBatchSize(SelectedProvider);

        foreach (var chunk in _batchItems.Chunk(chunkSize))
        {
            var chunkList = chunk.ToList();
            var unsupportedImages = chunkList.Where(x => x.IsImage && !SupportsVision).ToList();
            foreach (var item in unsupportedImages)
            {
                results.Add(AiExtractBatchResults.Fail(item.FileName, VisionUnsupportedMessage));
                AppendLog($"✗ {item.FileName}: 不支持图片");
                BumpProgress();
            }

            var processable = chunkList.Where(x => !x.IsImage || SupportsVision).ToList();
            if (processable.Count == 0)
                continue;

            await RunMergedChunkAsync(processable, results, includeImages: processable.Any(x => x.IsImage));
        }
    }

    private async Task RunMergedChunkAsync(
        IReadOnlyList<BatchInputItem> chunk,
        List<Dictionary<string, object?>> results,
        bool includeImages)
    {
        var label = string.Join(", ", chunk.Select(x => x.FileName));
        try
        {
            AppendLog($"→ 合并请求（{chunk.Count} 个）：{label}");
            var chunkResults = await ExtractMergedAsync(chunk, includeImages);
            results.AddRange(chunkResults);
            foreach (var item in chunkResults)
            {
                var source = item.TryGetValue("source", out var sourceNode) ? sourceNode?.ToString() : "?";
                var ok = item.TryGetValue("success", out var successNode) && successNode is bool success && success;
                AppendLog(ok ? $"✓ {source}" : $"✗ {source}: {item.GetValueOrDefault("error")}");
                BumpProgress();
            }
        }
        catch (Exception ex)
        {
            foreach (var item in chunk)
            {
                results.Add(AiExtractBatchResults.Fail(item.FileName, ex.Message));
                AppendLog($"✗ {item.FileName}: {ex.Message}");
                BumpProgress();
            }
        }
    }

    private async Task<List<Dictionary<string, object?>>> ExtractMergedAsync(
        IReadOnlyList<BatchInputItem> items,
        bool includeImages)
    {
        var imageUrls = includeImages
            ? items.Where(x => x.IsImage).Select(x => x.ImageDataUrl!).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            : [];

        var request = new AiChatRequest
        {
            Provider = SelectedProvider,
            ApiKey = ApiKey,
            Model = Model,
            SystemPrompt = AiExtractTemplates.MergeBatchSystemPrompt,
            UserPrompt = BuildMergedUserPrompt(items, includeImages),
            ImageDataUrls = imageUrls.Count > 0 ? imageUrls : null,
            JsonOnly = true,
            JsonArrayOutput = true
        };

        var raw = await aiChatService.CompleteAsync(request);
        var json = AiJsonResponseHelp.NormalizeToJson(raw);
        return ParseMergedResults(json, items);
    }

    private string BuildMergedUserPrompt(IReadOnlyList<BatchInputItem> items, bool includeImages)
    {
        var template = EffectiveExtractTemplate;

        var builder = new StringBuilder();
        builder.AppendLine("提取模板：");
        builder.AppendLine(template);
        builder.AppendLine();
        builder.AppendLine($"共 {items.Count} 个来源。请分别提取，严格输出 JSON 数组，每项格式：");
        builder.AppendLine("{\"source\":\"文件名\",\"data\":{...}}");
        builder.AppendLine("数组顺序必须与下列来源顺序一致。");
        builder.AppendLine();

        var imageIndex = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            builder.AppendLine($"--- 来源 {i + 1}: {item.FileName} ---");
            if (!item.IsImage && !string.IsNullOrWhiteSpace(item.Text))
            {
                builder.AppendLine("待提取内容：");
                builder.AppendLine(item.Text.Trim());
            }
            else if (item.IsImage && includeImages)
            {
                imageIndex++;
                builder.AppendLine($"（见附带的第 {imageIndex} 张图片，与来源顺序对应）");
            }
            else if (item.IsImage)
            {
                builder.AppendLine("（当前提供商不支持图片，已跳过图像内容）");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static List<Dictionary<string, object?>> ParseMergedResults(string json, IReadOnlyList<BatchInputItem> items)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var array = root.EnumerateArray().ToList();
            var results = new List<Dictionary<string, object?>>(items.Count);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (i >= array.Count)
                {
                    results.Add(AiExtractBatchResults.Fail(item.FileName, "合并响应缺少该项结果。"));
                    continue;
                }

                results.Add(MapMergedArrayElement(array[i], item.FileName));
            }

            return results;
        }

        if (items.Count == 1)
        {
            return [AiExtractBatchResults.Ok(items[0].FileName, root.Clone())];
        }

        throw new InvalidOperationException("合并响应不是 JSON 数组，无法拆分多项结果。");
    }

    private static Dictionary<string, object?> MapMergedArrayElement(JsonElement element, string fallbackSource)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("success", out var successNode) &&
            successNode.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            var mapped = new Dictionary<string, object?>
            {
                ["source"] = element.TryGetProperty("source", out var sourceNode) && sourceNode.ValueKind == JsonValueKind.String
                    ? sourceNode.GetString() ?? fallbackSource
                    : fallbackSource,
                ["success"] = successNode.GetBoolean()
            };

            if (successNode.GetBoolean())
            {
                if (element.TryGetProperty("data", out var dataNode))
                    mapped["data"] = dataNode.Clone();
            }
            else if (element.TryGetProperty("error", out var errorNode) && errorNode.ValueKind == JsonValueKind.String)
            {
                mapped["error"] = errorNode.GetString();
            }

            return mapped;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("data", out var nestedData))
        {
            return new Dictionary<string, object?>
            {
                ["source"] = element.TryGetProperty("source", out var sourceNode) && sourceNode.ValueKind == JsonValueKind.String
                    ? sourceNode.GetString() ?? fallbackSource
                    : fallbackSource,
                ["success"] = true,
                ["data"] = nestedData.Clone()
            };
        }

        return new Dictionary<string, object?>
        {
            ["source"] = fallbackSource,
            ["success"] = true,
            ["data"] = element.Clone()
        };
    }

    public void ClearOutput()
    {
        OutputJson = string.Empty;
        LogText = string.Empty;
        StatusMessage = "输出已清空。";
    }

    private async Task<string> ExtractOneAsync(string sourceName, string? text, string? imageDataUrl)
    {
        var userPrompt = BuildUserPrompt(text, sourceName);
        var request = new AiChatRequest
        {
            Provider = SelectedProvider,
            ApiKey = ApiKey,
            Model = Model,
            SystemPrompt = SystemPrompt,
            UserPrompt = userPrompt,
            ImageDataUrl = imageDataUrl,
            JsonOnly = true
        };

        var raw = await aiChatService.CompleteAsync(request);
        return AiJsonResponseHelp.NormalizeToJson(raw);
    }

    private string BuildUserPrompt(string? text, string sourceName)
    {
        var template = EffectiveExtractTemplate;

        if (!string.IsNullOrWhiteSpace(text))
        {
            return $"""
                来源：{sourceName}

                提取模板：
                {template}

                待提取内容：
                {text.Trim()}
                """;
        }

        return $"""
            来源：{sourceName}

            提取模板：
            {template}

            请从附件图片中提取字段。
            """;
    }

    private void ApplyTemplatePreset(string templateId)
    {
        var preset = AiExtractTemplates.Presets.FirstOrDefault(x => x.Id == templateId);
        if (preset is not null)
            ExtractTemplate = preset.Template;
    }

    private void BumpProgress()
    {
        ProcessedCount++;
        ProgressPercent = TotalCount == 0 ? 0 : ProcessedCount * 100.0 / TotalCount;
    }

    private void AppendLog(string line)
    {
        var stamp = DateTime.Now.ToString("HH:mm:ss");
        LogText = string.IsNullOrEmpty(LogText) ? $"[{stamp}] {line}" : $"{LogText}{Environment.NewLine}[{stamp}] {line}";
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    public sealed record BatchInputItem(
        string FileName,
        bool IsImage,
        string? ImageDataUrl,
        string? Text);
}
