# 多任务系统（MultiTaskRunner）实现说明

路由：`/multi-task-runner`

## 文件结构

| 文件 | 职责 |
| --- | --- |
| `MultiTaskRunner.razor` | 页面布局：工具栏、新建任务/引用配置 Tab、任务队列表、右侧日志与详情 |
| `MultiTaskRunnerVM.cs` | 队列管理、并发调度、Roslyn 脚本执行、日志 |
| `MultiTaskRunner.razor.css` | 深色日志面板、选中行高亮、代码输入区等样式 |

ViewModel 与 Razor 同目录，遵循项目「组件 + VM 同文件夹」约定。

## 整体架构

```
用户提交 C# 脚本 → 入队 (Pending)
       ↓
StartSchedulerAsync → RunSchedulerLoopAsync（轮询 + 并发槽位）
       ↓
ExecuteTaskAsync → CSharpScript.EvaluateAsync（Roslyn）
       ↓
Completed / Failed / Canceled + 日志 / 返回值
```

- **UI**：Blazor + MudBlazor + Blazing.Mvvm（`MvvmComponentBase<MultiTaskRunnerVM>`）
- **脚本引擎**：`Microsoft.CodeAnalysis.CSharp.Scripting`（NuGet `Microsoft.CodeAnalysis.CSharp.Scripting`）
- **并发**：`MaxConcurrency`（默认 2，范围 1–32）；为 1 时等价串行

## 数据模型

定义在 `MultiTaskRunnerVM.cs` 末尾：

- **`ScheduledTaskItem`**：单条任务（Id、Name、Code、Status、时间戳、ResultText、ErrorMessage、Logs、CancelCts）
- **`TaskRunStatus`**：`Pending` → `Running` → `Completed` / `Failed` / `Canceled`
- **`ScriptTaskGlobals`**：注入脚本的 globals（`TaskId`、`Token`、`Log(string)`）
- **`TaskTemplate`**：快速模板（延迟求和、循环日志、JSON 演示）

## 调度器

`RunSchedulerLoopAsync` 核心逻辑：

1. 每 150ms 轮询一次（`Task.Delay(150, token)`）
2. 在 `runningTasks.Count < MaxConcurrency` 时，从队列取**第一个** `Pending` 任务启动 `ExecuteTaskAsync`
3. 无 `Pending` 且无运行中任务时结束，写 `[Scheduler] Queue drained`
4. 停止调度：`StopScheduler()` → 取消 `_schedulerCts`，运行中任务通过 linked token 协作取消

单任务执行流程（`ExecuteTaskAsync`）：

1. `CancellationTokenSource.CreateLinkedTokenSource(schedulerToken)` 绑定调度器与单任务取消
2. 状态置 `Running`，记录 `StartedAt`
3. `BuildScriptOptions()` 组装引用 → `RewriteForCooperativeCancellation(code)` → `CSharpScript.EvaluateAsync`
4. 成功：最后一行表达式值写入 `ResultText`；编译错误走 `CompilationErrorException`
5. `finally`：写 `FinishedAt`，释放 `CancelCts`

## 脚本 API（任务代码内可用）

| 符号 | 说明 |
| --- | --- |
| `Log("...")` | 追加到该任务日志，并刷新「全部任务日志 / 单任务日志」 |
| `Token` | `CancellationToken`，取消任务或停止调度时触发 |
| `TaskId` | 当前任务 ID |
| 最后一行表达式 | 作为脚本返回值（`ResultText`） |

默认 imports：`System`、`System.Linq`、`System.Collections.Generic`、`System.Threading`、`System.Threading.Tasks`。

默认引用：`mscorlib` 相关、`System.Text.Json`、当前程序集；可在「引用配置」追加程序集名或 DLL 路径。

### 协作取消

`RewriteForCooperativeCancellation` 用正则把无第二参数的 `Task.Delay(ms)` 改写为 `Task.Delay(ms, Token)`。

循环等长耗时逻辑需手动调用：

```csharp
Token.ThrowIfCancellationRequested();
```

## 引用配置

- **程序集名称**：逗号 / 分号 / 换行分隔，`Assembly.Load(AssemblyName)`
- **DLL 路径**：每行一个，`Assembly.LoadFrom(path)`（文件不存在或加载失败会写入调度日志）
- 每次执行任务时重新 `BuildScriptOptions()`；成功加载的列表显示在「上次执行已加载」

## 队列操作

| 操作 | 说明 |
| --- | --- |
| 编辑 / 复制 / 删除 | 非 `Running` 状态可用 |
| 上移 / 下移 | 仅相邻 `Pending` 任务可交换顺序 |
| 重排队 | 清空结果与日志，回到 `Pending` |
| 取消 | `Pending` 直接标 `Canceled`；`Running` 调用 `CancelCts.Cancel()` |
| 清空等待 / 清除已完成 | 批量移除对应状态任务 |

## UI 布局要点

- **左侧（lg=7）**：Tab「新建任务」「引用配置」+ 任务队列表（行点击选中，操作列 `@onclick:stopPropagation`）
- **右侧（lg=5）**：Tab「调度日志」「全部任务日志」「单任务日志」「任务详情」
- 统计 Chip：等待 / 运行 / 完成 / 异常（失败 + 已取消）

日志上限：全局 800 行、单任务 400 行，超出从头部裁剪。

## 复用到其他项目

1. 复制上述三个文件，命名空间改为目标项目
2. 添加 NuGet：`Microsoft.CodeAnalysis.CSharp.Scripting`、`Blazing.Mvvm`、`MudBlazor`
3. 注册路由 `@page "/multi-task-runner"`
4. 若需剪贴板等，按宿主实现 `IClipboardService`（本页不依赖剪贴板）

核心可只抽 `MultiTaskRunnerVM.cs` 中的调度 + `ExecuteTaskAsync` + `BuildScriptOptions` + 模型类型，UI 可换成 WPF / WinForms / 纯控制台。
