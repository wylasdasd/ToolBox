# Tech Context

## 技术栈

- .NET MAUI + Blazor Hybrid（桌面/移动端）
- Blazor Web App：SSR 壳 + Interactive WebAssembly（浏览器）
- MudBlazor、Blazing.Mvvm
- Roslyn Scripting（C# Runner，仅 MAUI）
- DiffPlex / BlazorTextDiff
- YamlDotNet

## 目标框架

**MAUI（`ToolBox/`）**

- `net10.0-android`
- `net10.0-ios`
- `net10.0-maccatalyst`
- `net10.0-windows10.0.19041.0`（Windows 条件追加）

**Web / Shared / CommonHelp**

- `net10.0`

## 常用命令

```bash
# MAUI Windows
dotnet build ToolBox/ToolBox.csproj -c Debug -f net10.0-windows10.0.19041.0

# Web 本地运行
dotnet run --project ToolBox.Web/ToolBox.Web.csproj

# Web 发布
dotnet publish ToolBox.Web/ToolBox.Web.csproj -c Release -o ./publish/web

# 整个解决方案
dotnet build ToolBox.slnx -c Debug
```

## 关键入口

| 项目 | 入口 | 说明 |
|------|------|------|
| MAUI | `ToolBox/MauiProgram.cs` | `AddToolBoxCore(HybridMaui)` + 平台服务 |
| MAUI | `ToolBox/MainPage.xaml` | WebView 挂载 `ToolBox.Shared` 的 `Routes` |
| Shared | `ToolBox.Shared/ServiceExtensions.cs` | `AddToolBoxCore()`（MudBlazor、Mvvm、DiffPlex） |
| Shared | `ToolBox.Shared/Components/Routes.razor` | MAUI 路由根 |
| Web 宿主 | `ToolBox.Web/Program.cs` | `AddToolBoxWeb(WebApp)`、`MapRazorComponents<App>()` |
| Web 宿主 | `ToolBox.Web/Components/App.razor` | SSR 壳 + `<ToolRouter @rendermode="InteractiveWebAssembly" />` |
| Web 客户端 | `ToolBox.Web.Client/Program.cs` | `AddToolBoxWeb(WebAssembly)` |
| Web 客户端 | `ToolBox.Web.Client/Components/ToolRouter.razor` | Web 路由根 |
| Web 客户端 | `ToolBox.Web.Client/WebRouteRegistry.cs` | Web 开放路由白名单 |
| Tools | `ToolBox.Tools/**/**Service.cs` | 纯逻辑，VM 与 MCP 共用 |
| MCP | `ToolBox.Mcp/Tools/*.cs` | MCP 薄适配；`Program.cs` 注册 stdio |
| MCP 文档 | `ToolBox.Mcp/README.md` | 编辑器 MCP 配置、约 33 Tool 列表 |

## Agent / MCP

- 项目级 MCP：`.cursor/mcp.json`、`.vscode/mcp.json`、`.cline/mcp.json`、`.codex/config.toml`
- Skill 真源：`.agents/skills/`（`toolbox-mcp`、`toolbox-encoding`、`toolbox-system` 等）
- 改 `ToolBox.Tools` 或 MCP 后：`dotnet build ToolBox.Mcp/ToolBox.Mcp.csproj -c Release`，再 Refresh MCP

## Web 渲染模型

- **SSR 壳**：`App.razor` 中无 `@rendermode` 的部分（HTML、CSS/JS 引用、`HeadOutlet`）
- **WASM 交互**：`ToolRouter` 及其子树（含 Shared 里所有已开放工具页）
- Shared 页面**不设** `@rendermode`（MAUI Hybrid 不支持）；Web 交互性由 `App.razor` 统一注入
- `InteractiveServer` 已在 `Program.cs` 注册，当前无页面使用

## 依赖策略

- 未明确需求时，不随意新增/删除 NuGet 包或安装 workloads
- 优先复用现有 `CommonHelp` 能力
- 平台差异通过 `ToolBox.Shared/Services/` 接口 + MAUI/Web 各自实现
