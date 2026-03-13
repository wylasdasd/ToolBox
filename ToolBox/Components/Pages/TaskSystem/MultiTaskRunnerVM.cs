using Blazing.Mvvm.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ToolBox.Components.Pages.TaskSystem;

public sealed class MultiTaskRunnerVM : ViewModelBase
{
    private readonly List<ScheduledTaskItem> _tasks = [];
    private readonly List<string> _globalLogs = [];
    private readonly object _syncRoot = new();
    private readonly HashSet<int> _runningTaskIds = [];

    private int _nextTaskId = 1;
    private int _maxConcurrency = 2;
    private bool _isSchedulerRunning;
    private string _newTaskName = "Task 1";
    private string _newTaskCode = """
        await Task.Delay(1000);
        Log("task started");
        var sum = Enumerable.Range(1, 100).Sum();
        Log($"sum={sum}");
        sum
        """;
    private int? _selectedTaskId;
    private string _referenceInput = "System.Text.Json";
    private CancellationTokenSource? _schedulerCts;
    private string _globalLogText = string.Empty;
    private string _selectedTaskLogText = string.Empty;
    private string? _errorMessage;

    public int MaxConcurrency
    {
        get => _maxConcurrency;
        set => SetProperty(ref _maxConcurrency, Math.Max(1, value));
    }

    public bool IsSchedulerRunning
    {
        get => _isSchedulerRunning;
        private set => SetProperty(ref _isSchedulerRunning, value);
    }

    public string NewTaskName
    {
        get => _newTaskName;
        set => SetProperty(ref _newTaskName, value);
    }

    public string NewTaskCode
    {
        get => _newTaskCode;
        set => SetProperty(ref _newTaskCode, value);
    }

    public string ReferenceInput
    {
        get => _referenceInput;
        set => SetProperty(ref _referenceInput, value);
    }

    public int? SelectedTaskId
    {
        get => _selectedTaskId;
        set
        {
            if (SetProperty(ref _selectedTaskId, value))
            {
                RefreshSelectedTaskLog();
            }
        }
    }

    public string GlobalLogText
    {
        get => _globalLogText;
        private set => SetProperty(ref _globalLogText, value);
    }

    public string SelectedTaskLogText
    {
        get => _selectedTaskLogText;
        private set => SetProperty(ref _selectedTaskLogText, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public IReadOnlyList<ScheduledTaskItem> Tasks => _tasks;

    public MultiTaskRunnerVM()
    {
        AddTask();
    }

    public void AddTask()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(NewTaskCode))
        {
            ErrorMessage = "任务代码不能为空。";
            return;
        }

        var item = new ScheduledTaskItem
        {
            Id = _nextTaskId++,
            Name = string.IsNullOrWhiteSpace(NewTaskName) ? $"Task {_nextTaskId}" : NewTaskName.Trim(),
            Code = NewTaskCode,
            Status = TaskRunStatus.Pending,
            CreatedAt = DateTime.Now
        };

        _tasks.Add(item);
        SelectedTaskId ??= item.Id;
        NewTaskName = $"Task {_nextTaskId}";
        OnPropertyChanged(nameof(Tasks));
        RefreshSelectedTaskLog();
        AppendGlobalLog($"[Queue] Add {item.Name}#{item.Id}");
    }

    public void RemoveTask(int id)
    {
        var item = _tasks.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return;
        }

        if (item.Status == TaskRunStatus.Running)
        {
            ErrorMessage = "运行中的任务不能直接删除，请先取消。";
            return;
        }

        _tasks.Remove(item);
        if (SelectedTaskId == id)
        {
            SelectedTaskId = _tasks.FirstOrDefault()?.Id;
        }

