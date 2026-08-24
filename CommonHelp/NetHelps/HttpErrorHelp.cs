using System.Net;
using System.Text.Json;

namespace CommonHelp;

/// <summary>
/// HTTP 错误响应消息提取工具：从 JSON 错误体（error / error.message / message，兼容 OpenAI 风格）提取用户可读消息。
/// </summary>
public static class HttpErrorHelp
{
    /// <summary>
    /// 格式化 HTTP 失败消息：能从响应体提取到消息时拼为「prefix：HTTP 500，detail」，否则「prefix：HTTP 500。」。
    /// </summary>
    public static string FormatFailure(string prefix, HttpStatusCode code, string responseText)
    {
        var detail = TryExtractMessage(responseText);
        return string.IsNullOrWhiteSpace(detail)
            ? $"{prefix}：HTTP {(int)code}。"
            : $"{prefix}：HTTP {(int)code}，{detail}";
    }

    /// <summary>
    /// 聚合异常链消息：按 InnerException 深度拼接（默认「 → 」分隔），跳过空消息与相邻重复；全部为空时返回异常类型名。
    /// </summary>
    /// <param name="ex">起始异常。</param>
    /// <param name="separator">消息之间的分隔符。</param>
    public static string FormatExceptionChain(Exception ex, string separator = " → ")
    {
        var parts = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message) &&
                (parts.Count == 0 || parts[^1] != current.Message))
            {
                parts.Add(current.Message);
            }
        }

        return parts.Count == 0 ? ex.GetType().Name : string.Join(separator, parts);
    }

    /// <summary>
    /// 尝试从 JSON 错误响应体提取消息：依次识别 error（字符串）、error.message（嵌套对象）、message（字符串）；解析失败或无匹配返回 null。
    /// </summary>
    public static string? TryExtractMessage(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                    return error.GetString();

                if (error.ValueKind == JsonValueKind.Object &&
                    error.TryGetProperty("message", out var nestedMessage) &&
                    nestedMessage.ValueKind == JsonValueKind.String)
                {
                    return nestedMessage.GetString();
                }
            }

            if (root.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch
        {
        }

        return null;
    }
}
