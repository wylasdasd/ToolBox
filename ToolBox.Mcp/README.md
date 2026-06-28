# ToolBox.Mcp

ToolBox 的本地 **stdio MCP Server**（Phase B）。通过 [Model Context Protocol](https://modelcontextprotocol.io/) 把 `ToolBox.Tools` 里的开发小工具暴露给 Cursor / Claude Desktop 等 Agent。

**不包含 HTTP 出站**（`request_to_curl_*` 仅做字符串转换）。

## 前置条件

- .NET 10 SDK
- 已完成 Phase A：`ToolBox.Tools` 与 VM 解耦

## 构建

```bash
dotnet build ToolBox.Mcp/ToolBox.Mcp.csproj -c Release
```

## 多编辑器 MCP 配置

同一 `ToolBox.Mcp.dll`，项目内已提供：

| 编辑器 | 配置文件 |
| :--- | :--- |
| Cursor | [`.cursor/mcp.json`](../.cursor/mcp.json) |
| VS Code Copilot | [`.vscode/mcp.json`](../.vscode/mcp.json) |
| Cline | [`.cline/mcp.json`](../.cline/mcp.json) |
| Codex | [`.codex/config.toml`](../.codex/config.toml) |

Agent Skill 真源：[`.agents/skills/`](../.agents/skills/)（Cline 副本：[`.cline/skills/`](../.cline/skills/)）。

## Cursor 接入

本仓库已包含项目级配置：[`.cursor/mcp.json`](../.cursor/mcp.json)。用 Cursor 打开 `ToolBox` 根目录即可，**一般无需再改路径**。

### 一次性步骤

1. 构建 MCP（改代码后也要重做）：
   ```bash
   dotnet build ToolBox.Mcp/ToolBox.Mcp.csproj -c Release
   ```
2. 用 Cursor 打开文件夹 `C:\a_code\work2\ToolBox`（不是只打开子目录）。
3. **Cursor Settings → MCP**，确认 `toolbox` 为绿点；否则点 Refresh。
4. 在 **Agent** 里试：`用 toolbox 格式化 JSON：{"b":2,"a":1}`

### 完整配置（与 `.cursor/mcp.json` 相同）

若改用手动配置，复制到 **用户级** `C:\Users\<你>\.cursor\mcp.json` 或项目 `.cursor/mcp.json`：

```json
{
  "mcpServers": {
    "toolbox": {
      "command": "C:/Program Files/dotnet/dotnet.exe",
      "args": [
        "C:/a_code/work2/ToolBox/ToolBox.Mcp/bin/Release/net10.0/ToolBox.Mcp.dll"
      ]
    }
  }
}
```

说明：

- `command`：`dotnet` 可执行文件的绝对路径（避免 Cursor 找不到 PATH）。
- `args[0]`：已编译的 `ToolBox.Mcp.dll`（需先 `dotnet build -c Release`）。

开发态备选（每次 run 时自动编译，稍慢）：

```json
{
  "mcpServers": {
    "toolbox": {
      "command": "C:/Program Files/dotnet/dotnet.exe",
      "args": [
        "run",
        "--project",
        "C:/a_code/work2/ToolBox/ToolBox.Mcp/ToolBox.Mcp.csproj",
        "-c",
        "Release",
        "--no-launch-profile"
      ]
    }
  }
}
```

## 架构

```text
Cursor Agent (L6/L7)
    ↓ stdio MCP (L4)
ToolBox.Mcp/Tools/*  ← 薄适配，[McpServerTool]
    ↓
ToolBox.Tools/*Service  ← 业务逻辑（与 Blazor VM 共用）
    ↓
CommonHelp
```

## 已注册 Tool（约 31 个）

| 分类 | Tools |
| :--- | :--- |
| 编码 | `base64_encode`, `base64_decode`, `url_encode`, `url_decode`, `jwt_parse`, `text_to_hex`, `hex_to_text` |
| 文本 | `naming_style_convert`, `regex_match`, `text_lines_process`, `text_diff_compare` |
| 格式 | `json_format`, `json_minify`, `json_validate`, `json_path_query`, `format_convert`, `json_to_csharp` |
| 网络 | `cron_parse`, `ip_calculate`, `endian_value_to_hex`, `endian_hex_to_value`, `request_to_curl_from_raw`, `request_to_curl_from_form` |
| 计算 | `radix_convert`, `timestamp_from_unix`, `color_from_hex`, `unit_convert`, `unit_transfer_time`, `bitwise_compute`, `struct_layout_calculate` |
| 生成 | `uuid_generate` |

Tool 名称以 Cursor 中显示的为准（SDK 可能将 PascalCase 转为 snake_case）。

## 范围

- **包含**：Web 与 MAUI 共通的工具页（见 `WebRouteRegistry` 子集）
- **排除**：AI/OCR、目录同步、C# Runner、Socket/WebSocket 服务端、图片/SVG 预览页

## 故障排查

- MCP 进程日志输出到 **stderr**，不会污染 stdio 协议通道
- 若 Cursor 报找不到 dotnet，在 `command` 中写 dotnet 的绝对路径
- 修改 `ToolBox.Tools` 后需重新 build MCP 项目
