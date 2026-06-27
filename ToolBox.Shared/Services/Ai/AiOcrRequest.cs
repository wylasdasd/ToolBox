namespace ToolBox.Services.Ai;

public sealed class AiOcrRequest
{
    public AiProviderKind Provider { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string ImageDataUrl { get; init; } = string.Empty;
    public string Prompt { get; init; } = "请提取图片中的全部可读文本，保持原始换行。";
}
