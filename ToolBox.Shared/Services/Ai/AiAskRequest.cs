namespace ToolBox.Services.Ai;

public sealed class AiAskRequest
{
    public AiProviderKind Provider { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public string OcrText { get; init; } = string.Empty;
}
