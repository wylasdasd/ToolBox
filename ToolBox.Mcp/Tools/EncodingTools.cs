using System.ComponentModel;
using ModelContextProtocol.Server;
using CommonHelp;
using ToolBox.Tools.Encoding;

namespace ToolBox.Mcp.Tools;

[McpServerToolType]
public static class EncodingTools
{
    [McpServerTool, Description("Encode plain text to Base64.")]
    public static string Base64Encode(string plainText, bool useUrlSafe = false) =>
        McpToolResponses.From(Base64Help.Encode(plainText, useUrlSafe));

    [McpServerTool, Description("Decode Base64 to plain text.")]
    public static string Base64Decode(string base64Text, bool useUrlSafe = false) =>
        McpToolResponses.From(Base64Help.Decode(base64Text, useUrlSafe));

    [McpServerTool, Description("URL-encode plain text.")]
    public static string UrlEncode(string plainText, bool useEscapeDataString = false) =>
        McpToolResponses.From(UrlCodecHelp.Encode(plainText, useEscapeDataString));

    [McpServerTool, Description("URL-decode encoded text.")]
    public static string UrlDecode(string encodedText, bool useEscapeDataString = false) =>
        McpToolResponses.From(UrlCodecHelp.Decode(encodedText, useEscapeDataString));

    [McpServerTool, Description("Parse a JWT and return header/payload details.")]
    public static string JwtParse(string token) =>
        McpToolResponses.From(JwtParseHelp.Parse(token));

    [McpServerTool, Description("Encode text to hex using the specified encoding (default utf-8).")]
    public static string TextToHex(
        string text,
        string encodingId = "utf-8",
        string displayModeId = "spaced",
        bool uppercaseHex = true) =>
        McpToolResponses.From(TextHexHelp.EncodeTextToHex(
            text, encodingId, displayModeId, uppercaseHex, showUnicodeEscape: false, showCEscape: false));

    [McpServerTool, Description("Decode hex bytes to text.")]
    public static string HexToText(string hexInput, string encodingId = "utf-8") =>
        McpToolResponses.From(TextHexHelp.DecodeHexToText(hexInput, encodingId));

    [McpServerTool, Description("Parse a data-URI or raw Base64 image string and return mime type plus data URL.")]
    public static string DataUriImageParse(string input) =>
        McpToolResponses.From(DataUriImageService.Parse(input));
}
