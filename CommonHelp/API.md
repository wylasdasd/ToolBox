# CommonHelp API 索引

> 命名空间：`CommonHelp`（file-scoped）
> 约定：公开类/方法均使用 XML doc 注释；Service 层返回 `ToolResult<T>`（Ok/Fail）。

---

## 目录

- [Common（通用）](#common)
- [ArrayHelps（数组与枚举）](#arrayhelps)
- [CalculationHelps（计算）](#calculationhelps)
- [EncodingHelps（编码）](#encodinghelps)
- [Exceptions（异常）](#exceptions)
- [FileHelps（文件）](#filehelps)
- [JsonHelps（JSON）](#jsonhelps)
- [NetHelps（网络）](#nethelps)
- [StringHelps（文本）](#stringhelps)
- [TimeHelps（时间）](#timehelps)
- [WindowsHelps（Windows 平台）](#windowshelps)

---

## Common

### `ToolResult<T>`  `sealed record`

统一的操作结果封装，用于 Service 层向 UI 层返回成功/失败信息。

| 成员 | 说明 |
|:---|:---|
| `Success` (bool) | 操作是否成功 |
| `Value` (T?) | 成功时的返回值 |
| `Error` (string?) | 失败时的错误说明 |
| `static Ok(T value)` | 创建成功结果 |
| `static Fail(string error)` | 创建失败结果 |

---

## ArrayHelps

### `ArrayHelp`  `static class`

数组随机打乱工具。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Shuffle` | `void Shuffle<T>(T[] array)` | Fisher-Yates 洗牌算法，原地打乱数组 |
| `Shuffle` | `void Shuffle<T>(IList<T> list)` | Fisher-Yates 洗牌算法，原地打乱 `IList<T>` |

### `EnumHelp`  `static class`

枚举扩展工具：通过 `DescriptionAttribute` 获取描述文本。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `GetDescription` | `string? GetDescription<T>(T value)` | 获取枚举值的 `[Description]` 标注文本 |
| `ToDictionary` | `Dictionary<T, string?> ToDictionary<T>()` | 将枚举所有值与描述转为字典 |
| `GetList` | `List<EnumItemDetail> GetList<T>()` | 返回枚举所有值的 `EnumItemDetail` 列表 |
| `GetEnumByDescription` | `T? GetEnumByDescription<T>(string? description)` | 按描述文本反向查找枚举值 |

### `EnumItemDetail`  `class`

枚举值详情：包含枚举值（`Value`）与描述文本（`Description`）。

### `RandomHelp`  `class`

随机抽取与 UUID 批量生成。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `GetRandomList<T>` | `List<T> GetRandomList<T>(List<T> source, int count)` | 从列表中随机抽取指定数量元素 |
| `GenerateUuids` | `UuidGenerateResult GenerateUuids(int count, bool upperCase, bool withHyphens)` | 批量生成 UUID/GUID 字符串 |

### `UuidGenerateResult`  `record`

UUID 生成结果：`Uuids`（生成的列表）、`Count`（数量）。

---

## CalculationHelps

### `BitwiseHelp`  `static class`

位运算计算工具：支持 AND、OR、XOR、NOT 及左右移位。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Compute` | `ToolResult<BitwiseComputeResult> Compute(string? input, string? op, string? operand)` | 执行位运算（AND/OR/XOR/NOT/左移/逻辑右移/算术右移） |
| `IsShiftOp` | `bool IsShiftOp(string op)` | 判断是否为移位操作 |
| `GetMask` | `long GetMask(int bits)` | 生成指定位宽的掩码 |
| `SignExtend` | `long SignExtend(long value, int originalBits)` | 将无符号值按原始位宽做符号扩展 |
| `NormalizeResult` | `long NormalizeResult(long value, int bits)` | 将结果裁剪到指定位宽 |
| `FormatValue` | `string FormatValue(long value, int bits)` | 格式化值为十进制/十六进制/二进制展示 |
| `FormatBinary` | `string FormatBinary(long value, int bits)` | 格式化为二进制（每 8 位空格分隔） |
| `FormatHex` | `string FormatHex(long value, int bits)` | 格式化为十六进制 |

### `BitwiseComputeResult`  `record`

位运算结果：`Result`（十进制）、`Hex`（十六进制）、`Binary`（二进制）、`Operation`（操作说明）。

### `RadixHelp`  `static class`

进制转换工具：支持 2–128 进制互转。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ConvertFrom` | `ToolResult<RadixConvertResult> ConvertFrom(string? input, int fromBase, int toBase)` | 从 `fromBase` 进制转换到 `toBase` 进制 |
| `Parse` | `long Parse(string? value, int fromBase)` | 将指定进制的字符串解析为 long |
| `Format` | `string Format(long value, int toBase)` | 将 long 格式化为指定进制的字符串 |
| `GetDigitAlphabet` | `string GetDigitAlphabet(int base)` | 获取指定进制使用的字符集（如 16 进制返回 `0-9A-F`） |
| `DigitAlphabetHint` | `string DigitAlphabetHint(int base)` | 获取进制字符集的人类可读提示 |
| `ClearRadixValues` | — | 清除/重置输入值（UI 辅助） |

### `RadixConvertResult`  `record`

进制转换结果：`InputValue`、`FromBase`、`ToBase`、`OutputValue`、`ToAlphabet`。

### `ColorConvertHelp`  `static class`

颜色转换工具：HEX ↔ RGBA ↔ HSL。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `NormalizeHexDigits` | `string NormalizeHexDigits(string? hex)` | 规范化 HEX 输入（去 #、补齐位数） |
| `FromHex` | `ColorState FromHex(string? hex)` | 从 HEX 字符串解析颜色状态 |
| `FromRgba` | `ColorState FromRgba(int r, int g, int b, int a)` | 从 RGBA 分量构建颜色状态 |
| `ClampByte` | `int ClampByte(int value)` | 将值裁剪到 0–255 范围 |
| `AlphaPercentToByte` | `int AlphaPercentToByte(double percent)` | 透明度百分比 (0–1) 转字节值 |

### `ColorState`  `record`

颜色状态：`HexDigits`、`R`/`G`/`B`/`A`（0–255）、`H`/`S`/`L`（0–360/0–1/0–1）。

---

## EncodingHelps

### `Base64Help`  `static class`

Base64 编解码，支持标准与 URL-safe 两种字母表。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Encode` | `string Encode(string? input, bool urlSafe)` | 编码为 Base64 字符串 |
| `Decode` | `string Decode(string? input, bool urlSafe)` | 从 Base64 字符串解码 |

### `EncodingHelp`  `static class`

编码辅助工具：常见编码获取与十六进制字节解析/格式化。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `TextHexEncodings` | `static IReadOnlyList<EncodingOption> TextHexEncodings` | 预定义编码列表（UTF-8、GBK、Big5、Shift_JIS 等） |
| `GetEncoding` | `Encoding? GetEncoding(string? codePageOrName)` | 按编码名/CodePage 获取 Encoding 实例 |
| `ParseHexBytes` | `byte[] ParseHexBytes(string? hex, int expectedLength)` | 解析 HEX 字符串为字节数组（可指定长度） |
| `FormatHexBytes` | `string FormatHexBytes(byte[] bytes)` | 字节数组格式化为空格分隔的 HEX 字符串 |

### `EncodingOption`  `record`

编码选项：`Name`（编码名）、`DisplayName`（展示名）、`CodePage`（代码页号）。

### `TextHexHelp`  `static class`

文本与十六进制互转工具。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `EncodeTextToHex` | `TextHexEncodeResult EncodeTextToHex(string? text, string encodingName, bool showUnicodeEscape, bool spaceSeparate)` | 文本编码为 HEX（可选 Unicode 转义展示） |
| `DecodeHexToText` | `TextHexDecodeResult DecodeHexToText(string? hex, string encodingName)` | HEX 解码为文本 |

### `TextHexEncodeResult`  `record`

编码结果：`Output`（HEX 输出）、`ByteCount`（字节数）。

### `TextHexDecodeResult`  `record`

解码结果：`Output`（文本输出）、`ByteCount`（字节数）。

### `JwtParseHelp`  `static class`

JWT（JSON Web Token）解析工具：解码 Header/Payload 并提取过期时间。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Parse` | `ToolResult<JwtParseResult> Parse(string? token)` | 解析 JWT，返回 Header/Payload JSON 与 exp 过期时间 |

### `JwtParseResult`  `record`

JWT 解析结果：`HeaderJson`、`PayloadJson`、`Expiry`（过期时间，可为 null）。

### `UrlCodecHelp`  `static class`

URL 编解码工具。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Encode` | `string Encode(string? input, bool useWebUtilityMode)` | URL 编码；`useWebUtilityMode=true` 时使用 `WebUtility.UrlEncode` |
| `Decode` | `string Decode(string? input, bool useWebUtilityMode)` | URL 解码；`useWebUtilityMode=true` 时使用 `WebUtility.UrlDecode` |

---

## Exceptions

### `FileBigException`  `class : Exception`

文件过大时抛出的自定义异常。

| 构造函数 | 说明 |
|:---|:---|
| `FileBigException(string message)` | 指定错误消息 |
| `FileBigException(string message, Exception inner)` | 指定错误消息与内部异常 |

---

## FileHelps

### `FileHelp`  `static class`

文件操作工具集：创建、读写、复制、删除、目录遍历等。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `EnsureDirectoryExists` | `void EnsureDirectoryExists(string filePath)` | 确保文件所在目录存在 |
| `CreateFile` | `void CreateFile(string filePath)` | 创建或覆盖文件 |
| `WriteAllText` | `void WriteAllText(string filePath, string contents)` | 同步写入全部文本 |
| `WriteAllTextAsync` | `Task WriteAllTextAsync(string filePath, string contents, CancellationToken ct)` | 异步写入全部文本 |
| `ReadAllText` | `string ReadAllText(string filePath)` | 同步读取全部文本 |
| `ReadAllTextAsync` | `Task<string> ReadAllTextAsync(string filePath, CancellationToken ct)` | 异步读取全部文本 |
| `DeleteIfExists` | `bool DeleteIfExists(string filePath)` | 文件存在时删除；返回是否实际删除 |
| `CopyFile` | `void CopyFile(string source, string target, bool overwrite)` | 复制文件 |
| `GetFilesRecursively` | `List<string> GetFilesRecursively(string directory, string? pattern, string? excludedDir)` | 递归获取目录下所有文件（可排除子目录） |
| `GetDirectorySize` | `long GetDirectorySize(string directory)` | 计算目录总大小（字节） |
| `SafeFileName` | `string SafeFileName(string fileName)` | 替换文件名中的非法字符 |
| `CreateFileStream` | `FileStream CreateFileStream(string filePath, FileMode mode, FileAccess access, FileShare share)` | 创建文件流 |
| `WriteBytesAsync` | `Task WriteBytesAsync(string filePath, byte[] data, CancellationToken ct)` | 异步写入字节数组 |
| `WriteStreamAsync` | `Task WriteStreamAsync(string filePath, Stream source, CancellationToken ct)` | 将流内容异步写入文件 |
| `CreateEmptyFileDurable` | `void CreateEmptyFileDurable(string filePath)` | 原子性创建空文件（防止并发冲突） |

### `AtomicJsonFileHelper`  `class`

原子性 JSON 文件读写工具：通过 `FileShare.None` 独占 + 进程内 `Monitor` 锁实现跨进程/线程安全。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ReadAtomic<T>` | `static AtomicFileHandle<T> ReadAtomic<T>(string filePath)` | 以独占方式打开文件并返回反序列化的数据句柄；修改后调用 `handle.Commit()` 写回 |
| `WriteAtomic<T>` | `static void WriteAtomic<T>(string filePath, T data)` | 独占写入（覆盖），不做读出 |

### `AtomicFileHandle<T>`  `sealed class : IDisposable` （`AtomicJsonFileHelper` 内部类）

原子性文件读写句柄，持有独占访问与监视器锁直到释放。

| 成员 | 说明 |
|:---|:---|
| `Data` (T?) | 反序列化后的数据（可直接修改） |
| `Commit()` | 将修改后的数据序列化写回文件，然后释放锁 |
| `Dispose()` | 释放文件句柄与监视器锁（不写回） |

---

## JsonHelps

### `JsonExtensions`  `static class`

`System.Text.Json` 序列化/反序列化扩展方法。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `JsonSeToLow<T>` | `string JsonSeToLow<T>(this T obj)` | 序列化为缩进 JSON，属性名全小写，中文不转义 |
| `JsonSeNoLow<T>` | `string JsonSeNoLow<T>(this T obj)` | 序列化为缩进 JSON，保留原属性名，中文不转义 |
| `JsonDe<T>` | `T? JsonDe<T>(this string json)` | 反序列化：忽略大小写、容忍尾逗号 |
| `JsonSe<T>` | `string JsonSe<T>(this T obj, JsonSerializerOptions options)` | 使用自定义 Options 序列化 |
| `JsonDe<T>` | `T? JsonDe<T>(this string json, JsonSerializerOptions options)` | 使用自定义 Options 反序列化 |

#### `LowerCaseNamingPolicy`  `class : JsonNamingPolicy`

属性名全小写命名策略（`ConvertName` → `name.ToLowerInvariant()`）。

### `FlexibleBoolConverter`  `class : JsonConverter<bool>`

容错 bool 转换器：接受 `true/false`、`"true"/"false"`、`"1"/"0"`、`1/0` 等多种 JSON 表示。

| 方法 | 说明 |
|:---|:---|
| `Read` | 读取并容错转换为 bool；无法识别时返回 false |
| `Write` | 写出标准 JSON 布尔值 |

---

## NetHelps

### `HttpExtensions`  `static class`

`HttpRequestMessage` 扩展：将 HTTP 请求导出为等价 curl 命令。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ToCurlStringAsync` | `Task<string> ToCurlStringAsync(this HttpRequestMessage request)` | 转换为 curl 命令字符串（含方法、URL、请求头、JSON/表单体） |

### `EndianHexHelp`  `static class`

数值与十六进制字节互转：支持 int8–uint64、float32/float64 十种类型与大端/小端字节序。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ConvertValueToHex` | `ToolResult<EndianHexResult> ConvertValueToHex(string? input, string? dataTypeId, bool bigEndian, bool inputIsHex)` | 数值转 HEX 字节；同时输出二进制与十进制 |
| `ConvertHexToValue` | `ToolResult<EndianHexResult> ConvertHexToValue(string? input, string? dataTypeId, bool bigEndian)` | HEX 字节转十进制数值 |
| `DataTypes` | `static IReadOnlyList<EndianHexDataType> DataTypes` | 全部支持的数据类型（int8/uint8/int16/…/float64） |

### `EndianHexDataType`  `sealed record`

数据类型定义：`Id`、`Label`、`ByteSize`、`IsFloat`。

### `EndianHexResult`  `sealed record`

端序转换结果：`HexOutput`（空格分隔）、`BinaryOutput`（每字节 8 位）、`DecimalOutput`。

### `IpCalculatorHelp`  `static class`

IPv4 子网计算器：由 IP + CIDR/掩码计算网络地址、广播地址、主机范围等。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Calculate` | `ToolResult<IpCalculatorResult> Calculate(string? input)` | 计算子网信息；支持 `192.168.1.10/24`、`192.168.1.10 255.255.255.0`、纯 IP（/32）三种输入 |

### `IpCalculatorResult`  `sealed record`

子网计算结果：`Address`、`IntegerValue`、`HexValue`、`SubnetMask`、`WildcardMask`、`NetworkAddress`、`BroadcastAddress`、`FirstHost`、`LastHost`、`HostCount`、`Cidr`。

### `RequestHelp`  `class`

静态 `HttpClient` 快捷请求工具（适用于无 DI/日志的简单场景）。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `SendRequestAsync` | `static Task<int> SendRequestAsync(string url, string body, Action<HttpRequestHeaders>? action)` | 发送 JSON POST 请求，返回 HTTP 状态码 |

### `IHttpService`  `interface`

基于 `IHttpClientFactory` 的 HTTP 服务接口（推荐通过 DI 注入）。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `PostRequestAsync<T>` | `Task<T?> PostRequestAsync<T>(string url, string body, int timeoutSeconds, Action<HttpRequestHeaders>? action)` | POST JSON 请求并反序列化响应 |
| `PutRequestAsync<T>` | `Task<T?> PutRequestAsync<T>(string url, string body, int timeoutSeconds, Action<HttpRequestHeaders>? action)` | PUT JSON 请求并反序列化响应 |
| `DeleteRequestAsync<T>` | `Task<T?> DeleteRequestAsync<T>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action)` | DELETE 请求并反序列化响应 |
| `GetRequestAsync<T>` | `Task<T?> GetRequestAsync<T>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action)` | GET 请求并反序列化响应 |
| `PostFormRequestAsync<T>` | `Task<T?> PostFormRequestAsync<T>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action, int timeoutSeconds)` | POST 表单（urlencoded）请求并反序列化响应 |

### `HttpService`  `class : IHttpService`

`IHttpService` 的默认实现：封装 POST/PUT/DELETE/GET/表单 POST，超时或异常时返回 default 并记录日志。`T` 为 `string` 时直接返回响应文本。

---

## StringHelps

### `StringHelp`  `class`

文本统计工具：行数、字符数/字节数统计。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `CountLines` | `static int CountLines(string text, string? lineSeparator)` | 计算文本行数（支持自定义行分隔符） |
| `CountCharacters` | `static long CountCharacters(string text, Encoding? encoding)` | 计算字符数或字节数（encoding 为 null 返回字符数，否则返回字节数） |
| `CountFileLinesAndChars` | `static (long Lines, long Count) CountFileLinesAndChars(string filePath, Encoding? encoding)` | 读取文件并计算行数与字符/字节数 |
| `CountFileCharacters` | `static long CountFileCharacters(string filePath, Encoding? encoding)` | 计算文件的字符数或字节数 |

### `NamingStyleHelp`  `static class`

标识符命名风格转换：将任意形式标识符拆词后一次性转换为 8 种常见风格。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Convert` | `ToolResult<NamingStyleResult> Convert(string? input)` | 转换为 camelCase/PascalCase/snake_case/kebab-case/SCREAMING_SNAKE/dot.case/Title Case/lowercase |

### `NamingStyleResult`  `sealed record`

命名风格转换结果：`CamelCase`、`PascalCase`、`SnakeCase`、`KebabCase`、`ScreamingSnake`、`DotCase`、`TitleCase`、`LowerCase`。空输入时所有字段为空字符串（`Empty` 静态属性）。

### `TextLinesHelp`  `static class`

多行文本处理工具：去重、排序、去空行、裁剪空白与行级统计。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ComputeStats` | `static TextLineStats ComputeStats(string? text, bool applyTrim, bool removeEmpty, bool ignoreCase)` | 统计行数、非空行数、去重行数、单词数、字符数 |
| `Deduplicate` | `static ToolResult<TextLinesOperationResult> Deduplicate(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase, bool keepOrder)` | 多行文本去重（可选保留首次出现顺序） |
| `SortAsc` | `static ToolResult<TextLinesOperationResult> SortAsc(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase)` | 按字典序升序排序 |
| `SortDesc` | `static ToolResult<TextLinesOperationResult> SortDesc(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase)` | 按字典序降序排序 |
| `RemoveEmptyLines` | `static ToolResult<TextLinesOperationResult> RemoveEmptyLines(string? text, bool trimLines)` | 移除所有空白行 |
| `TrimOnly` | `static ToolResult<TextLinesOperationResult> TrimOnly(string? text)` | 裁剪每行首尾空白（保留空行） |

### `TextLineStats`  `sealed record`

文本统计信息：`LineCount`、`NonEmptyLineCount`、`UniqueLineCount`、`WordCount`、`CharCount`、`CharNoWhitespaceCount`。

### `TextLinesOperationResult`  `sealed record`

多行文本操作结果：`Output`（处理后的文本）、`StatusMessage`（状态提示）。

### `OcrTextNormalizeHelp`  `static class`

OCR 文本归一化工具：全角转半角 + 中文标点映射为英文标点。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `NormalizeSymbolsToAscii` | `static string NormalizeSymbolsToAscii(string? text, bool removeSpaces)` | 归一化 OCR 文本：先 FormKC 归一化，再映射中文标点为英文标点；可选去除空格 |

---

## TimeHelps

### `TimestampConvertHelp`  `static class`

Unix 时间戳与日期时间互转工具：支持智能识别秒/毫秒、任意时区显示、ISO 8601 解析与相对时间提示。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ParseUnix` | `ToolResult<DateTimeOffset> ParseUnix(string? unixSeconds, string? unixMilliseconds, UnixFieldPrefer preferField)` | 解析 Unix 秒/毫秒时间戳（≥12 位按毫秒处理）；按 preferField 优先取值 |
| `ParseUnix` | `ToolResult<DateTimeOffset> ParseUnix(string? unixSeconds, string? unixMilliseconds)` | 简化重载（优先秒字段） |
| `ParseFromTime` | `ToolResult<DateTimeOffset> ParseFromTime(string? localTime, string? utcTime, string timeZoneId)` | 由时区时间/UTC 时间（支持 ISO 8601）解析 DateTimeOffset |
| `BuildDisplay` | `TimestampDisplayResult BuildDisplay(DateTimeOffset dto, string timeZoneId)` | 构建完整展示信息（秒/毫秒戳、本地/UTC 时间、ISO 8601、相对时间提示） |
| `FormatLocalTimeLabel` | `string FormatLocalTimeLabel(string timeZoneId)` | 生成时区展示标签（如 `China Standard Time (UTC+08:00)`） |
| `TryParseUnixToken` | `bool TryParseUnixToken(string text, out DateTimeOffset dto)` | 尝试解析单个时间戳文本（≥12 位按毫秒，否则按秒） |
| `TimeZoneOptions` | `static IReadOnlyList<TimestampTimeZoneOption> TimeZoneOptions` | 时区选项列表（常用时区置顶，其余按 UTC 偏移排序） |

### `TimestampTimeZoneOption`  `sealed record`

时区下拉选项：`Id`、`Label`。

### `TimestampDisplayResult`  `sealed record`

时间戳展示结果：`UnixSeconds`、`UnixMilliseconds`、`LocalTime`、`UtcTime`、`Iso8601Local`、`Iso8601Utc`、`RelativeHint`。

### `UnixFieldPrefer`  `enum`

时间戳解析优先字段：`Seconds`（优先秒）、`Milliseconds`（优先毫秒）。

---

## WindowsHelps

### `KeyboardInputHelp`  `static class`

Windows 键盘输入模拟：基于 Win32 `SendInput` 逐字符键入文本（Unicode 直发）。仅支持 Windows。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `TypeTextAsync` | `static Task TypeTextAsync(string text, int startDelayMs, int charIntervalMs, CancellationToken ct)` | 延时后按间隔逐字符键入；`\n` → 回车、`\t` → Tab、`\r` 忽略 |
| `TypeBatchAsync` | `static Task TypeBatchAsync(IReadOnlyList<string> items, int startDelayMs, int charIntervalMs, int itemIntervalMs, bool pressEnterAfterEach, CancellationToken ct)` | 依次键入多段文本；首项前有起始延时，项间停顿，可选每项后补回车 |
