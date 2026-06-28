using System.Globalization;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Network;

public static class EndianHexService
{
    public static IReadOnlyList<EndianHexDataType> DataTypes { get; } =
    [
        new("int8", "int8 / sbyte", 1, false),
        new("uint8", "uint8 / byte", 1, false),
        new("int16", "int16", 2, false),
        new("uint16", "uint16", 2, false),
        new("int32", "int32", 4, false),
        new("uint32", "uint32", 4, false),
        new("int64", "int64", 8, false),
        new("uint64", "uint64", 8, false),
        new("float32", "float32", 4, true),
        new("float64", "float64", 8, true),
    ];

    public static ToolResult<EndianHexResult> ConvertValueToHex(
        string? input, string? dataTypeId, bool bigEndian, bool inputIsHex)
    {
        var type = ResolveDataType(dataTypeId);
        try
        {
            var bytes = inputIsHex
                ? ParseHexBytes(input ?? string.Empty, type.ByteSize)
                : EncodeValue(input ?? string.Empty, type, bigEndian);

            return ToolResult<EndianHexResult>.Ok(BuildResult(bytes, type, bigEndian));
        }
        catch (Exception ex)
        {
            return ToolResult<EndianHexResult>.Fail(ex.Message);
        }
    }

    public static ToolResult<EndianHexResult> ConvertHexToValue(string? input, string? dataTypeId, bool bigEndian)
    {
        var type = ResolveDataType(dataTypeId);
        try
        {
            var bytes = ParseHexBytes(input ?? string.Empty, type.ByteSize);
            return ToolResult<EndianHexResult>.Ok(BuildResult(bytes, type, bigEndian));
        }
        catch (Exception ex)
        {
            return ToolResult<EndianHexResult>.Fail(ex.Message);
        }
    }

    private static EndianHexDataType ResolveDataType(string? dataTypeId) =>
        DataTypes.FirstOrDefault(t => t.Id == dataTypeId) ?? DataTypes[4];

    private static EndianHexResult BuildResult(byte[] bytes, EndianHexDataType type, bool bigEndian) =>
        new(
            FormatHex(bytes),
            string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))),
            DecodeBytes(bytes, type, bigEndian));

    private static byte[] EncodeValue(string text, EndianHexDataType type, bool bigEndian)
    {
        text = text.Trim();
        return type.Id switch
        {
            "int8" => [(byte)sbyte.Parse(text, CultureInfo.InvariantCulture)],
            "uint8" => [byte.Parse(text, CultureInfo.InvariantCulture)],
            "int16" => Int16ToBytes(short.Parse(text, CultureInfo.InvariantCulture), bigEndian),
            "uint16" => UInt16ToBytes(ushort.Parse(text, CultureInfo.InvariantCulture), bigEndian),
            "int32" => Int32ToBytes(int.Parse(text, CultureInfo.InvariantCulture), bigEndian),
            "uint32" => UInt32ToBytes(uint.Parse(text, CultureInfo.InvariantCulture), bigEndian),
            "int64" => Int64ToBytes(long.Parse(text, CultureInfo.InvariantCulture), bigEndian),
            "uint64" => UInt64ToBytes(ulong.Parse(text, CultureInfo.InvariantCulture), bigEndian),
            "float32" => FloatToBytes(float.Parse(text, CultureInfo.InvariantCulture), bigEndian),
            "float64" => DoubleToBytes(double.Parse(text, CultureInfo.InvariantCulture), bigEndian),
            _ => throw new InvalidOperationException("未知类型。"),
        };
    }

    private static string DecodeBytes(byte[] bytes, EndianHexDataType type, bool bigEndian) =>
        type.Id switch
        {
            "int8" => ((sbyte)bytes[0]).ToString(CultureInfo.InvariantCulture),
            "uint8" => bytes[0].ToString(CultureInfo.InvariantCulture),
            "int16" => BitConverter.ToInt16(Order(bytes, bigEndian), 0).ToString(CultureInfo.InvariantCulture),
            "uint16" => BitConverter.ToUInt16(Order(bytes, bigEndian), 0).ToString(CultureInfo.InvariantCulture),
            "int32" => BitConverter.ToInt32(Order(bytes, bigEndian), 0).ToString(CultureInfo.InvariantCulture),
            "uint32" => BitConverter.ToUInt32(Order(bytes, bigEndian), 0).ToString(CultureInfo.InvariantCulture),
            "int64" => BitConverter.ToInt64(Order(bytes, bigEndian), 0).ToString(CultureInfo.InvariantCulture),
            "uint64" => BitConverter.ToUInt64(Order(bytes, bigEndian), 0).ToString(CultureInfo.InvariantCulture),
            "float32" => BitConverter.ToSingle(Order(bytes, bigEndian), 0).ToString("R", CultureInfo.InvariantCulture),
            "float64" => BitConverter.ToDouble(Order(bytes, bigEndian), 0).ToString("R", CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("未知类型。"),
        };

    private static byte[] ParseHexBytes(string input, int expectedSize)
    {
        var cleaned = input.Trim()
            .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
            .Replace("\\x", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(",", "");

        if (cleaned.Length == 0)
            throw new FormatException("请输入 Hex 字节。");

        if (cleaned.Length % 2 != 0)
            cleaned = "0" + cleaned;

        var bytes = new byte[cleaned.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = byte.Parse(cleaned.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        if (expectedSize > 0 && bytes.Length != expectedSize)
            throw new FormatException($"期望 {expectedSize} 字节，实际 {bytes.Length} 字节。");

        return bytes;
    }

    private static byte[] Order(byte[] bytes, bool bigEndian)
    {
        if (bigEndian == BitConverter.IsLittleEndian)
        {
            var copy = (byte[])bytes.Clone();
            Array.Reverse(copy);
            return copy;
        }
        return bytes;
    }

    private static byte[] Int16ToBytes(short v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static byte[] UInt16ToBytes(ushort v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static byte[] Int32ToBytes(int v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static byte[] UInt32ToBytes(uint v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static byte[] Int64ToBytes(long v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static byte[] UInt64ToBytes(ulong v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static byte[] FloatToBytes(float v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static byte[] DoubleToBytes(double v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static string FormatHex(byte[] bytes) =>
        string.Join(" ", bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
}
