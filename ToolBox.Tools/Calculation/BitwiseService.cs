using System.Text;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Calculation;

public sealed record BitwiseComputeResult(long Result);

public static class BitwiseService
{
    public static bool IsShiftOp(string op) => op is "SHL" or "SHR_A" or "SHR_L";

    public static long GetMask(int bitWidth) =>
        bitWidth == 64 ? -1L : (1L << bitWidth) - 1;

    public static long SignExtend(long value, int bitWidth)
    {
        if (bitWidth == 64) return value;

        var mask = GetMask(bitWidth);
        var signBit = 1L << (bitWidth - 1);
        if ((value & signBit) != 0)
            return value | ~mask;

        return value & mask;
    }

    public static long NormalizeResult(long res, int bitWidth, bool isSigned, bool signExtendWhenSigned = true)
    {
        res &= GetMask(bitWidth);
        if (isSigned && signExtendWhenSigned)
            res = SignExtend(res, bitWidth);
        return res;
    }

    public static ToolResult<BitwiseComputeResult> Compute(
        string operation,
        long operandA,
        long operandB,
        int operandC,
        int bitWidth,
        bool isSigned,
        long currentResult,
        bool shiftFromResult)
    {
        try
        {
            if (IsShiftOp(operation))
            {
                var res = ApplyShift(operation, operandA, currentResult, operandC, bitWidth, isSigned, shiftFromResult);
                return ToolResult<BitwiseComputeResult>.Ok(new BitwiseComputeResult(res));
            }

            var mask = GetMask(bitWidth);
            var am = operandA & mask;
            var bm = operandB & mask;
            if (isSigned)
            {
                am = SignExtend(am, bitWidth);
                bm = SignExtend(bm, bitWidth);
            }

            var result = operation switch
            {
                "AND" => am & bm,
                "OR" => am | bm,
                "XOR" => am ^ bm,
                "NOT" => ~(am & mask),
                _ => 0L,
            };

            return ToolResult<BitwiseComputeResult>.Ok(new BitwiseComputeResult(NormalizeResult(result, bitWidth, isSigned)));
        }
        catch (Exception ex)
        {
            return ToolResult<BitwiseComputeResult>.Fail(ex.Message);
        }
    }

    public static long ApplyShift(
        string operation,
        long operandA,
        long currentResult,
        int operandC,
        int bitWidth,
        bool isSigned,
        bool fromResult)
    {
        var mask = GetMask(bitWidth);
        var shiftAmount = Math.Clamp(operandC, 0, bitWidth);
        var input = fromResult ? currentResult & mask : operandA & mask;

        var res = operation switch
        {
            "SHL" => ShiftLeft(input, shiftAmount, bitWidth, mask),
            "SHR_A" => isSigned
                ? ShiftRightArithmetic(input, shiftAmount, bitWidth, mask)
                : ShiftRightLogical(input, shiftAmount, bitWidth, mask),
            "SHR_L" => ShiftRightLogical(input, shiftAmount, bitWidth, mask),
            _ => input,
        };

        if (isSigned && operation is "SHR_A" or "SHL")
            return NormalizeResult(res, bitWidth, isSigned);
        return NormalizeResult(res, bitWidth, isSigned, signExtendWhenSigned: false);
    }

    public static string FormatValue(long value, int bitWidth, bool isSigned)
    {
        var maskedValue = value & GetMask(bitWidth);

        if (isSigned)
            return SignExtend(maskedValue, bitWidth).ToString();

        return ((ulong)maskedValue).ToString();
    }

    public static string FormatBinary(long value, int bitWidth)
    {
        var maskedValue = value & GetMask(bitWidth);
        var binary = Convert.ToString(maskedValue, 2).PadLeft(bitWidth, '0');

        if (binary.Length > bitWidth)
            binary = binary.Substring(binary.Length - bitWidth);

        var sb = new StringBuilder();
        for (var i = 0; i < binary.Length; i++)
        {
            if (i > 0 && (binary.Length - i) % 4 == 0)
                sb.Append(' ');
            sb.Append(binary[i]);
        }

        return sb.ToString();
    }

    public static string FormatHex(long value, int bitWidth) =>
        (value & GetMask(bitWidth)).ToString("X");

    private static long ShiftLeft(long value, int shift, int bitWidth, long mask)
    {
        if (shift >= bitWidth) return 0;
        return (value & mask) << shift & mask;
    }

    private static long ShiftRightLogical(long value, int shift, int bitWidth, long mask)
    {
        if (shift >= bitWidth) return 0;
        return (long)((ulong)(value & mask) >> shift);
    }

    private static long ShiftRightArithmetic(long value, int shift, int bitWidth, long mask)
    {
        if (shift >= bitWidth)
        {
            var msb = 1L << (bitWidth - 1);
            return (value & msb) != 0 ? mask : 0;
        }

        var bits = value & mask;
        var msbBit = 1L << (bitWidth - 1);
        ulong u = (ulong)bits >> shift;
        if ((bits & msbBit) != 0)
        {
            var uMask = bitWidth == 64 ? ulong.MaxValue : (ulong)mask;
            u |= uMask ^ (uMask >> shift);
        }

        return bitWidth == 64 ? (long)u : (long)(u & (ulong)mask);
    }
}
