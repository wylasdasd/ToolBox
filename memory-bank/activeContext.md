# Active Context

## 当前阶段

Blazor Web App 拆分已完成：Shared RCL + Web 宿主 + Web.Client WASM 三项目架构落地，MAUI 全功能保持不变。

## 最近完成（2026-06）

- 新建 `ToolBox.Shared`，从 MAUI 迁入 `Components/` 与 `AddToolBoxCore()`
- 新建 `ToolBox.Web`（SSR 壳）与 `ToolBox.Web.Client`（WASM 交互）
- 抽象 `IClipboardService`、`IDefaultRouteProvider` 等平台接口
- Web 路由/布局（`ToolRouter`、`WebRouteView`、`WebMainLayout`）迁入 `ToolBox.Web.Client`
- `WebRouteRegistry` 白名单控制 Web 开放页面；桌面专属功能 Web 端 stub + NotFound
- 从 Shared 移除全部 `@rendermode`（MAUI 兼容）
- 更新 `ToolBox.slnx`、`AGENTS.md`

## 当前关注点

- Web 端到端验证：首页跳转、各工具页 WASM 交互
- 新增工具时同步评估是否加入 `WebRouteRegistry`
- 功能变更后及时同步 memory-bank 与 `README.md`

## 已知限制 / 待办

- Web 端 `WebPlatformStubs` 为 Blazing.Mvvm 自动注册全部 VM 的权宜之计；长期可改为 Web 仅注册可用 VM
- `request-to-curl` 的「调用系统 curl」在 Web 上可能需额外隐藏
- `.vscode/launch.json` 尚未添加 Web 启动配置
- 构建时若 VS 锁定 DLL，需关闭 VS 或正在运行的 Web 进程后重试

## 下一步建议

- 补充 Web 的 VS Code launch 配置
- 按模块补充 Web/MAUI 功能差异表
- 为各工具页面补充最小手工验证清单
