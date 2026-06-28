using System.Globalization;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Calculation;

public sealed record UnitDef(string Id, string Label, decimal FactorToBase);

public sealed record UnitCategory(string Id, string Label, string BaseHint, UnitDef[] Units);

public sealed record UnitConversionRow(string Label, string Value, bool IsActive);

public sealed record UnitConversionResult(IReadOnlyList<UnitConversionRow> Rows, string? NetworkHint, string? ErrorMessage);

public static class UnitConvertService
{
    public static IReadOnlyList<UnitCategory> Categories { get; } =
    [
        new("storage-iec", "存储 IEC (1024)", "基准：Byte",
        [
            new("b", "B (Byte)", 1m),
            new("kib", "KiB", 1024m),
            new("mib", "MiB", 1024m * 1024m),
            new("gib", "GiB", 1024m * 1024m * 1024m),
            new("tib", "TiB", 1024m * 1024m * 1024m * 1024m),
            new("bit", "bit", 1m / 8m),
            new("kibit", "Kibit", 1024m / 8m),
            new("mibit", "Mibit", 1024m * 1024m / 8m),
        ]),
        new("storage-si", "存储 SI (1000)", "基准：Byte · 硬盘/U 盘标称常用",
        [
            new("b", "B (Byte)", 1m),
            new("kb", "KB", 1000m),
            new("mb", "MB", 1_000_000m),
            new("gb", "GB", 1_000_000_000m),
            new("tb", "TB", 1_000_000_000_000m),
            new("bit", "bit", 1m / 8m),
            new("kbit", "Kbit", 1000m / 8m),
            new("mbit", "Mbit", 1_000_000m / 8m),
        ]),
        new("network-bit", "网络带宽 (bit/s)", "基准：bit/s · 运营商/网卡标称",
        [
            new("bps", "bps", 1m),
            new("kbps", "Kbps", 1_000m),
            new("mbps", "Mbps", 1_000_000m),
            new("gbps", "Gbps", 1_000_000_000m),
            new("tbps", "Tbps", 1_000_000_000_000m),
        ]),
        new("network-byte", "网络吞吐 (Byte/s)", "基准：B/s · 下载器/磁盘写入常用",
        [
            new("bps", "B/s", 1m),
            new("kibps", "KiB/s", 1024m),
            new("mibps", "MiB/s", 1024m * 1024m),
            new("gibps", "GiB/s", 1024m * 1024m * 1024m),
            new("kbps-si", "KB/s (SI)", 1000m),
            new("mbps-si", "MB/s (SI)", 1_000_000m),
            new("gbps-si", "GB/s (SI)", 1_000_000_000m),
        ]),
        new("time", "时间", "基准：秒 (s) · 超时/TTL/延迟",
        [
            new("ns", "ns", 1e-9m),
            new("us", "μs", 1e-6m),
            new("ms", "ms", 1e-3m),
            new("s", "s", 1m),
            new("min", "min", 60m),
            new("h", "h", 3600m),
            new("day", "day", 86400m),
        ]),
        new("frequency", "频率", "基准：Hz · CPU/时钟",
        [
            new("hz", "Hz", 1m),
            new("khz", "KHz", 1_000m),
            new("mhz", "MHz", 1_000_000m),
            new("ghz", "GHz", 1_000_000_000m),
        ]),
        new("memory-page", "内存页", "基准：Byte · 常见页大小 4 KiB / 8 KiB",
        [
            new("b", "B", 1m),
            new("kib", "KiB", 1024m),
            new("page4k", "页 (4 KiB)", 4096m),
            new("page8k", "页 (8 KiB)", 8192m),
            new("mib", "MiB", 1024m * 1024m),
            new("gib", "GiB", 1024m * 1024m * 1024m),
        ]),
    ];

    public static IEnumerable<UnitDef> FileSizeUnits =>
        Categories.First(c => c.Id == "storage-si").Units.Where(u => u.Id is "b" or "kb" or "mb" or "gb" or "tb");

    public static IEnumerable<UnitDef> BandwidthUnits =>
        Categories.First(c => c.Id == "network-bit").Units;

