# AI 服务（ToolBox.Shared + 平台 Key 存储）

## 提供商

- **OpenRouter**：`https://openrouter.ai/api/v1`（多模型网关，含 OpenAI / Gemini / Claude 等）
- **DeepSeek**：`https://api.deepseek.com/v1`（仅文本）
- **Kimi (Moonshot)**：`https://api.moonshot.cn/v1`（OpenAI 兼容；默认 `kimi-k2.6`，支持视觉）
- **Cursor**：`https://api.cursor.com/v1/agents`（Cloud Agents API；默认 `composer-2.5`，支持图片）

## 核心接口

- `IAiChatService` / `AiChatServiceRouter`：按提供商路由到 OpenAI 兼容或 Cursor Agent 实现
- `OpenAiCompatChatService`：OpenAI 兼容 Chat Completions（DeepSeek / OpenRouter / Kimi）
- `CursorCloudAgentChatService`：Cursor Cloud Agents（创建 Agent → 轮询 Run 结果）
- `IAiApiKeyService`：MAUI 用 SecureStorage；Web 用浏览器 localStorage（AES-GCM 加密后存储，按站点 origin 派生密钥）

## 页面

- `/ai-extract`：AI 结构化提取（单条/批量、模板、JSON 输出）

旧路由 `/ocr-ai`、`/ai-ocr`、`/ocr-regex-extract`、`/ocr-regex-batch` 重定向至 `/ai-extract`。
