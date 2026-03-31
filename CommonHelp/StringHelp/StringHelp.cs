using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonTool.StringHelp
{
    public class StringHelp
    {
        /// <summary>
        /// 计算给定文本的行数。支持自定义行分隔符（默认为 Environment.NewLine）。
        /// </summary>
        public static int CountLines(string text, string? lineSeparator = null)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            lineSeparator ??= Environment.NewLine;
            // 使用 Split 会处理末尾没有换行的情况
            return text.Split(new string[] { lineSeparator }, StringSplitOptions.None).Length;
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
        /// 读取文件并计算其行数与字符数（默认按 UTF8 编码计算字节数）。
        /// 返回元组： (lines, charsOrBytes) 。
        /// </summary>
        public static (long Lines, long Count) CountFileLinesAndChars(string filePath, Encoding? encoding = null)
        {
            if (!File.Exists(filePath))
                return (0, 0);

            var countCharacters = encoding == null;
            encoding ??= Encoding.UTF8;
            var text = File.ReadAllText(filePath, encoding);
            return (CountLogicalLines(text), countCharacters ? text.Length : encoding.GetByteCount(text));
        }

        /// <summary>
        /// 计算文件的字符数（或字节数，取决于 encoding 参数）。默认使用 UTF8 编码计算字节数。
        /// </summary>
        public static long CountFileCharacters(string filePath, Encoding? encoding = null)
        {
            if (!File.Exists(filePath))
                return 0;

            var countCharacters = encoding == null;
            encoding ??= Encoding.UTF8;
            var text = File.ReadAllText(filePath, encoding);
            return countCharacters ? text.Length : encoding.GetByteCount(text);
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
}
