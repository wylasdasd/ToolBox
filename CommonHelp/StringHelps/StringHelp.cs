using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonHelp;

/// <summary>
/// 文本统计工具：行数、字符数/字节数统计（字符串与文件两种入口）。
/// </summary>
public class StringHelp
{
    /// <summary>
    /// 计算给定文本的行数。支持自定义行分隔符（默认为 Environment.NewLine）。
    /// </summary>
    public static int CountLines(string text, string? lineSeparator = null)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // 默认按逻辑行计数（\r\n、\r、\n 均识别，无分配），避免 Windows 下按 \r\n Split 数错纯 \n 文本
        if (lineSeparator is null)
            return (int)CountLogicalLines(text);

        // 自定义分隔符：无分配顺序计数
        var count = 1;
        var index = 0;
        while ((index = text.IndexOf(lineSeparator, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += lineSeparator.Length;
        }

        return count;
    }

    /// <summary>
    /// 计算给定文本的字符数（基于字符串长度），默认按 UTF8 编码计算字节数也可通过参数更改。
    /// </summary>
    /// <param name="text">输入文本。</param>
    /// <param name="encoding">用于计算字节数的编码，若为 null 则返回字符数。</param>
    public static long CountCharacters(string text, Encoding? encoding = null)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        if (encoding == null)
            return text.Length;
        return encoding.GetByteCount(text);
    }

    /// <summary>
    /// 读取文件并计算行数与字符数（或字节数）。
    /// <paramref name="encoding"/> 为 null 时按字符数（<see cref="string.Length"/>）；传入编码时按该编码的字节数。
    /// 文件不存在时返回 (0, 0)。
    /// </summary>
    public static (long Lines, long Count) CountFileLinesAndChars(string filePath, Encoding? encoding = null)
    {
        var text = FileHelp.ReadAllText(filePath, encoding ?? Encoding.UTF8);
        var count = encoding is null ? text.Length : encoding.GetByteCount(text);
        return (CountLogicalLines(text), count);
    }

    /// <summary>
    /// 计算文件的字符数（<paramref name="encoding"/> 为 null）或指定编码下的字节数。
    /// 文件不存在时返回 0。
    /// </summary>
    public static long CountFileCharacters(string filePath, Encoding? encoding = null)
    {
        var text = FileHelp.ReadAllText(filePath, encoding ?? Encoding.UTF8);
        return encoding is null ? text.Length : encoding.GetByteCount(text);
    }

    private static long CountLogicalLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        long lines = 1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                lines++;
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
                continue;
            }

            if (text[i] == '\n')
            {
                lines++;
            }
        }

        return lines;
    }
}
