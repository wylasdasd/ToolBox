---
name: toolbox-encoding
description: >-
  通过 user-toolbox MCP 做 Base64、URL、JWT、Hex 编解码与 Data URI 图片解析。
  适用于 Base64/URL 编码解码、JWT 解析、文本与十六进制互转、Data URI/裸 Base64 识别 mime；
  或 @toolbox-encoding、编码工具。
---

# ToolBox 编码工具（toolbox-encoding）

MCP 服务器：`toolbox`（Cursor 中可能为 `user-toolbox`）

| 任务 | toolName | 关键参数 |
| :--- | :--- | :--- |
| Base64 编码 | `base64_encode` | `plainText`; 可选 `useUrlSafe` |
| Base64 解码 | `base64_decode` | `base64Text`; 可选 `useUrlSafe` |
| URL 编码 | `url_encode` | `plainText`; 可选 `useEscapeDataString` |
| URL 解码 | `url_decode` | `encodedText`; 可选 `useEscapeDataString` |
| JWT 解析 | `jwt_parse` | `token` |
| 文本→Hex | `text_to_hex` | `text`; 可选 `encodingId`（默认 utf-8）、`displayModeId`、`uppercaseHex` |
| Hex→文本 | `hex_to_text` | `hexInput`; 可选 `encodingId` |
| Data URI / 图片 Base64 | `data_uri_image_parse` | `input`（`data:image/...;base64,...` 或裸 Base64） |

## data_uri_image_parse 返回字段

- `DataUrl`：可用于 `<img src>` 的完整 data URL
- `MimeType`：来自 data URI 元数据（裸 Base64 时为 null）
- `DetectedMimeType`：由 Base64 前缀推断（如 `image/jpeg`）
- `IsDataUrlInput`：输入是否已是 data URI

## 注意

- 往返验证：encode 后再 decode，结果应一致。
- JWT 只解析结构，不验证签名；勿在对话中重复实现 JWT 分段逻辑。
- `data_uri_image_parse` 只做解析，不做图片渲染或文件写入。
