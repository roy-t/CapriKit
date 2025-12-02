namespace CapriKit.Build;

public delegate BuildTaskResult BuildTask();

public record class BuildTaskResult(bool Success, Exception? Exception = null);
