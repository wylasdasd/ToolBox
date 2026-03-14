using System.Text;

namespace CommonTool.StringHelp;

/// <summary>
/// OCR 文本归一化工具：
/// 1) 先做 FormKC 归一化（全角转半角等）
/// 2) 再把常见中文标点统一映射为英文标点
/// </summary>
public static class OcrTextNormalizeHelp
{
    private static readonly IReadOnlyDictionary<char, char> SymbolMap = new Dictionary<char, char>
    {
        ['，'] = ',',
        ['。'] = '.',
        ['；'] = ';',
        ['：'] = ':',
        ['？'] = '?',
        ['！'] = '!',
        ['（'] = '(',
        ['）'] = ')',
        ['【'] = '[',
        ['】'] = ']',
        ['｛'] = '{',
        ['｝'] = '}',
        ['《'] = '<',
        ['》'] = '>',
        ['“'] = '"',
        ['”'] = '"',
        ['‘'] = '\'',
        ['’'] = '\'',
        ['、'] = '/',
        ['—'] = '-',
        ['－'] = '-',
        ['～'] = '~',
        ['·'] = '.',
        ['￥'] = '$'
    };

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
                if (removeSpaces && mapped == ' ')
                {
                    continue;
                }

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
}
