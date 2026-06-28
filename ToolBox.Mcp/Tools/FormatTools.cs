using System.ComponentModel;
using ModelContextProtocol.Server;
using ToolBox.Tools.Format;

namespace ToolBox.Mcp.Tools;

[McpServerToolType]
public static class FormatTools
{
    [McpServerTool, Description("Format JSON with optional key sorting.")]
    public static string JsonFormat(string inputJson, bool sortKeys = true) =>
        McpToolResponses.From(JsonFormatService.Process(inputJson, indent: true, sortKeys));

    [McpServerTool, Description("Minify JSON (remove whitespace).")]
    public static string JsonMinify(string inputJson, bool sortKeys = true) =>
        McpToolResponses.From(JsonFormatService.Process(inputJson, indent: false, sortKeys));

    [McpServerTool, Description("Validate JSON syntax.")]
    public static string JsonValidate(string inputJson) =>
        McpToolResponses.From(JsonFormatService.Validate(inputJson));

    [McpServerTool, Description("Query JSON with a simple JSONPath expression.")]
    public static string JsonPathQuery(string inputJson, string jsonPath) =>
        McpToolResponses.From(JsonFormatService.QueryJsonPath(inputJson, jsonPath));

    [McpServerTool, Description("Convert between JSON, YAML, XML, TOML, and CSV.")]
    public static string FormatConvert(string input, string sourceFormat, string targetFormat) =>
        McpToolResponses.From(FormatConvertService.Convert(input, sourceFormat, targetFormat));

    [McpServerTool, Description("Convert JSON to C# model classes.")]
    public static string JsonToCSharp(
        string jsonInput,
        bool lowerCaseProperties = false,
        bool csharpStyleNames = true,
        bool nullable = true,
        string numberType = "decimal",
        bool detectTypes = true,
        bool recordType = false,
        bool mergeArrays = false)
    {
        var options = new JsonToCSharpOptions(
            lowerCaseProperties,
            csharpStyleNames,
            nullable,
            numberType,
            detectTypes,
            recordType,
            mergeArrays);
        return McpToolResponses.From(JsonToCSharpService.Convert(jsonInput, options));
    }
}
