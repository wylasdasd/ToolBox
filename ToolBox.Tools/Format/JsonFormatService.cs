using System.Text;
using System.Text.Json;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Format;

public static class JsonFormatService
{
    public static IReadOnlyList<JsonTemplateOption> TemplateOptions { get; } =
    [
        new JsonTemplateOption("simple", "简单对象"),
        new JsonTemplateOption("list", "列表数据"),
        new JsonTemplateOption("user", "用户资料"),
        new JsonTemplateOption("api", "接口响应")
    ];

    public static ToolResult<JsonFormatOutputResult> Process(string? input, bool indent, bool sortKeys)
    {
        try
        {
            using var doc = JsonDocument.Parse(input ?? string.Empty);
            var options = new JsonWriterOptions { Indented = indent };
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, options))
            {
                WriteElement(writer, doc.RootElement, sortKeys);
            }

            return ToolResult<JsonFormatOutputResult>.Ok(new JsonFormatOutputResult(System.Text.Encoding.UTF8.GetString(stream.ToArray())));
        }
        catch (Exception ex)
        {
            return ToolResult<JsonFormatOutputResult>.Fail(ex.Message);
        }
    }

    public static ToolResult<JsonValidateResult> Validate(string? input)
    {
        try
        {
            _ = JsonDocument.Parse(input ?? string.Empty);
            return ToolResult<JsonValidateResult>.Ok(new JsonValidateResult("JSON 校验通过。"));
        }
        catch (Exception ex)
        {
            return ToolResult<JsonValidateResult>.Fail(ex.Message);
        }
    }

    public static ToolResult<JsonPathQueryResult> QueryJsonPath(string? input, string? jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
            return ToolResult<JsonPathQueryResult>.Fail("JSONPath 为空。");

        try
        {
            using var doc = JsonDocument.Parse(input ?? string.Empty);
            var matches = EvaluateJsonPath(doc.RootElement, jsonPath.Trim());
            if (matches.Count == 0)
                return ToolResult<JsonPathQueryResult>.Ok(new JsonPathQueryResult("未匹配到结果。", true));

            if (matches.Count == 1)
                return ToolResult<JsonPathQueryResult>.Ok(new JsonPathQueryResult(matches[0].GetRawText(), false));

            var items = matches.Select(m => m.GetRawText());
            var result = "[\n" + string.Join(",\n", items) + "\n]";
            return ToolResult<JsonPathQueryResult>.Ok(new JsonPathQueryResult(result, false));
        }
        catch (Exception ex)
        {
            return ToolResult<JsonPathQueryResult>.Fail(ex.Message);
        }
    }


    public static string GetTemplate(string key)
    {
        return key switch
        {
            "list" => """
                      {
                        "total": 2,
                        "items": [
                          { "id": 1, "name": "Alpha" },
                          { "id": 2, "name": "Beta" }
                        ]
                      }
                      """,
            "user" => """
                      {
                        "id": "u_10001",
                        "name": "张三",
                        "email": "user@example.com",
                        "roles": ["admin", "editor"],
                        "profile": {
                          "company": "ToolBox",
                          "location": "Shanghai"
                        }
                      }
                      """,
            "api" => """
                     {
                       "code": 0,
                       "message": "ok",
                       "data": {
                         "requestId": "req_abc",
                         "timestamp": "2026-02-11T12:00:00Z"
                       }
                     }
                     """,
            _ => """
                 {
                   "name": "ToolBox",
                   "version": 1,
                   "enabled": true,
                   "tags": ["json", "format"]
                 }
                 """
        };
    }

    public sealed record JsonTemplateOption(string Key, string Label);
    private static List<JsonElement> EvaluateJsonPath(JsonElement root, string jsonPath)
    {
        var tokens = JsonPathTokenizer.Parse(jsonPath);
        var current = new List<JsonElement> { root };

        foreach (var token in tokens)
        {
            var next = new List<JsonElement>();
            foreach (var element in current)
            {
                switch (token.Kind)
                {
                    case JsonPathTokenKind.Property:
                        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(token.Value, out var property))
                        {
                            next.Add(property);
                        }
                        break;
                    case JsonPathTokenKind.Index:
                        if (element.ValueKind == JsonValueKind.Array)
                        {
                            var index = token.Index;
                            if (index >= 0 && index < element.GetArrayLength())
                            {
                                next.Add(element[index]);
                            }
                        }
                        break;
                    case JsonPathTokenKind.Wildcard:
                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in element.EnumerateObject())
                            {
                                next.Add(prop.Value);
                            }
                        }
                        else if (element.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in element.EnumerateArray())
                            {
                                next.Add(item);
                            }
                        }
                        break;
                }
            }

            current = next;
            if (current.Count == 0)
            {
                break;
            }
        }

        return current;
    }

    
    private static class JsonPathTokenizer
    {
        public static List<JsonPathToken> Parse(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                throw new ArgumentException("JSONPath 为空。");
            }

            var path = jsonPath.Trim();
            if (!path.StartsWith("$", StringComparison.Ordinal))
            {
                throw new ArgumentException("JSONPath 必须以 $ 开头。");
            }

            var tokens = new List<JsonPathToken>();
            var index = 1;
            while (index < path.Length)
            {
                var current = path[index];
                if (current == '.')
                {
                    index++;
                    var start = index;
                    while (index < path.Length && path[index] != '.' && path[index] != '[')
                    {
                        index++;
                    }

                    if (start == index)
                    {
                        throw new ArgumentException("JSONPath 属性名为空。");
                    }

                    var name = path[start..index];
                    tokens.Add(JsonPathToken.Property(name));
                    continue;
                }

                if (current == '[')
                {
                    var close = path.IndexOf(']', index);
                    if (close < 0)
                    {
                        throw new ArgumentException("JSONPath 缺少 ]。");
                    }

                    var content = path[(index + 1)..close].Trim();
                    if (content == "*")
                    {
                        tokens.Add(JsonPathToken.Wildcard());
                    }
                    else if (content.StartsWith("'") && content.EndsWith("'") && content.Length >= 2)
                    {
                        var name = content[1..^1];
                        tokens.Add(JsonPathToken.Property(name));
                    }
                    else if (content.StartsWith("\"") && content.EndsWith("\"") && content.Length >= 2)
                    {
                        var name = content[1..^1];
                        tokens.Add(JsonPathToken.Property(name));
                    }
                    else if (int.TryParse(content, out var arrayIndex))
                    {
                        tokens.Add(JsonPathToken.ArrayIndex(arrayIndex));
                    }
                    else
                    {
                        throw new ArgumentException("JSONPath 下标格式不正确。");
                    }

                    index = close + 1;
                    continue;
                }

                throw new ArgumentException("JSONPath 格式不正确。");
            }

            return tokens;
        }
    }

    private sealed record JsonPathToken(JsonPathTokenKind Kind, string Value, int Index)
    {
        public static JsonPathToken Property(string name) => new(JsonPathTokenKind.Property, name, -1);
        public static JsonPathToken ArrayIndex(int index) => new(JsonPathTokenKind.Index, string.Empty, index);
        public static JsonPathToken Wildcard() => new(JsonPathTokenKind.Wildcard, string.Empty, -1);
    }

    private enum JsonPathTokenKind
    {
        Property,
        Index,
        Wildcard
    }

    private static void WriteElement(Utf8JsonWriter writer, JsonElement element, bool sortKeys)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                IEnumerable<JsonProperty> props = element.EnumerateObject();
                if (sortKeys)
                {
                    props = props.OrderBy(p => p.Name, StringComparer.Ordinal);
                }

                foreach (var prop in props)
                {
                    writer.WritePropertyName(prop.Name);
                    WriteElement(writer, prop.Value, sortKeys);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(writer, item, sortKeys);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                {
                    writer.WriteNumberValue(longValue);
                }
                else
                {
                    writer.WriteNumberValue(element.GetDouble());
                }
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }
}
