using Blazing.Mvvm.ComponentModel;
using CommonTool.StringHelp;
using ToolBox.Services.Ai;
using ToolBox.Services.Ocr;

namespace ToolBox.Components.Pages.OcrAi;

public sealed class OcrAiVM : ViewModelBase
{
    private readonly IImageOcrService _imageOcrService;
    private readonly IAiApiKeyService _aiApiKeyService;
    private readonly IAiAskService _aiAskService;

    private string _sourceFileName = "111.jpg";
    private string _ocrLanguage = "zh-Hans";
    private AiProviderKind _selectedProvider = AiProviderKind.Gemini;
    private string _apiKey = string.Empty;
    private string _model = AiProviderCatalog.GetDefaultModel(AiProviderKind.Gemini);
    private string _prompt = "请基于 OCR 文本提取关键信息，并给出简洁结论。";
    private bool _removeSpaces;
    private string _ocrText = string.Empty;
    private string _aiResult = string.Empty;
    private string _statusMessage = "请先配置 API Key。";
    private bool _isBusy;

    public OcrAiVM(
        IImageOcrService imageOcrService,
        IAiApiKeyService aiApiKeyService,
        IAiAskService aiAskService)
    {
        _imageOcrService = imageOcrService;
        _aiApiKeyService = aiApiKeyService;
        _aiAskService = aiAskService;
    }

    public string SourceFileName
    {
        get => _sourceFileName;
        set => SetProperty(ref _sourceFileName, value);
    }

    public AiProviderKind SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (!SetProperty(ref _selectedProvider, value))
            {
                return;
            }

            Model = AiProviderCatalog.GetDefaultModel(value);
            _ = LoadApiKeyForSelectedProviderAsync();
        }
    }

    public string ProviderDisplayName => AiProviderCatalog.GetDisplayName(SelectedProvider);

    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    public string Prompt
    {
        get => _prompt;
        set => SetProperty(ref _prompt, value);
    }

    public bool RemoveSpaces
    {
        get => _removeSpaces;
        set => SetProperty(ref _removeSpaces, value);
    }

    public string OcrLanguage
    {
        get => _ocrLanguage;
        set => SetProperty(ref _ocrLanguage, value);
    }

    public string OcrText
    {
        get => _ocrText;
        private set => SetProperty(ref _ocrText, value);
    }

    public string AiResult
    {
        get => _aiResult;
        private set => SetProperty(ref _aiResult, value);
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

    public override async Task OnInitializedAsync()
    {
        await LoadApiKeyForSelectedProviderAsync();
    }

    public async Task SaveApiKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "API Key 不能为空。";
            return;
        }

        await _aiApiKeyService.SaveApiKeyAsync(SelectedProvider, ApiKey);
        StatusMessage = $"{ProviderDisplayName} API Key 保存成功。";
    }

    public async Task RunOcrAndAskAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Prompt))
        {
            StatusMessage = "Prompt 不能为空。";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "正在 OCR 识别...";
            AiResult = string.Empty;

            await using var imageStream = await FileSystem.Current.OpenAppPackageFileAsync(SourceFileName);
            var recognized = await _imageOcrService.RecognizeTextAsync(imageStream, OcrLanguage);
            OcrText = OcrTextNormalizeHelp.NormalizeSymbolsToAscii(recognized, RemoveSpaces);

            StatusMessage = $"OCR 完成，正在调用 {ProviderDisplayName}...";
            var apiKey = await _aiApiKeyService.GetApiKeyAsync(SelectedProvider);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = ApiKey;
            }

            var result = await _aiAskService.AskByOcrAsync(new AiAskRequest
            {
                Provider = SelectedProvider,
                ApiKey = apiKey ?? string.Empty,
                Model = Model,
                UserPrompt = Prompt,
                OcrText = OcrText
            });

            AiResult = result;
            StatusMessage = "处理完成。";
        }
        catch (FileNotFoundException)
        {
            OcrText = string.Empty;
            AiResult = string.Empty;
            StatusMessage = $"未找到文件：{SourceFileName}。请确认文件已打包到 Resources/Raw。";
        }
        catch (Exception ex)
        {
            AiResult = string.Empty;
            StatusMessage = $"处理失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadApiKeyForSelectedProviderAsync()
    {
        var saved = await _aiApiKeyService.GetApiKeyAsync(SelectedProvider);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            ApiKey = saved;
            StatusMessage = $"已加载保存的 {ProviderDisplayName} API Key。";
            return;
        }

        ApiKey = string.Empty;
        StatusMessage = $"请先配置 {ProviderDisplayName} API Key。";
    }
}
