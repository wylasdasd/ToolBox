using Microsoft.Extensions.Logging;

using Blazing.Mvvm;
using DiffPlex;
using DiffPlex.DiffBuilder;
using MudBlazor.Services;

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

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
