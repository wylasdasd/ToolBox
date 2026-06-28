using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Network;

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

    public static IReadOnlyList<EndianHexDataType> DataTypes => EndianHexService.DataTypes;

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

        var result = EndianHexService.ConvertValueToHex(Input, DataTypeId, BigEndian, InputIsHex);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        ApplyResult(result.Value!);
    }

    public void ConvertHexToValue()
    {
        ErrorMessage = null;
        HexOutput = BinaryOutput = DecimalOutput = string.Empty;

        var result = EndianHexService.ConvertHexToValue(Input, DataTypeId, BigEndian);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        ApplyResult(result.Value!);
        Input = result.Value!.DecimalOutput;
        InputIsHex = false;
    }

    public void Clear()
    {
        Input = string.Empty;
        SelectedExampleId = string.Empty;
        HexOutput = BinaryOutput = DecimalOutput = string.Empty;
        ErrorMessage = null;
    }

    private void ApplyResult(EndianHexResult value)
    {
        HexOutput = value.HexOutput;
        BinaryOutput = value.BinaryOutput;
        DecimalOutput = value.DecimalOutput;
    }
}
