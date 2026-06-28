namespace ToolBox.Tools.Encoding;

public sealed record JwtParseResult(
    string HeaderJson,
    string PayloadJson,
    string ExpiryUtc,
    string ExpiryLocal);
