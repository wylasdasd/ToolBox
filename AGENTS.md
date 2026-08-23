# ToolBox：Agent 约定

## 仓库结构

| 目录 | 职责 |
|:---|:---|
| `ToolBox/` | .NET MAUI Blazor 应用壳 + 平台服务实装 |
| `ToolBox.Shared/` | 共享 Razor 组件、ViewModel、服务接口、`AddToolBoxCore()` |
| `ToolBox.Web/` | Blazor Web App 宿主（SSR 壳） |
| `ToolBox.Web.Client/` | WASM 客户端、Web 路由/布局、浏览器平台服务 |
| `ToolBox.Tools/` | 可复用工具 Service 层（VM 与 MCP 共用，纯逻辑） |
| `ToolBox.Mcp/` | 本地 stdio MCP Server（见 `README.md`） |
| `CommonHelp/` | 跨项目通用工具库（namespace `CommonHelp`，file-scoped） |
| `TestCommonHelp/` | CommonHelp 单元测试 |

## 构建

- MAUI：`dotnet build ToolBox/ToolBox.csproj -c Debug -f net10.0-windows10.0.19041.0`
- Web 运行：`dotnet run --project ToolBox.Web/ToolBox.Web.csproj`
- Web 发布：`dotnet publish ToolBox.Web/ToolBox.Web.csproj -c Release -o ./publish/web`
- Docker：`docker compose up -d --build`（默认 http://localhost:18488）
- 全量：`dotnet build ToolBox.slnx -c Debug`

## Web 架构要点

- **SSR 壳**：`ToolBox.Web/Components/App.razor`（无 `@rendermode`）
- **WASM 交互**：`<ToolRouter @rendermode="InteractiveWebAssembly" />` 及其子树
- Shared **非 WASM 专属**；MAUI/Web 均引用 Shared
- **禁止**在 Shared 页面使用 `@rendermode`（破坏 MAUI Hybrid）
- Web 交互组件（`ToolRouter`、`WebNavMenu` 等）必须在 `ToolBox.Web.Client`
- 导航定义：`ToolNavDefinition.cs`；路由注册：`WebRouteRegistry.cs`
- Docker 发布**勿**加 `--no-restore`；`ToolBox.Web.csproj` 需 `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>`

## 分层与职责

| 层 | 放什么 | 不放什么 |
|:---|:---|:---|
| `ToolBox.Tools/` | `static *Service`、`*Result` record、`ToolResult<T>` | UI/MudBlazor/平台API |
| `Shared/Services/` | `I*` 接口、DTO、平台无关服务 | MAUI/浏览器专属API |
| `Shared/Components/` | `.razor` + 同目录 `*VM.cs`（**仅交互**） | 算法/解析/转换 |
| `ToolBox/Services/` | MAUI 平台实装 | Web逻辑 |
| `Web.Client/Services/` | WASM 实装 / `Unsupported*` stub | MAUI逻辑 |
| `CommonHelp/` | 跨项目通用工具库 | 新功能优先放 `ToolBox.Tools` |

**VM vs Tools**：VM 只做交互编排（绑定、状态清理 `ErrorMessage=null`、防重入 `if(IsBusy)return`、格式化展示、计算属性）；核心算法（`JsonDocument.Parse`、Base64 编解码、正则、子网掩码计算等解析/转换/校验/计算）必须下沉 `ToolBox.Tools`。流程：`用户点击 → VM命令 → Tools.*Service / I*平台服务 → ToolResult → VM更新绑定 → .razor渲染`

## 核心约定

- 命名空间与目录结构一致；优先复用 `CommonHelp`，确需新增放 `CommonHelp/`
- 平台差异走 `Shared/Services/I*` 接口：MAUI 实装 `ToolBox/Services/`（`MauiProgram.cs` 注册）；Web 实装/stub `ToolBox.Web.Client/Services/`（`WebServiceExtensions.AddToolBoxWeb` 注册）；Web 不支持的能力给明确 stub（如 `UnsupportedFolderPickerService`，`IsNativeSupported => false`），避免 silent 失败
- 保持既有编码风格，避免无关格式化

## C# 风格与命名

