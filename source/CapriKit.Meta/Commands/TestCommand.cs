using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using TrxFileParser;
using static CapriKit.Build.MSBuildManager;

namespace CapriKit.Meta.Commands;

internal sealed class TestCommand : Command<TestCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        MSBuildManager.InitializeMsBuild();
        var (solutionPath, packagePath, testResultsDirectory, testResultsFileName) = BuildUtilities.GatherBuildInputs();
        var testResultsPath = Path.Combine(testResultsDirectory, testResultsFileName);

        using var logger = BuildUtilities.CreateBuildLogger();

        AnsiConsole.MarkupLineInterpolated($"Logging to: [link={logger.File.FullName}]{logger.File.FullName}[/]");

        var results = new List<TaskExecutionResult>();

        var startTime = DateTime.Now;

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
                var taskList = new TaskList(context);
                taskList.AddTask("Restore", DotNetManager.Restore(logger.Writer, solutionPath));
                taskList.AddTask("Build Test", MSBuildManager.BuildSolution(logger.Writer, solutionPath, WellKnownConfigurations.Test, WellKnownTargets.Build));
                taskList.AddTask("Test", DotNetManager.Test(logger.Writer, solutionPath, testResultsDirectory, testResultsFileName));

                results.AddRange(taskList.Execute(cancellationToken));
            });

        foreach (var result in results)
        {
            if (result.Exception != null)
            {
                AnsiConsoleExt.ErrorMarkupLineInterpolated($"Task {result.Description} failed");
                AnsiConsole.WriteException(result.Exception);
            }
        }

        var testFile = new FileInfo(testResultsPath);
        if (!testFile.Exists)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Test report file does not exist: {testResultsPath}");
            return 1;
        }

        if (testFile.LastWriteTime < startTime)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Test report file is out of date: {testResultsPath}");
            return 1;
        }

        var testResults = ParseTrxFile(testResultsPath)
            .OrderBy(t => t.Class)
            .ThenBy(t => t.Test)
            .ThenByDescending(t => t.Duration);

        var table = new Table();
        table.AddColumn("Class");
        table.AddColumn("Test");
        table.AddColumn("Outcome");
        table.AddColumn("Duration");

        foreach (var result in testResults)
        {
            var clazz = new Markup(result.Class);
            var test = new Markup(result.Test);
            var outcome = result.Passed
                ? new Markup($"[bold green]Passed[/]")
                : new Markup($"[bold red]Failed[/]");
            var duration = new Markup($"{result.Duration.TotalMilliseconds:F2} ms");
            table.AddRow(clazz, test, outcome, duration);
        }

        AnsiConsole.Write(table);

        var passed = testResults.Count(t => t.Passed);
        var failed = testResults.Count() - passed;
        AnsiConsole.MarkupLineInterpolated($"Test results: [bold green]{passed} Passed [/], [bold red]{failed} Failed[/]");

        return 0;
    }


    private record TestResult(string Class, string Test, bool Passed, TimeSpan Duration);

    private List<TestResult> ParseTrxFile(string filePath)
    {
        var results = new List<TestResult>();
        var testRun = TrxDeserializer.Deserialize(filePath);
        var executions = testRun.TestDefinitions.UnitTests.ToDictionary(t => t.Execution.Id, t => t.TestMethod);

        foreach (var result in testRun.Results.UnitTestResults)
        {
            if (executions.TryGetValue(result.Id, out var execution))
            {
                var passed = string.Equals(result.Outcome, "Passed", StringComparison.OrdinalIgnoreCase);
                var duration = TimeSpan.Parse(result.Duration);
                results.Add(new TestResult(execution.ClassName, execution.Name, passed, duration));
            }

        }

        return results;
    }

    public sealed class Settings : CommandSettings
    {
    }
}
