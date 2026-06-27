# Progress

## Milestones

- 2026-02-25：完善根目录 `README.md`
- 2026-02-25：建立 `memory-bank/` 并创建核心上下文文件
- 2026-06-27：拆分 `ToolBox.Shared` / `ToolBox.Web` / `ToolBox.Web.Client`，MAUI 引用 Shared
- 2026-06-27：Web WASM 交互组件迁入 `ToolBox.Web.Client`，`App.razor` SSR 壳 + `ToolRouter` WASM
- 2026-06-27：平台服务抽象（Clipboard、DefaultRoute、OCR/AI/Picker stub），Web 路由白名单

## Current Status

- MAUI：全功能，组件来自 Shared，Blazor Hybrid WebView
- Web：文本/计算/部分网络工具可用；AI/OCR、C# Runner、目录同步、Socket 等未开放
- 构建：`ToolBox.Shared`、`ToolBox.Web.Client`、`ToolBox.Web` 均可成功构建
- Memory Bank 已同步多项目架构

## Next

- Web launch 配置与端到端验证
- 记录 Web vs MAUI 功能矩阵
- 为各工具页面补充最小测试清单（手工验证路径）
- 逐步沉淀常见问题与排障路径（如 MSB3027 文件锁定）
