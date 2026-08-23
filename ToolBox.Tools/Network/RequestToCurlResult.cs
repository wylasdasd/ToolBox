namespace ToolBox.Tools.Network;

/// <summary>
/// HttpRequestData：用于表示和处理相关的。
/// </summary>
public sealed record HttpRequestData(
    string Method,
    string Url,
    IReadOnlyList<(string Name, string Value)> Headers,
    string Body);

/// <summary>
/// RequestToCurlResult：用于表示和处理相关的。
/// </summary>
public sealed record RequestToCurlResult(string Command, HttpRequestData Request);
