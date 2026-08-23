using System.Text;

namespace CommonHelp;

/// <summary>
/// 位运算结果：按位宽归一化后的运算值。
/// </summary>
public sealed record BitwiseComputeResult(long Result);

/// <summary>
/// 位运算工具：支持 AND/OR/XOR/NOT/SHL/SHR_A/SHR_L 在 1–64 位宽下的运算，统一处理掩码截断与符号扩展。
/// </summary>
public static class BitwiseHelp
{
    /// <summary>
    /// 判断操作名是否为移位操作（SHL/SHR_A/SHR_L）。
    /// </summary>
    public static bool IsShiftOp(string op) => op is "SHL" or "SHR_A" or "SHR_L";

    /// <summary>
    /// 取指定位宽的低位全 1 掩码；64 位时返回 -1L（全 1）。
    /// </summary>
    public static long GetMask(int bitWidth) =>
        bitWidth == 64 ? -1L : (1L << bitWidth) - 1;

    /// <summary>
    /// 将截断在 bitWidth 位内的值按最高位符号位做符号扩展。
    /// </summary>
    public static long SignExtend(long value, int bitWidth)
    {
        if (bitWidth == 64) return value;

        var mask = GetMask(bitWidth);
        var signBit = 1L << (bitWidth - 1);
        if ((value & signBit) != 0)
            return value | ~mask;

        return value & mask;
    }

    /// <summary>
    /// 把结果截断到位宽内；有符号时可选做符号扩展。
    /// </summary>
    public static long NormalizeResult(long res, int bitWidth, bool isSigned, bool signExtendWhenSigned = true)
    {
        res &= GetMask(bitWidth);
        if (isSigned && signExtendWhenSigned)
            res = SignExtend(res, bitWidth);
        return res;
    }

    /// <summary>
    /// 执行一次位运算（AND/OR/XOR/NOT 或移位）并按位宽归一化结果；NOT 只取操作数 A。
    /// </summary>
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

    /// <summary>
    /// 执行移位运算：shiftFromResult 为 true 时以 currentResult 为被移位数，否则以 operandA；移位量钳制在 0–bitWidth。
    /// </summary>
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

    /// <summary>
    /// 按有/无符号把位宽内的值格式化为十进制字符串。
    /// </summary>
    public static string FormatValue(long value, int bitWidth, bool isSigned)
    {
        var maskedValue = value & GetMask(bitWidth);

        if (isSigned)
            return SignExtend(maskedValue, bitWidth).ToString();

        return ((ulong)maskedValue).ToString();
    }

    /// <summary>
    /// 格式化为二进制字符串，左侧补零到目标位宽并每 4 位分组。
    /// </summary>
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

    /// <summary>
    /// 按位宽截断后格式化为大写十六进制字符串。
    /// </summary>
    public static string FormatHex(long value, int bitWidth) =>
        (value & GetMask(bitWidth)).ToString("X");

    /// <summary>
    /// 左移并在位宽内截断（超出位宽的结果为 0）。
    /// </summary>
    private static long ShiftLeft(long value, int shift, int bitWidth, long mask)
    {
        if (shift >= bitWidth) return 0;
        return (value & mask) << shift & mask;
    }

    /// <summary>
    /// 逻辑右移（高位补 0）。
    /// </summary>
    private static long ShiftRightLogical(long value, int shift, int bitWidth, long mask)
    {
        if (shift >= bitWidth) return 0;
        return (long)((ulong)(value & mask) >> shift);
    }

    /// <summary>
    /// 算术右移（高位补符号位），正确处理 1–64 位宽边界。
    /// </summary>
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
