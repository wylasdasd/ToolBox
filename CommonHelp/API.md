# CommonHelp API 索引

> 命名空间：`CommonHelp`（file-scoped）
> 约定：公开类/方法均使用 XML doc 注释；Service 层返回 `Result<T>`（Ok/Fail）。
> 注：部分 API 为跨项目通用预留，当前解决方案内未必有直接调用。

---

## 目录

- [ResultFormat（请求响应结果格式）](#common)
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

## ResultFormat

### Result 类型（统一返回结果）

库中以泛型 record 的形式提供了三种常用的结果封装：`Result<T>`、`HttpResult<T>` 与 `BLLResult<T>`。推荐优先使用 `Result<T>` 或 `HttpResult<T>`，`BLLResult<T>` 仅为兼容旧业务场景保留。

#### `Result<T>`  `sealed record`

通用的工具操作返回类型：成功时通过 `IsSuccess` 判断并在 `Value` 中携带结果，失败时在 `Error` 中携带用户可读的错误说明。

| 成员 | 说明 |
|:---|:---|
| `IsSuccess` (bool) | 操作是否成功 |
| `Value` (T?) | 成功时的返回值 |
| `Error` (string?) | 失败时的错误说明 |
| `static Ok(T value)` | 创建成功结果 |
| `static Fail(string error)` | 创建失败结果 |

#### `HttpResult<T>`  `sealed record`

用于表示带有 HTTP 状态码语义的返回：包含 `Code` 字段并以 200–299 范围判断为成功。

| 成员 | 说明 |
|:---|:---|
| `Code` (int) | HTTP 状态码 |
| `IsSuccess` (bool) | 根据 `Code`（200–299 为成功）计算得出 |
| `Value` (T?) | 成功时的返回值 |
| `Error` (string?) | 失败时的错误说明 |
| `static Ok(T value, int code = 200)` | 创建成功结果，默认 code = 200 |
| `static Fail(string error, int code = 500)` | 创建失败结果，默认 code = 500 |

#### `BLLResult<T>`  `sealed record`

为兼容部分业务代码保留的结果类型（不推荐新代码使用）。语义上假设 `Code == 0` 为成功，非 0 表示失败。

| 成员 | 说明 |
|:---|:---|
| `Code` (int) | 业务码（约定 0 为成功） |
| `IsSuccess` (bool) | `Code == 0` 表示成功 |
| `Value` (T?) | 成功时的返回值 |
| `Error` (string?) | 失败时的错误说明 |
| `static Ok(T value, int code = 0)` | 创建成功结果，默认 code = 0 |
| `static Fail(string error, int code = -1)` | 创建失败结果，默认 code = -1 |

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
| `GetDescription` | `string GetDescription(this Enum value)` | 获取枚举值的 `[Description]` 标注文本（结果缓存；无描述时返回枚举名） |
| `ToDictionary` | `Dictionary<int, string> ToDictionary<TEnum>()` | 将枚举所有值（int）与描述转为字典 |
| `GetList` | `List<EnumItemDetail> GetList<TEnum>()` | 返回枚举所有值的 `EnumItemDetail` 列表 |
| `GetEnumByDescription` | `TEnum? GetEnumByDescription<TEnum>(string description)` | 按描述文本（或字段名）反向查找枚举值 |

### `EnumItemDetail`  `class`

枚举项详情：`Value`（int 值）、`Name`（枚举名）、`Description`（描述）。

### `RandomHelp`  `class`

随机抽取与 UUID 批量生成（静态方法）。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `GetRandomList<T>` | `List<T> GetRandomList<T>(IEnumerable<T> list, int count)` | 从序列随机抽取 count 个元素（拷贝后洗牌截断，count 自动钳制到 [0, n]） |
| `GenerateUuids` | `Result<UuidGenerateResult> GenerateUuids(int count, bool includeHyphens, bool upperCase)` | 批量生成 UUID/GUID 字符串 |

### `UuidGenerateResult`  `sealed record`

UUID 生成结果：`Output`（每行一个的 UUID 文本）。

---

## CalculationHelps

### `BitwiseHelp`  `static class`

位运算计算工具：支持 AND、OR、XOR、NOT 及左右移位。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Compute` | `Result<BitwiseComputeResult> Compute(string operation, long operandA, long operandB, int operandC, int bitWidth, bool isSigned, long currentResult, bool shiftFromResult)` | 执行位运算（AND/OR/XOR/NOT/SHL/SHR_A/SHR_L）并按位宽归一化 |
| `ApplyShift` | `long ApplyShift(string operation, long operandA, long currentResult, int operandC, int bitWidth, bool isSigned, bool fromResult)` | 移位运算：被移位数可选操作数 A 或当前结果，移位量钳制在 0–bitWidth |
| `IsShiftOp` | `bool IsShiftOp(string op)` | 判断是否为移位操作 |
| `GetMask` | `long GetMask(int bits)` | 生成指定位宽的掩码 |
| `SignExtend` | `long SignExtend(long value, int originalBits)` | 将无符号值按原始位宽做符号扩展 |
| `NormalizeResult` | `long NormalizeResult(long res, int bitWidth, bool isSigned, bool signExtendWhenSigned = true)` | 将结果裁剪到指定位宽（有符号时可选符号扩展） |
| `FormatValue` | `string FormatValue(long value, int bitWidth, bool isSigned)` | 格式化值为十进制/十六进制/二进制展示 |
| `FormatBinary` | `string FormatBinary(long value, int bitWidth)` | 格式化为二进制（每 8 位空格分隔） |
| `FormatHex` | `string FormatHex(long value, int bitWidth)` | 格式化为十六进制 |

### `BitwiseComputeResult`  `record`

位运算结果：`Result`（十进制）、`Hex`（十六进制）、`Binary`（二进制）、`Operation`（操作说明）。

### `RadixHelp`  `static class`

进制转换工具：支持 2–128 进制互转。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ConvertFrom` | `Result<RadixConvertResult> ConvertFrom(string? value, int fromBase)` | 将 `fromBase` 进制输入批量换算为所有常用进制（2/8/10/16/32/64/128） |
| `Parse` | `BigInteger Parse(string value, int radix)` | 将指定进制的字符串解析为 BigInteger（2/8/10/16 走内置快路径；暂不支持负数） |
| `Format` | `string Format(BigInteger value, int radix, bool upperCaseForHex = true)` | 将 BigInteger 格式化为指定进制字符串 |
| `GetDigitAlphabet` | `string GetDigitAlphabet(int radix)` | 获取指定进制使用的字符集（如 16 进制返回 `0-9A-F`） |
| `DigitAlphabetHint` | `string DigitAlphabetHint { get; }` | 进制字符表的人类可读提示（属性） |
| `ProgrammerBases` | `static IReadOnlyList<int>` | 常用进制列表（2/8/10/16/32/64/128） |
| `ClearRadixValues` | `RadixConvertResult ClearRadixValues()` | 清除/重置输入值（UI 辅助） |

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
| `Encode` | `Result<string> Encode(string? plainText, bool useUrlSafe)` | 将 UTF-8 文本编码为 Base64（可选 URL-safe 字母表） |
| `Decode` | `Result<string> Decode(string? base64Text, bool useUrlSafe)` | 将 Base64 解码为 UTF-8 文本（URL-safe 时先还原字母表与填充） |

### `AesGcmCryptoHelp`  `static class`

AES-GCM 字符串加解密：密钥由上下文（如站点 Origin）+ 前缀派生，密文带版本前缀，适合浏览器存储等半可信环境。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Encrypt` | `string Encrypt(string plaintext, string context, string keyMaterialPrefix = "")` | 加密为 `v1:Base64(nonce‖cipher‖tag)` |
| `TryDecrypt` | `bool TryDecrypt(string stored, string context, out string? plaintext, string keyMaterialPrefix = "")` | 尝试解密；前缀不符/载荷损坏/上下文不匹配返回 false |
| `EncryptedPrefix` | `const string` | 密文版本前缀 `v1:` |

### `EncodingHelp`  `static class`

编码辅助工具：常见编码获取与十六进制字节解析/格式化。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `TextHexEncodings` | `static IReadOnlyList<EncodingOption> TextHexEncodings` | 预定义编码列表（UTF-8、UTF-16、ASCII、Latin-1、GB2312/GBK/GB18030、Big5） |
| `GetEncoding` | `Encoding GetEncoding(string id)` | 按编码 Id（如 "gbk"）获取 Encoding；未知 Id 回退 UTF-8（静态构造注册 CodePages 提供程序） |
| `ParseHexBytes` | `byte[] ParseHexBytes(string? hex, int expectedLength)` | 解析 HEX 字符串为字节数组（可指定长度） |
| `FormatHexBytes` | `string FormatHexBytes(byte[] bytes)` | 字节数组格式化为空格分隔的 HEX 字符串 |

### `EncodingHelp.EncodingOption`  `sealed record`（嵌套）

编码选项：`Id`（编码 Id）、`Label`（展示名）、`CodePageName`（传给 `Encoding.GetEncoding` 的名称）。

### `TextHexHelp`  `static class`

文本与十六进制互转工具。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `EncodeTextToHex` | `Result<TextHexEncodeResult> EncodeTextToHex(string? textInput, string encodingId, string displayModeId, bool uppercaseHex, bool showUnicodeEscape, bool showCEscape)` | 文本按指定编码转 HEX，输出空格分隔 Hex 与附加展示块（连续 Hex、\xNN、\uXXXX） |
| `DecodeHexToText` | `Result<TextHexDecodeResult> DecodeHexToText(string? hexInput, string encodingId)` | HEX（容忍 0x/\x 前缀与分隔符）按指定编码解码为文本 |

### `TextHexEncodeResult`  `sealed record`

编码结果：`HexInput`（空格分隔 Hex）、`Output`（按展示模式组装的完整输出）。

### `TextHexDecodeResult`  `sealed record`

解码结果：`TextInput` 与 `Output`（解码文本，两字段一致便于双向绑定）。

### `MimeHelp`  `static class`

MIME 类型辅助工具：扩展名推断、Base64 图片魔数检测、Base64 校验与 Data URI 解析/构建。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `GuessContentType` | `string GuessContentType(string path)` | 按扩展名推断常见 MIME（png/jpeg/webp/gif/bmp/svg/json/csv/md 等）；未知返回 text/plain |
| `GuessImageMime` | `string GuessImageMime(string fileName, string contentType)` | contentType 以 image/ 开头时原样返回，否则按扩展名推断（缺省 image/jpeg） |
| `DetectImageMimeType` | `string DetectImageMimeType(string base64)` | 按 Base64 前缀魔数检测图片类型（png/jpeg/gif/svg/bmp/webp）；无法识别返回空字符串 |
| `IsValidBase64` | `bool IsValidBase64(string value)` | 校验字符串是否为合法 Base64 |
| `TryParseDataUrl` | `bool TryParseDataUrl(string input, out string dataUrl, out string base64Part, out string mimeType)` | 拆解 `data:<mime>;base64,<payload>`（RFC 2397）；非该形式返回 false |
| `BuildDataUrl` | `string BuildDataUrl(string mimeType, byte[] bytes)` | 构建 `data:<mime>;base64,<payload>` 形式的 Data URI |

### `JwtParseHelp`  `static class`

JWT（JSON Web Token）解析工具：解码 Header/Payload 并提取过期时间。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Parse` | `Result<JwtParseResult> Parse(string? token)` | 解析 JWT，返回 Header/Payload JSON 与 exp 过期时间 |

### `JwtParseResult`  `sealed record`

JWT 解析结果：`HeaderJson`、`PayloadJson`（缩进 JSON）、`ExpiryUtc`、`ExpiryLocal`（过期时间文本，无 exp 时为空字符串）。

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
| `ReadAllBytesAsync` | `Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken ct)` | 将任意流完整读入内存并返回字节数组（可寻址流先重置到起始位置）；适用于上传文件/网络流 |
| `FormatFileSize` | `string FormatFileSize(long bytes)` | 字节数格式化为人类可读大小（如 `1.5 MB`），按 1024 进位，最多两位小数 |
| `CreateFile` | `bool CreateFile(string path)` | 文件不存在时创建（含目录）；返回是否实际创建 |
| `WriteAllText` | `void WriteAllText(string path, string content, Encoding? encoding = null)` | 同步写入全部文本（自动建目录，默认 UTF8，null 内容按空串） |
| `WriteAllTextAsync` | `Task WriteAllTextAsync(string filePath, string contents, CancellationToken ct)` | 异步写入全部文本 |
| `ReadAllText` | `string ReadAllText(string path, Encoding? encoding = null)` | 同步读取全部文本（文件缺失返回空字符串） |
| `ReadAllTextAsync` | `Task<string> ReadAllTextAsync(string filePath, CancellationToken ct)` | 异步读取全部文本 |
| `DeleteIfExists` | `bool DeleteIfExists(string filePath)` | 文件存在时删除；返回是否实际删除 |
| `CopyFile` | `void CopyFile(string source, string target, bool overwrite)` | 复制文件 |
| `GetFilesRecursively` | `IEnumerable<string> GetFilesRecursively(string directory, string? searchPattern = null)` | 惰性递归枚举目录下所有文件；目录不存在返回空 |
| `GetDirectorySize` | `long GetDirectorySize(string directory, string? searchPattern = null)` | 计算目录总大小（字节，忽略不可访问文件） |
| `SafeFileName` | `string SafeFileName(string name)` | 将文件名中的非法字符替换为下划线 |
| `CreateFileStream` | `FileStream CreateFileStream(string path, int bufferSize = 81920, bool overwrite = true)` | 创建异步写入文件流（Asynchronous | SequentialScan） |
| `WriteBytesAsync` | `Task WriteBytesAsync(string filePath, byte[] data, CancellationToken ct)` | 异步写入字节数组 |
| `WriteStreamAsync` | `Task WriteStreamAsync(string path, Stream source, bool overwrite = true, CancellationToken cancellationToken = default)` | 将源流从当前位置异步复制写入文件（不关闭源流） |
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

`System.Text.Json` 序列化/反序列化扩展方法。序列化方法（`JsonSeToLow`/`JsonSeNoLow`/`JsonSe`）不限制泛型约束，支持值类型（如 `JsonElement`/`JsonNode`）；内部缓存 `JsonSerializerOptions` 实例，避免每次调用重建序列化元数据。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `JsonSeToLow<T>` | `string JsonSeToLow<T>(this T obj)` | 序列化为缩进 JSON，属性名全小写，中文不转义 |
| `JsonSeNoLow<T>` | `string JsonSeNoLow<T>(this T obj)` | 序列化为缩进 JSON，保留原属性名，中文不转义 |
| `JsonDe<T>` | `T? JsonDe<T>(this string json)` | 反序列化：忽略大小写、容忍尾逗号 |
| `JsonSe<T>` | `string JsonSe<T>(this T obj, JsonSerializerOptions options)` | 使用自定义 Options 序列化 |
| `JsonDe<T>` | `T? JsonDe<T>(this string json, JsonSerializerOptions options)` | 使用自定义 Options 反序列化 |

#### `LowerCaseNamingPolicy`  `class : JsonNamingPolicy`

属性名全小写命名策略（`ConvertName` → `name.ToLowerInvariant()`）。

### `MarkdownJsonHelp`  `static class`

Markdown 围栏 JSON 提取工具：剥离代码围栏并规范化 JSON 文本（LLM 输出清洗常用）。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `StripCodeFence` | `string StripCodeFence(string text)` | 剥离 ```/```json 围栏：命中时返回围栏内文本（已 Trim），否则返回原文本 Trim |
| `NormalizeToJson` | `string NormalizeToJson(string raw)` | 剥围栏后解析并重新序列化为缩进、非 ASCII 不转义的展示格式；输入为空或解析失败时抛出异常 |

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

### `HttpErrorHelp`  `static class`

HTTP 错误响应消息提取工具：从 JSON 错误体提取用户可读消息。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `FormatFailure` | `string FormatFailure(string prefix, HttpStatusCode code, string responseText)` | 拼接用户可读失败消息：能提取到消息时为「prefix：HTTP 500，detail」，否则「prefix：HTTP 500。」 |
| `TryExtractMessage` | `string? TryExtractMessage(string responseText)` | 依次识别 `error`（字符串）、`error.message`（嵌套对象）、`message`（字符串）；解析失败或无匹配返回 null |
| `FormatExceptionChain` | `string FormatExceptionChain(Exception ex, string separator = " → ")` | 聚合异常链消息（跳过空消息与相邻重复），全部为空时返回异常类型名 |

### `HttpTransportHelp`  `class`

HTTP JSON 传输工具：共享连接池的 `HttpClient` 工厂、JSON POST 构建、瞬时失败自动重试与响应体读取；适用于无 `IHttpClientFactory` 场景的高频 API 调用（如 LLM/OpenAI 风格接口）。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `HttpTransportHelp` | `HttpTransportHelp(HttpClient httpClient)` | 注入 `HttpClient` 创建传输服务；也可手动创建 |
| `CreateJsonContent<T>` | `ByteArrayContent CreateJsonContent<T>(T payload)` | 序列化为 UTF-8 JSON 内容（`application/json; charset=utf-8`） |
| `CreateJsonPost` | `HttpRequestMessage CreateJsonPost(Uri uri, byte[] jsonBody)` | 构建 JSON POST 请求（HTTP/1.1 优先，允许降级） |
| `SendJsonPostWithRetryAsync` | `virtual Task<HttpResponseMessage> SendJsonPostWithRetryAsync(Uri, byte[], Action<HttpRequestMessage>?, CancellationToken)` | 发送 JSON POST，瞬时网络失败自动重试；可在派生类中重写 |
| `ReadBodyAsync` | `virtual Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken ct)` | 流式读取响应体为文本；可在派生类中重写 |
| `SendAsync` | `virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)` | 发送原始 HTTP 请求；可在派生类中重写 |

### `EndianHexHelp`  `static class`

数值与十六进制字节互转：支持 int8–uint64、float32/float64 十种类型与大端/小端字节序。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ConvertValueToHex` | `Result<EndianHexResult> ConvertValueToHex(string? input, string? dataTypeId, bool bigEndian, bool inputIsHex)` | 数值转 HEX 字节；同时输出二进制与十进制 |
| `ConvertHexToValue` | `Result<EndianHexResult> ConvertHexToValue(string? input, string? dataTypeId, bool bigEndian)` | HEX 字节转十进制数值 |
| `DataTypes` | `static IReadOnlyList<EndianHexDataType> DataTypes` | 全部支持的数据类型（int8/uint8/int16/…/float64） |

### `EndianHexDataType`  `sealed record`

数据类型定义：`Id`、`Label`、`ByteSize`、`IsFloat`。

### `EndianHexResult`  `sealed record`

端序转换结果：`HexOutput`（空格分隔）、`BinaryOutput`（每字节 8 位）、`DecimalOutput`。

### `IpCalculatorHelp`  `static class`

IPv4 子网计算器：由 IP + CIDR/掩码计算网络地址、广播地址、主机范围等。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Calculate` | `Result<IpCalculatorResult> Calculate(string? input)` | 计算子网信息；支持 `192.168.1.10/24`、`192.168.1.10 255.255.255.0`、纯 IP（/32）三种输入 |

### `IpCalculatorResult`  `sealed record`

子网计算结果：`Address`、`IntegerValue`、`HexValue`、`SubnetMask`、`WildcardMask`、`NetworkAddress`、`BroadcastAddress`、`FirstHost`、`LastHost`、`HostCount`、`Cidr`。

### `RequestHelp`  `class : IHttpService`

使用调用方提供的 `HttpClient` 的快捷请求工具；实现 `IHttpService`，不依赖 `IHttpClientFactory` 和日志服务。
请求体参数使用已准备好的 JSON 字符串或表单字典，泛型参数 `T` 表示响应类型而不是请求体类型。
所有发送方法为 `virtual`，支持继承重写，也可手动创建。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `RequestHelp` | `RequestHelp(HttpClient httpClient)` | 注入 `HttpClient` 创建请求服务 |
| `SendRequestAsync` | `virtual Task<int> SendRequestAsync(HttpMethod method, string url, string body, Action<HttpRequestHeaders>? action)` | 使用 JSON 请求体发送指定 HTTP 方法并返回状态码；可在派生类中重写 |
| `PostRequestAsync<T>` | `virtual Task<T?> PostRequestAsync<T>(string url, string body, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class` | 发送 JSON POST 请求并将响应反序列化为 `T`；`T` 为 `string` 时直接返回响应文本 |
| `PutRequestAsync<T>` | `virtual Task<T?> PutRequestAsync<T>(string url, string body, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class` | 发送 JSON PUT 请求并将响应反序列化为 `T` |
| `DeleteRequestAsync<T>` | `virtual Task<T?> DeleteRequestAsync<T>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class` | 发送无请求体的 DELETE 请求并将响应反序列化为 `T` |
| `GetRequestAsync<T>` | `virtual Task<T?> GetRequestAsync<T>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class` | 发送无请求体的 GET 请求并将响应反序列化为 `T` |
| `PostFormRequestAsync<T>` | `virtual Task<T?> PostFormRequestAsync<T>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action, int timeoutSeconds) where T : class` | 发送 `application/x-www-form-urlencoded` 表单并将响应反序列化为 `T` |
| `SendAsync` | `virtual Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? body, string? mediaType, int timeoutSeconds, Action<HttpRequestHeaders>? action, CancellationToken cancellationToken)` | 发送原始 HTTP 请求并返回 `HttpResponseMessage`；异常直接抛出，响应由调用方读取并释放 |

### `TypedHttpClientFactoryService`  `class : ITypedHttpService`

强类型 JSON 请求工具。请求方法接收 `TRequest` 对象并在内部序列化，不需要调用方手动准备 `body` 字符串；响应使用 `TResponse` 反序列化。
通过 `IHttpClientFactory` 创建客户端，通过 `ILogger<ITypedHttpService>` 记录超时和异常。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `TypedHttpClientFactoryService` | `TypedHttpClientFactoryService(IHttpClientFactory httpClientFactory, ILogger<ITypedHttpService> logger)` | 注入 HTTP 客户端工厂和日志服务 |
| `SendRequestAsync<TRequest>` | `virtual Task<int> SendRequestAsync<TRequest>(HttpMethod method, string url, TRequest requestBody, Action<HttpRequestHeaders>? action)` | 使用强类型请求对象发送 JSON 请求，只返回 HTTP 状态码 |
| `PostRequestAsync<TRequest, TResponse>` | `virtual Task<TResponse?> PostRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 使用强类型请求对象发送 JSON POST，并将响应反序列化为 `TResponse` |
| `PutRequestAsync<TRequest, TResponse>` | `virtual Task<TResponse?> PutRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 使用强类型请求对象发送 JSON PUT，并将响应反序列化为 `TResponse` |
| `DeleteRequestAsync<TResponse>` | `virtual Task<TResponse?> DeleteRequestAsync<TResponse>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 发送 DELETE 请求并将响应反序列化为 `TResponse` |
| `GetRequestAsync<TResponse>` | `virtual Task<TResponse?> GetRequestAsync<TResponse>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 发送 GET 请求并将响应反序列化为 `TResponse` |
| `PostFormRequestAsync<TResponse>` | `virtual Task<TResponse?> PostFormRequestAsync<TResponse>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action, int timeoutSeconds) where TResponse : class` | 发送 urlencoded 表单并将响应反序列化为 `TResponse` |
| `SendAsync<TRequest>` | `virtual Task<HttpResponseMessage> SendAsync<TRequest>(HttpMethod method, string url, TRequest? requestBody, string? mediaType, int timeoutSeconds, Action<HttpRequestHeaders>? action, CancellationToken cancellationToken)` | 使用强类型请求对象发送原始 HTTP 请求 |

### `TypedRequestHelp`  `class : ITypedHttpService`

强类型 JSON 请求工具。请求方法接收 `TRequest` 对象并在内部序列化，不需要调用方手动准备 `body` 字符串；响应使用 `TResponse` 反序列化。
使用构造函数注入的 `HttpClient`，适用于无需 `IHttpClientFactory` 和日志服务的简单场景。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `TypedRequestHelp` | `TypedRequestHelp(HttpClient httpClient)` | 注入 `HttpClient` 创建请求服务 |
| `SendRequestAsync<TRequest>` | `virtual Task<int> SendRequestAsync<TRequest>(HttpMethod method, string url, TRequest requestBody, Action<HttpRequestHeaders>? action)` | 使用强类型请求对象发送 JSON 请求，只返回 HTTP 状态码 |
| `PostRequestAsync<TRequest, TResponse>` | `virtual Task<TResponse?> PostRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 使用强类型请求对象发送 JSON POST，并将响应反序列化为 `TResponse` |
| `PutRequestAsync<TRequest, TResponse>` | `virtual Task<TResponse?> PutRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 使用强类型请求对象发送 JSON PUT，并将响应反序列化为 `TResponse` |
| `DeleteRequestAsync<TResponse>` | `virtual Task<TResponse?> DeleteRequestAsync<TResponse>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 发送 DELETE 请求并将响应反序列化为 `TResponse` |
| `GetRequestAsync<TResponse>` | `virtual Task<TResponse?> GetRequestAsync<TResponse>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 发送 GET 请求并将响应反序列化为 `TResponse` |
| `PostFormRequestAsync<TResponse>` | `virtual Task<TResponse?> PostFormRequestAsync<TResponse>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action, int timeoutSeconds) where TResponse : class` | 发送 urlencoded 表单并将响应反序列化为 `TResponse` |
| `SendAsync<TRequest>` | `virtual Task<HttpResponseMessage> SendAsync<TRequest>(HttpMethod method, string url, TRequest? requestBody, string? mediaType, int timeoutSeconds, Action<HttpRequestHeaders>? action, CancellationToken cancellationToken)` | 使用强类型请求对象发送原始 HTTP 请求 |

### `ITypedHttpService`  `interface`

强类型 JSON 请求服务接口。`TRequest` 表示请求对象类型，`TResponse` 表示响应类型；实现类可使用 `IHttpClientFactory` 和日志服务，也可直接使用调用方提供的 `HttpClient`。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `SendRequestAsync<TRequest>` | `Task<int> SendRequestAsync<TRequest>(HttpMethod method, string url, TRequest requestBody, Action<HttpRequestHeaders>? action)` | 使用强类型请求对象发送请求，只返回 HTTP 状态码 |
| `PostRequestAsync<TRequest, TResponse>` | `Task<TResponse?> PostRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 使用强类型请求对象发送 JSON POST |
| `PutRequestAsync<TRequest, TResponse>` | `Task<TResponse?> PutRequestAsync<TRequest, TResponse>(string url, TRequest requestBody, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 使用强类型请求对象发送 JSON PUT |
| `DeleteRequestAsync<TResponse>` | `Task<TResponse?> DeleteRequestAsync<TResponse>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 发送 DELETE 请求并反序列化响应 |
| `GetRequestAsync<TResponse>` | `Task<TResponse?> GetRequestAsync<TResponse>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where TResponse : class` | 发送 GET 请求并反序列化响应 |
| `PostFormRequestAsync<TResponse>` | `Task<TResponse?> PostFormRequestAsync<TResponse>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action, int timeoutSeconds) where TResponse : class` | 发送 urlencoded 表单并反序列化响应 |
| `SendAsync<TRequest>` | `Task<HttpResponseMessage> SendAsync<TRequest>(HttpMethod method, string url, TRequest? requestBody, string? mediaType, int timeoutSeconds, Action<HttpRequestHeaders>? action, CancellationToken cancellationToken)` | 使用强类型请求对象发送原始请求 |

### `IHttpService`  `interface`

HTTP 请求服务接口。`T` 表示响应类型；请求体由调用方以 JSON 字符串或表单字典提供。
`HttpClientFactoryService` 使用 `IHttpClientFactory`，`RequestHelp` 使用构造函数注入的 `HttpClient`。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `PostRequestAsync<T>` | `Task<T?> PostRequestAsync<T>(string url, string body, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class` | POST JSON 请求并将响应反序列化为 `T` |
| `PutRequestAsync<T>` | `Task<T?> PutRequestAsync<T>(string url, string body, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class` | PUT JSON 请求并将响应反序列化为 `T` |
| `DeleteRequestAsync<T>` | `Task<T?> DeleteRequestAsync<T>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class` | DELETE 请求并将响应反序列化为 `T`；请求不包含请求体 |
| `GetRequestAsync<T>` | `Task<T?> GetRequestAsync<T>(string url, int timeoutSeconds, Action<HttpRequestHeaders>? action) where T : class` | GET 请求并将响应反序列化为 `T`；请求不包含请求体 |
| `PostFormRequestAsync<T>` | `Task<T?> PostFormRequestAsync<T>(string url, Dictionary<string, object> formData, Action<HttpRequestHeaders>? action, int timeoutSeconds) where T : class` | POST `application/x-www-form-urlencoded` 表单并将响应反序列化为 `T` |
| `SendAsync` | `Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? body, string? mediaType, int timeoutSeconds, Action<HttpRequestHeaders>? action, CancellationToken cancellationToken)` | 发送原始请求返回 `HttpResponseMessage`：不吞异常、不反序列化，超时/网络错误直接抛出，响应由调用方释放 |

### `HttpClientFactoryService`  `class : IHttpService`

`IHttpService` 的默认实现：封装 POST/PUT/DELETE/GET/表单 POST，超时或异常时返回 default 并记录日志。`T` 为 `string` 时直接返回响应文本。需要原始响应（错误体、状态码、流式读取）时用 `SendAsync`，异常直接抛出、不记录不吞。

---

## StringHelps

### `StringHelp`  `class`

文本统计工具：行数、字符数/字节数统计。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `CountLines` | `static int CountLines(string text, string? lineSeparator = null)` | 计算文本行数；默认识别 \r\n、\r、\n 逻辑行，可指定自定义分隔符 |
| `CountCharacters` | `static long CountCharacters(string text, Encoding? encoding)` | 计算字符数或字节数（encoding 为 null 返回字符数，否则返回字节数） |
| `CountFileLinesAndChars` | `static (long Lines, long Count) CountFileLinesAndChars(string filePath, Encoding? encoding)` | 读取文件并计算行数与字符/字节数 |
| `CountFileCharacters` | `static long CountFileCharacters(string filePath, Encoding? encoding)` | 计算文件的字符数或字节数 |

### `NamingStyleHelp`  `static class`

标识符命名风格转换：将任意形式标识符拆词后一次性转换为 8 种常见风格。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `Convert` | `Result<NamingStyleResult> Convert(string? input)` | 转换为 camelCase/PascalCase/snake_case/kebab-case/SCREAMING_SNAKE/dot.case/Title Case/lowercase |

### `NamingStyleResult`  `sealed record`

命名风格转换结果：`CamelCase`、`PascalCase`、`SnakeCase`、`KebabCase`、`ScreamingSnake`、`DotCase`、`TitleCase`、`LowerCase`。空输入时所有字段为空字符串（`Empty` 静态属性）。

### `TextLinesHelp`  `static class`

多行文本处理工具：去重、排序、去空行、裁剪空白与行级统计。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ComputeStats` | `static TextLineStats ComputeStats(string? text, bool applyTrim, bool removeEmpty, bool ignoreCase)` | 统计行数、非空行数、去重行数、单词数、字符数 |
| `Deduplicate` | `static Result<TextLinesOperationResult> Deduplicate(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase, bool keepOrder)` | 多行文本去重（可选保留首次出现顺序） |
| `SortAsc` | `static Result<TextLinesOperationResult> SortAsc(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase)` | 按字典序升序排序 |
| `SortDesc` | `static Result<TextLinesOperationResult> SortDesc(string? text, bool trimLines, bool ignoreEmptyLines, bool ignoreCase)` | 按字典序降序排序 |
| `RemoveEmptyLines` | `static Result<TextLinesOperationResult> RemoveEmptyLines(string? text, bool trimLines)` | 移除所有空白行 |
| `TrimOnly` | `static Result<TextLinesOperationResult> TrimOnly(string? text)` | 裁剪每行首尾空白（保留空行） |

### `TextLineStats`  `sealed record`

文本统计信息：`LineCount`、`NonEmptyLineCount`、`UniqueLineCount`、`WordCount`、`CharCount`、`CharNoWhitespaceCount`。

### `TextLinesOperationResult`  `sealed record`

多行文本操作结果：`Output`（处理后的文本）、`StatusMessage`（状态提示）。

### `TextNormalizeHelp`  `static class`

文本符号归一化工具：支持全角/半角字符与中英文标点之间的转换。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `NormalizeSymbolsToAscii` | `static string NormalizeSymbolsToAscii(string? text, bool removeSpaces)` | 归一化 OCR 文本：先 FormKC 归一化，再映射中文标点为英文标点；可选去除空格 |
| `NormalizeSymbolsToFullWidth` | `static string NormalizeSymbolsToFullWidth(string? text, bool convertSpaces)` | ASCII 可打印字符转全角，并将常见英文标点映射为中文标点；可选将空格转为全角空格 |

---

## TimeHelps

### `TimestampConvertHelp`  `static class`

Unix 时间戳与日期时间互转工具：支持智能识别秒/毫秒、任意时区显示、ISO 8601 解析与相对时间提示。

| 方法 | 签名 | 说明 |
|:---|:---|:---|
| `ParseUnix` | `Result<DateTimeOffset> ParseUnix(string? unixSeconds, string? unixMilliseconds, UnixFieldPrefer preferField)` | 解析 Unix 秒/毫秒时间戳（≥12 位按毫秒处理）；按 preferField 优先取值 |
| `ParseUnix` | `Result<DateTimeOffset> ParseUnix(string? unixSeconds, string? unixMilliseconds)` | 简化重载（优先秒字段） |
| `ParseFromTime` | `Result<DateTimeOffset> ParseFromTime(string? localTime, string? utcTime, string timeZoneId)` | 由时区时间/UTC 时间（支持 ISO 8601）解析 DateTimeOffset |
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
