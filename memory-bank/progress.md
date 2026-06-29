# Progress

## Milestones

- 2026-02-25：完善根目录 `README.md`
- 2026-02-25：建立 `memory-bank/` 并创建核心上下文文件
- 2026-06-27：拆分 `ToolBox.Shared` / `ToolBox.Web` / `ToolBox.Web.Client`，MAUI 引用 Shared
- 2026-06-27：Web WASM 交互组件迁入 `ToolBox.Web.Client`，`App.razor` SSR 壳 + `ToolRouter` WASM
- 2026-06-27：平台服务抽象（Clipboard、DefaultRoute、OCR/AI/Picker stub），Web 路由白名单
- 2026-06-29：Phase A' — 剩余 9 个 VM 核心逻辑下沉 `ToolBox.Tools`；Phase B' — MCP 新增 `data_uri_image_parse`、`directory_sync_plan`（共约 **33** Tool）；修复 `unit_transfer_time` 的 `gib` 等单位

## Current Status

- MAUI：全功能，组件来自 Shared，Blazor Hybrid WebView
- Web：文本/计算/部分网络工具可用；AI/OCR、C# Runner、目录同步、Socket 等未开放
- **ToolBox.Tools**：22+ Service，VM 与 MCP 共用
- **ToolBox.Mcp**：stdio MCP，约 33 个 Tool；改代码后 `dotnet build ToolBox.Mcp -c Release` + MCP Refresh
- 构建：`ToolBox.Shared`、`ToolBox.Web.Client`、`ToolBox.Web`、Windows MAUI、`ToolBox.Mcp`（Debug）均可成功构建
- Memory Bank 已同步多项目架构

## Next

- Web launch 配置与端到端验证
- 记录 Web vs MAUI 功能矩阵
- 为各工具页面补充最小测试清单（手工验证路径）
- MCP Release 构建前 Disable 编辑器 MCP，避免 MSB3026 DLL 锁定
