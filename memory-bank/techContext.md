# Tech Context

## 技术栈

- .NET MAUI + Blazor Hybrid
- MudBlazor
- Blazing.Mvvm
- Roslyn Scripting（C# Runner）
- DiffPlex / BlazorTextDiff
- YamlDotNet

## 目标框架

- `net10.0-android`
- `net10.0-ios`
- `net10.0-maccatalyst`
- `net10.0-windows10.0.19041.0`（Windows 条件追加）

## 常用构建命令

```bash
dotnet build ToolBox/ToolBox.csproj -c Debug -f net10.0-windows10.0.19041.0
dotnet build ToolBox.slnx -c Debug
```

## 关键入口

- 启动配置：`ToolBox/MauiProgram.cs`
- 路由入口：`ToolBox/Components/Routes.razor`
- 导航入口：`ToolBox/Components/Layout/NavMenu.razor`

## 依赖策略

- 未明确需求时，不随意新增/删除 NuGet 包或安装 workloads
- 优先复用现有 `CommonHelp` 能力

