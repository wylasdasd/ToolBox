using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Calculation;

namespace ToolBox.Components.Pages.Converters;

public sealed class TimestampConverterVM : ViewModelBase
{
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

    public static IReadOnlyList<TimeZoneOption> TimeZoneOptions { get; } =
        TimestampConvertService.TimeZoneOptions
            .Select(o => new TimeZoneOption(o.Id, o.Label))
            .ToList();

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
                TryFromUnixInput(UnixFieldPrefer.Seconds);
        }
    }

    public string UnixMilliseconds
    {
        get => _unixMilliseconds;
        set
        {
            if (SetProperty(ref _unixMilliseconds, value) && !_isUpdating)
                TryFromUnixInput(UnixFieldPrefer.Milliseconds);
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
        var result = TimestampConvertService.ParseUnix(UnixSeconds, UnixMilliseconds);
        if (!result.Success)
        {
            ErrorMessage = "请输入有效的 Unix 时间戳（秒 10 位左右 / 毫秒 13 位左右，可自动识别）。";
            return;
        }

        Apply(result.Value!);
    }

    public void FromTime()
    {
        ErrorMessage = null;
        var result = TimestampConvertService.ParseFromTime(LocalTime, UtcTime, SelectedTimeZoneId);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        Apply(result.Value!);
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

    private void TryFromUnixInput(UnixFieldPrefer preferField)
    {
        if (string.IsNullOrWhiteSpace(UnixSeconds) && string.IsNullOrWhiteSpace(UnixMilliseconds))
            return;

        var result = TimestampConvertService.ParseUnix(UnixSeconds, UnixMilliseconds, preferField);
        if (result.Success)
        {
            ErrorMessage = null;
            Apply(result.Value!);
        }
    }

    private void Apply(DateTimeOffset dto)
    {
        _lastInstant = dto;
        ApplyDisplay(dto);
    }

    private void ApplyDisplay(DateTimeOffset dto)
    {
        _isUpdating = true;
        var display = TimestampConvertService.BuildDisplay(dto, SelectedTimeZoneId);

        UnixSeconds = display.UnixSeconds;
        UnixMilliseconds = display.UnixMilliseconds;
        LocalTime = display.LocalTime;
        UtcTime = display.UtcTime;
        Iso8601Local = display.Iso8601Local;
        Iso8601Utc = display.Iso8601Utc;
        RelativeHint = display.RelativeHint;
        ErrorMessage = null;
        _isUpdating = false;
    }

    private void UpdateLocalTimeLabel() =>
        LocalTimeLabel = TimestampConvertService.FormatLocalTimeLabel(SelectedTimeZoneId);
}
