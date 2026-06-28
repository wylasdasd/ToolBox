using Blazing.Mvvm.ComponentModel;
using Cronos;
using System.Globalization;
using System.Text;

namespace ToolBox.Components.Pages.Network;

public sealed class CronParserVM : ViewModelBase
{
    private string _expression = "0 0 * * *";
    private bool _includeSeconds;
    private string _description = string.Empty;
    private string _nextRuns = string.Empty;
    private string? _errorMessage;

    public string Expression
    {
        get => _expression;
        set => SetProperty(ref _expression, value);
    }

    public bool IncludeSeconds
    {
        get => _includeSeconds;
        set => SetProperty(ref _includeSeconds, value);
    }

    public string Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

    public string NextRuns
    {
        get => _nextRuns;
        private set => SetProperty(ref _nextRuns, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void Parse()
    {
        ErrorMessage = null;
        Description = string.Empty;
        NextRuns = string.Empty;

        var expr = (Expression ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(expr))
        {
            ErrorMessage = "请输入 Cron 表达式。";
            return;
        }

        try
        {
            var format = IncludeSeconds ? CronFormat.IncludeSeconds : CronFormat.Standard;
            var cron = CronExpression.Parse(expr, format);
            Description = Describe(expr, format);
            NextRuns = BuildNextRuns(cron);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void Clear()
    {
        Expression = string.Empty;
        Description = string.Empty;
        NextRuns = string.Empty;
        ErrorMessage = null;
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

        if (format == CronFormat.IncludeSeconds)
            return DescribeSixField(parts);
        return DescribeFiveField(parts);
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
