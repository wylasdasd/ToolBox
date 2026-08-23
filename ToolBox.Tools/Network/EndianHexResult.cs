namespace ToolBox.Tools.Network;

/// <summary>
/// EndianHexDataType：用于表示和处理相关的。
/// </summary>
public sealed record EndianHexDataType(string Id, string Label, int ByteSize, bool IsFloat);

/// <summary>
/// EndianHexResult：用于表示和处理相关的。
/// </summary>
public sealed record EndianHexResult(string HexOutput, string BinaryOutput, string DecimalOutput);
