namespace ToolBox.Tools.Network;

public sealed record EndianHexDataType(string Id, string Label, int ByteSize, bool IsFloat);

public sealed record EndianHexResult(string HexOutput, string BinaryOutput, string DecimalOutput);
