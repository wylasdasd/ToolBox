using Blazing.Mvvm.ComponentModel;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Tomlyn;
using Tomlyn.Model;
using YamlDotNet.Serialization;

namespace ToolBox.Components.Pages.Converters;

public partial class FormatConverterVM : ViewModelBase
{
    public static IReadOnlyList<string> SupportedFormats { get; } =
        ["JSON", "YAML", "XML", "TOML", "CSV"];
    private string _input = string.Empty;
    private string _output = string.Empty;
    private string _sourceFormat = "JSON";
    private string _targetFormat = "YAML";
    private string? _errorMessage;

    public string Input
    {
        get => _input;
        set => SetProperty(ref _input, value);
    }

    public string Output
    {
        get => _output;
        set => SetProperty(ref _output, value);
    }

    public string SourceFormat
    {
        get => _sourceFormat;
        set => SetProperty(ref _sourceFormat, value);
    }

    public string TargetFormat
    {
        get => _targetFormat;
        set => SetProperty(ref _targetFormat, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public void ConvertFormat()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Input)) return;

        try
        {
            JsonNode? intermediateObj = null;

            // 1. Source -> JsonNode
            if (SourceFormat == "JSON")
            {
                intermediateObj = JsonNode.Parse(Input);
            }
            else if (SourceFormat == "YAML")
            {
                var deserializer = new DeserializerBuilder().Build();
                var yamlObject = deserializer.Deserialize(new StringReader(Input));

                var serializer = new SerializerBuilder().JsonCompatible().Build();
                string tempJson = serializer.Serialize(yamlObject);
                intermediateObj = JsonNode.Parse(tempJson);
            }
            else if (SourceFormat == "XML")
            {
                var doc = XDocument.Parse(Input);
                if (doc.Root != null)
                {
                    var rootObj = new JsonObject();
                    rootObj[doc.Root.Name.LocalName] = XmlToJson(doc.Root);
                    intermediateObj = rootObj;
                }
            }
            else if (SourceFormat == "TOML")
            {
                intermediateObj = TomlToJsonNode(Input);
            }
            else if (SourceFormat == "CSV")
            {
                intermediateObj = CsvToJsonNode(Input);
            }

            if (intermediateObj is null)
            {
                ErrorMessage = "Error: 无法解析输入或结果为空。";
                return;
            }

            // 2. JsonNode -> Target
            if (TargetFormat == "JSON")
            {
                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                Output = intermediateObj?.ToJsonString(options) ?? "";
            }
            else if (TargetFormat == "YAML")
            {
                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                string jsonStr = intermediateObj?.ToJsonString(options) ?? "{}";

                var deserializer = new DeserializerBuilder().Build();
                var yamlObj = deserializer.Deserialize(new StringReader(jsonStr));
                var serializer = new SerializerBuilder().Build();
                Output = serializer.Serialize(yamlObj);
            }
            else if (TargetFormat == "XML")
            {
                if (intermediateObj != null)
                {
                    var xDoc = JsonToXml(intermediateObj);
                    Output = xDoc.ToString();
                }
            }
            else if (TargetFormat == "TOML")
            {
                Output = intermediateObj is null ? string.Empty : JsonNodeToToml(intermediateObj);
            }
            else if (TargetFormat == "CSV")
            {
                Output = intermediateObj is null ? string.Empty : JsonNodeToCsv(intermediateObj);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }

    // Helpers for XML <-> JSON without Newtonsoft
    private JsonNode? XmlToJson(XElement element)
    {
        var obj = new JsonObject();
        bool hasAttributes = element.HasAttributes;

        foreach (var attr in element.Attributes())
        {
            obj["@" + attr.Name.LocalName] = attr.Value;
        }

        if (!element.HasElements)
        {
            if (hasAttributes)
            {
                obj["#text"] = element.Value;
                return obj;
            }
            else
            {
                return JsonValue.Create(element.Value);
            }
        }

        foreach (var group in element.Elements().GroupBy(e => e.Name.LocalName))
        {
            if (group.Count() > 1)
            {
                var arr = new JsonArray();
                foreach (var item in group) arr.Add(XmlToJson(item));
                obj[group.Key] = arr;
            }
            else
            {
                obj[group.Key] = XmlToJson(group.First());
            }
        }

        return obj;
    }

    private XDocument JsonToXml(JsonNode node)
    {
        XElement rootElement;

        if (node is JsonObject jObj)
        {
            var props = jObj.ToList();
            if (props.Count == 1)
            {
                rootElement = CreateElement(props[0].Key, props[0].Value);
            }
            else
            {
                rootElement = new XElement("Root");
                foreach (var prop in jObj)
                {
                    AddJsonContent(rootElement, prop.Key, prop.Value);
                }
            }
        }
        else
        {
            rootElement = new XElement("Root", node.ToString());
        }

        return new XDocument(rootElement);
    }

    private void AddJsonContent(XElement parent, string name, JsonNode? node)
    {
        if (node == null) return;

        if (name.StartsWith("@"))
        {
            parent.Add(new XAttribute(name.Substring(1), node.ToString()));
            return;
        }

        if (name == "#text")
        {
            parent.Add(new XText(node.ToString()));
            return;
        }

        if (node is JsonValue)
        {
            parent.Add(new XElement(name, node.ToString()));
        }
        else if (node is JsonObject)
        {
            parent.Add(CreateElement(name, node));
        }
        else if (node is JsonArray jArr)
        {
            foreach (var item in jArr)
            {
                AddJsonContent(parent, name, item);
            }
        }
    }

    private XElement CreateElement(string name, JsonNode? node)
    {
        var elem = new XElement(name);
        if (node is JsonObject jObj)
        {
            foreach (var prop in jObj)
            {
                AddJsonContent(elem, prop.Key, prop.Value);
            }
        }
        else if (node is JsonValue)
        {
            elem.Value = node.ToString();
        }
        return elem;
    }

    private static JsonNode? TomlToJsonNode(string toml)
    {
        var model = Toml.ToModel(toml);
        var json = JsonSerializer.Serialize(model);
        return JsonNode.Parse(json);
    }

    private static string JsonNodeToToml(JsonNode node)
    {
        if (node is JsonObject obj)
            return Toml.FromModel(JsonObjectToTomlTable(obj));
        if (node is JsonArray arr && arr.Count > 0 && arr[0] is JsonObject)
            throw new InvalidOperationException("TOML 根节点需为表结构，数组根请先转为 JSON 对象。");
        return Toml.FromModel(new TomlTable { ["value"] = JsonNodeToTomlValue(node) ?? string.Empty });
    }

    private static TomlTable JsonObjectToTomlTable(JsonObject obj)
    {
        var table = new TomlTable();
        foreach (var prop in obj)
        {
            if (prop.Key is null) continue;
            table[prop.Key] = JsonNodeToTomlValue(prop.Value) ?? string.Empty;
        }
        return table;
    }

    private static object? JsonNodeToTomlValue(JsonNode? node) => node switch
    {
        null => null,
        JsonObject o => JsonObjectToTomlTable(o),
        JsonArray a => a.Select(JsonNodeToTomlValue).ToList(),
        JsonValue v => v.TryGetValue(out bool b) ? b
            : v.TryGetValue(out long l) ? l
            : v.TryGetValue(out double d) ? d
            : v.GetValue<string>(),
        _ => node.ToString()
    };

    private static JsonNode? CsvToJsonNode(string csv)
    {
        var rows = ParseCsv(csv);
        if (rows.Count == 0) return new JsonArray();

        if (rows.Count == 1)
        {
            var arr = new JsonArray();
            foreach (var cell in rows[0]) arr.Add(cell);
            return arr;
        }

        var headers = rows[0];
        var result = new JsonArray();
        foreach (var row in rows.Skip(1))
        {
            if (row.All(string.IsNullOrEmpty)) continue;
            var obj = new JsonObject();
            for (var i = 0; i < headers.Count; i++)
            {
                var key = headers[i];
                if (string.IsNullOrWhiteSpace(key)) key = $"col{i}";
                obj[key] = i < row.Count ? row[i] : string.Empty;
            }
            result.Add(obj);
        }
        return result;
    }

    private static string JsonNodeToCsv(JsonNode node)
    {
        var sb = new StringBuilder();
        if (node is JsonArray { Count: > 0 } arr && arr[0] is JsonObject firstObj)
        {
            var headers = firstObj.Select(p => p.Key).ToList();
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvCell)));
            foreach (var item in arr)
            {
                if (item is not JsonObject row) continue;
                var cells = headers.Select(h => row.TryGetPropertyValue(h, out var v) ? v?.ToString() ?? "" : "");
                sb.AppendLine(string.Join(",", cells.Select(EscapeCsvCell)));
            }
            return sb.ToString().TrimEnd();
        }

        if (node is JsonArray plainArr)
        {
            foreach (var item in plainArr)
                sb.AppendLine(EscapeCsvCell(item?.ToString() ?? ""));
            return sb.ToString().TrimEnd();
        }

        throw new InvalidOperationException("CSV 输出需要 JSON 数组（对象数组或纯值数组）。");
    }

    private static List<List<string>> ParseCsv(string csv)
    {
        var rows = new List<List<string>>();
        using var reader = new StringReader(csv);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            rows.Add(ParseCsvLine(line));
        }
        return rows;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                cells.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        cells.Add(sb.ToString());
        return cells;
    }

    private static string EscapeCsvCell(string? value)
    {
        value ??= string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
