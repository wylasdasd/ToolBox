using Blazing.Mvvm.ComponentModel;
using System.Globalization;

namespace ToolBox.Components.Pages.Network;

public sealed class EndianHexVM : ViewModelBase
{
    private string _dataTypeId = "int32";
    private string _input = "305419896";
    private bool _bigEndian = true;
    private bool _inputIsHex;
    private string _selectedExampleId = string.Empty;
    private string _hexOutput = string.Empty;
    private string _binaryOutput = string.Empty;
    private string _decimalOutput = string.Empty;
    private string? _errorMessage;

    public sealed record DataTypeOption(string Id, string Label, int ByteSize, bool IsFloat);

    public sealed record ExampleCase(string Id, string Label, string DataTypeId, string Input, bool BigEndian, bool InputIsHex);

    public static IReadOnlyList<ExampleCase> Examples { get; } =
    [
        new("int32-12345678", "int32 · 305419896（0x12345678）", "int32", "305419896", true, false),
        new("int32-hex-be", "int32 · 大端 Hex：12 34 56 78", "int32", "12 34 56 78", true, true),
        new("int32-minus1", "int32 · -1（0xFFFFFFFF）", "int32", "-1", true, false),
        new("int32-hex-le", "int32 · 小端 Hex：78 56 34 12", "int32", "78 56 34 12", false, true),
        new("float32-pi", "float32 · 3.14", "float32", "3.14", false, false),
        new("float32-one-le", "float32 · 1.0 小端 Hex：00 00 80 3F", "float32", "00 00 80 3F", false, true),
        new("int16-minus1", "int16 · -1（0xFFFF）", "int16", "-1", true, false),
        new("uint8-255", "uint8 · 255（0xFF）", "uint8", "255", true, false),
        new("int64-max", "int64 · MaxValue", "int64", "9223372036854775807", true, false),
    ];

    public static IReadOnlyList<DataTypeOption> DataTypes { get; } =
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

    public string DataTypeId
    {
        get => _dataTypeId;
        set => SetProperty(ref _dataTypeId, value);
    }

    public string Input
    {
        get => _input;
        set => SetProperty(ref _input, value);
    }

    public bool BigEndian
    {
        get => _bigEndian;
        set => SetProperty(ref _bigEndian, value);
    }

    public bool InputIsHex
    {
        get => _inputIsHex;
        set => SetProperty(ref _inputIsHex, value);
    }

    public string SelectedExampleId
    {
        get => _selectedExampleId;
        private set => SetProperty(ref _selectedExampleId, value);
    }

    public string HexOutput
    {
        get => _hexOutput;
        private set => SetProperty(ref _hexOutput, value);
    }

    public string BinaryOutput
    {
        get => _binaryOutput;
        private set => SetProperty(ref _binaryOutput, value);
    }

    public string DecimalOutput
    {
        get => _decimalOutput;
        private set => SetProperty(ref _decimalOutput, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void SelectExample(string? exampleId)
    {
        SelectedExampleId = exampleId ?? string.Empty;
        if (string.IsNullOrEmpty(exampleId))
            return;

        var example = Examples.FirstOrDefault(e => e.Id == exampleId);
        if (example is null)
            return;

        DataTypeId = example.DataTypeId;
        Input = example.Input;
        BigEndian = example.BigEndian;
        InputIsHex = example.InputIsHex;

        if (example.InputIsHex)
            ConvertHexToValue();
        else
            ConvertValueToHex();
    }

    public void ConvertValueToHex()
    {
        ErrorMessage = null;
        HexOutput = BinaryOutput = DecimalOutput = string.Empty;

        var type = DataTypes.FirstOrDefault(t => t.Id == DataTypeId) ?? DataTypes[4];
        try
        {
            var bytes = InputIsHex
                ? ParseHexBytes(Input, type.ByteSize)
                : EncodeValue(Input, type, BigEndian);

            HexOutput = FormatHex(bytes);
            BinaryOutput = string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            DecimalOutput = DecodeBytes(bytes, type, BigEndian);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void ConvertHexToValue()
    {
        ErrorMessage = null;
        HexOutput = BinaryOutput = DecimalOutput = string.Empty;

        var type = DataTypes.FirstOrDefault(t => t.Id == DataTypeId) ?? DataTypes[4];
        try
        {
            var bytes = ParseHexBytes(Input, type.ByteSize);
            HexOutput = FormatHex(bytes);
            BinaryOutput = string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            DecimalOutput = DecodeBytes(bytes, type, BigEndian);
            Input = DecimalOutput;
            InputIsHex = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void Clear()
    {
        Input = string.Empty;
        SelectedExampleId = string.Empty;
        HexOutput = BinaryOutput = DecimalOutput = string.Empty;
        ErrorMessage = null;
    }

    private static byte[] EncodeValue(string text, DataTypeOption type, bool bigEndian)
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

    private static string DecodeBytes(byte[] bytes, DataTypeOption type, bool bigEndian)
    {
        return type.Id switch
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
    }

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
