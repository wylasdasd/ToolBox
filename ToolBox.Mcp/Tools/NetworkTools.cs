using System.ComponentModel;
using ModelContextProtocol.Server;
using ToolBox.Tools.Network;

namespace ToolBox.Mcp.Tools;

[McpServerToolType]
public static class NetworkTools
{
    [McpServerTool, Description("Parse a cron expression and list upcoming run times.")]
    public static string CronParse(string expression, bool includeSeconds = false) =>
        McpToolResponses.From(CronParseService.Parse(expression, includeSeconds));

    [McpServerTool, Description("Calculate IPv4 network details from CIDR or address + mask.")]
    public static string IpCalculate(string input) =>
        McpToolResponses.From(IpCalculatorService.Calculate(input));

    [McpServerTool, Description("Convert a numeric value to hex bytes with endianness control.")]
    public static string EndianValueToHex(
        string input,
        string dataTypeId = "int32",
        bool bigEndian = false,
        bool inputIsHex = false) =>
        McpToolResponses.From(EndianHexService.ConvertValueToHex(input, dataTypeId, bigEndian, inputIsHex));

    [McpServerTool, Description("Decode hex bytes to a typed numeric value.")]
    public static string EndianHexToValue(
        string hexInput,
        string dataTypeId = "int32",
        bool bigEndian = false) =>
        McpToolResponses.From(EndianHexService.ConvertHexToValue(hexInput, dataTypeId, bigEndian));

    [McpServerTool, Description("Convert a raw HTTP request text to curl or PowerShell (no outbound HTTP).")]
    public static string RequestToCurlFromRaw(
        string rawRequest,
        bool isHttps = true,
        string outputFormat = "curl") =>
        McpToolResponses.From(RequestToCurlService.ConvertFromRaw(rawRequest, isHttps, outputFormat));

    [McpServerTool, Description("Build curl or PowerShell from method/url/headers/body fields (no outbound HTTP).")]
    public static string RequestToCurlFromForm(
        string method,
        string url,
        string? headersText = null,
        string? body = null,
        bool isHttps = true,
        string outputFormat = "curl") =>
        McpToolResponses.From(RequestToCurlService.ConvertFromForm(
            method, url, headersText, body, isHttps, outputFormat));
}
