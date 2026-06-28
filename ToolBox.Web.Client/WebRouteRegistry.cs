using BlazorRouteData = Microsoft.AspNetCore.Components.RouteData;

namespace ToolBox.Web.Client;

public static class WebRouteRegistry
{
    private static readonly HashSet<string> EnabledRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/text-lines", "/regex", "/diff", "/base64", "/url-codec", "/jwt",
        "/json-format", "/json-to-csharp", "/timestamp", "/uuid", "/base64-image", "/svg-preview",
        "/bitwise", "/struct-layout", "/converters", "/request-to-curl", "/ai-extract",
        "/ocr-ai", "/ai-ocr", "/ocr-regex-extract", "/ocr-regex-batch", "/not-found",
    };

    public static bool IsEnabled(BlazorRouteData routeData)
    {
        var template = routeData.Template;
        if (string.IsNullOrWhiteSpace(template)) template = "/";
        if (!template.StartsWith('/')) template = "/" + template;
        var path = template.Split('?', 2)[0].TrimEnd('/');
        if (path.Length == 0) path = "/";
        return EnabledRoutes.Contains(path);
    }
}