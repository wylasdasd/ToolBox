using Blazing.Mvvm;
using DiffPlex;
using DiffPlex.DiffBuilder;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace ToolBox;

public static class ServiceExtensions
{
    public static IServiceCollection AddToolBoxCore(
        this IServiceCollection services,
        BlazorHostingModelType hostingModel)
    {
        services.AddMudServices();
        services.AddMvvm(options => options.HostingModelType = hostingModel);
        services.AddScoped<ISideBySideDiffBuilder, SideBySideDiffBuilder>();
        services.AddScoped<IDiffer, Differ>();
        return services;
    }
}