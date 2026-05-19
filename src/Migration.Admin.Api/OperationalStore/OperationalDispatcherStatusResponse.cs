namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherStatusResponse
{
    public bool Enabled { get; init; }

    public string WorkerId { get; init; } = string.Empty;

    public int PollingIntervalSeconds { get; init; }

    public int LeaseCount { get; init; }

    public bool SimulateExecution { get; init; }

    public string Mode { get; init; } = string.Empty;
}
