namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunAutoFinalizationStatusResponse
{
    public bool Enabled { get; init; }

    public int IntervalSeconds { get; init; }

    public int BatchSize { get; init; }

    public string Mode { get; init; } = string.Empty;
}


