# Product Context

## 目标用户

- 日常进行 C#/.NET 开发的工程师
- 需要频繁进行文本处理、协议调试、格式转换的开发者

## 主要使用场景

- 编写接口前快速转换请求为 cURL
- 调试 Socket 通讯或文本协议（MAUI）
- 进行正则匹配、JSON 格式化、Base64/URL/JWT 解析
- 快速执行 C# 代码片段验证思路（MAUI）
- 浏览器内快速使用文本/转换类工具（Web，无需安装）

## 交付形态

| 形态 | 入口 | 能力范围 |
|------|------|----------|
| MAUI 桌面/移动 | 本地安装 | 全功能 |
| Web 浏览器 | `dotnet run --project ToolBox.Web` | 文本/计算/部分网络工具 |

## 当前功能版图

**MAUI 全量**

- C# Runner
- 网络：请求转 cURL、Socket、WebSocket
- 计算：位运算、结构体布局、常用转换
- 文本：整理、正则、Base64、URL、JWT、时间戳、JSON、UUID、Diff、SVG、剪贴板序列等
- AI / OCR / 目录同步 / 多任务等桌面专属能力

**Web 已开放**（见 `WebRouteRegistry`）

- 文本：text-lines、regex、diff、base64、url-codec、jwt、json-format、json-to-csharp、timestamp、uuid、base64-image、svg-preview
- 计算：bitwise、struct-layout、converters
- 网络：request-to-curl（部分能力可能受浏览器限制）

**Web 未开放**（导航隐藏，直链 NotFound）

- C# Runner、Socket/WebSocket、AI/OCR、目录同步、剪贴板序列等

## 产品原则

- 轻量、直接、低学习成本
- 输入输出清晰，避免过度装饰
- 优先可用性和开发效率
- Web 不强行移植桌面专属能力，保持体验 honest
