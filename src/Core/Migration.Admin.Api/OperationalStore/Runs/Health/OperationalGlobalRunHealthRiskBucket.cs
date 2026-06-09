namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthRiskBucket
{
    public string BucketKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public int ScoreContribution { get; init; }

    public int Count { get; init; }

    public string Message { get; init; } = string.Empty;
}


