using MudBlazor;

namespace ToolBox.Components.Layout;

public sealed record ToolNavItem(string Href, string Title, string Icon, bool WebEnabled = true, bool MauiEnabled = true);

public sealed record ToolNavGroup(string Title, string Icon, IReadOnlyList<ToolNavItem> Items);

public static class ToolNavDefinition
{
    public static IReadOnlyList<ToolNavGroup> AllGroups { get; } =
    [
        new("编码与安全", Icons.Material.Filled.Lock,
        [
            new("base64", "Base64 编解码", Icons.Material.Filled.SwapHoriz),
            new("url-codec", "URL 编解码", Icons.Material.Filled.Link),
            new("jwt", "JWT 解析", Icons.Material.Filled.Key),
            new("text-hex", "文本 ↔ Hex", Icons.Material.Filled.ViewArray),
            new("naming-style", "命名风格转换", Icons.Material.Filled.TextFields),
            new("format-convert", "格式转换 (TOML/CSV)", Icons.Material.Filled.Transform),
        ]),
        new("格式与生成", Icons.Material.Filled.DataObject,
        [
            new("json-format", "JSON 格式化/压缩", Icons.Material.Filled.DataObject),
            new("uuid", "UUID/GUID 批量生成", Icons.Material.Filled.Fingerprint),
            new("json-to-csharp", "JSON 转 C# Model", Icons.Material.Filled.IntegrationInstructions),
            new("base64-image", "Base64 图片预览", Icons.Material.Filled.Image),
            new("svg-preview", "SVG 预览", Icons.Material.Filled.ImageSearch),
        ]),
        new("文本处理", Icons.Material.Filled.EditNote,
        [
            new("regex", "正则匹配", Icons.Material.Filled.Search),
            new("diff", "文本差异对比", Icons.Material.Filled.Difference),
            new("text-lines", "文本整理", Icons.Material.Filled.FormatListBulleted),
            new("clipboard-sequence", "剪贴板固定序列输入", Icons.Material.Filled.Keyboard, WebEnabled: false),
        ]),
        new("计算工具", Icons.Material.Filled.Calculate,
        [
            new("converters", "常用转换", Icons.Material.Filled.CompareArrows),
            new("bitwise", "位运算", Icons.Material.Filled.Memory),
            new("struct-layout", "结构体内存布局", Icons.Material.Filled.ViewModule),
        ]),
        new("网络调试", Icons.Material.Filled.Public,
        [
            new("request-to-curl", "请求转 cURL", Icons.Material.Filled.SwapHoriz),
            new("cron-parser", "Cron 表达式解析", Icons.Material.Filled.Schedule),
            new("ip-calculator", "IPv4 / 子网计算", Icons.Material.Filled.Lan),
            new("endian-hex", "字节序 / 整数 ↔ Hex", Icons.Material.Filled.Memory),
            new("socket-tool", "Socket 测试", Icons.Material.Filled.SettingsEthernet, WebEnabled: false),
            new("websocket-tool", "WebSocket 测试", Icons.Material.Filled.Wifi, WebEnabled: false),
        ]),
        new("AI 提取", Icons.Material.Filled.AutoAwesome,
        [
            new("ai-extract", "AI 结构化提取", Icons.Material.Filled.DataObject),
        ]),
        new("开发效率", Icons.Material.Filled.Code,
        [
            new("csharp-run", "C# Runner", Icons.Material.Filled.Code, WebEnabled: false),
            new("directory-sync", "目录同步", Icons.Material.Filled.Sync, WebEnabled: false),
            new("multi-task-runner", "多任务系统", Icons.Material.Filled.Queue, WebEnabled: false),
        ]),
    ];

    public static IReadOnlyList<ToolNavGroup> GetGroupsForWeb() => FilterGroups(i => i.WebEnabled);

    public static IReadOnlyList<ToolNavGroup> GetGroupsForMaui() => FilterGroups(i => i.MauiEnabled);

    public static IEnumerable<string> GetWebRoutePaths() =>
        AllGroups
            .SelectMany(g => g.Items)
            .Where(i => i.WebEnabled)
            .Select(i => NormalizePath(i.Href));

    private static IReadOnlyList<ToolNavGroup> FilterGroups(Func<ToolNavItem, bool> predicate) =>
        AllGroups
            .Select(g => new ToolNavGroup(g.Title, g.Icon, g.Items.Where(predicate).ToList()))
            .Where(g => g.Items.Count > 0)
            .ToList();

    internal static string NormalizePath(string href)
    {
        var path = href.TrimStart('/');
        return path.Length == 0 ? "/" : "/" + path;
    }
}
