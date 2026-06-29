using ToolBox.Components.Layout;
using BlazorRouteData = Microsoft.AspNetCore.Components.RouteData;

namespace ToolBox.Web.Client;

public static class WebRouteRegistry
{
    private static readonly string[] AdditionalWebRoutes =
    [
        "/timestamp",
        "/ocr-ai",
        "/ai-ocr",
        "/ocr-regex-extract",
        "/ocr-regex-batch",
        "/not-found",
    ];

    private static readonly HashSet<string> EnabledRoutes = BuildEnabledRoutes();

    public static bool IsEnabled(BlazorRouteData routeData)
    {
        var template = routeData.Template;
        if (string.IsNullOrWhiteSpace(template)) template = "/";
        if (!template.StartsWith('/')) template = "/" + template;
        var path = template.Split('?', 2)[0].TrimEnd('/');
        if (path.Length == 0) path = "/";
        return EnabledRoutes.Contains(path);
    }

    private static HashSet<string> BuildEnabledRoutes()
    {
        var routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/" };
        foreach (var path in ToolNavDefinition.GetWebRoutePaths())
            routes.Add(path);
        foreach (var path in AdditionalWebRoutes)
            routes.Add(path);
        return routes;
    }
}
