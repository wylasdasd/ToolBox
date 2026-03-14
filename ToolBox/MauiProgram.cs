using Microsoft.Extensions.Logging;

using Blazing.Mvvm;
using DiffPlex;
using DiffPlex.DiffBuilder;
using MudBlazor.Services;
using ToolBox.Services.Ai;
using ToolBox.Services.Ocr;
using ToolBox.Services.Picker;

namespace ToolBox
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();
            builder.Services.AddMvvm(options =>
            {
                options.HostingModelType = BlazorHostingModelType.HybridMaui;
            });
            builder.Services.AddScoped<ISideBySideDiffBuilder, SideBySideDiffBuilder>();
            builder.Services.AddScoped<IDiffer, Differ>();
            builder.Services.AddScoped<IImageOcrService, WindowsImageOcrService>();
            builder.Services.AddScoped<IFolderPickerService, FolderPickerService>();
            builder.Services.AddScoped<IGeminiApiKeyService, GeminiApiKeyService>();
            builder.Services.AddScoped<IGeminiAskService, GeminiAskService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
