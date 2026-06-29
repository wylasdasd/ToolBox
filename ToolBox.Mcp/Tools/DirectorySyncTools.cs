using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using ToolBox.Tools.DirectorySync;

namespace ToolBox.Mcp.Tools;

[McpServerToolType]
public static class DirectorySyncTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [McpServerTool, Description("Build a directory sync preview plan from source/target file manifests (no disk sync).")]
    public static string DirectorySyncPlan(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        string sourceFilesJson,
        string targetFilesJson)
    {
        var sourceFiles = ParseManifest(sourceFilesJson, nameof(sourceFilesJson));
        if (sourceFiles.Error is not null)
            return McpToolResponses.Error(sourceFiles.Error);

        var targetFiles = ParseManifest(targetFilesJson, nameof(targetFilesJson));
        if (targetFiles.Error is not null)
            return McpToolResponses.Error(targetFiles.Error);

        var result = DirectorySyncPlanService.BuildPlan(
            sourcePath,
            targetPath,
            deleteExtraFiles,
            sourceFiles.Value!,
            targetFiles.Value!);

        return McpToolResponses.From(result);
    }

    private static (Dictionary<string, DirectorySyncFileEntry>? Value, string? Error) ParseManifest(
        string json,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (new Dictionary<string, DirectorySyncFileEntry>(StringComparer.OrdinalIgnoreCase), null);

        try
        {
            var items = JsonSerializer.Deserialize<List<DirectorySyncManifestItem>>(json, JsonOptions);
            if (items is null)
                return (null, $"{parameterName} 不是有效的 JSON 数组。");

            var map = new Dictionary<string, DirectorySyncFileEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.RelativePath))
                    return (null, $"{parameterName} 中存在空的 relativePath。");

                if (!DateTime.TryParse(item.LastWriteTimeUtc, out var lastWrite))
                    return (null, $"无法解析 lastWriteTimeUtc：{item.LastWriteTimeUtc}");

                map[item.RelativePath] = new DirectorySyncFileEntry(item.Length, lastWrite.ToUniversalTime());
            }

            return (map, null);
        }
        catch (JsonException ex)
        {
            return (null, $"{parameterName} JSON 解析失败：{ex.Message}");
        }
    }

    private sealed record DirectorySyncManifestItem(
        string RelativePath,
        long Length,
        string LastWriteTimeUtc);
}
