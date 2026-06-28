---
name: toolbox-text
description: >-
  通过 user-toolbox MCP 做命名风格、正则、多行文本、diff。
  适用于 camelCase/snake_case 转换、正则匹配替换、去重排序、两段文本对比；或 @toolbox-text、文本工具。
---

# ToolBox 文本工具（toolbox-text）

MCP 服务器：`toolbox`（Cursor 中可能为 `user-toolbox`）

| 任务 | toolName | 关键参数 |
| :--- | :--- | :--- |
| 命名风格 | `naming_style_convert` | `input` → 返回多种命名形式 |
| 正则 | `regex_match` | `pattern`, `inputText`; 可选 `replacementText`, `ignoreCase`, `multiline`, `singleline`, `matchesOnly` |
| 多行处理 | `text_lines_process` | `text`, `operation`: `deduplicate` \| `sort_asc` \| `sort_desc` \| `remove_empty` \| `trim` |
| 文本 diff | `text_diff_compare` | `text1`, `text2`; 可选 `ignoreWhitespace`, `ignoreCase` |

## 注意

- 正则使用 .NET 引擎语法。
- 多行 `operation` 用小写 snake_case 字符串，与 UI 一致。
