using Blazing.Mvvm;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ToolBox;
using ToolBox.Services;
using ToolBox.Services.Ai;
using ToolBox.Services.DirectorySync;
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
        services.AddScoped<IAiApiKeyService, BrowserAiApiKeyService>();
        services.AddScoped<IFolderPickerService, UnsupportedFolderPickerService>();
        services.AddScoped<IDirectorySyncService, UnsupportedDirectorySyncService>();

        // WASM 客户端走 HTTP 代理；WebApp 服务端由 AddToolBoxAiChatBackend 注册 IAiChatService
        if (hostingModel == BlazorHostingModelType.WebAssembly)
        {
            services.AddScoped<IAiChatService, WebAiChatClientService>();
            services.AddScoped(sp =>
            {
                var navigation = sp.GetRequiredService<NavigationManager>();
                return new HttpClient { BaseAddress = new Uri(navigation.BaseUri) };
            });
        }

        return services;
    }
}