    public static UnitConversionResult Convert(string? input, string categoryId, string fromUnitId)
    {
        if (!TryParseInput(input, out var inputValue, out var parseError))
            return new UnitConversionResult([], null, parseError);

        var cat = GetCategory(categoryId);
        var from = cat.Units.FirstOrDefault(u => u.Id == fromUnitId) ?? cat.Units[0];
        var baseValue = inputValue * from.FactorToBase;

        var rows = cat.Units
            .Select(unit => new UnitConversionRow(unit.Label, FormatValue(baseValue / unit.FactorToBase), unit.Id == from.Id))
            .ToList();

        var hint = BuildNetworkHint(categoryId, inputValue, fromUnitId, cat, from);
        return new UnitConversionResult(rows, hint, null);
    }

    public static ToolResult<string> CalculateTransferTime(string? fileSize, string fileSizeUnitId, string? bandwidth, string bandwidthUnitId)
    {
        if (!TryParseInput(fileSize, out var size, out _) || !TryParseInput(bandwidth, out var bw, out _) || bw <= 0)
            return ToolResult<string>.Ok(string.Empty);

        var sizeUnit = FileSizeUnits.FirstOrDefault(u => u.Id == fileSizeUnitId) ?? FileSizeUnits.First();
        var bwUnit = BandwidthUnits.FirstOrDefault(u => u.Id == bandwidthUnitId) ?? BandwidthUnits.First();

        var bytes = size * sizeUnit.FactorToBase;
        var bitsPerSec = bw * bwUnit.FactorToBase;
        if (bitsPerSec <= 0)
            return ToolResult<string>.Ok(string.Empty);

        var seconds = bytes * 8m / bitsPerSec;
        return ToolResult<string>.Ok(FormatDuration(seconds));
    }

    public static bool TryParseInput(string? text, out decimal value, out string? errorMessage)
    {
        value = 0;
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (!decimal.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && !decimal.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            errorMessage = "请输入有效数字。";
            return false;
        }

        return true;
    }

    public static UnitCategory GetCategory(string id) =>
        Categories.FirstOrDefault(c => c.Id == id) ?? Categories[0];

    private static string? BuildNetworkHint(string categoryId, decimal v, string fromUnitId, UnitCategory cat, UnitDef from)
    {
        if (categoryId is not ("network-bit" or "network-byte" or "storage-iec" or "storage-si"))
            return null;
        if (v == 0)
            return "提示：1 Byte = 8 bit；100 Mbps ≈ 12.5 MB/s (SI) ≈ 11.92 MiB/s (IEC)。";

        if (categoryId == "network-bit")
        {
            var bps = v * from.FactorToBase;
            var bsSi = bps / 8m;
            var bsIec = bps / 8m;
            return $"≈ {FormatValue(bsSi / 1_000_000m)} MB/s (SI) · {FormatValue(bsIec / (1024m * 1024m))} MiB/s (IEC)";
        }

        if (categoryId == "network-byte")
        {
            var bs = v * from.FactorToBase;
            var mbps = bs * 8m / 1_000_000m;
            return $"≈ {FormatValue(mbps)} Mbps (标称带宽，SI)";
        }

        return null;
    }

    private static string FormatValue(decimal value)
    {
        if (value == 0) return "0";
        var abs = Math.Abs(value);
        if (abs >= 1_000_000_000_000m) return value.ToString("0.########", CultureInfo.InvariantCulture);
        if (abs >= 1) return value.ToString("0.########", CultureInfo.InvariantCulture);
        if (abs >= 0.000_001m) return value.ToString("0.########", CultureInfo.InvariantCulture);
        return value.ToString("0.##e+0", CultureInfo.InvariantCulture);
    }

    private static string FormatDuration(decimal seconds)
    {
        if (seconds < 0) return "—";
        if (seconds < 1e-3m) return $"{(seconds * 1_000_000m):0.##} μs";
        if (seconds < 1m) return $"{(seconds * 1000m):0.##} ms";
        if (seconds < 60m) return $"{seconds:0.##} s";
        if (seconds < 3600m) return $"{(seconds / 60m):0.##} min ({seconds:0.##} s)";
        if (seconds < 86400m) return $"{(seconds / 3600m):0.##} h ({seconds:0.##} s)";
        return $"{(seconds / 86400m):0.##} day ({seconds:0.##} s)";
    }
}
