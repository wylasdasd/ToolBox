namespace ToolBox.Services.Ai;

public sealed class AiChatRequest
{
    public AiProviderKind Provider { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public string? ImageDataUrl { get; init; }
    public IReadOnlyList<string>? ImageDataUrls { get; init; }
    public bool JsonOnly { get; init; } = true;
    public bool JsonArrayOutput { get; init; }
}
