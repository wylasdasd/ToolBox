# System Patterns

## 架构概览

```
ToolBox.Shared/     共享 Razor 页面、ViewModel、服务接口、AddToolBoxCore()
ToolBox/            MAUI 壳 + 平台实现（OCR、Picker、SecureStorage 等）
ToolBox.Web/        ASP.NET Core 宿主（App.razor SSR 壳、Program.cs）
ToolBox.Web.Client/ WASM 客户端 + 浏览器服务 + Web 专用路由/布局
CommonHelp/         通用工具类库
TestCommonHelp/     CommonHelp 单元测试
```

Shared **不是 WASM 专属**：MAUI 通过 BlazorWebView 引用，Web 通过 `ToolBox.Web.Client` 引用。

## 关键模式

- 组件与 ViewModel 同目录放置（位于 `ToolBox.Shared/Components/`）
- ViewModel 文件统一 `*VM.cs` 命名
- 通过 `Blazing.Mvvm` 将视图与状态逻辑解耦
- 公共逻辑优先进入 `CommonHelp`，避免页面内散落工具函数
- 平台能力抽象为接口（`IClipboardService`、`IDefaultRouteProvider`、OCR/AI/Picker 等），MAUI 实装、Web 用 stub 或浏览器实现

## 路由与导航

**MAUI**

- 路由根：`ToolBox.Shared/Components/Routes.razor`
- 导航：`ToolBox.Shared/Components/Layout/NavMenu.razor` + `MainLayout.razor`
- 首页：`IndexRedirect.razor` → `/csharp-run`

**Web**

- 路由根：`ToolBox.Web.Client/Components/ToolRouter.razor`
- 路由过滤：`WebRouteView.razor` + `WebRouteRegistry.IsEnabled()`
- 导航/布局：`WebMainLayout.razor`、`WebNavMenu.razor`（仅 Web.Client）
- 首页：`IndexRedirect.razor` → `/json-format`
- 未开放路由直接渲染 `NotFound`

## Web 开放路由（WebRouteRegistry）

`/`, `/text-lines`, `/regex`, `/diff`, `/base64`, `/url-codec`, `/jwt`, `/json-format`, `/json-to-csharp`, `/timestamp`, `/uuid`, `/base64-image`, `/svg-preview`, `/bitwise`, `/struct-layout`, `/converters`, `/request-to-curl`, `/not-found`

## 代码约束（项目内约定）

- 命名空间与目录结构一致，Shared 根命名空间为 `ToolBox`
- Razor 组件命名空间与 `ToolBox.Shared/Components/**` 对齐（如 `ToolBox.Components.Pages`）
- **禁止**在 Shared 页面使用 `@rendermode`（会破坏 MAUI Hybrid）
- Web 交互组件（`ToolRouter`、`WebRouteView`、Web 布局）放在 `ToolBox.Web.Client`
- 避免无关格式化，尽量保持现有风格

## 服务注册

```csharp
// Shared
services.AddToolBoxCore(hostingModel);  // MudBlazor + Mvvm + DiffPlex

// MAUI — MauiProgram.cs
AddToolBoxCore(HybridMaui) + 平台 OCR/AI/Picker/Clipboard 等

// Web 服务端 — ToolBox.Web/Program.cs
AddToolBoxWeb(WebApp)

// Web WASM — ToolBox.Web.Client/Program.cs
AddToolBoxWeb(WebAssembly)  // 含 WebPlatformStubs 满足全部 VM 的 DI
```
