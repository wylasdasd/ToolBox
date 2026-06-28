namespace ToolBox.Tools.Network;

public sealed record HttpRequestData(
    string Method,
    string Url,
    IReadOnlyList<(string Name, string Value)> Headers,
    string Body);

public sealed record RequestToCurlResult(string Command, HttpRequestData Request);
