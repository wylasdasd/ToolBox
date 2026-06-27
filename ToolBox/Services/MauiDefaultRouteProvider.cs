using ToolBox.Services;

namespace ToolBox.Services.MauiPlatform;

public sealed class MauiDefaultRouteProvider : IDefaultRouteProvider
{
    public string DefaultRoute => "/csharp-run";
}