namespace ToolBox.Tools.Calculation;

public sealed record StructLayoutResult(
    string LayoutKindText,
    IReadOnlyList<FieldLayoutItem> Fields,
    IReadOnlyList<ByteRowItem> ByteRows,
    int RawSize,
    int TotalSize,
    int StructAlignment);

public enum LayoutKind
{
    Struct,
    Union
}

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

public sealed record ByteRowItem(int StartOffset, List<ByteCellItem> Bytes);

public sealed record ByteCellItem(int ByteOffset, List<BitCellItem> Bits);

public sealed record BitCellItem(
    int BitIndex,
    string Label,
    string Tooltip,
    string Background,
    bool IsPadding,
    bool IsOverlap);
