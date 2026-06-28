# ToolBox：Agent 约定

## 仓库结构

- `ToolBox/`：.NET MAUI Blazor 应用（平台专属服务与 MAUI 壳）
- `ToolBox.Shared/`：共享 Razor 组件、ViewModel、服务接口、`AddToolBoxCore()`
- `ToolBox.Web/`：Blazor Web App 宿主（SSR 壳、`Program.cs`）
- `ToolBox.Web.Client/`：WASM 客户端、Web 路由/布局、浏览器平台服务
- `CommonHelp/`：共享类库
- `TestCommonHelp/`：CommonHelp 单元测试

## 构建

- Windows MAUI 快速构建：`dotnet build ToolBox/ToolBox.csproj -c Debug -f net10.0-windows10.0.19041.0`
- Web 本地运行：`dotnet run --project ToolBox.Web/ToolBox.Web.csproj`
- Web 发布：`dotnet publish ToolBox.Web/ToolBox.Web.csproj -c Release -o ./publish/web`
- 构建整个解决方案：`dotnet build ToolBox.slnx -c Debug`

## Web 架构要点

- **SSR 壳**：`ToolBox.Web/Components/App.razor`（HTML、CSS/JS、`HeadOutlet`，无 `@rendermode`）
- **WASM 交互**：`<ToolRouter @rendermode="InteractiveWebAssembly" />` 及其子树（含 Shared 工具页）
- Shared **不是 WASM 专属**；MAUI 通过 WebView 引用 Shared，Web 通过 Client 引用 Shared
- **禁止**在 Shared 页面使用 `@rendermode`（会破坏 MAUI Hybrid）
- Web 交互组件（`ToolRouter`、`WebRouteView`、`WebMainLayout`、`WebNavMenu`）必须在 `ToolBox.Web.Client`
- Web 开放路由由 `ToolBox.Web.Client/WebRouteRegistry.cs` 白名单控制；新增 Web 工具需同步更新

## 约定

- C# 命名空间与 `RootNamespace`（`ToolBox`）及目录结构保持一致
- Razor 组件命名空间与 `ToolBox.Shared/Components/**` 目录保持一致（例如 `ToolBox.Components.Pages.TextTool`）
- 优先复用现有 `CommonHelp` 代码；避免随手新增零散工具函数；确需新增请放在 `CommonHelp/` 下
- 平台差异通过 `ToolBox.Shared/Services/` 接口抽象；MAUI 在 `ToolBox/Services/` 实装，Web 在 `ToolBox.Web.Client/Services/` 实装或 stub
- 避免无关格式化；尽量保持现有编码/换行风格

## 代码风格

以下约定来自本仓库现有代码的惯用写法，新增或修改代码时应优先对齐。

### 分层与职责

| 位置 | 放什么 | 不放什么 |
| :--- | :--- | :--- |
| `ToolBox.Tools/` | 可单测的纯逻辑：`static *Service`、`*Result` 模型、`ToolResult<T>` 返回值 | UI 状态、MudBlazor、平台 API |
| `ToolBox.Shared/Services/` | 跨平台接口 `I*`、共享 DTO/模型、与宿主无关的服务实现（如 AI 路由） | MAUI / 浏览器专属 API |
| `ToolBox.Shared/Components/` | `.razor` 视图 + 同目录 `*VM.cs`（**仅交互**） | 算法、解析、转换（应放 `ToolBox.Tools`） |
| `ToolBox/Services/` | MAUI / Windows 平台实装 | Web 逻辑 |
| `ToolBox.Web.Client/Services/` | WASM 实装；不支持的能力用 `Unsupported*` stub | MAUI 逻辑 |
| `CommonHelp/` | 历史通用工具；命名空间多为 `CommonTool.*` | 新功能优先放 `ToolBox.Tools`，除非与现有 Help 强相关 |

### VM 与 Tools 的分工

**VM 只处理交互**——一切与控件绑定、用户操作流程、界面反馈相关的状态与编排。  
**交互之外的可复用逻辑**——解析、转换、校验、计算、算法——一律放入 `ToolBox.Tools/`（或已有 `CommonHelp/`），VM 只负责调用并映射结果。

| 放在 VM | 放在 `ToolBox.Tools` | 放在 `I*` 平台服务 |
| :--- | :--- | :--- |
| 输入框/开关/下拉的可绑定值 | JSON 格式化、JWT 解析、正则匹配 | 选文件夹、目录同步、剪贴板 |
| `IsBusy`、`IsUpdating` 等 UI 状态 | IP/Cron/位运算/编码转换 | OCR、SecureStorage、原生文件访问 |
| `ErrorMessage`、`StatusMessage`、进度条数值 | 模板数据、选项列表的静态定义 | 需 OS / 浏览器 / MAUI API 的能力 |
| 命令入口：`Format()`、`Clear()`、`SyncAsync()` | `ToolResult<T>` 与 `*Result` 模型 | 异步 I/O + 进度回调 |
| 把 Service 返回值赋给输出属性 | 单元测试覆盖的核心算法 | MAUI / Web 分平台实装 |

