namespace ToolBox.Tools.Calculation;

/// <summary>
/// StructLayoutResult：用于表示和处理相关的。
/// </summary>
public sealed record StructLayoutResult(
    string LayoutKindText,
    IReadOnlyList<FieldLayoutItem> Fields,
    IReadOnlyList<ByteRowItem> ByteRows,
    int RawSize,
    int TotalSize,
    int StructAlignment);

/// <summary>
/// LayoutKind：用于表示和处理相关的。
/// </summary>
public enum LayoutKind
{
    Struct,
    Union
}

/// <summary>
/// FieldLayoutItem：用于表示和处理相关的。
/// </summary>
public sealed record FieldLayoutItem(
    string Name,
    string TypeDisplay,
    int OffsetByte,
    int SizeByte,
    int? BitOffsetInByte,
    int? BitWidth,
    int Alignment,
    string Color,
    int StartBit,
    int LengthBits,
    bool IsBitField,
    bool IsUnionMember);

/// <summary>
/// ByteRowItem：用于表示和处理相关的。
/// </summary>
public sealed record ByteRowItem(int StartOffset, List<ByteCellItem> Bytes);

/// <summary>
/// ByteCellItem：用于表示和处理相关的。
/// </summary>
public sealed record ByteCellItem(int ByteOffset, List<BitCellItem> Bits);

/// <summary>
/// BitCellItem：用于表示和处理相关的。
/// </summary>
public sealed record BitCellItem(
    int BitIndex,
    string Label,
    string Tooltip,
    string Background,
    bool IsPadding,
    bool IsOverlap);
