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
        OcrCropOptions? cropOptions = null,
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
        var transform = CreateBitmapTransform(decoder, cropOptions);
        var bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            transform,
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);

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

#if WINDOWS
    private static BitmapTransform CreateBitmapTransform(BitmapDecoder decoder, OcrCropOptions? cropOptions)
    {
        var transform = new BitmapTransform();
        if (cropOptions is null || !cropOptions.Enabled)
        {
            return transform;
        }

        var imageWidth = (int)decoder.PixelWidth;
        var imageHeight = (int)decoder.PixelHeight;
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return transform;
        }

        var x = Math.Clamp(cropOptions.X, 0, imageWidth - 1);
        var y = Math.Clamp(cropOptions.Y, 0, imageHeight - 1);
        var width = Math.Clamp(cropOptions.Width, 1, imageWidth - x);
        var height = Math.Clamp(cropOptions.Height, 1, imageHeight - y);

        // Windows OCR 先按区域裁剪再识别，可显著减少误识别干扰。
        transform.Bounds = new BitmapBounds
        {
            X = (uint)x,
            Y = (uint)y,
            Width = (uint)width,
            Height = (uint)height
        };
        return transform;
    }
#endif
}
