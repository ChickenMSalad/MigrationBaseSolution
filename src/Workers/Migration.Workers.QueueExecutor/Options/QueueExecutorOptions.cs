namespace Migration.Workers.QueueExecutor.Options;

public sealed class QueueExecutorOptions
{
    public const string SectionName = "QueueExecutor";

    public int PollDelaySeconds { get; init; } = 5;

    public int MaxMessagesPerPoll { get; init; } = 1;

    public int VisibilityTimeoutMinutes { get; init; } = 30;

    public bool DeleteMessageWhenRunRecordIsMissing { get; init; } = true;

    /// <summary>
    /// Safety valve for local testing. If false, messages are read and logged but not executed.
    /// </summary>
    public bool ExecuteRuns { get; init; } = true;
}
