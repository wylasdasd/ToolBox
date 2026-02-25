# Memory Bank

该目录用于沉淀长期上下文，参考 Cline 常见的 Memory Bank 工作流。

## 文件说明

- `projectbrief.md`：项目目标、范围、成功标准
- `productContext.md`：用户场景、核心价值、功能地图
- `systemPatterns.md`：架构模式、代码组织、约定规则
- `techContext.md`：技术栈、构建运行命令、环境依赖
- `activeContext.md`：当前工作焦点、最近改动、下一步
- `progress.md`：里程碑、变更日志、待办项

## 建议使用方式

1. 每次开始新任务前，先读 `activeContext.md` + `progress.md`。
2. 若任务影响架构或规则，同步更新 `systemPatterns.md`。
3. 若新增能力或需求变化，同步更新 `productContext.md`。
4. 每个完成的任务在 `progress.md` 记录日期、结果、后续动作。

## 更新原则

- 只记录对后续协作有价值的事实。
- 优先写“决策与原因”，避免纯过程流水账。
- 尽量保持短小、可检索、可持续维护。

