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
            sb.Insert(0, Digits[(int)rem]);
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
        var map = new Dictionary<char, int>(MaxRadix * 2);
        for (var i = 0; i < Digits.Length; i++)
        {
            map[Digits[i]] = i;
            if (Digits[i] is >= 'A' and <= 'Z')
                map[char.ToLowerInvariant(Digits[i])] = i;
            else if (Digits[i] is >= 'a' and <= 'z')
                map[char.ToUpperInvariant(Digits[i])] = i;
        }

        return map;
    }
}
