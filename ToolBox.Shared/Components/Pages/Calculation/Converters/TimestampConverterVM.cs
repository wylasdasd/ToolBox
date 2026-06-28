using Blazing.Mvvm.ComponentModel;
using System.Globalization;

namespace ToolBox.Components.Pages.Converters;

public sealed class TimestampConverterVM : ViewModelBase
{
    private const string DisplayFormat = "yyyy-MM-dd HH:mm:ss";

    private string _unixSeconds = string.Empty;
    private string _unixMilliseconds = string.Empty;
    private string _localTime = string.Empty;
    private string _utcTime = string.Empty;
    private string _iso8601Local = string.Empty;
    private string _iso8601Utc = string.Empty;
    private string _relativeHint = string.Empty;
    private string _localTimeLabel = "时区时间";
    private string _selectedTimeZoneId = TimeZoneInfo.Local.Id;
    private string? _errorMessage;
    private bool _isUpdating;
    private DateTimeOffset? _lastInstant;

    public sealed record TimeZoneOption(string Id, string Label);

    public static IReadOnlyList<TimeZoneOption> TimeZoneOptions { get; } = BuildTimeZoneOptions();

    public string SelectedTimeZoneId
    {
        get => _selectedTimeZoneId;
        set
        {
            if (SetProperty(ref _selectedTimeZoneId, value))
            {
                UpdateLocalTimeLabel();
                if (_lastInstant is { } instant)
                    ApplyDisplay(instant);
            }
        }
    }

    public string UnixSeconds
    {
        get => _unixSeconds;
        set
        {
            if (SetProperty(ref _unixSeconds, value) && !_isUpdating)
                TryFromUnixInput(preferField: UnixField.Seconds);
        }
    }

    public string UnixMilliseconds
    {
        get => _unixMilliseconds;
        set
        {
            if (SetProperty(ref _unixMilliseconds, value) && !_isUpdating)
                TryFromUnixInput(preferField: UnixField.Milliseconds);
        }
    }

    public string LocalTime
    {
        get => _localTime;
        set => SetProperty(ref _localTime, value);
    }

    public string LocalTimeLabel
    {
        get => _localTimeLabel;
        private set => SetProperty(ref _localTimeLabel, value);
    }

    public string UtcTime
    {
        get => _utcTime;
        set => SetProperty(ref _utcTime, value);
    }

    public string Iso8601Local
    {
        get => _iso8601Local;
        private set => SetProperty(ref _iso8601Local, value);
    }

    public string Iso8601Utc
    {
        get => _iso8601Utc;
        private set => SetProperty(ref _iso8601Utc, value);
    }

