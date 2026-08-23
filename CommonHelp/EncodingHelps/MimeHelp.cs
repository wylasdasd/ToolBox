namespace CommonHelp;

/// <summary>
/// MIME 类型辅助工具：扩展名推断 MIME、Base64 图片魔数检测、Base64 校验与 Data URI（RFC 2397）解析/构建。
/// </summary>
public static class MimeHelp
{
    /// <summary>
    /// 按文件扩展名推断常见 MIME 类型（png/jpeg/webp/gif/bmp/svg/json/csv 等）；未知扩展名返回 text/plain。
    /// </summary>
    public static string GuessContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".json" => "application/json",
            ".csv" => "text/csv",
            ".md" => "text/markdown",
            _ => "text/plain"
        };

    /// <summary>
    /// 按文件名或已有 Content-Type 推断图片 MIME：contentType 以 image/ 开头时原样返回，否则按扩展名推断（缺省 image/jpeg）。
    /// </summary>
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
            ".svg" => "image/svg+xml",
            _ => "image/jpeg"
        };
    }

    /// <summary>
    /// 按 Base64 前缀魔数检测图片类型（png/jpeg/gif/svg/bmp/webp）；无法识别返回空字符串。
    /// </summary>
    public static string DetectImageMimeType(string base64)
    {
        if (base64.StartsWith("iVBORw0KGgo")) return "image/png";
        if (base64.StartsWith("/9j/")) return "image/jpeg";
        if (base64.StartsWith("R0lGODlh")) return "image/gif";
        if (base64.StartsWith("PHN2Zy")) return "image/svg+xml";
        if (base64.StartsWith("Qk0")) return "image/bmp";
        if (base64.StartsWith("UklGR")) return "image/webp";
        return string.Empty;
    }

    /// <summary>
    /// 校验字符串是否为合法 Base64：空白返回 false，长度不足时按补齐容忍校验。
    /// </summary>
    public static bool IsValidBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var buffer = new Span<byte>(new byte[value.Length]);
        return Convert.TryFromBase64String(value, buffer, out _);
    }

    /// <summary>
    /// 尝试拆解 data:&lt;mime&gt;;base64,&lt;payload&gt; 形式的 Data URI：非该形式返回 false，输出参数为空字符串。
    /// </summary>
    public static bool TryParseDataUrl(
        string input,
        out string dataUrl,
        out string base64Part,
        out string mimeType)
    {
        dataUrl = string.Empty;
        base64Part = string.Empty;
        mimeType = string.Empty;

        if (!input.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return false;

        var commaIndex = input.IndexOf(',');
        if (commaIndex <= 5)
            return false;

        var metadata = input.Substring(5, commaIndex - 5);
        if (!metadata.Contains(";base64", StringComparison.OrdinalIgnoreCase))
            return false;

        mimeType = metadata.Split(';')[0];
        if (string.IsNullOrWhiteSpace(mimeType))
            mimeType = "image/*";

        base64Part = input[(commaIndex + 1)..];
        dataUrl = $"data:{mimeType};base64,{base64Part}";
        return true;
    }

    /// <summary>
    /// 构建 data:&lt;mimeType&gt;;base64,&lt;payload&gt; 形式的 Data URI。
    /// </summary>
    public static string BuildDataUrl(string mimeType, byte[] bytes) =>
        $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
}
