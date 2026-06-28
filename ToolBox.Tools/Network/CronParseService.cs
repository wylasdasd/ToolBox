using Cronos;
using System.Globalization;
using System.Text;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Network;

public static class CronParseService
{
    public static ToolResult<CronParseResult> Parse(string? expression, bool includeSeconds)
    {
        var expr = (expression ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(expr))
            return ToolResult<CronParseResult>.Fail("请输入 Cron 表达式。");

        try
        {
            var format = includeSeconds ? CronFormat.IncludeSeconds : CronFormat.Standard;
            var cron = CronExpression.Parse(expr, format);
            var description = Describe(expr, format);
            var nextRuns = BuildNextRuns(cron);
            return ToolResult<CronParseResult>.Ok(new CronParseResult(description, nextRuns));
        }
        catch (Exception ex)
        {
            return ToolResult<CronParseResult>.Fail(ex.Message);
        }
    }

    private static string BuildNextRuns(CronExpression cron)
    {
        var tz = TimeZoneInfo.Local;
        var now = DateTimeOffset.Now;
        var sb = new StringBuilder();
        var cursor = now;

        for (var i = 0; i < 10; i++)
        {
            var next = cron.GetNextOccurrence(cursor, tz);
            if (next is null)
            {
                if (i == 0) sb.AppendLine("（无后续执行时间）");
                break;
            }

            sb.AppendLine($"{i + 1,2}. {next.Value.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)}");
            cursor = next.Value.AddSeconds(1);
        }

        return sb.ToString().TrimEnd();
    }

    private static string Describe(string expr, CronFormat format)
    {
        var parts = expr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var expected = format == CronFormat.IncludeSeconds ? 6 : 5;
        if (parts.Length != expected)
            return $"字段数：{parts.Length}（期望 {expected} 个）";

        return format == CronFormat.IncludeSeconds
            ? DescribeSixField(parts)
            : DescribeFiveField(parts);
    }

    private static string DescribeFiveField(string[] p) =>
        $"分 {DescribeField(p[0], "分")} · 时 {DescribeField(p[1], "时")} · 日 {DescribeField(p[2], "日")} · 月 {DescribeField(p[3], "月")} · 周 {DescribeField(p[4], "周")}";

    private static string DescribeSixField(string[] p) =>
        $"秒 {DescribeField(p[0], "秒")} · 分 {DescribeField(p[1], "分")} · 时 {DescribeField(p[2], "时")} · 日 {DescribeField(p[3], "日")} · 月 {DescribeField(p[4], "月")} · 周 {DescribeField(p[5], "周")}";

    private static string DescribeField(string field, string unit)
    {
        if (field == "*") return $"每{unit}";
        if (field.StartsWith("*/", StringComparison.Ordinal))
            return $"每 {field[2..]} {unit}";
        if (field.Contains(',', StringComparison.Ordinal))
            return $"{unit} {field.Replace(',', '、')}";
        if (field.Contains('-', StringComparison.Ordinal))
            return $"{unit} {field.Replace('-', '~')}";
        return $"{unit} {field}";
    }
}