    public string RelativeHint
    {
        get => _relativeHint;
        private set => SetProperty(ref _relativeHint, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public TimestampConverterVM()
    {
        UpdateLocalTimeLabel();
    }

    public void FromUnix()
    {
        ErrorMessage = null;
        if (TryParseUnixTimestamp(out var dto))
            Apply(dto);
        else
            ErrorMessage = "请输入有效的 Unix 时间戳（秒 10 位左右 / 毫秒 13 位左右，可自动识别）。";
    }

    public void FromTime()
    {
        ErrorMessage = null;

        var hasLocal = !string.IsNullOrWhiteSpace(LocalTime);
        var hasUtc = !string.IsNullOrWhiteSpace(UtcTime);

        if (!hasLocal && !hasUtc)
        {
            ErrorMessage = "请填写时区时间或 UTC 时间（支持 ISO 8601）。";
            return;
        }

        DateTimeOffset dto;
        if (hasLocal && hasUtc)
        {
            if (!TryParseLocalTimeInSelectedZone(LocalTime, out dto))
            {
                ErrorMessage = "时区时间格式不正确。";
                return;
            }

            if (!TryParseDateTimeInput(UtcTime, preferUtc: true, out var utcDto))
            {
                ErrorMessage = "UTC 时间格式不正确。";
                return;
            }

            if (Math.Abs(dto.ToUnixTimeMilliseconds() - utcDto.ToUnixTimeMilliseconds()) > 60_000)
            {
                ErrorMessage = "时区时间与 UTC 相差超过 1 分钟，请检查输入。";
                return;
            }
        }
        else if (hasLocal)
        {
            if (!TryParseLocalTimeInSelectedZone(LocalTime, out dto))
            {
                ErrorMessage = "时区时间格式不正确。";
                return;
            }
        }
        else
        {
            if (!TryParseDateTimeInput(UtcTime, preferUtc: true, out dto))
            {
                ErrorMessage = "UTC 时间格式不正确。";
                return;
            }
        }

        Apply(dto);
    }

    public void SetNow()
    {
        ErrorMessage = null;
        Apply(DateTimeOffset.UtcNow);
    }

    public void Clear()
    {
        _isUpdating = true;
        _lastInstant = null;
        UnixSeconds = string.Empty;
        UnixMilliseconds = string.Empty;
        LocalTime = string.Empty;
        UtcTime = string.Empty;
        Iso8601Local = string.Empty;
        Iso8601Utc = string.Empty;
        RelativeHint = string.Empty;
        ErrorMessage = null;
        _isUpdating = false;
    }

    private enum UnixField { Seconds, Milliseconds }

    private void TryFromUnixInput(UnixField preferField)
    {
        if (string.IsNullOrWhiteSpace(UnixSeconds) && string.IsNullOrWhiteSpace(UnixMilliseconds))
            return;

        if (TryParseUnixTimestamp(preferField, out var dto))
        {
            ErrorMessage = null;
            Apply(dto);
        }
    }

    private bool TryParseUnixTimestamp(out DateTimeOffset dto) =>
        TryParseUnixTimestamp(UnixField.Seconds, out dto);

    private bool TryParseUnixTimestamp(UnixField preferField, out DateTimeOffset dto)
    {
        dto = default;
        var secText = UnixSeconds.Trim();
        var msText = UnixMilliseconds.Trim();

        if (preferField == UnixField.Seconds && TryParseUnixToken(secText, out dto))
            return true;
        if (preferField == UnixField.Milliseconds && TryParseUnixToken(msText, out dto))
            return true;
        if (TryParseUnixToken(secText, out dto))
            return true;
        return TryParseUnixToken(msText, out dto);
    }

    private static bool TryParseUnixToken(string text, out DateTimeOffset dto)
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

    private bool TryParseLocalTimeInSelectedZone(string value, out DateTimeOffset dto)
    {
        dto = default;
        if (!TryParseDateTimeOnly(value, out var dt))
            return false;

        var tz = GetSelectedTimeZone();
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

    private void Apply(DateTimeOffset dto)
    {
        _lastInstant = dto;
        ApplyDisplay(dto);
    }

    private void ApplyDisplay(DateTimeOffset dto)
    {
        _isUpdating = true;

        var tz = GetSelectedTimeZone();
        var zoned = TimeZoneInfo.ConvertTimeFromUtc(dto.UtcDateTime, tz);
        var offset = tz.GetUtcOffset(zoned);
        var zonedOffset = new DateTimeOffset(zoned, offset);

        UnixSeconds = dto.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        UnixMilliseconds = dto.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        LocalTime = zoned.ToString(DisplayFormat, CultureInfo.InvariantCulture);
        UtcTime = dto.UtcDateTime.ToString(DisplayFormat, CultureInfo.InvariantCulture);
        Iso8601Local = zonedOffset.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        Iso8601Utc = dto.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        RelativeHint = FormatRelative(dto, zoned);
        ErrorMessage = null;
        _isUpdating = false;
    }

    private void UpdateLocalTimeLabel()
    {
        var tz = GetSelectedTimeZone();
        var offset = tz.GetUtcOffset(DateTimeOffset.UtcNow);
        LocalTimeLabel = $"{tz.DisplayName} ({FormatUtcOffset(offset)})";
    }

    private TimeZoneInfo GetSelectedTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(SelectedTimeZoneId);
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

    private static IReadOnlyList<TimeZoneOption> BuildTimeZoneOptions()
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
        var options = new List<TimeZoneOption>();

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

    private static TimeZoneOption CreateOption(TimeZoneInfo tz, bool pin)
    {
        var offset = tz.GetUtcOffset(DateTimeOffset.UtcNow);
        var prefix = pin ? "★ " : string.Empty;
        var label = $"{prefix}{tz.DisplayName} ({FormatUtcOffset(offset)}) · {tz.Id}";
        return new TimeZoneOption(tz.Id, label);
    }

    private static string FormatUtcOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var abs = offset.Duration();
        return $"UTC{sign}{abs.Hours:00}:{abs.Minutes:00}";
    }
}
