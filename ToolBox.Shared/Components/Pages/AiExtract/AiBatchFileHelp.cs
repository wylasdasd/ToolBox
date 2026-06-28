namespace ToolBox.Components.Pages.AiExtract;

internal static class AiBatchFileHelp
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".csv", ".json", ".md",
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp"
    };

    public static bool IsSupported(string pathOrName) =>
        SupportedExtensions.Contains(Path.GetExtension(pathOrName));

    public static bool IsImage(string fileName, string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return true;

        return ImageExtensions.Contains(Path.GetExtension(fileName));
    }

    public static string GuessContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".json" => "application/json",
            ".csv" => "text/csv",
            _ => "text/plain"
        };

    public static string GuessImageMime(string fileName, string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return contentType;

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
    }
}
