---
name: toolbox-mcp
description: >-
  将开发小工具任务路由到 ToolBox 的 toolbox MCP，禁止在对话里手算。
  适用于 JSON 格式化、编解码、正则、Cron、IP 子网、大小端、单位换算、时间戳等；
  或用户说 toolbox MCP、走 toolbox、@toolbox-mcp 时。
---

# ToolBox MCP 总览（toolbox-mcp · Cline）

> Cline 专用副本。内容与 [`.agents/skills/toolbox-mcp/`](../../.agents/skills/toolbox-mcp/SKILL.md) 同步维护；更新 Skill 时请两处一起改。

## 用户怎么用

- 直接描述任务，或 `@toolbox-format` 等点名 Skill。
- **前提**：Cline 已加载 [`.cline/mcp.json`](../../.cline/mcp.json) 中的 `toolbox`；Skills 功能已开启（Settings → Features → Enable Skills）。

## MCP 服务器

- 名称：**`toolbox`**
- 配置：`.cline/mcp.json`

## 何时启用

任务属于编码/文本/格式/网络/计算/生成类小工具，且 toolbox MCP 已连接 → **必须**用 MCP，禁止在回复里手算。

## Agent 调用步骤

1. 在 Cline MCP 工具列表中确认 `toolbox` 下的 tool 名称与参数。
2. 调用对应 MCP 工具（snake_case，如 `json_format`）。
3. 将返回呈现给用户；若以 `ERROR:` 开头则如实说明。

## Skill 索引

| 中文名 | name | 工具 |
| :--- | :--- | :--- |
| 编码工具 | `toolbox-encoding` | base64_*, url_*, jwt_parse, text_to_hex, hex_to_text |
| 文本工具 | `toolbox-text` | naming_style_convert, regex_match, text_lines_process, text_diff_compare |
| 格式工具 | `toolbox-format` | json_*, format_convert, json_to_c_sharp |
| 网络工具 | `toolbox-network` | cron_parse, ip_calculate, endian_*, request_to_curl_* |
| 计算工具 | `toolbox-calculation` | radix_convert, timestamp_from_unix, color_from_hex, unit_*, bitwise_compute, struct_layout_calculate |
| 生成工具 | `toolbox-generate` | uuid_generate |

## 不在 MCP 范围

AI/OCR、目录同步、C# Runner、Socket 服务端、图片预览——用仓库代码，不调 toolbox MCP。
