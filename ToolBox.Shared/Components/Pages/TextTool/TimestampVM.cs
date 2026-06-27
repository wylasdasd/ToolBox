using Blazing.Mvvm.ComponentModel;
using System.Globalization;

namespace ToolBox.Components.Pages.TextTool;

public sealed class TimestampVM : ViewModelBase
{
    private string _unixSeconds = string.Empty;
    private string _unixMilliseconds = string.Empty;
    private string _localTime = string.Empty;
    private string _utcTime = string.Empty;
    private string? _errorMessage;

    public string UnixSeconds
    {
        get => _unixSeconds;
        set => SetProperty(ref _unixSeconds, value);
    }

    public string UnixMilliseconds
    {
        get => _unixMilliseconds;
        set => SetProperty(ref _unixMilliseconds, value);
    }

    public string LocalTime
    {
        get => _localTime;
        set => SetProperty(ref _localTime, value);
    }

    public string UtcTime
    {
        get => _utcTime;
        set => SetProperty(ref _utcTime, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void FromUnix()
    {
        ErrorMessage = null;
        if (long.TryParse(UnixSeconds, out var seconds))
        {
            var dto = DateTimeOffset.FromUnixTimeSeconds(seconds);
            UtcTime = dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            LocalTime = dto.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            UnixMilliseconds = dto.ToUnixTimeMilliseconds().ToString();
            return;
        }

        if (long.TryParse(UnixMilliseconds, out var milliseconds))
        {
            var dto = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
            UtcTime = dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            LocalTime = dto.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            UnixSeconds = dto.ToUnixTimeSeconds().ToString();
            return;
        }

        ErrorMessage = "请输入有效的 Unix 秒或毫秒时间戳。";
    }

    public void FromTime()
    {
        ErrorMessage = null;
        if (!TryParseDateTime(LocalTime, DateTimeKind.Local, out var local))
        {
            ErrorMessage = "本地时间格式不正确。";
            return;
        }

        if (!TryParseDateTime(UtcTime, DateTimeKind.Utc, out var utc))
        {
            ErrorMessage = "UTC 时间格式不正确。";
            return;
        }

        var localDto = new DateTimeOffset(local);
        var utcDto = new DateTimeOffset(utc, TimeSpan.Zero);

        UnixSeconds = utcDto.ToUnixTimeSeconds().ToString();
        UnixMilliseconds = utcDto.ToUnixTimeMilliseconds().ToString();
        LocalTime = localDto.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        UtcTime = utcDto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void SetNow()
    {
        ErrorMessage = null;
        var now = DateTimeOffset.Now;
        LocalTime = now.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        UtcTime = now.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        UnixSeconds = now.ToUnixTimeSeconds().ToString();
        UnixMilliseconds = now.ToUnixTimeMilliseconds().ToString();
    }

    public void Clear()
    {
        UnixSeconds = string.Empty;
        UnixMilliseconds = string.Empty;
        LocalTime = string.Empty;
        UtcTime = string.Empty;
        ErrorMessage = null;
    }

    private static bool TryParseDateTime(string value, DateTimeKind kind, out DateTime result)
    {
        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out result))
        {
            result = DateTime.SpecifyKind(result, kind);
            return true;
        }

        return false;
    }
}
