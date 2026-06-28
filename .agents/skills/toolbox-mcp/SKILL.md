---
name: toolbox-mcp
description: >-
  将开发小工具任务路由到 ToolBox 的 toolbox MCP，禁止在对话里手算。
  适用于 JSON 格式化、编解码、正则、Cron、IP 子网、大小端、单位换算、时间戳等；
  或用户说 toolbox MCP、走 toolbox、@toolbox-mcp 时。
---

# ToolBox MCP 总览（toolbox-mcp）

## 用户怎么用

- **最省事**：直接描述任务，例如「用 toolbox 格式化 JSON：{...}」——Agent 会自动读相关 Skill 并调 MCP。
- **指定分类**：`@toolbox-format`、`@toolbox-encoding` 等（见下表中文名）。
- **前提**：各编辑器 MCP 里 `toolbox` 已连接；用 Agent 模式。改 MCP 代码后先 `dotnet build ToolBox.Mcp/ToolBox.Mcp.csproj -c Release` 再 Refresh。

## MCP 配置（ToolBox 项目）

| 编辑器 | 配置文件 |
| :--- | :--- |
| Cursor | `.cursor/mcp.json` |
| VS Code Copilot | `.vscode/mcp.json` |
| Cline | `.cline/mcp.json` |
| Codex | `.codex/config.toml`（`[mcp_servers.toolbox]`，需信任项目） |

Cursor 中服务器标识可能显示为 **`user-toolbox`**；其它编辑器一般为 **`toolbox`**。

## 何时启用

任务属于编码/文本/格式/网络/计算/生成类小工具，且 toolbox MCP 已连接 → **必须**用 MCP，禁止在回复里手算。

## Agent 调用步骤

1. 读取 MCP 工具 schema 确认参数（Cursor：`mcps/user-toolbox/tools/<name>.json`）。
2. 调用 MCP：`toolName` 为 snake_case（如 `json_format`）。
3. 将工具返回呈现给用户；若以 `ERROR:` 开头则如实说明。

## Skill 索引

Skill 真源： [`.agents/skills/`](../../.agents/skills/)（Cline 副本见 [`.cline/skills/`](../../.cline/skills/)）。

| 中文名 | name（@ 引用） | 工具 |
| :--- | :--- | :--- |
| 编码工具 | `toolbox-encoding` | base64_*, url_*, jwt_parse, text_to_hex, hex_to_text |
| 文本工具 | `toolbox-text` | naming_style_convert, regex_match, text_lines_process, text_diff_compare |
| 格式工具 | `toolbox-format` | json_*, format_convert, json_to_c_sharp |
| 网络工具 | `toolbox-network` | cron_parse, ip_calculate, endian_*, request_to_curl_* |
| 计算工具 | `toolbox-calculation` | radix_convert, timestamp_from_unix, color_from_hex, unit_*, bitwise_compute, struct_layout_calculate |
| 生成工具 | `toolbox-generate` | uuid_generate |

## 不在 MCP 范围

AI/OCR、目录同步、C# Runner、Socket 服务端、图片预览——用仓库代码或平台服务，不调 toolbox MCP。
