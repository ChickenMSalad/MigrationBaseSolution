namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureQuery
{
    public Guid? RunId { get; init; }

    public string? FailureType { get; init; }

    public bool? IsRetriable { get; init; }

    public string? SourceSystem { get; init; }

    public string? TargetSystem { get; init; }

    public string? SearchText { get; init; }

    public int Limit { get; init; } = 50;
}
