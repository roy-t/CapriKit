using Spectre.Console;

namespace CapriKit.Meta.Utilities;

public record TaskExecutionResult(string Description, Exception? Exception);

internal class TaskList
{
    private record ProgressableTask(IReadOnlyList<Action> Steps, ProgressTask Progress, string Description);

    private readonly ProgressContext Context;
    private readonly List<ProgressableTask> Tasks;

    public TaskList(ProgressContext context)
    {
        Context = context;
        Tasks = [];
    }

    public void AddTask(string description, params IReadOnlyList<Action> tasks)
    {
        var progress = Context.AddTask(description, false, tasks.Count);
        Tasks.Add(new ProgressableTask(tasks, progress, description));
    }

    public IReadOnlyList<TaskExecutionResult> Execute()
    {
        var results = new List<TaskExecutionResult>();
        foreach (var task in Tasks)
        {
            var exception = Execute(task);
            results.Add(new TaskExecutionResult(task.Description, exception));
            if (exception != null)
            {
                break;
            }
        }

        return results;
    }

    private Exception? Execute(ProgressableTask task)
    {
        var (steps, progress, _) = task;
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
}
