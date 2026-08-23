using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommonHelp;

/// <summary>
/// System.Text.Json 序列化/反序列化扩展方法与命名策略。
/// </summary>
public static class JsonExtensions
{
    /// <summary>属性名全小写的命名策略单例。</summary>
    public static LowerCaseNamingPolicy lowerCaseNamingPolicy = new LowerCaseNamingPolicy();

    /// <summary>缩进 + 属性名全小写 + 非 ASCII 不转义（缓存实例，避免每次调用重建元数据）。</summary>
    private static readonly JsonSerializerOptions IndentedLowerCaseOptions = new()
    {
        PropertyNamingPolicy = lowerCaseNamingPolicy, // 关键：使用自定义命名策略（全小写操作）
        WriteIndented = true, // 让 JSON 格式化
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 关键：禁用不必要的转义
    };

    /// <summary>缩进 + 保留原属性名 + 非 ASCII 不转义（缓存实例，避免每次调用重建元数据）。</summary>
    private static readonly JsonSerializerOptions IndentedRelaxedOptions = new()
    {
        WriteIndented = true, // 让 JSON 格式化
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 关键：禁用不必要的转义
    };

    /// <summary>容错反序列化选项：忽略属性名大小写并容忍尾逗号。</summary>
    private static readonly JsonSerializerOptions LenientDeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true, // 忽略字段大小写（容错）
        AllowTrailingCommas = true //允许 JSON 结尾有逗号
    };

    /// <summary>
    /// 序列化为缩进 JSON：属性名全部转小写，中文等非 ASCII 字符不转义。支持值类型（如 JsonElement/JsonNode）。
    /// </summary>
    public static string JsonSeToLow<T>(this T obj)
    {
        // 使用 JsonSerializer.Serialize 方法进行序列化
        return JsonSerializer.Serialize(obj, IndentedLowerCaseOptions);
    }

    /// <summary>
    /// 序列化为缩进 JSON：保留原属性名，中文等非 ASCII 字符不转义。支持值类型（如 JsonElement/JsonNode）。
    /// </summary>
    public static string JsonSeNoLow<T>(this T obj)
    {
        // 使用 JsonSerializer.Serialize 方法进行序列化
        return JsonSerializer.Serialize(obj, IndentedRelaxedOptions);
    }

    /// <summary>
    /// 反序列化 JSON 字符串：忽略属性名大小写并容忍尾逗号。
    /// </summary>
    public static T? JsonDe<T>(this string json) where T : class
    {
        // 使用 JsonSerializer.Deserialize 方法进行反序列化
        return JsonSerializer.Deserialize<T>(json, LenientDeserializeOptions);
    }

    /// <summary>
    /// 使用自定义 JsonSerializerOptions 序列化对象。
    /// </summary>
    public static string JsonSe<T>(this T obj, JsonSerializerOptions options)
    {
        // 使用 JsonSerializer.Serialize 方法进行序列化
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// 使用自定义 JsonSerializerOptions 反序列化 JSON 字符串。
    /// </summary>
    public static T? JsonDe<T>(this string json, JsonSerializerOptions options) where T : class
    {
        // 使用 JsonSerializer.Deserialize 方法进行反序列化
        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// JSON 属性名全小写命名策略。
    /// </summary>
    public class LowerCaseNamingPolicy : JsonNamingPolicy
    {
        /// <summary>把属性名转换为全小写。</summary>
        public override string ConvertName(string name) => name.ToLowerInvariant();
    }
}

/// <summary>
/// 容错 bool 转换器：接受 true/false、"true"/"false"、"1"/"0"、1/0 等多种 JSON 表示。
/// </summary>
public class FlexibleBoolConverter : JsonConverter<bool>
{
    /// <summary>读取 JSON 值并容错转换为 bool；无法识别时返回 false。</summary>
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 如果 JSON 值是布尔类型 (true/false)
        if (reader.TokenType == JsonTokenType.True) return true;
        if (reader.TokenType == JsonTokenType.False) return false;

        // 如果 JSON 值是字符串 ("true"/"false")
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            // 处理可能的数字字符串，如 "1" 为 true
            if (value == "1") return true;
            if (value == "0") return false;
        }

        // 如果是数字类型 (1/0)
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32() != 0;
        }

        return false; // 默认值
    }

    /// <summary>写出标准 JSON 布尔值。</summary>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
