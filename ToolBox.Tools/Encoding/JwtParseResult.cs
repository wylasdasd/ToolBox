namespace ToolBox.Tools.Encoding;

/// <summary>
/// JwtParseResult：用于表示和处理相关的。
/// </summary>
public sealed record JwtParseResult(
    string HeaderJson,
    string PayloadJson,
    string ExpiryUtc,
    string ExpiryLocal);
