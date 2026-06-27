using Blazing.Mvvm.ComponentModel;
using System.Text;
using System.Text.Json;

namespace ToolBox.Components.Pages.TextTool;

public sealed class JsonFormatVM : ViewModelBase
{
    private string _inputJson = string.Empty;
    private string _outputJson = string.Empty;
    private string? _errorMessage;
    private string? _infoMessage;
    private bool _sortKeys = true;
    private string _jsonPath = string.Empty;
    private string _jsonPathResult = string.Empty;
    private string? _jsonPathError;
    private string _selectedTemplateKey = "simple";

    public IReadOnlyList<JsonTemplateOption> TemplateOptions { get; } =
    [
        new JsonTemplateOption("simple", "简单对象"),
        new JsonTemplateOption("list", "列表数据"),
        new JsonTemplateOption("user", "用户资料"),
        new JsonTemplateOption("api", "接口响应")
    ];

    public string InputJson
    {
        get => _inputJson;
        set => SetProperty(ref _inputJson, value);
    }

    public string OutputJson
    {
        get => _outputJson;
        private set => SetProperty(ref _outputJson, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string? InfoMessage
    {
        get => _infoMessage;
        private set => SetProperty(ref _infoMessage, value);
    }

    public bool SortKeys
    {
        get => _sortKeys;
        set => SetProperty(ref _sortKeys, value);
    }

    public string JsonPath
    {
        get => _jsonPath;
        set => SetProperty(ref _jsonPath, value);
    }

    public string JsonPathResult
    {
        get => _jsonPathResult;
        private set => SetProperty(ref _jsonPathResult, value);
    }

    public string? JsonPathError
    {
        get => _jsonPathError;
        private set => SetProperty(ref _jsonPathError, value);
    }

    public string SelectedTemplateKey
    {
        get => _selectedTemplateKey;
        set => SetProperty(ref _selectedTemplateKey, value);
    }

    public void Format()
    {
        ProcessJson(indent: true);
    }

    public void Minify()
    {
        ProcessJson(indent: false);
    }

    public void Validate()
    {
        ErrorMessage = null;
        InfoMessage = null;
        try
        {
            _ = JsonDocument.Parse(InputJson ?? string.Empty);
            InfoMessage = "JSON 校验通过。";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void Clear()
    {
        InputJson = string.Empty;
        OutputJson = string.Empty;
        ErrorMessage = null;
        InfoMessage = null;
        JsonPath = string.Empty;
        JsonPathResult = string.Empty;
        JsonPathError = null;
    }

    public void ApplyTemplate()
    {
        InputJson = GetTemplate(SelectedTemplateKey);
        OutputJson = string.Empty;
        ErrorMessage = null;
        InfoMessage = null;
        JsonPathError = null;
        JsonPathResult = string.Empty;
    }

    public void QueryJsonPath()
    {
        JsonPathError = null;
        JsonPathResult = string.Empty;
        ErrorMessage = null;
        InfoMessage = null;

        if (string.IsNullOrWhiteSpace(JsonPath))
        {
            JsonPathError = "JSONPath 为空。";
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(InputJson ?? string.Empty);
            var matches = EvaluateJsonPath(doc.RootElement, JsonPath.Trim());
            if (matches.Count == 0)
            {
                JsonPathResult = "未匹配到结果。";
                return;
            }

            if (matches.Count == 1)
            {
                JsonPathResult = matches[0].GetRawText();
                return;
            }

            var items = matches.Select(m => m.GetRawText());
            JsonPathResult = "[\n" + string.Join(",\n", items) + "\n]";
        }
        catch (Exception ex)
        {
            JsonPathError = ex.Message;
        }
    }

    public void ClearJsonPath()
    {
        JsonPathResult = string.Empty;
        JsonPathError = null;
    }

    private void ProcessJson(bool indent)
    {
        ErrorMessage = null;
        InfoMessage = null;
        try
        {
            using var doc = JsonDocument.Parse(InputJson ?? string.Empty);
            var options = new JsonWriterOptions { Indented = indent };
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, options))
            {
                WriteElement(writer, doc.RootElement, SortKeys);
            }

            OutputJson = Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

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

    private static string GetTemplate(string key)
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
