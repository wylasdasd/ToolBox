# Active Context

## 当前阶段

Blazor Web App 拆分已完成；**Phase A'/B'** 已将剩余 VM 逻辑下沉 `ToolBox.Tools`，并扩展 toolbox MCP（约 33 Tool）。

## 最近完成（2026-06-29）

- **Phase A'**：Base64 图片、SVG 校验、剪贴板序列、C# 引用解析、目录同步计划、AI 提取模板等迁入 `ToolBox.Tools`
- **Phase B'**：MCP 新增 `data_uri_image_parse`、`directory_sync_plan`（仅预览，不执行同步）
- **Bugfix**：`unit_transfer_time` 支持 `gib`/`mib` 等 IEC 单位
- Skill 真源 [`.agents/skills/`](../.agents/skills/) 新增 `toolbox-system`，同步 [`.cursor/skills/`](../.cursor/skills/)、[`.cline/skills/`](../.cline/skills/)
- 更新 `AGENTS.md`、`ToolBox.Mcp/README.md`

## 最近完成（2026-06）

- 新建 `ToolBox.Shared`，从 MAUI 迁入 `Components/` 与 `AddToolBoxCore()`
- 新建 `ToolBox.Web`（SSR 壳）与 `ToolBox.Web.Client`（WASM 交互）
- 抽象 `IClipboardService`、`IDefaultRouteProvider` 等平台接口
- Web 路由/布局（`ToolRouter`、`WebRouteView`、`WebMainLayout`）迁入 `ToolBox.Web.Client`
- `WebRouteRegistry` 白名单控制 Web 开放页面；桌面专属功能 Web 端 stub + NotFound
- 从 Shared 移除全部 `@rendermode`（MAUI 兼容）
- 更新 `ToolBox.slnx`、`AGENTS.md`

## 当前关注点

- MCP 与 Skill 文档与 Release DLL 同步（Refresh 后应见 33 个 Tool）
- Web 端到端验证：首页跳转、各工具页 WASM 交互
- 新增工具时同步评估是否加入 `WebRouteRegistry` 与 MCP 白名单

## 已知限制 / 待办

- Web 端 `WebPlatformStubs` 为 Blazing.Mvvm 自动注册全部 VM 的权宜之计；长期可改为 Web 仅注册可用 VM
- `request-to-curl` 的「调用系统 curl」在 Web 上可能需额外隐藏
- `.vscode/launch.json` 尚未添加 Web 启动配置
- 构建时若 MCP/VS 锁定 DLL，需 Disable MCP 或关闭 VS 后重试

## 下一步建议

- 补充 Web 的 VS Code launch 配置
- 按模块补充 Web/MAUI 功能差异表
- 为各工具页面补充最小手工验证清单
