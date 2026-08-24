using System.Numerics;
using System.Text;

namespace CommonHelp;

/// <summary>
/// Radix conversion for bases 2–128 using a fixed 128-character digit alphabet.
/// Digits 0–35 match .NET <see cref="Convert"/> (0-9, A-Z).
/// </summary>
public static class RadixHelp
{
    public const int MinRadix = 2;
    public const int MaxRadix = 128;

    private static readonly string Digits = BuildAlphabet();
    private static readonly IReadOnlyDictionary<char, int> CharToValue = BuildCharMap();

    public static IReadOnlyList<int> ProgrammerBases { get; } =
        [2, 8, 10, 16, 32, 64, 128];

    public static string GetDigitAlphabet(int radix)
    {
        ValidateRadix(radix);
        return Digits[..radix];
    }

    /// <summary>
    /// 32–128 进制数字符表的界面提示文案。
    /// </summary>
    public static string DigitAlphabetHint =>
        $"32–128 进制数字符表（前 {Math.Min(48, MaxRadix)} 个）：{GetDigitAlphabet(Math.Min(48, MaxRadix))}…";

    /// <summary>
    /// 将一个输入值从 fromBase 进制解析后，批量换算为所有常用进制的表示。
    /// </summary>
    public static Result<RadixConvertResult> ConvertFrom(string? value, int fromBase)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result<RadixConvertResult>.Ok(new RadixConvertResult(EmptyValues()));

            var big = Parse(value, fromBase);
            var dict = new Dictionary<int, string>();
            foreach (var radix in ProgrammerBases)
                dict[radix] = Format(big, radix);

            return Result<RadixConvertResult>.Ok(new RadixConvertResult(dict));
        }
        catch (Exception ex)
        {
            return Result<RadixConvertResult>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 清空结果：返回所有支持进制的空字符串映射。
    /// </summary>
    public static RadixConvertResult ClearRadixValues() => new(EmptyValues());

    private static Dictionary<int, string> EmptyValues() =>
        ProgrammerBases.ToDictionary(b => b, _ => string.Empty);

    public static BigInteger Parse(string value, int radix)
    {
        ValidateRadix(radix);
        if (string.IsNullOrWhiteSpace(value))
            return BigInteger.Zero;

        var s = value.Trim();
        if (s.StartsWith('-'))
            throw new FormatException("暂不支持负数。");

        if (radix is 2 or 8 or 10 or 16)
        {
            try
            {
                return new BigInteger(Convert.ToInt64(s, radix));
            }
            catch (OverflowException)
            {
                // fall through
            }
        }

        BigInteger result = BigInteger.Zero;
        var r = new BigInteger(radix);
        foreach (var ch in s)
        {
            if (!CharToValue.TryGetValue(ch, out var digit) || digit >= radix)
                throw new FormatException($"字符 '{ch}' 不是 {radix} 进制有效数字。");
            result = result * r + digit;
        }

        return result;
    }

    public static string Format(BigInteger value, int radix, bool upperCaseForHex = true)
    {
        ValidateRadix(radix);
        if (value.IsZero)
            return "0";

        if (radix is 2 or 8 or 10 or 16 && value >= long.MinValue && value <= long.MaxValue)
        {
            var s = Convert.ToString((long)value, radix);
            return radix == 16 && upperCaseForHex ? s.ToUpperInvariant() : s;
        }

        var sb = new StringBuilder();
        var r = new BigInteger(radix);
        while (value > BigInteger.Zero)
        {
            value = BigInteger.DivRem(value, r, out var rem);
            sb.Append(Digits[(int)rem]);
        }

        // 逐位 Append 后整体反转，避免 Insert(0, …) 的 O(n²) 搬移
        for (int i = 0, j = sb.Length - 1; i < j; i++, j--)
        {
            (sb[i], sb[j]) = (sb[j], sb[i]);
        }

        return sb.ToString();
    }

    private static void ValidateRadix(int radix)
    {
        if (radix is < MinRadix or > MaxRadix)
            throw new ArgumentOutOfRangeException(nameof(radix), $"进制须在 {MinRadix}–{MaxRadix} 之间。");
    }

    private static string BuildAlphabet()
    {
        var seen = new HashSet<char>();
        var sb = new StringBuilder(MaxRadix);

        void Add(char c)
        {
            if (sb.Length >= MaxRadix || !seen.Add(c))
                return;
            sb.Append(c);
        }

        for (var c = '0'; c <= '9'; c++) Add(c);
        for (var c = 'A'; c <= 'Z'; c++) Add(c);
        for (var c = 'a'; c <= 'z'; c++) Add(c);
        for (var c = (char)33; c <= (char)126 && sb.Length < MaxRadix; c++)
            Add(c);

        for (var c = (char)161; sb.Length < MaxRadix && c <= (char)255; c++)
            Add(c);

        if (sb.Length != MaxRadix)
            throw new InvalidOperationException($"Digit alphabet must be {MaxRadix} chars, got {sb.Length}.");

        return sb.ToString();
    }

    private static Dictionary<char, int> BuildCharMap()
    {
        // 严格按字母表索引建映射：'A'–'Z' = 10–35，'a'–'z' = 36–61（大小写是不同数字）。
        // 不做大小写别名：字母表同时含大小写且值不同，别名写入会覆盖主映射
        // （曾导致 36 进制无法解析 A–Z、64 进制 A/a 取值错乱）。
        var map = new Dictionary<char, int>(MaxRadix);
        for (var i = 0; i < Digits.Length; i++)
            map[Digits[i]] = i;

        return map;
    }
}

/// <summary>
/// 进制批量换算结果：各常用进制（2/8/10/16/32/64/128）对应的字符串表示。
/// </summary>
public sealed record RadixConvertResult(IReadOnlyDictionary<int, string> ValuesByRadix);
