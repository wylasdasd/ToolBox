namespace ToolBox.Tools.Encoding;

/// <summary>
/// TextHexEncodeResult：用于表示和处理相关的。
/// </summary>
public sealed record TextHexEncodeResult(string HexInput, string Output);

/// <summary>
/// TextHexDecodeResult：用于表示和处理相关的。
/// </summary>
public sealed record TextHexDecodeResult(string TextInput, string Output);
