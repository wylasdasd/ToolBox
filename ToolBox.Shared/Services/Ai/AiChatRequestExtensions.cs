namespace ToolBox.Services.Ai;

public static class AiChatRequestExtensions
{
    public static IReadOnlyList<string> GetImageUrls(this AiChatRequest request)
    {
        if (request.ImageDataUrls is { Count: > 0 })
            return request.ImageDataUrls.Where(url => !string.IsNullOrWhiteSpace(url)).ToList();

        if (!string.IsNullOrWhiteSpace(request.ImageDataUrl))
            return [request.ImageDataUrl];

        return [];
    }
}
