using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Tomlyn;
using Tomlyn.Model;
using ToolBox.Tools.Common;
using YamlDotNet.Serialization;

namespace ToolBox.Tools.Format;

public static class FormatConvertService
{
    public static IReadOnlyList<string> SupportedFormats { get; } =
        ["JSON", "YAML", "XML", "TOML", "CSV"];

    public static ToolResult<FormatConvertResult> Convert(string? input, string sourceFormat, string targetFormat)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ToolResult<FormatConvertResult>.Ok(new FormatConvertResult(string.Empty));

        try
        {
            JsonNode? intermediateObj = null;

            if (sourceFormat == "JSON")
            {
                intermediateObj = JsonNode.Parse(input);
            }
            else if (sourceFormat == "YAML")
            {
                var deserializer = new DeserializerBuilder().Build();
                var yamlObject = deserializer.Deserialize(new StringReader(input));

                var serializer = new SerializerBuilder().JsonCompatible().Build();
                string tempJson = serializer.Serialize(yamlObject);
                intermediateObj = JsonNode.Parse(tempJson);
            }
            else if (sourceFormat == "XML")
            {
                var doc = XDocument.Parse(input);
                if (doc.Root != null)
                {
                    var rootObj = new JsonObject();
                    rootObj[doc.Root.Name.LocalName] = XmlToJson(doc.Root);
                    intermediateObj = rootObj;
                }
            }
            else if (sourceFormat == "TOML")
            {
                intermediateObj = TomlToJsonNode(input);
            }
            else if (sourceFormat == "CSV")
            {
                intermediateObj = CsvToJsonNode(input);
            }

            if (intermediateObj is null)
                return ToolResult<FormatConvertResult>.Fail("Error: 无法解析输入或结果为空。");

            string output;

            if (targetFormat == "JSON")
            {
                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                output = intermediateObj?.ToJsonString(options) ?? "";
            }
            else if (targetFormat == "YAML")
            {
                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                string jsonStr = intermediateObj?.ToJsonString(options) ?? "{}";

                var deserializer = new DeserializerBuilder().Build();
                var yamlObj = deserializer.Deserialize(new StringReader(jsonStr));
                var serializer = new SerializerBuilder().Build();
                output = serializer.Serialize(yamlObj);
            }
            else if (targetFormat == "XML")
            {
                if (intermediateObj != null)
                {
                    var xDoc = JsonToXml(intermediateObj);
                    output = xDoc.ToString();
                }
                else
                {
                    output = string.Empty;
                }
            }
            else if (targetFormat == "TOML")
            {
                output = intermediateObj is null ? string.Empty : JsonNodeToToml(intermediateObj);
            }
            else if (targetFormat == "CSV")
            {
                output = intermediateObj is null ? string.Empty : JsonNodeToCsv(intermediateObj);
            }
            else
            {
                output = string.Empty;
            }

            return ToolResult<FormatConvertResult>.Ok(new FormatConvertResult(output));
        }
        catch (Exception ex)
        {
            return ToolResult<FormatConvertResult>.Fail($"Error: {ex.Message}");
        }
    }

    private static JsonNode? XmlToJson(XElement element)
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

    private static XDocument JsonToXml(JsonNode node)
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

    private static void AddJsonContent(XElement parent, string name, JsonNode? node)
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

    private static XElement CreateElement(string name, JsonNode? node)
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