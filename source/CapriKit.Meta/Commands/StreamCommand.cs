using Spectre.Console;
using Spectre.Console.Cli;

namespace CapriKit.Meta.Commands;

internal sealed class StreamCommand : Command<StreamCommand.Settings>
{   
    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.Status()
    .Start("Thinking...", ctx =>
    {
        // Simulate some work
        AnsiConsole.MarkupLine("Doing some work...");
        Thread.Sleep(1000);

        // Update the status and spinner
        ctx.Status("Thinking some more");
        ctx.Spinner(Spinner.Known.Star);
        ctx.SpinnerStyle(Style.Parse("green"));

        // Simulate some work
        AnsiConsole.MarkupLine("Doing some more work...");
        Thread.Sleep(2000);
    });


        AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),    // Task description
                new ProgressBarColumn(),        // Progress bar
                new PercentageColumn(),         // Percentage
                new RemainingTimeColumn(),      // Remaining time
                new SpinnerColumn(),            // Spinner
                new DownloadedColumn(),         // Downloaded
                new TransferSpeedColumn(),      // Transfer speed
            })
            .Start(ctx =>
            {
                var task = ctx.AddTask("[green]WIP #1[/]");
                var ind = ctx.AddTask("indet", false);

                while(!ctx.IsFinished)
                {
                    task.Increment(0.1);
                    Thread.Sleep(100);

                    ind.StartTask();
                    ind.IsIndeterminate = true;                   
                }
            });

        return 0;
    }
    public sealed class Settings : CommandSettings
    {        
    }
}