- 文件作用域命名空间；类型默认 `sealed`；优先现代语法（`[...]`、`record`、expression-bodied）；可空引用类型跟随现有文件风格
- 异步方法以 `Async` 结尾；`CancellationToken` 放末尾，默认 `default`
- 字段 `_camelCase` ↔ 属性 `PascalCase` 成对；输入/输出成对（`InputJson`/`OutputJson`、`PlainText`/`Base64Text`、`SourcePath`/`TargetPath`）；布尔前缀 `Is`/`Has`（`HasPreview` 而非 `IsHasPreview`）；选中项 `Selected`+名词（`SelectedTemplateKey`）
- 命令方法：动词或动词短语，不加 `Command` 后缀（`Format()`、`Clear()`、`PickSourceFolderAsync()`）；Tools 侧 `{Feature}Service` + 方法 `Process/Validate/Encode`，结果 `{Feature}Result` 或 `{Action}Result`（如 `JsonValidateResult`）
- setter：用户可编辑 → `public set`（配 `@bind-Value`）；仅 VM 内部写入 → `private set`；均配 `SetProperty(ref _field, value)`；需联动通知在 setter 内 `OnPropertyChanged(nameof(...))`（见 `DirectorySyncVM.Preview`）
- 默认值：文本 `= string.Empty`；消息 `string? = null`；bool 按语义（如 `_sortKeys = true`）；注入依赖 `private readonly` 构造函数赋值，不暴露属性；计算属性用表达式体（`HasPreview`），无 backing 字段

- 尽量使用C# 新语法（）要兼容项目net 版本 
## ViewModel（Blazing.Mvvm）

- `{Page}.razor` 与 `{Page}VM.cs` 必须**同一文件夹**，VM 文件名以 `VM.cs` 结尾；VM 继承 `ViewModelBase`
- Razor 通过 `@inherits MvvmComponentBase<{Page}VM>` 绑定，视图中统一用 `ViewModel` 访问；**禁止**在 `.razor` 写 `@code` 业务逻辑，用户入口是 VM 的公开 `void`/`Task` 方法
- 平台能力构造函数注入 `I*`，存 `private readonly`，不暴露为绑定属性
- 错误/提示：`ErrorMessage`（`string?`，`private set`）；按 `ToolResult<T>.Success` 分支赋值，视图用 `MudAlert` 条件渲染

## ToolBox.Tools 服务

- `public static class {Feature}Service`，按领域分子目录（`Format/`、`Encoding/`、`Network/` 等）；结果用 `sealed record`，单独 `{Feature}Result.cs` 或与 Service 同文件
- 对外返回 `ToolResult<T>`：成功 `Ok(value)`，失败 `Fail("用户可读中文说明")`；Service 内 `try/catch`，不抛异常到 UI 层

## Razor 页面

```razor
@page "/json-format"
@namespace ToolBox.Components.Pages.TextTool
@inherits MvvmComponentBase<JsonFormatVM>
```

- 路由用 **kebab-case**（与 `@page`、`NavMenu` 的 `Href` 一致）；UI 用 MudBlazor（`MudStack`、`MudPaper`、`MudButton`、`MudTextField` 等）
- 主按钮 `Variant="Variant.Filled" Color="Color.Primary"`，次要 `Variant="Variant.Outlined"`；布局块 `MudPaper Class="pa-4 mb-4" Elevation="1"`，标题 `MudText Typo="Typo.h4"`；输入框默认 `Outlined`，大段文本 `Lines="12"`

## 命名空间对照

| 目录 | 命名空间示例 |
| :--- | :--- |
| `ToolBox.Shared/Components/Pages/TextTool/` | `ToolBox.Components.Pages.TextTool` |
| `ToolBox.Shared/Components/Pages/Calculation/Converters/` | `ToolBox.Components.Pages.Converters` |
| `ToolBox.Shared/Services/DirectorySync/` | `ToolBox.Services.DirectorySync` |
| `ToolBox.Tools/Format/` | `ToolBox.Tools.Format` |
| `ToolBox.Web.Client/Services/` | `ToolBox.Web.Client.Services` |
| `ToolBox/Services/System/` | `ToolBox.Services.MauiPlatform` 或项目内既有命名 |

> `Calculation/Converters/` 下 VM 的命名空间是 `ToolBox.Components.Pages.Converters`，与文件夹名不完全一致——**以同目录现有文件为准**。

