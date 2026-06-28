using Blazing.Mvvm;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ToolBox;
using ToolBox.Services;
using ToolBox.Services.Ai;
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
        services.AddScoped<IAiChatService, WebAiChatClientService>();
        services.AddScoped<IFolderPickerService, UnsupportedFolderPickerService>();
        services.AddScoped(sp =>
        {
            var host = sp.GetRequiredService<IWebAssemblyHostEnvironment>();
            return new HttpClient { BaseAddress = new Uri(host.BaseAddress) };
        });
        return services;
    }
}
