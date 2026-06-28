using System.Globalization;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Calculation;

public sealed record TimestampTimeZoneOption(string Id, string Label);

public sealed record TimestampDisplayResult(
    string UnixSeconds,
    string UnixMilliseconds,
    string LocalTime,
    string UtcTime,
    string Iso8601Local,
    string Iso8601Utc,
    string RelativeHint);

public static class TimestampConvertService
{
    private const string DisplayFormat = "yyyy-MM-dd HH:mm:ss";

    public static IReadOnlyList<TimestampTimeZoneOption> TimeZoneOptions { get; } = BuildTimeZoneOptions();

    public static ToolResult<DateTimeOffset> ParseUnix(string? unixSeconds, string? unixMilliseconds, UnixFieldPrefer preferField)
    {
        if (string.IsNullOrWhiteSpace(unixSeconds) && string.IsNullOrWhiteSpace(unixMilliseconds))
            return ToolResult<DateTimeOffset>.Fail("empty");

        var secText = (unixSeconds ?? string.Empty).Trim();
        var msText = (unixMilliseconds ?? string.Empty).Trim();

        if (preferField == UnixFieldPrefer.Seconds && TryParseUnixToken(secText, out var dto))
            return ToolResult<DateTimeOffset>.Ok(dto);
        if (preferField == UnixFieldPrefer.Milliseconds && TryParseUnixToken(msText, out dto))
            return ToolResult<DateTimeOffset>.Ok(dto);
        if (TryParseUnixToken(secText, out dto))
            return ToolResult<DateTimeOffset>.Ok(dto);
        if (TryParseUnixToken(msText, out dto))
            return ToolResult<DateTimeOffset>.Ok(dto);

        return ToolResult<DateTimeOffset>.Fail("invalid");
    }

    public static ToolResult<DateTimeOffset> ParseUnix(string? unixSeconds, string? unixMilliseconds) =>
        ParseUnix(unixSeconds, unixMilliseconds, UnixFieldPrefer.Seconds);

    public static ToolResult<DateTimeOffset> ParseFromTime(string? localTime, string? utcTime, string timeZoneId)
    {
        var hasLocal = !string.IsNullOrWhiteSpace(localTime);
        var hasUtc = !string.IsNullOrWhiteSpace(utcTime);

        if (!hasLocal && !hasUtc)
            return ToolResult<DateTimeOffset>.Fail("请填写时区时间或 UTC 时间（支持 ISO 8601）。");

        DateTimeOffset dto;
        if (hasLocal && hasUtc)
        {
            if (!TryParseLocalTimeInZone(localTime!, timeZoneId, out dto))
                return ToolResult<DateTimeOffset>.Fail("时区时间格式不正确。");

            if (!TryParseDateTimeInput(utcTime!, preferUtc: true, out var utcDto))
                return ToolResult<DateTimeOffset>.Fail("UTC 时间格式不正确。");

            if (Math.Abs(dto.ToUnixTimeMilliseconds() - utcDto.ToUnixTimeMilliseconds()) > 60_000)
                return ToolResult<DateTimeOffset>.Fail("时区时间与 UTC 相差超过 1 分钟，请检查输入。");
        }
        else if (hasLocal)
        {
            if (!TryParseLocalTimeInZone(localTime!, timeZoneId, out dto))
                return ToolResult<DateTimeOffset>.Fail("时区时间格式不正确。");
        }
        else
        {
            if (!TryParseDateTimeInput(utcTime!, preferUtc: true, out dto))
                return ToolResult<DateTimeOffset>.Fail("UTC 时间格式不正确。");
        }

        return ToolResult<DateTimeOffset>.Ok(dto);
    }

    public static TimestampDisplayResult BuildDisplay(DateTimeOffset dto, string timeZoneId)
    {
        var tz = FindTimeZone(timeZoneId);
        var zoned = TimeZoneInfo.ConvertTimeFromUtc(dto.UtcDateTime, tz);
        var offset = tz.GetUtcOffset(zoned);
        var zonedOffset = new DateTimeOffset(zoned, offset);

        return new TimestampDisplayResult(
            dto.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            dto.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            zoned.ToString(DisplayFormat, CultureInfo.InvariantCulture),
            dto.UtcDateTime.ToString(DisplayFormat, CultureInfo.InvariantCulture),
            zonedOffset.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
            dto.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            FormatRelative(dto, zoned));
    }