```text
用户点击 → VM 命令方法 → ToolBox.Tools.*Service（纯逻辑）
                       ↘ I* 平台服务（文件/网络/设备）
         ← VM 更新绑定属性 ← ToolResult / DTO
         → .razor 渲染
```

**VM 里允许保留的「薄逻辑」**（不必强行下沉）：

- 调用 Service 前后的**状态清理**（清空 Error、重置进度）
- **防重入**（`if (IsBusy) return`）
- **日志行拼接**、把 DTO 格式化成展示用字符串（若规则变复杂，再抽到 Tools）
- **计算属性**（`HasPreview`、`PreviewSummary`）——仅做绑定便利，不承载核心算法

**反例（应下沉到 Tools）**：在 VM 里 `JsonDocument.Parse`、手写 Base64 编解码、正则引擎调用、子网掩码计算、颜色空间换算等。

```csharp
// ✅ VM：编排交互
public void Format()
{
    ErrorMessage = null;
    var result = JsonFormatService.Process(InputJson, indent: true, SortKeys);
    if (!result.Success) { ErrorMessage = result.Error; return; }
    OutputJson = result.Value!.Output;
}

// ✅ Tools：纯逻辑（ToolBox.Tools/Format/JsonFormatService.cs）
public static ToolResult<JsonFormatOutputResult> Process(string? input, bool indent, bool sortKeys) { ... }

// ❌ VM：不应出现核心算法
// using var doc = JsonDocument.Parse(InputJson);
```

### C# 通用

- 新代码使用**文件作用域命名空间**：`namespace ToolBox.Components.Pages.TextTool;`（编辑 `CommonHelp/` 时跟随该文件既有 block 风格）
- 类型默认 `sealed`（VM、Service、DTO、Stub），除非明确需要继承
- 优先现代 C# 语法：collection expression（`[...]`）、`record`、expression-bodied member
- 异步方法以 `Async` 结尾；`CancellationToken cancellationToken = default` 放参数列表末尾
- 可空引用类型与现有文件保持一致：VM 中用户输入/输出常用 `string`，错误信息用 `string?`

### 字段与属性命名

私有字段与公开属性**成对出现**：字段 `_camelCase`，属性 `PascalCase`，属性名 = 字段名去掉前导 `_` 并首字母大写。

```csharp
private string _inputJson = string.Empty;   // 字段
public string InputJson { get => _inputJson; set => SetProperty(ref _inputJson, value); }  // 属性
```

#### 字段 / 属性分类

| 分类 | 字段示例 | 属性示例 | `set` 可见性 | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| 用户可编辑输入 | `_inputJson`、`_sourcePath`、`_pattern` | `InputJson`、`SourcePath`、`Pattern` | `public set` | 与 `@bind-Value` 双向绑定 |
| 用户可编辑选项 | `_sortKeys`、`_deleteExtraFiles` | `SortKeys`、`DeleteExtraFiles` | `public set` |  checkbox / 开关 |
| 下拉选中项 | `_selectedTemplateKey` | `SelectedTemplateKey` | `public set` | 统一 `Selected` + 名词 |
| 只读输出 | `_outputJson`、`_replacementResult` | `OutputJson`、`ReplacementResult` | `private set` | 由命令/Service 结果写入 |
| 错误 / 提示 | `_errorMessage`、`_jsonPathError` | `ErrorMessage`、`JsonPathError` | `private set` | 类型 `string?`；全局用 `ErrorMessage`，分区用 `{Feature}Error` |
| 状态文案 | `_statusMessage` | `StatusMessage` | `private set` | 页面底部或标题旁说明 |
| UI 状态 | `_isBusy`、`_isUpdating` | `IsBusy`、`IsUpdating` | `private set` | 布尔前缀 `Is`；用于禁用按钮、防重入 |
| 进度 / 计数 | `_progressPercent`、`_copiedFiles` | `ProgressPercent`、`CopiedFiles` | `private set` | 由回调或命令更新 |
| 注入依赖 | `_directorySyncService` | （通常不暴露属性） | — | `private readonly`，构造函数赋值 |
| 无字段计算属性 | — | `HasPreview`、`PreviewSummary` | — | 表达式体，仅依赖其它属性 |

#### 命名习惯

