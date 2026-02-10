using Blazing.Mvvm.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using YamlDotNet.Serialization;

namespace ToolBox.Components.Pages.Converters;

public partial class FormatConverterVM : ViewModelBase
{
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
}
