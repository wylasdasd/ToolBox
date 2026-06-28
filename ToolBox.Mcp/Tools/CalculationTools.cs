using System.ComponentModel;
using ModelContextProtocol.Server;
using ToolBox.Tools.Calculation;
using ToolBox.Tools.Common;
using ToolBox.Tools.Generate;

namespace ToolBox.Mcp.Tools;

[McpServerToolType]
public static class CalculationTools
{
    [McpServerTool, Description("Convert a number from one radix to all programmer bases (2/8/10/16/32/64/128).")]
    public static string RadixConvert(string value, int fromBase = 10) =>
        McpToolResponses.From(RadixConvertService.ConvertFrom(value, fromBase));

    [McpServerTool, Description("Parse Unix timestamp (seconds or milliseconds) to ISO8601 UTC/local.")]
    public static string TimestampFromUnix(string? unixSeconds = null, string? unixMilliseconds = null)
    {
        var result = TimestampConvertService.ParseUnix(unixSeconds, unixMilliseconds);
        if (!result.Success)
            return McpToolResponses.Error(result.Error ?? "Parse failed");

        return McpToolResponses.FromValue(new
        {
            utc = result.Value!.UtcDateTime.ToString("O"),
            local = result.Value.LocalDateTime.ToString("O"),
            offsetMinutes = result.Value.Offset.TotalMinutes,
        });
    }

    [McpServerTool, Description("Convert hex color digits to RGB/HSL components.")]
    public static string ColorFromHex(string hexDigits) =>
        McpToolResponses.From(ColorConvertService.FromHex(hexDigits));

    [McpServerTool, Description("Convert a value across units in a category (storage-iec, storage-si, network-bit, network-byte, time, frequency, memory-page).")]
    public static string UnitConvert(string input, string categoryId, string fromUnitId)
    {
        var result = UnitConvertService.Convert(input, categoryId, fromUnitId);
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            return McpToolResponses.Error(result.ErrorMessage);

        return McpToolResponses.FromValue(result);
    }

    [McpServerTool, Description("Estimate transfer time from file size and bandwidth.")]
    public static string UnitTransferTime(
        string fileSize,
        string fileSizeUnitId,
        string bandwidth,
        string bandwidthUnitId) =>
        McpToolResponses.From(UnitConvertService.CalculateTransferTime(
            fileSize, fileSizeUnitId, bandwidth, bandwidthUnitId));

    [McpServerTool, Description("Compute a bitwise operation (AND, OR, XOR, NOT, SHL, SHR_A, SHR_L).")]
    public static string BitwiseCompute(
        string operation,
        long operandA,
        long operandB = 0,
        int operandC = 1,
        int bitWidth = 32,
        bool isSigned = true,
        long currentResult = 0,
        bool shiftFromResult = false) =>
        McpToolResponses.From(BitwiseService.Compute(
            operation, operandA, operandB, operandC, bitWidth, isSigned, currentResult, shiftFromResult));

    [McpServerTool, Description("Calculate C/C++ struct or union memory layout from source snippet.")]
    public static string StructLayoutCalculate(string structDefinition, int pack = 0) =>
        McpToolResponses.From(StructLayoutService.Calculate(structDefinition, pack));
}

[McpServerToolType]
public static class GenerateTools
{
    [McpServerTool, Description("Generate one or more UUID/GUID strings.")]
    public static string UuidGenerate(int count = 1, bool includeHyphens = true, bool upperCase = false) =>
        McpToolResponses.From(UuidGenerateService.Generate(count, includeHyphens, upperCase));
}
