---
name: toolbox-calculation
description: >-
  通过 user-toolbox MCP 做进制、Unix 时间戳、颜色、单位换算、位运算、struct 布局。
  适用于进制转换、时间戳、十六进制颜色、存储带宽单位、位运算、C 结构体内存布局；或 @toolbox-calculation、计算工具。
---

# ToolBox 计算工具（toolbox-calculation）

MCP 服务器：`toolbox`

| 任务 | toolName | 关键参数 |
| :--- | :--- | :--- |
| 进制 | `radix_convert` | `value`, `fromBase`（默认 10） |
| Unix 时间戳 | `timestamp_from_unix` | `unixSeconds` 和/或 `unixMilliseconds` |
| 颜色 | `color_from_hex` | `hexDigits`（如 FF0000 或 #FF0000） |
| 单位换算 | `unit_convert` | `input`, `categoryId`, `fromUnitId` |
| 传输时间 | `unit_transfer_time` | `fileSize`, `fileSizeUnitId`, `bandwidth`, `bandwidthUnitId` |
| 位运算 | `bitwise_compute` | `operation`, `operandA`; 可选 `operandB`, `operandC`, `bitWidth`, `isSigned`, `currentResult`, `shiftFromResult` |
| Struct 布局 | `struct_layout_calculate` | `structDefinition`; 可选 `pack` |

## categoryId（unit_convert）

`storage-iec`, `storage-si`, `network-bit`, `network-byte`, `time`, `frequency`, `memory-page`。单位 id 见 ToolBox 单位页（如 `b`, `kib`, `mbps`, `s`）。

## bitwise operation

`AND`, `OR`, `XOR`, `NOT`, `SHL`, `SHR_A`, `SHR_L`。

## 注意

- 位宽、有符号与 UI 选项保持一致后再调用。
- struct 定义传 C/C++ 风格 snippet，与 App 输入格式相同。