    public static string FormatLocalTimeLabel(string timeZoneId)
    {
        var tz = FindTimeZone(timeZoneId);
        var offset = tz.GetUtcOffset(DateTimeOffset.UtcNow);
        return $"{tz.DisplayName} ({FormatUtcOffset(offset)})";
    }

    public static bool TryParseUnixToken(string text, out DateTimeOffset dto)
    {
        dto = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return false;

        var digits = text.TrimStart('-').Length;
        try
        {
            dto = digits >= 12
                ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                : DateTimeOffset.FromUnixTimeSeconds(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseLocalTimeInZone(string value, string timeZoneId, out DateTimeOffset dto)
    {
        dto = default;
        if (!TryParseDateTimeOnly(value, out var dt))
            return false;

        var tz = FindTimeZone(timeZoneId);
        dt = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);

        try
        {
            var offset = tz.GetUtcOffset(dt);
            dto = new DateTimeOffset(dt, offset);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryParseDateTimeOnly(string value, out DateTime dt)
    {
        dt = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();
        const DateTimeStyles styles = DateTimeStyles.AllowWhiteSpaces;

        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, styles, out dt))
            return true;

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, styles, out dt);
    }

    private static bool TryParseDateTimeInput(string value, bool preferUtc, out DateTimeOffset dto)
    {
        dto = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();
        var styles = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal;
        if (preferUtc || value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            styles = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        if (DateTimeOffset.TryParse(value, CultureInfo.CurrentCulture, styles, out dto))
            return true;

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, styles, out dto);
    }

    private static TimeZoneInfo FindTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
    }

    private static string FormatRelative(DateTimeOffset dto, DateTime zoned)
    {
        var delta = dto - DateTimeOffset.UtcNow;
        var abs = delta.Duration();

        if (abs.TotalSeconds < 1)
            return "与当前时间几乎相同";

        var future = delta.TotalSeconds > 0;
        var prefix = future ? "之后" : "之前";

        if (abs.TotalDays >= 1)
            return $"{abs.TotalDays:0.#} 天{prefix}（{zoned:dddd}）";
        if (abs.TotalHours >= 1)
            return $"{abs.TotalHours:0.#} 小时{prefix}";
        if (abs.TotalMinutes >= 1)
            return $"{abs.TotalMinutes:0.#} 分钟{prefix}";
        return $"{abs.TotalSeconds:0.#} 秒{prefix}";
    }

    private static IReadOnlyList<TimestampTimeZoneOption> BuildTimeZoneOptions()
    {
        var preferredIds = new[]
        {
            TimeZoneInfo.Local.Id,
            TimeZoneInfo.Utc.Id,
            "China Standard Time",
            "Asia/Shanghai",
            "Tokyo Standard Time",
            "Asia/Tokyo",
            "GMT Standard Time",
            "Europe/London",
            "Central European Standard Time",
            "Europe/Berlin",
            "Eastern Standard Time",
            "America/New_York",
            "Pacific Standard Time",
            "America/Los_Angeles",
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var options = new List<TimestampTimeZoneOption>();

        foreach (var id in preferredIds)
            TryAddTimeZone(id, pin: true);

        var rest = TimeZoneInfo.GetSystemTimeZones()
            .Where(tz => seen.Add(tz.Id))
            .OrderBy(tz => tz.BaseUtcOffset)
            .ThenBy(tz => tz.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(tz => CreateOption(tz, pin: false));

        options.AddRange(rest);
        return options;

        void TryAddTimeZone(string timeZoneId, bool pin)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                if (!seen.Add(tz.Id))
                    return;

                options.Add(CreateOption(tz, pin));
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }
    }

    private static TimestampTimeZoneOption CreateOption(TimeZoneInfo tz, bool pin)
    {
        var offset = tz.GetUtcOffset(DateTimeOffset.UtcNow);
        var prefix = pin ? "★ " : string.Empty;
        var label = $"{prefix}{tz.DisplayName} ({FormatUtcOffset(offset)}) · {tz.Id}";
        return new TimestampTimeZoneOption(tz.Id, label);
    }

    private static string FormatUtcOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var abs = offset.Duration();
        return $"UTC{sign}{abs.Hours:00}:{abs.Minutes:00}";
    }
}

public enum UnixFieldPrefer
{
    Seconds,
    Milliseconds
}
