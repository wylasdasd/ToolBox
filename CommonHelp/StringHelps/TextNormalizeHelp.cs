using System.Text;

namespace CommonHelp;

/// <summary>
/// 文本符号归一化工具：支持全角/半角字符与中英文标点之间的转换。
/// </summary>
public static class TextNormalizeHelp
{
    private static readonly IReadOnlyDictionary<char, char> SymbolMap = new Dictionary<char, char>
    {
        ['，'] = ',', ['。'] = '.', ['；'] = ';', ['：'] = ':', ['？'] = '?', ['！'] = '!',
        ['（'] = '(', ['）'] = ')', ['【'] = '[', ['】'] = ']', ['｛'] = '{', ['｝'] = '}',
        ['《'] = '<', ['》'] = '>', ['“'] = '"', ['”'] = '"', ['‘'] = '\'', ['’'] = '\'',
        ['、'] = '/', ['—'] = '-', ['－'] = '-', ['～'] = '~', ['·'] = '.', ['￥'] = '$'
    };

    private static readonly IReadOnlyDictionary<char, char> ReverseSymbolMap = new Dictionary<char, char>
    {
        [','] = '，', ['.'] = '。', [';'] = '；', [':'] = '：', ['?'] = '？', ['!'] = '！',
        ['('] = '（', [')'] = '）', ['['] = '【', [']'] = '】', ['{'] = '｛', ['}'] = '｝',
        ['<'] = '《', ['>'] = '》', ['"'] = '“', ['\''] = '‘', ['/'] = '、', ['-'] = '—',
        ['~'] = '～', ['$'] = '￥'
    };

    /// <summary>
    /// 先 FormKC 归一化（全角转半角），再把常见中文标点映射为英文标点。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <param name="removeSpaces">是否去掉空格（不影响换行）。</param>
    public static string NormalizeSymbolsToAscii(string? text, bool removeSpaces = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormKC);
        var buffer = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (c is '\u3000' or '\u00A0')
            {
                if (!removeSpaces)
                {
                    buffer.Append(' ');
                }
                continue;
            }

            if (SymbolMap.TryGetValue(c, out var mapped))
            {
                buffer.Append(mapped);
                continue;
            }

            if (removeSpaces && c == ' ')
            {
                continue;
            }

            buffer.Append(c);
        }

        return buffer.ToString();
    }

    /// <summary>
    /// 将 ASCII 可打印字符转为全角字符，并将常见英文标点映射为中文标点。
    /// </summary>
    /// <remarks>
    /// 这是格式转换，不保证可逆；例如英文双引号统一转换为左双引号，短横线统一转换为破折号。
    /// </remarks>
    /// <param name="text">待处理文本。</param>
    /// <param name="convertSpaces">是否将普通空格转换为全角空格。</param>
    public static string NormalizeSymbolsToFullWidth(string? text, bool convertSpaces = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (ReverseSymbolMap.TryGetValue(c, out var mapped))
            {
                buffer.Append(mapped);
            }
            else if (convertSpaces && c == ' ')
            {
                buffer.Append('\u3000');
            }
            else if (c is >= '\u0021' and <= '\u007E')
            {
                buffer.Append((char)(c + 0xFEE0));
            }
            else
            {
                buffer.Append(c);
            }
        }

        return buffer.ToString();
    }
}