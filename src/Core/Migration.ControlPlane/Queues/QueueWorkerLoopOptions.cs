namespace Migration.ControlPlane.Queues;

public sealed class QueueWorkerLoopOptions
{
    public const string SectionName = "QueueWorkerLoop";

    public bool Enabled { get; init; }

    public int MaxMessages { get; init; } = 1;

    public int PollIntervalSeconds { get; init; } = 10;

    public int VisibilityTimeoutSeconds { get; init; } = 300;

    public bool CompleteMessages { get; init; }

    public bool DryRun { get; init; } = true;
}
