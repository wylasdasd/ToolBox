---
name: toolbox-format
description: >-
  通过 user-toolbox MCP 做 JSON 格式化/校验/Path、格式互转、JSON 转 C#。
  适用于 JSON YAML XML TOML CSV 或生成 C# 模型；或 @toolbox-format、格式工具。
---

# ToolBox 格式工具（toolbox-format）

MCP 服务器：`toolbox`（Cursor 中可能为 `user-toolbox`）

| 任务 | toolName | 关键参数 |
| :--- | :--- | :--- |
| JSON 格式化 | `json_format` | `inputJson`; 可选 `sortKeys`（默认 true） |
| JSON 压缩 | `json_minify` | `inputJson`; 可选 `sortKeys` |
| JSON 校验 | `json_validate` | `inputJson` |
| JSONPath | `json_path_query` | `inputJson`, `jsonPath`（必须以 `$` 开头，如 `$.a.b`） |
| 格式互转 | `format_convert` | `input`, `sourceFormat`, `targetFormat` |
| JSON→C# | `json_to_c_sharp` | `jsonInput`; 可选 `lowerCaseProperties`, `csharpStyleNames`, `nullable`, `numberType`, `detectTypes`, `recordType`, `mergeArrays` |

## format_convert 格式名

`sourceFormat` / `targetFormat` 使用**大写**：`JSON`, `YAML`, `XML`, `TOML`, `CSV`。

## 注意

- 非法 JSON 会返回 `ERROR:`，不要自行 `JsonDocument.Parse` 替代。
- JSON→C# 工具名在 MCP 中为 `json_to_c_sharp`（非 json_to_csharp）。