- **输入 / 输出成对**：`InputJson` / `OutputJson`，`PlainText` / `Base64Text`，`SourcePath` / `TargetPath`
- **布尔**：`IsBusy`、`IsUpdating`、`IsSigned`；存在性用 `HasPreview` 而非 `IsHasPreview`
- **选中项**：`Selected` + 名词（`SelectedTemplateKey`、`SelectedTimeZoneId`、`SelectedOperation`）
- **命令方法**：动词或动词短语，不加 `Command` 后缀——`Format()`、`Clear()`、`Validate()`、`PickSourceFolderAsync()`、`SyncAsync()`
- **Tools 侧**：服务类 `{Feature}Service`，方法动词 `Process`、`Validate`、`Encode`；结果 `{Feature}Result` 或 `{Action}Result`（如 `JsonFormatOutputResult`、`JsonValidateResult`）
- **默认值**：文本字段 `= string.Empty`；可选消息 `string?` 初始 `null`；bool 按语义默认 `false` 或 `true`（如 `_sortKeys = true`）

#### SetProperty 与 setter 约定

- 可绑定且用户或视图可写 → `public set` + `SetProperty(ref _field, value)`
- 仅 VM 内部命令/回调可写 → `private set` + `SetProperty`
- 赋值时需联动其它属性通知 → 在 `private set` 内 `SetProperty` 后 `OnPropertyChanged(nameof(...))`（见 `DirectorySyncVM.Preview`）

### ViewModel（Blazing.Mvvm）

- 文件名 `{Page}VM.cs`，类名 `{Page}VM`，继承 `ViewModelBase`
- Razor 通过 `@inherits MvvmComponentBase<{Page}VM>` 绑定，视图中统一用 `ViewModel` 访问
- **职责边界**：只做交互编排（见上文「VM 与 Tools 的分工」），核心逻辑委托 `ToolBox.Tools.*Service` 或 `I*` 平台服务
- 需要平台能力时**构造函数注入** `I*` 服务，存为 `private readonly` 字段，不暴露为绑定属性
- 用户操作入口：公开 `void` / `Task` 方法，**不要在 `.razor` 里写 `@code` 业务逻辑**
- 错误与提示：由视图用 `MudAlert` 条件渲染；调用 Tools 后根据 `ToolResult<T>.Success` 分支赋值

```csharp
// JsonFormatVM.cs — 典型模式（交互 + 委托 Tools）
public sealed class JsonFormatVM : ViewModelBase
{
    private string _inputJson = string.Empty;
    private string _outputJson = string.Empty;
    private string? _errorMessage;
    private bool _sortKeys = true;

    public string InputJson
    {
        get => _inputJson;
        set => SetProperty(ref _inputJson, value);
    }

    public string OutputJson
    {
        get => _outputJson;
        private set => SetProperty(ref _outputJson, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool SortKeys
    {
        get => _sortKeys;
        set => SetProperty(ref _sortKeys, value);
    }

    public void Format() => ProcessJson(indent: true);

    private void ProcessJson(bool indent)
    {
        ErrorMessage = null;
        var result = JsonFormatService.Process(InputJson, indent, SortKeys);
        if (!result.Success) { ErrorMessage = result.Error; return; }
        OutputJson = result.Value!.Output;
    }
}
```

### ToolBox.Tools 服务

- `public static class {Feature}Service`，按领域分子目录（`Format/`、`Encoding/`、`Network/` 等）
- 对外返回 `ToolResult<T>`，失败时用 `ToolResult<T>.Fail("用户可读中文说明")`
- 结果类型用 `sealed record`，单独文件 `{Feature}Result.cs` 或与 Service 同文件
- 不抛异常到 UI 层；在 Service 内 `try/catch` 后转为 `Fail`

```csharp
// ToolBox.Tools/Common/ToolResult.cs
public sealed record ToolResult<T>(bool Success, T? Value, string? Error)
{
    public static ToolResult<T> Ok(T value) => new(true, value, null);
    public static ToolResult<T> Fail(string error) => new(false, default, error);
}
```

### Razor 页面

- 文件顶部的典型顺序：

```razor
@page "/json-format"
@namespace ToolBox.Components.Pages.TextTool
@inherits MvvmComponentBase<JsonFormatVM>
```

- 路由用 **kebab-case**（与 `@page`、`NavMenu` 的 `Href` 一致）
- UI 使用 **MudBlazor**（`MudStack`、`MudPaper`、`MudButton`、`MudTextField` 等）
- 主按钮：`Variant="Variant.Filled" Color="Color.Primary"`；次要操作：`Variant="Variant.Outlined"`
- 布局块：`MudPaper Class="pa-4 mb-4" Elevation="1"`；标题区常用 `MudText Typo="Typo.h4"`
- 输入框默认 `Variant="Variant.Outlined"`；大段文本用 `Lines="12"` 或 `TextFieldType="TextFieldType.MultiLine"`
- **禁止**在 Shared 页面使用 `@rendermode`

### 平台服务接口

