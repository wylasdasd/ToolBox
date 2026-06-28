---
name: toolbox-network
description: >-
  通过 user-toolbox MCP 做 Cron、IPv4 子网、大小端、HTTP 转 curl。
  适用于 Cron 表达式、CIDR、字节序、生成 curl（无出站 HTTP）；或 @toolbox-network、网络工具。
---

# ToolBox 网络工具（toolbox-network）

MCP 服务器：`toolbox`

| 任务 | toolName | 关键参数 |
| :--- | :--- | :--- |
| Cron | `cron_parse` | `expression`; 可选 `includeSeconds` |
| IPv4 子网 | `ip_calculate` | `input`（如 `192.168.1.0/24` 或地址+掩码） |
| 数值→Hex | `endian_value_to_hex` | `input`; 可选 `dataTypeId`（如 int32）、`bigEndian`, `inputIsHex` |
| Hex→数值 | `endian_hex_to_value` | `hexInput`; 可选 `dataTypeId`, `bigEndian` |
| Raw→curl | `request_to_curl_from_raw` | `rawRequest`; 可选 `isHttps`, `outputFormat`（curl / powershell） |
| 表单→curl | `request_to_curl_from_form` | `method`, `url`; 可选 `headersText`, `body`, `isHttps`, `outputFormat` |

## 注意

- **无出站 HTTP**；仅做字符串转换，与 App 行为一致。
- `dataTypeId` 常见值：`int8`, `int16`, `int32`, `int64`, `uint8`, `uint16`, `uint32`, `uint64`, `float`, `double` 等（与 ToolBox UI 一致）。
