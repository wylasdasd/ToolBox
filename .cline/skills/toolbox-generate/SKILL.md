---
name: toolbox-generate
description: >-
  通过 user-toolbox MCP 生成 UUID/GUID。
  适用于测试数据、Mock ID、批量生成 GUID；或 @toolbox-generate、生成工具。
---

# ToolBox 生成工具（toolbox-generate）

MCP 服务器：`toolbox`

| 任务 | toolName | 关键参数 |
| :--- | :--- | :--- |
| UUID | `uuid_generate` | 可选 `count`（默认 1）, `includeHyphens`（默认 true）, `upperCase`（默认 false） |

## 注意

- 需要多个 ID 时增大 `count`，不要在对话里手写随机 UUID。
