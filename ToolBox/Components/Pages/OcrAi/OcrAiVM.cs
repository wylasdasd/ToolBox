using Blazing.Mvvm.ComponentModel;
using CommonTool.StringHelp;
using ToolBox.Services.Ai;
using ToolBox.Services.Ocr;

namespace ToolBox.Components.Pages.OcrAi;

public sealed class OcrAiVM : ViewModelBase
{
    private readonly IImageOcrService _imageOcrService;
    private readonly IGeminiApiKeyService _geminiApiKeyService;
    private readonly IGeminiAskService _geminiAskService;

    private string _sourceFileName = "111.jpg";
    private string _ocrLanguage = "zh-Hans";
    private string _geminiApiKey = string.Empty;
    private string _geminiModel = "gemini 3 flash";
    private string _prompt = "请基于 OCR 文本提取关键信息，并给出简洁结论。";
    private bool _removeSpaces;
    private string _ocrText = string.Empty;
    private string _aiResult = string.Empty;
    private string _statusMessage = "请先配置 Gemini API Key。";
    private bool _isBusy;

    public OcrAiVM(
        IImageOcrService imageOcrService,
        IGeminiApiKeyService geminiApiKeyService,
        IGeminiAskService geminiAskService)
    {
        _imageOcrService = imageOcrService;
        _geminiApiKeyService = geminiApiKeyService;
        _geminiAskService = geminiAskService;
    }

    public string SourceFileName
    {
        get => _sourceFileName;
        set => SetProperty(ref _sourceFileName, value);
    }

    public string GeminiApiKey
    {
        get => _geminiApiKey;
        set => SetProperty(ref _geminiApiKey, value);
    }

    public string GeminiModel
    {
        get => _geminiModel;
        set => SetProperty(ref _geminiModel, value);
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
        var saved = await _geminiApiKeyService.GetApiKeyAsync();
        if (!string.IsNullOrWhiteSpace(saved))
        {
            GeminiApiKey = saved;
            StatusMessage = "已加载保存的 Gemini API Key。";
        }
    }

    public async Task SaveApiKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(GeminiApiKey))
        {
            StatusMessage = "API Key 不能为空。";
            return;
        }

        await _geminiApiKeyService.SaveApiKeyAsync(GeminiApiKey);
        StatusMessage = "Gemini API Key 保存成功。";
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

            StatusMessage = "OCR 完成，正在调用 Gemini...";
            var apiKey = await _geminiApiKeyService.GetApiKeyAsync();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = GeminiApiKey;
            }

            var result = await _geminiAskService.AskByOcrAsync(
                apiKey ?? string.Empty,
                GeminiModel,
                Prompt,
                OcrText);

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
}
