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
    private string _newTaskCode = DefaultSampleCode;
    private int? _editingTaskId;
    private int? _selectedTaskId;
    private int? _logFilterTaskId;
    private string _referenceInput = "System.Text.Json";
    private string _referencePaths = string.Empty;
    private IReadOnlyList<string> _activeReferences = Array.Empty<string>();
    private CancellationTokenSource? _schedulerCts;
    private string _globalLogText = string.Empty;
    private string _allTaskLogsText = string.Empty;
    private string _filteredTaskLogText = string.Empty;
    private string? _errorMessage;

    public sealed record TaskSelectOption(int Id, string Label);

    public const string DefaultSampleCode = """
        await Task.Delay(1000);
        Log("task started");
        var sum = Enumerable.Range(1, 100).Sum();
        Log($"sum={sum}");
        sum
        """;

    public static IReadOnlyList<TaskTemplate> Templates { get; } =
    [
        new("delay-sum", "延迟 + 求和", DefaultSampleCode),
        new("loop-log", "循环日志", """
            for (var i = 1; i <= 5; i++)
            {
                Token.ThrowIfCancellationRequested();
                Log($"step {i}");
                await Task.Delay(500, Token);
            }
            "done"
            """),
        new("json-demo", "JSON 序列化", """
            var obj = new { Name = "ToolBox", At = DateTime.Now };
            var json = System.Text.Json.JsonSerializer.Serialize(obj);
            Log(json);
            json
            """),
    ];

    public int MaxConcurrency
    {
        get => _maxConcurrency;
        set => SetProperty(ref _maxConcurrency, Math.Clamp(value, 1, 32));
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

    public string ReferencePaths
    {
        get => _referencePaths;
        set => SetProperty(ref _referencePaths, value);
    }

    public IReadOnlyList<string> ActiveReferences
    {
        get => _activeReferences;
        private set => SetProperty(ref _activeReferences, value);
    }

    public int? EditingTaskId
    {
        get => _editingTaskId;
        private set
        {
            if (SetProperty(ref _editingTaskId, value))
            {
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(SubmitButtonText));
            }
        }
    }

    public bool IsEditing => EditingTaskId is not null;

    public string SubmitButtonText => IsEditing ? "保存修改" : "加入队列";

    public int? SelectedTaskId
    {
        get => _selectedTaskId;
        set
        {
            if (!SetProperty(ref _selectedTaskId, value))
                return;

            if (value is not null)
                LogFilterTaskId = value;

            OnPropertyChanged(nameof(SelectedTask));
            OnPropertyChanged(nameof(HasSelectedTask));
            OnPropertyChanged(nameof(TaskSelectOptions));
        }
    }

    public int? LogFilterTaskId
    {
        get => _logFilterTaskId;
        set
        {
            if (!SetProperty(ref _logFilterTaskId, value))
                return;

            RefreshFilteredTaskLog();
        }
    }

    public IEnumerable<TaskSelectOption> TaskSelectOptions =>
        _tasks.Select(t => new TaskSelectOption(t.Id, FormatTaskOptionLabel(t)));

    public ScheduledTaskItem? SelectedTask =>
        SelectedTaskId is int id ? _tasks.FirstOrDefault(x => x.Id == id) : null;

    public ScheduledTaskItem? FilteredLogTask =>
        LogFilterTaskId is int id ? _tasks.FirstOrDefault(x => x.Id == id) : null;

    public bool HasSelectedTask => SelectedTask is not null;

    public string GlobalLogText
    {
        get => _globalLogText;
        private set => SetProperty(ref _globalLogText, value);
    }

    public string AllTaskLogsText
    {
        get => _allTaskLogsText;
        private set => SetProperty(ref _allTaskLogsText, value);
    }

    public string FilteredTaskLogText
    {
        get => _filteredTaskLogText;
        private set => SetProperty(ref _filteredTaskLogText, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void ClearError() => ErrorMessage = null;

    public IReadOnlyList<ScheduledTaskItem> Tasks => _tasks;

    public int PendingCount => _tasks.Count(t => t.Status == TaskRunStatus.Pending);
    public int RunningCount => _tasks.Count(t => t.Status == TaskRunStatus.Running);
    public int CompletedCount => _tasks.Count(t => t.Status == TaskRunStatus.Completed);
    public int FailedCount => _tasks.Count(t => t.Status is TaskRunStatus.Failed or TaskRunStatus.Canceled);

    public void NotifyQueueStatsChanged()
    {
        EnsureDefaultSelection();
        RefreshAllTaskLogs();
        OnPropertyChanged(nameof(Tasks));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(RunningCount));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(FailedCount));
        OnPropertyChanged(nameof(SelectedTask));
        OnPropertyChanged(nameof(TaskSelectOptions));
    }

    public void SelectTask(int? id)
    {
        if (id is null)
            return;

        SelectedTaskId = id;
    }

    public static string FormatTaskOptionLabel(ScheduledTaskItem task) =>
        $"#{task.Id} {task.Name} [{GetStatusLabel(task.Status)}]";

    public void LoadTemplate(string code)
    {
        NewTaskCode = code;
        ErrorMessage = null;
    }

    public void ResetNewTaskForm()
    {
        EditingTaskId = null;
        NewTaskName = $"Task {_nextTaskId}";
        NewTaskCode = DefaultSampleCode;
        ErrorMessage = null;
    }

    public void SubmitTask()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(NewTaskCode))
        {
            ErrorMessage = "任务代码不能为空。";
            return;
        }

        if (EditingTaskId is int editId)
        {
            var existing = _tasks.FirstOrDefault(x => x.Id == editId);
            if (existing is null)
            {
                EditingTaskId = null;
                return;
            }

            if (existing.Status == TaskRunStatus.Running)
            {
                ErrorMessage = "运行中的任务不能编辑，请先取消。";
                return;
            }

            existing.Name = string.IsNullOrWhiteSpace(NewTaskName) ? existing.Name : NewTaskName.Trim();
            existing.Code = NewTaskCode;
            if (existing.Status is not TaskRunStatus.Pending)
                RequeueTask(existing.Id);
            else
                NotifyQueueStatsChanged();

            AppendGlobalLog($"[Queue] Update {existing.Name}#{existing.Id}");
            EditingTaskId = null;
            SelectedTaskId = existing.Id;
            NewTaskName = $"Task {_nextTaskId}";
            return;
        }

        var item = new ScheduledTaskItem
        {
            Id = _nextTaskId++,
            Name = string.IsNullOrWhiteSpace(NewTaskName) ? $"Task {_nextTaskId - 1}" : NewTaskName.Trim(),
            Code = NewTaskCode,
            Status = TaskRunStatus.Pending,
            CreatedAt = DateTime.Now
        };

        _tasks.Add(item);
        SelectedTaskId = item.Id;
        NewTaskName = $"Task {_nextTaskId}";
        NotifyQueueStatsChanged();
        AppendGlobalLog($"[Queue] Add {item.Name}#{item.Id}");
    }

    public void EditTask(int id)
    {
        var item = _tasks.FirstOrDefault(x => x.Id == id);
        if (item is null || item.Status == TaskRunStatus.Running)
            return;

        EditingTaskId = id;
        NewTaskName = item.Name;
        NewTaskCode = item.Code;
        SelectedTaskId = id;
        ErrorMessage = null;
    }

    public void DuplicateTask(int id)
    {
        var item = _tasks.FirstOrDefault(x => x.Id == id);
        if (item is null)
            return;

        var copy = new ScheduledTaskItem
        {
            Id = _nextTaskId++,
            Name = $"{item.Name} (copy)",
            Code = item.Code,
            Status = TaskRunStatus.Pending,
            CreatedAt = DateTime.Now
        };
        _tasks.Add(copy);
        SelectedTaskId = copy.Id;
        NotifyQueueStatsChanged();
        AppendGlobalLog($"[Queue] Duplicate {copy.Name}#{copy.Id}");
    }

    public void RemoveTask(int id)
    {
        var item = _tasks.FirstOrDefault(x => x.Id == id);
        if (item is null)
            return;

        if (item.Status == TaskRunStatus.Running)
        {
            ErrorMessage = "运行中的任务不能直接删除，请先取消。";
            return;
        }

        _tasks.Remove(item);
        if (EditingTaskId == id)
            EditingTaskId = null;
        if (SelectedTaskId == id)
            SelectedTaskId = _tasks.FirstOrDefault()?.Id;

        NotifyQueueStatsChanged();
        AppendGlobalLog($"[Queue] Remove {item.Name}#{item.Id}");
    }

    public void RequeueTask(int id)
    {
        var item = _tasks.FirstOrDefault(x => x.Id == id);
        if (item is null || item.Status == TaskRunStatus.Running)
            return;

        item.Status = TaskRunStatus.Pending;
        item.StartedAt = null;
        item.FinishedAt = null;
        item.ErrorMessage = null;
        item.ResultText = null;
        item.Logs.Clear();
        RefreshAllTaskLogs();
        AppendGlobalLog($"[Queue] Requeue {item.Name}#{item.Id}");
    }

    public void CancelTask(int id)
    {
        var item = _tasks.FirstOrDefault(x => x.Id == id);
        if (item is null)
            return;

        if (item.Status == TaskRunStatus.Pending)
        {
            item.Status = TaskRunStatus.Canceled;
            NotifyQueueStatsChanged();
            AppendGlobalLog($"[Queue] Cancel pending {item.Name}#{item.Id}");
            return;
        }

        if (item.Status != TaskRunStatus.Running)
            return;

        lock (_syncRoot)
            item.CancelCts?.Cancel();
    }

    public void MoveTask(int id, int delta)
    {
        var index = _tasks.FindIndex(x => x.Id == id);
        if (index < 0)
            return;

        var item = _tasks[index];
        if (item.Status != TaskRunStatus.Pending)
            return;

        var target = index + delta;
        if (target < 0 || target >= _tasks.Count)
            return;

        if (_tasks[target].Status != TaskRunStatus.Pending)
            return;

        (_tasks[index], _tasks[target]) = (_tasks[target], _tasks[index]);
        NotifyQueueStatsChanged();
    }

    public void ClearCompletedTasks()
    {
        var removed = _tasks.RemoveAll(t => t.Status is TaskRunStatus.Completed or TaskRunStatus.Canceled or TaskRunStatus.Failed);
        if (removed == 0)
            return;

        if (SelectedTaskId is int id && _tasks.All(t => t.Id != id))
            SelectedTaskId = _tasks.FirstOrDefault()?.Id;
        if (EditingTaskId is int editId && _tasks.All(t => t.Id != editId))
            EditingTaskId = null;

        NotifyQueueStatsChanged();
        AppendGlobalLog($"[Queue] Cleared {removed} finished task(s)");
    }

    public void ClearPendingTasks()
    {
        if (IsSchedulerRunning)
        {
            ErrorMessage = "调度运行中，无法清空等待队列。";
            return;
        }

        var removed = _tasks.RemoveAll(t => t.Status == TaskRunStatus.Pending);
        if (removed == 0)
            return;

        if (SelectedTaskId is int id && _tasks.All(t => t.Id != id))
            SelectedTaskId = _tasks.FirstOrDefault()?.Id;

        NotifyQueueStatsChanged();
        AppendGlobalLog($"[Queue] Cleared {removed} pending task(s)");
    }

    public async Task StartSchedulerAsync()
    {
        if (IsSchedulerRunning)
            return;

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
                _runningTaskIds.Clear();
            NotifyQueueStatsChanged();
        }
    }

    public void StopScheduler() => _schedulerCts?.Cancel();

    public void ClearAllTaskLogs()
    {
        foreach (var task in _tasks)
            task.Logs.Clear();

        RefreshAllTaskLogs();
        AppendGlobalLog("[Log] Cleared all task logs");
    }

    public void ClearFilteredTaskLogs()
    {
        if (FilteredLogTask is null)
            return;

        FilteredLogTask.Logs.Clear();
        RefreshAllTaskLogs();
        AppendGlobalLog($"[Log] Cleared logs for {FilteredLogTask.Name}#{FilteredLogTask.Id}");
    }

    public void ClearGlobalLogs()
    {
        _globalLogs.Clear();
        GlobalLogText = string.Empty;
    }

    private void EnsureDefaultSelection()
    {
        if (_tasks.Count == 0)
        {
            SelectedTaskId = null;
            LogFilterTaskId = null;
            return;
        }

        if (SelectedTaskId is null || _tasks.All(t => t.Id != SelectedTaskId))
            SelectedTaskId = _tasks[0].Id;

        if (LogFilterTaskId is null || _tasks.All(t => t.Id != LogFilterTaskId))
            LogFilterTaskId = _tasks[0].Id;
    }

    private void RefreshAllTaskLogs()
    {
        if (_tasks.Count == 0)
        {
            AllTaskLogsText = string.Empty;
            FilteredTaskLogText = string.Empty;
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var task in _tasks)
        {
            sb.AppendLine($"──── {task.Name} #{task.Id} [{GetStatusLabel(task.Status)}] ────");
            if (task.Logs.Count == 0)
                sb.AppendLine("  （暂无输出）");
            else
            {
                foreach (var line in task.Logs)
                    sb.AppendLine($"  {line}");
            }
            sb.AppendLine();
        }

        AllTaskLogsText = sb.ToString().TrimEnd();
        RefreshFilteredTaskLog();
    }

    private void RefreshFilteredTaskLog()
    {
        if (FilteredLogTask is null)
        {
            FilteredTaskLogText = string.Empty;
            return;
        }

        FilteredTaskLogText = FilteredLogTask.Logs.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, FilteredLogTask.Logs);
    }

    public static string GetStatusLabel(TaskRunStatus status) => status switch
    {
        TaskRunStatus.Pending => "等待中",
        TaskRunStatus.Running => "运行中",
        TaskRunStatus.Completed => "完成",
        TaskRunStatus.Failed => "失败",
        TaskRunStatus.Canceled => "已取消",
        _ => status.ToString()
    };

    public static string FormatDuration(DateTime? started, DateTime? finished)
    {
        if (started is null)
            return "—";
        var end = finished ?? DateTime.Now;
        var span = end - started.Value;
        if (span.TotalMilliseconds < 1000)
            return $"{span.TotalMilliseconds:0} ms";
        return $"{span.TotalSeconds:0.##} s";
    }

    private async Task RunSchedulerLoopAsync(CancellationToken token)
    {
        var runningTasks = new List<Task>();

        while (!token.IsCancellationRequested)
        {
            runningTasks.RemoveAll(t => t.IsCompleted);

            while (runningTasks.Count < MaxConcurrency)
            {
                var next = _tasks.FirstOrDefault(t => t.Status == TaskRunStatus.Pending);
                if (next is null)
                    break;

                runningTasks.Add(ExecuteTaskAsync(next, token));
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
        NotifyQueueStatsChanged();

        try
        {
            var options = BuildScriptOptions();
            var globals = new ScriptTaskGlobals(taskItem.Id, msg => AppendTaskLog(taskItem, msg), taskToken);
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
            NotifyQueueStatsChanged();
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
                refs.Add(Assembly.Load(new AssemblyName(name)));
            }
            catch
            {
                AppendGlobalLog($"[Ref] Skip invalid assembly name: {name}");
            }
        }

        foreach (var path in ParseReferencePaths(ReferencePaths))
        {
            if (!File.Exists(path))
            {
                AppendGlobalLog($"[Ref] Skip missing dll: {path}");
                continue;
            }

            try
            {
                refs.Add(Assembly.LoadFrom(path));
            }
            catch (Exception ex)
            {
                AppendGlobalLog($"[Ref] Failed to load dll: {path} ({ex.Message})");
            }
        }

        ActiveReferences = refs
            .Distinct()
            .Select(a => a.FullName ?? a.GetName().Name ?? a.ToString())
            .ToArray();

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
            _globalLogs.RemoveRange(0, _globalLogs.Count - 800);

        GlobalLogText = string.Join(Environment.NewLine, _globalLogs);
    }

    private void AppendTaskLog(ScheduledTaskItem item, string text)
    {
        item.Logs.Add($"{DateTime.Now:HH:mm:ss.fff} {text}");
        if (item.Logs.Count > 400)
            item.Logs.RemoveRange(0, item.Logs.Count - 400);

        RefreshAllTaskLogs();
    }

    private static IReadOnlyList<string> ParseReferenceNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
    }

    private static IReadOnlyList<string> ParseReferencePaths(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
    }

    private static string RewriteForCooperativeCancellation(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return code;

        return Regex.Replace(
            code,
            @"Task\s*\.\s*Delay\s*\(\s*([^) ,][^)]*?)\s*\)",
            match =>
            {
                var inner = match.Groups[1].Value.Trim();
                if (inner.Contains(',', StringComparison.Ordinal))
                    return match.Value;
                return $"Task.Delay({inner}, Token)";
            });
    }
}

public sealed record TaskTemplate(string Id, string Label, string Code);

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
