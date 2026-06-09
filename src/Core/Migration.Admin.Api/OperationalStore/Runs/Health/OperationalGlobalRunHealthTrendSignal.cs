namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthTrendSignal
{
    public string SignalKey { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public int Weight { get; init; }
}


