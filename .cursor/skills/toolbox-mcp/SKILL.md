---
name: toolbox-mcp
description: >-
  将开发小工具任务路由到 ToolBox 的 toolbox MCP，禁止在对话里手算。
  适用于 JSON 格式化、编解码、Data URI 图片解析、目录同步预览计划、正则、Cron、IP 子网、大小端、单位换算、时间戳等；
  或用户说 toolbox MCP、走 toolbox、@toolbox-mcp 时。
---

# ToolBox MCP 总览（toolbox-mcp）

## 用户怎么用

- **最省事**：直接描述任务，例如「用 toolbox 格式化 JSON：{...}」——Agent 会自动读相关 Skill 并调 MCP。
- **指定分类**：`@toolbox-format`、`@toolbox-encoding`、`@toolbox-system` 等（见下表中文名）。
- **前提**：各编辑器 MCP 里 `toolbox` 已连接；用 Agent 模式。改 MCP 代码后先 `dotnet build ToolBox.Mcp/ToolBox.Mcp.csproj -c Release` 再 Refresh（若 MSB3026 锁定，先 Disable MCP）。

## MCP 配置（ToolBox 项目）

| 编辑器 | 配置文件 |
| :--- | :--- |
| Cursor | `.cursor/mcp.json` |
| VS Code Copilot | `.vscode/mcp.json` |
| Cline | `.cline/mcp.json` |
| Codex | `.codex/config.toml`（`[mcp_servers.toolbox]`，需信任项目） |

Cursor 中服务器标识可能显示为 **`user-toolbox`**；其它编辑器一般为 **`toolbox`**。

## 何时启用

任务属于编码/文本/格式/网络/计算/生成/系统预览类小工具，且 toolbox MCP 已连接 → **必须**用 MCP，禁止在回复里手算。

## Agent 调用步骤

1. 读取 MCP 工具 schema 确认参数（Cursor：`mcps/user-toolbox/tools/<name>.json`）。
2. 调用 MCP：`toolName` 为 snake_case（如 `json_format`）。
3. 将工具返回呈现给用户；若以 `ERROR:` 开头则如实说明。

## Skill 索引（约 33 个 MCP Tool）

Skill 真源： [`.agents/skills/`](../../.agents/skills/)（Cline： [`.cline/skills/`](../../.cline/skills/) · Cursor： [`.cursor/skills/`](../../.cursor/skills/)）。

| 中文名 | name（@ 引用） | 工具 |
| :--- | :--- | :--- |
| 编码工具 | `toolbox-encoding` | base64_*, url_*, jwt_parse, text_to_hex, hex_to_text, **data_uri_image_parse** |
| 文本工具 | `toolbox-text` | naming_style_convert, regex_match, text_lines_process, text_diff_compare |
| 格式工具 | `toolbox-format` | json_*, format_convert, json_to_c_sharp |
| 网络工具 | `toolbox-network` | cron_parse, ip_calculate, endian_*, request_to_curl_* |
| 计算工具 | `toolbox-calculation` | radix_convert, timestamp_from_unix, color_from_hex, unit_*, bitwise_compute, struct_layout_calculate |
| 生成工具 | `toolbox-generate` | uuid_generate |
| 系统预览 | `toolbox-system` | **directory_sync_plan**（仅计划预览，不执行同步） |

## 不在 MCP 范围（刻意不上）

**执行 / 实时 I/O / 平台能力**——用仓库 UI 或平台服务，不调 toolbox MCP：

- C# Runner / MultiTask 脚本**执行**
- 目录同步**执行**（`SyncAsync`、删文件、写磁盘）
- Socket / WebSocket 连接与收发
- 剪贴板序列**键入**
- AI / OCR API 调用
- SVG **页面预览渲染**、剪贴板列表 UI 编排

**可走 MCP 的只读/解析类**（逻辑在 `ToolBox.Tools`，与 UI 共用）：

- `data_uri_image_parse`：Data URI / 裸 Base64 → mime + data URL
- `directory_sync_plan`：JSON 文件清单 → 复制/删除预览统计（无磁盘 I/O）

## 构建与 Refresh

```bash
dotnet build ToolBox.Mcp/ToolBox.Mcp.csproj -c Release
```

完成后在编辑器 MCP 面板 **Refresh**。工具列表应为 **33** 个；若仍显示 31 个，说明仍在跑旧 DLL。
