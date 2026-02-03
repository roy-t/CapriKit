using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using TrxFileParser;

namespace CapriKit.Meta.Builds;

// TODO: Ensure that the test filters can be passed on so that you can run a single test or single suite
// This works like `dotnet test --treenode-filter /*/*/*/SampleStandardDeviation_OfArray_ReturnsSD`

internal sealed class TestCommand : Command<TestCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;

        var solutionPath = Config.SolutionPath;
        var testResultsDirectory = Config.Outputs.TestDirectory;
        var testResultsFileName = Config.Outputs.TestResultsFileName;
        var testResultsPath = Config.Outputs.TestResultsPath;

        using var logger = BuildLogger.CreateBuildLogger();

        var taskList = new TaskList();
        taskList.AddTask("Restore", DotNetManager.Restore(logger.Writer, solutionPath));
        taskList.AddTask("Build Test", DotNetManager.Build(logger.Writer, solutionPath, WellKnownConfigurations.Test));
        taskList.AddTask("Test", DotNetManager.Test(logger.Writer, solutionPath, WellKnownConfigurations.Test, testResultsDirectory, testResultsFileName));

        var results = taskList.Execute(logger, cancellationToken);

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

        return TaskList.ExitCodeFromResult(results);
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
