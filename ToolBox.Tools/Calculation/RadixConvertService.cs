using CommonHelp;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Calculation;

/// <summary>
/// RadixConvertResult：用于表示和处理相关的。
/// </summary>
public sealed record RadixConvertResult(IReadOnlyDictionary<int, string> ValuesByRadix);

/// <summary>
/// RadixConvertService：用于表示和处理相关的。
/// </summary>
public static class RadixConvertService
{
    public static IReadOnlyList<int> ProgrammerBases => RadixHelp.ProgrammerBases;

    public static string DigitAlphabetHint =>
        $"32–128 进制数字符表（前 {Math.Min(48, RadixHelp.MaxRadix)} 个）：{RadixHelp.GetDigitAlphabet(Math.Min(48, RadixHelp.MaxRadix))}…";

    /// <summary>
    /// 执行 ConvertFrom 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static ToolResult<RadixConvertResult> ConvertFrom(string? value, int fromBase)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
                return ToolResult<RadixConvertResult>.Ok(new RadixConvertResult(EmptyValues()));

            var big = RadixHelp.Parse(value, fromBase);
            var dict = new Dictionary<int, string>();
            foreach (var radix in RadixHelp.ProgrammerBases)
                dict[radix] = RadixHelp.Format(big, radix);

            return ToolResult<RadixConvertResult>.Ok(new RadixConvertResult(dict));
        }
        catch (Exception ex)
        {
            return ToolResult<RadixConvertResult>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 执行 Clear 操作，完成该方法对应的工具处理逻辑。
    /// </summary>
    public static RadixConvertResult Clear() => new(EmptyValues());

    /// <summary>
    /// 创建所有支持进制的空结果映射。
    /// </summary>
    private static Dictionary<int, string> EmptyValues() =>
        RadixHelp.ProgrammerBases.ToDictionary(b => b, _ => string.Empty);
}
