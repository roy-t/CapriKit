using Spectre.Console;

namespace CapriKit.Meta.Utilities;

public record TaskExecutionResult(string Description, Exception? Exception);

internal class TaskList
{
    private record ProgressableTask(IReadOnlyList<Action> Steps, string Description);

    private readonly List<ProgressableTask> Tasks;

    public TaskList()
    {
        Tasks = [];
    }

    public void AddTask(string description, params IReadOnlyList<Action> tasks)
    {
        Tasks.Add(new ProgressableTask(tasks, description));
    }

    public IReadOnlyList<TaskExecutionResult> Execute(BuildLogger logger, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLineInterpolated($"Logging to: [link={logger.File.FullName}]{logger.File.FullName}[/]");

        var results = new List<TaskExecutionResult>();

        AnsiConsole.Progress()
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new OutcomeColumn(),
                new SpinnerColumn(),
                ])
            .Start(context =>
            {
                results.AddRange(Execute(context, cancellationToken));
            });

        foreach (var result in results)
        {
            if (result.Exception != null)
            {
                AnsiConsoleExt.ErrorMarkupLineInterpolated($"Task {result.Description} failed");
                AnsiConsole.WriteException(result.Exception);
            }
        }
        return results;
    }

    private List<TaskExecutionResult> Execute(ProgressContext context, CancellationToken cancellationToken)
    {
        var results = new List<TaskExecutionResult>();
        var progressList = new ProgressTask[Tasks.Count];
        for (var i = 0; i < Tasks.Count; i++)
        {
            var task = Tasks[i];
            progressList[i] = context.AddTask(task.Description, false, task.Steps.Count);
        }

        for (var i = 0; i < Tasks.Count; i++)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                var progress = progressList[i];
                var task = Tasks[i];

                var exception = Execute(context, progress, task);
                results.Add(new TaskExecutionResult(task.Description, exception));
                if (exception != null)
                {
                    break;
                }
            }
        }

        return results;
    }

    private static Exception? Execute(ProgressContext context, ProgressTask progress, ProgressableTask task)
    {
        var (steps, _) = task;
        try
        {
            if (steps.Count == 0)
            {
                progress.State.Update<OutcomeColumn.Outcome>(OutcomeColumn.OutcomeKey, _ => OutcomeColumn.Outcome.Skipped);
                return null;
            }

            if (steps.Count == 1)
            {
                progress.IsIndeterminate = true;
            }

            progress.StartTask();
            foreach (var step in steps)
            {
                step();
                progress.Increment(1.0);
            }

            progress.State.Update<OutcomeColumn.Outcome>(OutcomeColumn.OutcomeKey, _ => OutcomeColumn.Outcome.Success);
            return null;
        }
        catch (Exception ex)
        {
            progress.State.Update<OutcomeColumn.Outcome>(OutcomeColumn.OutcomeKey, _ => OutcomeColumn.Outcome.Failure);
            return ex;
        }
        finally
        {
            progress.StopTask();
        }
    }

    public static int ExitCodeFromResult(params IReadOnlyList<TaskExecutionResult> results)
    {
        if (results.Any(r => r.Exception != null))
        {
            return 1;
        }

        return 0;
    }
}