        OnPropertyChanged(nameof(Tasks));
        RefreshSelectedTaskLog();
        AppendGlobalLog($"[Queue] Remove {item.Name}#{item.Id}");
    }

    public void RequeueTask(int id)
    {
        var item = _tasks.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return;
        }

        if (item.Status == TaskRunStatus.Running)
        {
            return;
        }

        item.Status = TaskRunStatus.Pending;
        item.StartedAt = null;
        item.FinishedAt = null;
        item.ErrorMessage = null;
        item.ResultText = null;
        item.Logs.Clear();
        OnPropertyChanged(nameof(Tasks));
        RefreshSelectedTaskLog();
        AppendGlobalLog($"[Queue] Requeue {item.Name}#{item.Id}");
    }

    public void CancelTask(int id)
    {
        var item = _tasks.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return;
        }

        if (item.Status == TaskRunStatus.Pending)
        {
            item.Status = TaskRunStatus.Canceled;
            OnPropertyChanged(nameof(Tasks));
            AppendGlobalLog($"[Queue] Cancel pending {item.Name}#{item.Id}");
            return;
        }

        if (item.Status != TaskRunStatus.Running)
        {
            return;
        }

        lock (_syncRoot)
        {
            item.CancelCts?.Cancel();
        }
    }

    public async Task StartSchedulerAsync()
    {
        if (IsSchedulerRunning)
        {
            return;
        }

        ErrorMessage = null;
        _schedulerCts = new CancellationTokenSource();
        IsSchedulerRunning = true;
        AppendGlobalLog($"[Scheduler] Start (MaxConcurrency={MaxConcurrency})");

        try
        {
            await RunSchedulerLoopAsync(_schedulerCts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendGlobalLog("[Scheduler] Canceled");
        }
        finally
        {
            IsSchedulerRunning = false;
            lock (_syncRoot)
            {
                _runningTaskIds.Clear();
            }
            OnPropertyChanged(nameof(Tasks));
        }
    }

    public void StopScheduler()
    {
        _schedulerCts?.Cancel();
    }

    public void ClearGlobalLogs()
    {
        _globalLogs.Clear();
        GlobalLogText = string.Empty;
    }

    private async Task RunSchedulerLoopAsync(CancellationToken token)
    {
        var runningTasks = new List<Task>();

        while (!token.IsCancellationRequested)
        {
            runningTasks.RemoveAll(t => t.IsCompleted);

            // 仅当有并发空位时，从 Pending 队列取任务并启动执行。
            while (runningTasks.Count < MaxConcurrency)
            {
                var next = _tasks.FirstOrDefault(t => t.Status == TaskRunStatus.Pending);
                if (next is null)
                {
                    break;
                }

                var execTask = ExecuteTaskAsync(next, token);
                runningTasks.Add(execTask);
            }

            var hasPending = _tasks.Any(t => t.Status == TaskRunStatus.Pending);
            if (!hasPending && runningTasks.Count == 0)
            {
                AppendGlobalLog("[Scheduler] Queue drained");
                break;
            }

            await Task.Delay(150, token);
        }

        await Task.WhenAll(runningTasks);
    }

    private async Task ExecuteTaskAsync(ScheduledTaskItem taskItem, CancellationToken schedulerToken)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(schedulerToken);
        var taskToken = linkedCts.Token;

        lock (_syncRoot)
        {
            _runningTaskIds.Add(taskItem.Id);
            taskItem.CancelCts = linkedCts;
        }

        taskItem.Status = TaskRunStatus.Running;
        taskItem.StartedAt = DateTime.Now;
        taskItem.FinishedAt = null;
        taskItem.ErrorMessage = null;
        taskItem.ResultText = null;
        AppendTaskLog(taskItem, "started");
        AppendGlobalLog($"[Run] {taskItem.Name}#{taskItem.Id} started");
        OnPropertyChanged(nameof(Tasks));

        try
        {
            var options = BuildScriptOptions();
            var globals = new ScriptTaskGlobals(taskItem.Id, msg => AppendTaskLog(taskItem, msg), taskToken);
            // 对常见的 Task.Delay(x) 做协作取消增强，避免“取消按钮无效”的体验。
            var preparedCode = RewriteForCooperativeCancellation(taskItem.Code);
            var result = await CSharpScript.EvaluateAsync(preparedCode, options, globals, cancellationToken: taskToken);
            taskItem.ResultText = result?.ToString();
            taskItem.Status = TaskRunStatus.Completed;
            AppendTaskLog(taskItem, $"completed result={taskItem.ResultText ?? "<null>"}");
            AppendGlobalLog($"[Done] {taskItem.Name}#{taskItem.Id}");
        }
        catch (OperationCanceledException)
        {
            taskItem.Status = TaskRunStatus.Canceled;
            AppendTaskLog(taskItem, "canceled");
            AppendGlobalLog($"[Cancel] {taskItem.Name}#{taskItem.Id}");
        }
        catch (CompilationErrorException ex)
        {
            taskItem.Status = TaskRunStatus.Failed;
            taskItem.ErrorMessage = string.Join(Environment.NewLine, ex.Diagnostics);
            AppendTaskLog(taskItem, "failed: compile error");
            AppendGlobalLog($"[Fail] {taskItem.Name}#{taskItem.Id} compile error");
        }
        catch (Exception ex)
        {
            taskItem.Status = TaskRunStatus.Failed;
            taskItem.ErrorMessage = ex.Message;
            AppendTaskLog(taskItem, $"failed: {ex.Message}");
            AppendGlobalLog($"[Fail] {taskItem.Name}#{taskItem.Id}");
        }
        finally
        {
            taskItem.FinishedAt = DateTime.Now;
            lock (_syncRoot)
            {
                _runningTaskIds.Remove(taskItem.Id);
                taskItem.CancelCts?.Dispose();
                taskItem.CancelCts = null;
            }
            OnPropertyChanged(nameof(Tasks));
            RefreshSelectedTaskLog();
        }
    }

    private ScriptOptions BuildScriptOptions()
    {
        var refs = new List<Assembly>
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Task).Assembly,
            typeof(Console).Assembly,
            typeof(System.Text.Json.JsonSerializer).Assembly,
            typeof(MultiTaskRunnerVM).Assembly
        };

        foreach (var name in ParseReferenceNames(ReferenceInput))
        {
            try
            {
                var asm = Assembly.Load(new AssemblyName(name));
                refs.Add(asm);
            }
            catch
            {
                AppendGlobalLog($"[Ref] Skip invalid assembly name: {name}");
            }
        }

        return ScriptOptions.Default
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Threading",
                "System.Threading.Tasks")
            .WithReferences(refs.Distinct());
    }

    private void AppendGlobalLog(string text)
    {
        _globalLogs.Add($"{DateTime.Now:HH:mm:ss.fff} {text}");
        if (_globalLogs.Count > 800)
        {
            _globalLogs.RemoveRange(0, _globalLogs.Count - 800);
        }

        GlobalLogText = string.Join(Environment.NewLine, _globalLogs);
    }

    private void AppendTaskLog(ScheduledTaskItem item, string text)
    {
        item.Logs.Add($"{DateTime.Now:HH:mm:ss.fff} {text}");
        if (item.Logs.Count > 400)
        {
            item.Logs.RemoveRange(0, item.Logs.Count - 400);
        }

        if (SelectedTaskId == item.Id)
        {
            SelectedTaskLogText = string.Join(Environment.NewLine, item.Logs);
        }
    }

    private void RefreshSelectedTaskLog()
    {
        var item = _tasks.FirstOrDefault(x => x.Id == SelectedTaskId);
        SelectedTaskLogText = item is null ? string.Empty : string.Join(Environment.NewLine, item.Logs);
    }

    private static IReadOnlyList<string> ParseReferenceNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
    }

    private static string RewriteForCooperativeCancellation(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return code;
        }

        // 仅改写单参数 Task.Delay(...)，若原本已有 token 参数则保持不变。
        // 示例：Task.Delay(1000) -> Task.Delay(1000, Token)
        return Regex.Replace(
            code,
            @"Task\s*\.\s*Delay\s*\(\s*([^) ,][^)]*?)\s*\)",
            match =>
            {
                var inner = match.Groups[1].Value.Trim();
                if (inner.Contains(',', StringComparison.Ordinal))
                {
                    return match.Value;
                }

                return $"Task.Delay({inner}, Token)";
            });
    }
}

public sealed class ScheduledTaskItem
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public TaskRunStatus Status { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? ResultText { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Logs { get; } = [];
    public CancellationTokenSource? CancelCts { get; set; }
}

public enum TaskRunStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Canceled
}

public sealed class ScriptTaskGlobals(int taskId, Action<string> logger, CancellationToken token)
{
    public int TaskId { get; } = taskId;
    public CancellationToken Token { get; } = token;

    public void Log(string text) => logger(text);
}
