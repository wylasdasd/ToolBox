using System.Globalization;

namespace CommonHelp;

/// <summary>
/// 支持的数据类型定义：类型 Id、展示标签、字节宽度与是否浮点。
/// </summary>
public sealed record EndianHexDataType(string Id, string Label, int ByteSize, bool IsFloat);

/// <summary>
/// 数值/字节序换算结果：Hex（空格分隔）、二进制（每字节 8 位）与十进制展示。
/// </summary>
public sealed record EndianHexResult(string HexOutput, string BinaryOutput, string DecimalOutput);

/// <summary>
/// 数值与十六进制字节互转工具：支持 int8–uint64、float32/float64 十种类型与大端/小端字节序。
/// </summary>
public static class EndianHexHelp
{
    /// <summary>全部支持的数据类型；未知类型 Id 回退到 int32。</summary>
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

    /// <summary>
    /// 数值转 Hex 字节：inputIsHex 为 true 时直接解析 Hex 输入，否则按十进制数值编码；同时输出二进制与按目标类型解码的十进制。
    /// </summary>
    public static Result<EndianHexResult> ConvertValueToHex(
        string? input, string? dataTypeId, bool bigEndian, bool inputIsHex)
    {
        var type = ResolveDataType(dataTypeId);
        try
        {
            var bytes = inputIsHex
                ? EncodingHelp.ParseHexBytes(input ?? string.Empty, type.ByteSize)
                : EncodeValue(input ?? string.Empty, type, bigEndian);

            return Result<EndianHexResult>.Ok(BuildResult(bytes, type, bigEndian));
        }
        catch (Exception ex)
        {
            return Result<EndianHexResult>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Hex 字节转十进制：解析指定长度的 Hex 输入，按目标类型与端序解码为数值字符串。
    /// </summary>
    public static Result<EndianHexResult> ConvertHexToValue(string? input, string? dataTypeId, bool bigEndian)
    {
        var type = ResolveDataType(dataTypeId);
        try
        {
            var bytes = EncodingHelp.ParseHexBytes(input ?? string.Empty, type.ByteSize);
            return Result<EndianHexResult>.Ok(BuildResult(bytes, type, bigEndian));
        }
        catch (Exception ex)
        {
            return Result<EndianHexResult>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 按类型 Id 查找类型定义；未知 Id 回退到 int32。
    /// </summary>
    private static EndianHexDataType ResolveDataType(string? dataTypeId) =>
        DataTypes.FirstOrDefault(t => t.Id == dataTypeId) ?? DataTypes[4];

    /// <summary>
    /// 组装 Hex、二进制与十进制三种展示。
    /// </summary>
    private static EndianHexResult BuildResult(byte[] bytes, EndianHexDataType type, bool bigEndian) =>
        new(
            EncodingHelp.FormatHexBytes(bytes),
            string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))),
            DecodeBytes(bytes, type, bigEndian));

    /// <summary>
    /// 十进制字符串按目标类型编码为字节序列（按端序排列）。
    /// </summary>
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

    /// <summary>
    /// 字节序列按目标类型与端序解码为十进制字符串（浮点用往返格式 "R"）。
    /// </summary>
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

    /// <summary>
    /// 按目标端序重排字节：与本机端序不一致时反转副本。
    /// </summary>
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

    /// <summary>short 转字节（按端序）。</summary>
    private static byte[] Int16ToBytes(short v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    /// <summary>ushort 转字节（按端序）。</summary>
    private static byte[] UInt16ToBytes(ushort v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    /// <summary>int 转字节（按端序）。</summary>
    private static byte[] Int32ToBytes(int v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    /// <summary>uint 转字节（按端序）。</summary>
    private static byte[] UInt32ToBytes(uint v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    /// <summary>long 转字节（按端序）。</summary>
    private static byte[] Int64ToBytes(long v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    /// <summary>ulong 转字节（按端序）。</summary>
    private static byte[] UInt64ToBytes(ulong v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    /// <summary>float 转字节（按端序）。</summary>
    private static byte[] FloatToBytes(float v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    /// <summary>double 转字节（按端序）。</summary>
    private static byte[] DoubleToBytes(double v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }
}