## 新增工具页清单

1. **先**把算法与转换逻辑放入 `ToolBox.Tools/{领域}/{Name}Service.cs`（及 `*Result.cs`），确保无 UI 依赖
2. 页面 → `ToolBox.Shared/Components/Pages/{分类}/{Name}.razor` + `{Name}VM.cs`（VM 只做绑定与命令编排，调用上一步 Service）
3. 路由 → `@page "/kebab-case"`
4. 导航 → `ToolBox.Shared/Components/Layout/ToolNavDefinition.cs`（`WebEnabled` / `MauiEnabled`）；`NavMenu.razor` / `WebNavMenu.razor` 无需再手写链接
5. Web 别名/重定向路由（如有）→ `ToolBox.Web.Client/WebRouteRegistry.cs` 的 `AdditionalWebRoutes`
6. 若涉及 OS/设备 → Shared `I*` 接口 + MAUI 实装 + Web stub/实装 + DI 注册

## DI 注册

- 共享（MudBlazor + Mvvm + DiffPlex）：`services.AddToolBoxCore(hostingModel)`
- MAUI `MauiProgram.cs`：`AddToolBoxCore(HybridMaui)` + 平台服务 `AddScoped/I*`
- Web WASM `ToolBox.Web.Client/Program.cs`：`AddToolBoxWeb(WebAssembly)`
- Web 服务端 `ToolBox.Web/Program.cs`：`AddToolBoxWeb(WebApp)`




## 主题色

- Primary `#111111` · Secondary `#0E7490` · Tertiary/Accent `#F59E0B` · Surface `#FFFFFF` · Background `#F6F7FB` · Text `#0F172A` / `#475569` · Divider `#E2E8F0`


## MCP（toolbox）

本地 stdio MCP Server 见 `ToolBox.Mcp/README.md`。项目级配置（同一 dll）：Cursor `.cursor/mcp.json`（标识 **`user-toolbox`**，设置界面可能显示为 `toolbox`）；VS Code `.vscode/mcp.json`；Cline `.cline/mcp.json`；Codex `.codex/config.toml`（标识 **`toolbox`**）。

### 必须走 MCP 的场景（**禁止**手算、手写等价逻辑或「猜」结果，应调用 `user-toolbox` MCP）

| 领域 | 典型任务 |
| :--- | :--- |
| 编码 | Base64、URL、JWT 解析、文本↔Hex、Data URI 图片解析 |
| 文本 | 命名风格转换、正则匹配/替换、多行处理、文本 diff |
| 格式 | JSON 格式化/压缩/校验/JSONPath、JSON/YAML/XML/TOML/CSV 互转、JSON→C# |
| 网络 | Cron 解析、IPv4 子网、大小端、HTTP→curl/PowerShell（无出站 HTTP） |
| 计算 | 进制、Unix 时间戳、颜色、单位换算、传输时间、位运算、struct 布局 |
| 生成 | UUID/GUID |

**不必/刻意不上 MCP**：改代码、读仓库、跑 build/test、**执行**目录同步、OCR、AI 对话、`csharp_run` 执行、Socket/WebSocket 实时 I/O、剪贴板序列键入、AI API 调用、SVG 预览渲染、剪贴板列表解析（后几项无安全/可预测边界，逻辑仅在 Tools 供 UI）。

**预览类 MCP**（只读）：`directory_sync_plan`（JSON 文件清单 → 同步预览统计，不读写磁盘）、`data_uri_image_parse`。

### 调用约定

1. 调用前读 `mcps/user-toolbox/tools/<tool>.json` 确认参数名与必填项。
2. 失败返回 `ERROR:` 开头 → 原样告知用户或换合法输入重试；不绕过 Service 重算。
3. 修改 `ToolBox.Tools` 或 `ToolBox.Mcp` 后：`dotnet build ToolBox.Mcp/ToolBox.Mcp.csproj -c Release` + MCP 面板 Refresh。
4. 分类细则见 [`.agents/skills/`](.agents/skills/) 各 `toolbox-*` Skill（含 `toolbox-system`；副本 `.cursor/skills/` · `.cline/skills/`）。

## 安全

- 未明确要求时，不要随意新增/删除 NuGet 包或安装工作负载（workloads）
