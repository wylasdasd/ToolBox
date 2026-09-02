using CommonHelp;

namespace ToolBox.Services.Ai;

public static class AiJsonResponseHelp
{
    public static string NormalizeToJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("AI 返回为空。");

        return MarkdownJsonHelp.NormalizeToJson(raw);
    }

    public static string SerializeForDisplay<T>(T value) => value.JsonSe(JsonExtensions.IndentedRelaxedOptions);
}
