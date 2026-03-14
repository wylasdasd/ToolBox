#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
#endif

namespace ToolBox.Services.Ocr;

public sealed class WindowsImageOcrService : IImageOcrService
{
    public async Task<string> RecognizeTextAsync(
        Stream imageStream,
        string? languageTag = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

#if WINDOWS
        // 使用 WinRT OCR 需要 IRandomAccessStream，这里先复制一份。
        await using var inputStream = imageStream;
        using var randomAccessStream = new InMemoryRandomAccessStream();
        await inputStream.CopyToAsync(randomAccessStream.AsStreamForWrite(), cancellationToken);
        randomAccessStream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);

        OcrEngine? engine = null;
        if (!string.IsNullOrWhiteSpace(languageTag))
        {
            engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(languageTag.Trim()));
        }

        engine ??= OcrEngine.TryCreateFromUserProfileLanguages()
            ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("zh-Hans"));
        if (engine is null)
        {
            throw new InvalidOperationException("系统未找到可用 OCR 语言包。");
        }

        var result = await engine.RecognizeAsync(bitmap);
        return result.Text?.Trim() ?? string.Empty;
#else
        throw new PlatformNotSupportedException("当前平台不支持 Windows OCR。");
#endif
    }
}
