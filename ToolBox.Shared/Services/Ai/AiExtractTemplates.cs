namespace ToolBox.Services.Ai;

public sealed record AiExtractTemplate(string Id, string Name, string Template);

public static class AiExtractTemplates
{
    public const string DefaultSystemPrompt =
        "你是结构化数据提取助手。严格按用户模板提取信息，只输出一个合法 JSON 对象，不要 Markdown 代码块，不要额外说明。";

    public const string MergeBatchSystemPrompt =
        "你是结构化数据提取助手。严格按用户模板分别提取多个来源，只输出一个合法 JSON 数组，不要 Markdown 代码块，不要额外说明。";

    public static IReadOnlyList<AiExtractTemplate> Presets { get; } =
    [
        new("generic-kv", "通用键值", """
            从内容中提取以下字段（缺失则 null）：
            {
              "title": "标题或主题",
              "summary": "简要摘要",
              "tags": ["标签1", "标签2"]
            }
            """),
        new("device-log", "设备日志", """
            从设备日志或截图中提取：
            {
              "address": "MAC 或设备地址",
              "battery": "电池电压",
              "value": "读数",
              "sensorTemp": "传感器温度",
              "timestamp": "时间戳或时间字符串"
            }
            """),
        new("invoice", "票据/发票", """
            从票据内容中提取：
            {
              "vendor": "商户",
              "date": "日期",
              "total": "总金额",
              "currency": "币种",
              "items": [{ "name": "品名", "qty": 1, "price": "单价" }]
            }
            """),
        new("contact", "联系人", """
            从内容中提取：
            {
              "name": "姓名",
              "phone": "电话",
              "email": "邮箱",
              "company": "公司",
              "address": "地址"
            }
            """)
    ];
}
