---
name: toolbox-system
description: >-
  通过 user-toolbox MCP 做目录同步计划预览（不执行磁盘同步）。
  适用于给定源/目标文件清单 JSON、计算待复制/删除文件与字节数；或 @toolbox-system、目录同步预览。
---

# ToolBox 系统预览（toolbox-system）

MCP 服务器：`toolbox`（Cursor 中可能为 `user-toolbox`）

| 任务 | toolName | 关键参数 |
| :--- | :--- | :--- |
| 目录同步计划预览 | `directory_sync_plan` | `sourcePath`, `targetPath`, `deleteExtraFiles`, `sourceFilesJson`, `targetFilesJson` |

## sourceFilesJson / targetFilesJson 格式

JSON **数组**，每项：

```json
{
  "relativePath": "src/foo.txt",
  "length": 1234,
  "lastWriteTimeUtc": "2024-06-01T12:00:00Z"
}
```

空数组 `[]` 表示该侧无文件。

## 返回要点

- `FilesToCopy` / `FilesToDelete`：计划项列表
- `FilesToCopyCount`, `FilesToDeleteCount`, `EstimatedCopyBytes`：预览统计
- `DirectoriesToDelete`：MCP 清单模式下通常为空（无目标目录树输入）

## 刻意不提供

- **不执行** `SyncAsync`、不创建/删除磁盘文件
- 需要真实同步时使用 MAUI **目录同步**页面或 `IDirectorySyncService`

## 注意

- 路径校验与 App 一致：源/目标不能相同或互相嵌套。
- 比较规则：相对路径 + 文件大小 + `LastWriteTimeUtc` 均相同则视为无需复制。
