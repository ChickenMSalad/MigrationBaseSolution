namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherOptions
{
    public const string SectionName = "OperationalDispatcher";

    public bool Enabled { get; init; }

    public string WorkerId { get; init; } = "local-dispatcher";

    public int PollingIntervalSeconds { get; init; } = 10;

    public int LeaseCount { get; init; } = 5;

    public bool SimulateExecution { get; init; } = true;
}


