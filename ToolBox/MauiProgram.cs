using Microsoft.Extensions.Logging;

using Blazing.Mvvm;
using ToolBox.Services;
using ToolBox.Services.Ai;
using ToolBox.Services.DirectorySync;
using ToolBox.Services.MauiPlatform;
using ToolBox.Services.Ocr;
using ToolBox.Services.Picker;

namespace ToolBox
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiPlatformFileAccess.Register();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddToolBoxCore(BlazorHostingModelType.HybridMaui);
            builder.Services.AddScoped<IImageOcrService, WindowsImageOcrService>();
            builder.Services.AddScoped<IFolderPickerService, FolderPickerService>();
            builder.Services.AddScoped<IDirectorySyncService, DirectorySyncService>();
            builder.Services.AddScoped<IImagePickerService, ImagePickerService>();
            builder.Services.AddScoped<IClipboardService, MauiClipboardService>();
            builder.Services.AddSingleton<IDefaultRouteProvider, MauiDefaultRouteProvider>();
            builder.Services.AddScoped<IAiApiKeyService, AiApiKeyService>();
            builder.Services.AddScoped<IAiAskService, SemanticKernelAiAskService>();
            builder.Services.AddScoped<IAiOcrService, OpenAiCompatAiOcrService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}