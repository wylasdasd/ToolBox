---
name: toolbox-encoding
description: >-
  通过 user-toolbox MCP 做 Base64、URL、JWT、Hex 编解码。
  适用于 Base64/URL 编码解码、JWT 解析、文本与十六进制互转；或 @toolbox-encoding、编码工具。
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

## 注意

- 往返验证：encode 后再 decode，结果应一致。
- JWT 只解析结构，不验证签名；勿在对话中重复实现 JWT 分段逻辑。
