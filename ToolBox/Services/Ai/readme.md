# Service/Ai 功能说明

## 目标
- 在 OCR 场景下统一支持 `OpenAI`、`DeepSeek`、`Gemini`。
- 模型名规范统一按 **OpenAI Chat Completion** 风格传入，不做厂商私有格式转换。

## 目录结构
- `AiProviderKind.cs`：厂商枚举（OpenAI / DeepSeek / Gemini）
- `AiProviderCatalog.cs`：厂商元数据（显示名、默认模型、端点、Key 存储名）
- `IAiApiKeyService.cs` + `AiApiKeyService.cs`：分厂商 API Key 读写
- `AiAskRequest.cs`：统一请求模型
- `IAiAskService.cs` + `SemanticKernelAiAskService.cs`：统一 AI 提问服务（基于 Semantic Kernel）

## 支持厂商与端点
- OpenAI：使用 SK OpenAI 连接器默认端点
- DeepSeek：`https://api.deepseek.com/v1`
- Gemini：`https://generativelanguage.googleapis.com/v1beta/openai/`

以上三家都通过 `Microsoft.SemanticKernel.Connectors.OpenAI` 接口调用，行为保持一致。

## 模型名规范（统一）
- 必须按 OpenAI Chat Completion 模型 ID 输入。
- 允许字符：字母、数字、`.`、`_`、`:`、`-`
- 不允许空格，不再做 `gemini 3 flash -> gemini-3-flash` 这类自动转换。

示例：
- `gpt-4.1-mini`
- `deepseek-chat`
- `gemini-2.5-flash`

## 统一调用流程
1. OCR 识别得到文本。
2. 构建 `AiAskRequest`（Provider / ApiKey / Model / Prompt / OcrText）。
3. `SemanticKernelAiAskService` 根据 Provider 选择端点并发起 Chat Completion。
4. 返回文本结果；内置重试和友好错误信息。

## 重试与错误处理
- 可重试状态：`408/425/429/500/502/503/504`
- 指数退避 + 随机抖动，最多 4 次
- 对 `401/403/404/400/429` 给出更清晰的提示
- 若识别到配额为 0，不继续重试

## API Key 存储
- 每个厂商独立存储：
  - `openai_api_key`
  - `deepseek_api_key`
  - `gemini_api_key`
- 优先使用 `SecureStorage`，不可用时降级到 `Preferences`。

## UI 侧行为（OcrAi 页面）
- 可选择厂商（OpenAI / DeepSeek / Gemini）
- 可输入模型名（统一规范）
- 切换厂商时自动加载该厂商已保存的 API Key
- `保存 Key` 仅保存当前厂商的 Key

## 扩展新厂商
1. 在 `AiProviderKind` 增加枚举值。
2. 在 `AiProviderCatalog` 增加显示名、默认模型、端点、Key 名。
3. UI 中增加厂商选项（如需）。
4. 无需改 `SemanticKernelAiAskService` 主流程（仍走 OpenAI 兼容 Chat Completion）。
