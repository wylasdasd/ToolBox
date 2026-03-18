using Blazing.Mvvm.ComponentModel;
using ToolBox.Services.Ai;
using ToolBox.Services.Picker;

namespace ToolBox.Components.Pages.AiOcr;

public sealed class AiOcrVM : ViewModelBase
{
    private readonly IAiApiKeyService _aiApiKeyService;
    private readonly IAiOcrService _aiOcrService;
    private readonly IImagePickerService _imagePickerService;
    private string _sourceFileName = "111.jpg";
    private string _imagePreviewDataUrl = string.Empty;
    private AiProviderKind _selectedProvider = AiProviderKind.Gemini;
    private string _apiKey = string.Empty;
    private string _model = AiProviderCatalog.GetDefaultModel(AiProviderKind.Gemini);
    private string _prompt = "请提取图片中的全部可读文本，保持原始换行。";
    private string _ocrText = string.Empty;
    private string _statusMessage = "请选择图片并配置 API Key。";
    private bool _isBusy;

    public AiOcrVM(
        IAiApiKeyService aiApiKeyService,
        IAiOcrService aiOcrService,
        IImagePickerService imagePickerService)
    {
        _aiApiKeyService = aiApiKeyService;
        _aiOcrService = aiOcrService;
        _imagePickerService = imagePickerService;
    }

    public string SourceFileName
    {
        get => _sourceFileName;
        set => SetProperty(ref _sourceFileName, value);
    }

    public string ImagePreviewDataUrl
    {
        get => _imagePreviewDataUrl;
        private set => SetProperty(ref _imagePreviewDataUrl, value);
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

    public string OcrText
    {
        get => _ocrText;
        private set => SetProperty(ref _ocrText, value);
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

    public async Task RunAiOcrAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            OcrText = string.Empty;
            StatusMessage = $"正在调用 {ProviderDisplayName} OCR...";

            await using var imageStream = await OpenSourceStreamAsync(SourceFileName);
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var imageDataUrl = BuildImageDataUrl(bytes, SourceFileName);
            ImagePreviewDataUrl = imageDataUrl;

            var apiKey = await _aiApiKeyService.GetApiKeyAsync(SelectedProvider);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = ApiKey;
            }

            var text = await _aiOcrService.RecognizeImageTextAsync(new AiOcrRequest
            {
                Provider = SelectedProvider,
                ApiKey = apiKey ?? string.Empty,
                Model = Model,
                Prompt = Prompt,
                ImageDataUrl = imageDataUrl
            });

            OcrText = text;
            StatusMessage = "识别完成。";
        }
        catch (FileNotFoundException)
        {
            OcrText = string.Empty;
            StatusMessage = $"未找到图片：{SourceFileName}。请先通过按钮选择图片，或输入已打包到 Resources/Raw 的文件名。";
        }
        catch (Exception ex)
        {
            OcrText = string.Empty;
            StatusMessage = $"识别失败：{ex.Message}";
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
            ImagePreviewDataUrl = BuildImageDataUrl(bytes, SourceFileName);
        }
        catch
        {
            ImagePreviewDataUrl = string.Empty;
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

    private static string BuildImageDataUrl(byte[] bytes, string sourceFileName)
    {
        var ext = Path.GetExtension(sourceFileName)?.ToLowerInvariant();
        var mime = ext switch
        {
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".tif" => "image/tiff",
            ".tiff" => "image/tiff",
            _ => "image/jpeg"
        };

        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }
}