- 接口与模型放 `ToolBox.Shared/Services/{Feature}/`（如 `IDirectorySyncService`、`DirectorySyncModels.cs`）
- MAUI 实装放 `ToolBox/Services/`，在 `MauiProgram.cs` 注册
- Web 实装或 stub 放 `ToolBox.Web.Client/Services/`，在 `WebServiceExtensions.AddToolBoxWeb` 注册
- Web 不支持的能力提供明确 stub（如 `UnsupportedFolderPickerService`），`IsNativeSupported => false`，避免 silent 失败

```csharp
public interface IDirectorySyncService
{
    Task<DirectorySyncPreview> PreviewAsync(
        string sourcePath,
        string targetPath,
        bool deleteExtraFiles,
        CancellationToken cancellationToken = default);
}
```

### 命名空间对照

| 目录 | 命名空间示例 |
| :--- | :--- |
| `ToolBox.Shared/Components/Pages/TextTool/` | `ToolBox.Components.Pages.TextTool` |
| `ToolBox.Shared/Components/Pages/Calculation/Converters/` | `ToolBox.Components.Pages.Converters` |
| `ToolBox.Shared/Services/DirectorySync/` | `ToolBox.Services.DirectorySync` |
| `ToolBox.Tools/Format/` | `ToolBox.Tools.Format` |
| `ToolBox.Web.Client/Services/` | `ToolBox.Web.Client.Services` |
| `ToolBox/Services/System/` | `ToolBox.Services.MauiPlatform` 或项目内既有命名 |

> 注意：`Calculation/Converters/` 目录下 VM 的命名空间是 `ToolBox.Components.Pages.Converters`，与文件夹名不完全一致——**以同目录现有文件为准**。

### 组件与 VM 同目录

- 所有 Blazor 组件（`.razor`）及其 ViewModel 必须放在**同一文件夹**
- ViewModel 文件名必须以 `VM.cs` 结尾（`JsonFormat.razor` → `JsonFormatVM.cs`）

### 新增工具页清单

1. **先**把算法与转换逻辑放入 `ToolBox.Tools/{领域}/{Name}Service.cs`（及 `*Result.cs`），确保无 UI 依赖
2. 页面 → `ToolBox.Shared/Components/Pages/{分类}/{Name}.razor` + `{Name}VM.cs`（VM 只做绑定与命令编排，调用上一步 Service）
3. 路由 → `@page "/kebab-case"`
4. 导航 → `ToolBox.Shared/Components/Layout/NavMenu.razor`（MAUI / Shared）
5. Web 开放 → `ToolBox.Web.Client/WebRouteRegistry.cs` 白名单 + `WebNavMenu.razor`
6. 若涉及 OS/设备 → Shared `I*` 接口 + MAUI 实装 + Web stub/实装 + DI 注册

### DI 注册入口

```csharp
// 共享（MudBlazor + Mvvm + DiffPlex）
services.AddToolBoxCore(hostingModel);

// MAUI — MauiProgram.cs
AddToolBoxCore(HybridMaui) + 平台服务 AddScoped/I*

// Web WASM — ToolBox.Web.Client/Program.cs
AddToolBoxWeb(WebAssembly);

// Web 服务端 — ToolBox.Web/Program.cs
AddToolBoxWeb(WebApp);
```

### 注释与 diff 纪律

- 代码以自解释为主；仅对非显而易见的业务规则、平台限制、安全约束加简短注释
- 不做无关格式化（重排 import、整文件 reformat、改引号风格等）
- 修改文件时保持 surrounding style；`CommonHelp` 等旧文件不必强行统一为 file-scoped namespace


## 主题与布局

### 主题色

用于 MudBlazor 主题和控件但不要滥用。

| 角色 | 颜色预览 | 十六进制 |
| :--- | :--- | :--- |
| Primary | ![#111111](https://via.placeholder.com/15/2F6DF6/2F6DF6.png) | `#111111` |
| Secondary | ![#0E7490](https://via.placeholder.com/15/0E7490/0E7490.png) | `#0E7490` |
| Tertiary/Accent | ![#F59E0B](https://via.placeholder.com/15/F59E0B/F59E0B.png) | `#F59E0B` |
| Surface | ![#FFFFFF](https://via.placeholder.com/15/FFFFFF/FFFFFF.png) | `#FFFFFF` |
| Background | ![#F6F7FB](https://via.placeholder.com/15/F6F7FB/F6F7FB.png) | `#F6F7FB` |
| Text Primary | ![#0F172A](https://via.placeholder.com/15/0F172A/0F172A.png) | `#0F172A` |
| Text Secondary | ![#475569](https://via.placeholder.com/15/475569/475569.png) | `#475569` |
| Divider | ![#E2E8F0](https://via.placeholder.com/15/E2E8F0/E2E8F0.png) | `#E2E8F0` |


## 安全

- 未明确要求时，不要随意新增/删除 NuGet 包或安装工作负载（workloads）
