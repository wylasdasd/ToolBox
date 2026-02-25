# System Patterns

## 架构概览

- `ToolBox/`：MAUI Blazor Hybrid 主应用
- `CommonHelp/`：共享类库（复用优先）
- UI 层以 Razor 组件组织，按功能目录拆分页面

## 关键模式

- 组件与 ViewModel 同目录放置
- ViewModel 文件统一 `*VM.cs` 命名
- 通过 `Blazing.Mvvm` 将视图与状态逻辑解耦
- 公共逻辑优先进入 `CommonHelp`，避免页面内散落工具函数

## 路由与导航

- 页面路由通过 `@page` 声明
- 左侧导航集中在 `Components/Layout/NavMenu.razor`
- 新页面接入通常包括：页面文件、VM 文件、导航入口

## 代码约束（项目内约定）

- 命名空间与目录结构一致，根命名空间为 `ToolBox`
- Razor 组件命名空间与 `ToolBox/Components/**` 对齐
- 避免无关格式化，尽量保持现有风格

