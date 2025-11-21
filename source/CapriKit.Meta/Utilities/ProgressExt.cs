using CapriKit.Build;
using Spectre.Console;

namespace CapriKit.Meta.Utilities;

public static class ProgressExt
{
    public static ProgressTask AddAggregateTask(this ProgressContext context, string description, IReadOnlyList<BuildTask> aggregateTask)
    {
        return context.AddTask(description, false, aggregateTask.Count);
    }

    public static BuildTaskResult RunAggregateTask(this ProgressContext context, ProgressTask progress, IReadOnlyList<BuildTask> aggregateTask)
    {
        progress.StartTask();
        progress.State.Update<OutcomeColumn.Outcome>(OutcomeColumn.OutcomeKey, _ => OutcomeColumn.Outcome.Indeterminate);
        foreach (var task in aggregateTask)
        {            
            var result = task();
            if (result.Success)
            {                
                progress.Increment(1.0);
            }
            else
            {
                progress.StopTask();
                progress.State.Update<OutcomeColumn.Outcome>(OutcomeColumn.OutcomeKey, _ => OutcomeColumn.Outcome.Failure);
                return new BuildTaskResult(false, result.Exception);
            }            
        }

        progress.StopTask();
        progress.State.Update<OutcomeColumn.Outcome>(OutcomeColumn.OutcomeKey, _ => OutcomeColumn.Outcome.Success);
        return new BuildTaskResult(true, null);
    }
}
