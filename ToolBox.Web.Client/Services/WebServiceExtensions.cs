using Blazing.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using ToolBox;
using ToolBox.Services;
using ToolBox.Services.Ai;
using ToolBox.Services.DirectorySync;
using ToolBox.Services.Ocr;
using ToolBox.Services.Picker;

namespace ToolBox.Web.Client.Services;

public static class WebServiceExtensions
{
    public static IServiceCollection AddToolBoxWeb(
        this IServiceCollection services,
        BlazorHostingModelType hostingModel)
    {
        services.AddToolBoxCore(hostingModel);
        services.AddSingleton<IDefaultRouteProvider, WebDefaultRouteProvider>();
        services.AddScoped<IClipboardService, BrowserClipboardService>();
        services.AddWebPlatformStubs();
        return services;
    }

    public static IServiceCollection AddWebPlatformStubs(this IServiceCollection services)
    {
        services.AddScoped<IImageOcrService, UnsupportedImageOcrService>();
        services.AddScoped<IImagePickerService, UnsupportedImagePickerService>();
        services.AddScoped<IFolderPickerService, UnsupportedFolderPickerService>();
        services.AddScoped<IDirectorySyncService, UnsupportedDirectorySyncService>();
        services.AddScoped<IAiApiKeyService, UnsupportedAiApiKeyService>();
        services.AddScoped<IAiAskService, UnsupportedAiAskService>();
        services.AddScoped<IAiOcrService, UnsupportedAiOcrService>();
        return services;
    }
}