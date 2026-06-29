# ToolBox

<img width="1865" height="1288" alt="image" src="https://github.com/user-attachments/assets/16b27dc7-36a1-4a92-b0bd-2ef2b6f54331" />

基于 .NET MAUI Blazor 的本地开发工具箱，目标是把日常高频小工具集中到一个应用里，减少开发过程中的上下文切换。

2026-06 起支持 **Blazor Web App** 浏览器访问：文本/计算/部分网络工具可在 Web 使用；桌面专属能力（AI、OCR、C# Runner 等）仍仅在 MAUI 提供。

## 主要功能

- C# Runner：运行 C# 代码片段并查看输出（MAUI）
- 网络工具：请求转 cURL、Socket 测试（Socket 仅 MAUI）
- 计算工具：位运算、结构体布局、常用转换（进制、格式、颜色等）
- 文本工具：文本整理、正则、Base64、URL、JWT、时间戳、JSON、UUID、Diff、SVG 预览、剪贴板序列等
- AI / OCR / 目录同步等（MAUI）

## 项目结构

```
ToolBox/              MAUI Blazor Hybrid 应用（平台服务 + WebView 壳）
ToolBox.Shared/       共享 Razor 页面、ViewModel、服务接口
ToolBox.Web/          Blazor Web App 宿主（SSR 壳）
ToolBox.Web.Client/   WASM 客户端、Web 路由/布局、浏览器服务
ToolBox.Tools/        可复用工具 Service 层（VM 与 MCP 共用）
ToolBox.Mcp/          本地 stdio MCP Server（Agent 小工具，约 33 个 Tool）
CommonHelp/           共享工具类库
TestCommonHelp/       CommonHelp 单元测试
ToolBox.slnx          解决方案入口
```

Shared 组件被 MAUI 与 Web 共同引用；Web 通过 `WebRouteRegistry` 控制开放页面子集。

## 开发环境

- .NET SDK 10（与项目 `net10.0-*` 目标框架一致）
- Windows 本地开发建议先使用 Windows 目标框架进行 MAUI 构建验证

## 构建与运行

**MAUI Windows**

```bash
dotnet build ToolBox/ToolBox.csproj -c Debug -f net10.0-windows10.0.19041.0
```

**Web 本地运行**

```bash
dotnet run --project ToolBox.Web/ToolBox.Web.csproj
```

**Web 发布**

```bash
dotnet publish ToolBox.Web/ToolBox.Web.csproj -c Release -o ./publish/web
```

**整个解决方案**

```bash
dotnet build ToolBox.slnx -c Debug
```

## 开发约定

- 命名空间与目录结构保持一致（Shared 根命名空间：`ToolBox`）
- Razor 组件位于 `ToolBox.Shared/Components/**`，命名空间如 `ToolBox.Components.Pages`
- Blazor 组件与对应 ViewModel 必须同目录；ViewModel 文件名以 `VM.cs` 结尾
- 优先复用 `CommonHelp`，避免在业务目录新增零散工具函数
- Shared 页面**不要**加 `@rendermode`（MAUI 不支持）；Web 交互由 `App.razor` 的 `ToolRouter` 统一提供
- Web 专用路由/布局放在 `ToolBox.Web.Client`

## 技术栈

- .NET MAUI + Blazor Hybrid
- Blazor Web App（SSR + Interactive WebAssembly）
- MudBlazor、Blazing.Mvvm
- Roslyn Scripting（C# Runner）
- DiffPlex / BlazorTextDiff（文本对比）

## 文档

- [`AGENTS.md`](AGENTS.md)：Agent / 协作者约定（含 MCP 与 Skill 路由）
- [`ToolBox.Mcp/README.md`](ToolBox.Mcp/README.md)：本地 toolbox MCP 配置与工具列表
- [`.agents/skills/`](.agents/skills/)：MCP 分类 Skill 真源（`.cursor/skills/`、`.cline/skills/` 为副本）
- [`memory-bank/`](memory-bank/)：长期上下文（架构、进度、产品范围）
