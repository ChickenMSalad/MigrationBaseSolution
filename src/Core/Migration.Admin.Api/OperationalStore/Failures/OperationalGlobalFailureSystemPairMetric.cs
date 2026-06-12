namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureSystemPairMetric
{
    public string SourceSystem { get; init; } = string.Empty;

    public string TargetSystem { get; init; } = string.Empty;

    public int Count { get; init; }
}


