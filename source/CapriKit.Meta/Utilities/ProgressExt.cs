using CapriKit.Build;
using Spectre.Console;

namespace CapriKit.Meta.Utilities;

public static class ProgressExt
{
    public static ProgressTask AddBuildTask(this ProgressContext context, string description, params IReadOnlyList<BuildTask> steps)
    {
        return context.AddTask(description, false, steps.Count);
    }

    public static BuildTaskResult RunBuildTask(this ProgressContext context, ProgressTask progress, params IReadOnlyList<BuildTask> steps)
    {
        progress.StartTask();
        progress.State.Update<OutcomeColumn.Outcome>(OutcomeColumn.OutcomeKey, _ => OutcomeColumn.Outcome.Indeterminate);
        foreach (var task in steps)
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
        return new BuildTaskResult(true);
    }
}
