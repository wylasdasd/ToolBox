using Blazing.Mvvm;
using Microsoft.Extensions.Logging;
using ToolBox.Services;
using ToolBox.Services.Ai;
using ToolBox.Services.DirectorySync;
using ToolBox.Services.MauiPlatform;
using ToolBox.Services.Picker;

namespace ToolBox;

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
        builder.Services.AddScoped<IAiApiKeyService, MauiAiApiKeyService>();
        builder.Services.AddScoped<OpenAiCompatChatService>();
        builder.Services.AddScoped<CursorCloudAgentChatService>();
        builder.Services.AddScoped<IAiChatService, AiChatServiceRouter>();
        builder.Services.AddScoped<IClipboardService, MauiClipboardService>();
        builder.Services.AddSingleton<IDefaultRouteProvider, MauiDefaultRouteProvider>();
        builder.Services.AddScoped<IFolderPickerService, FolderPickerService>();
        builder.Services.AddScoped<IDirectorySyncService, DirectorySyncService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
