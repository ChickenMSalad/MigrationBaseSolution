namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureCatalogResponse
{
    public IReadOnlyCollection<string> FailureTypes { get; init; } =
        Array.Empty<string>();

    public IReadOnlyCollection<string> RunStatuses { get; init; } =
        Array.Empty<string>();

    public IReadOnlyCollection<string> SourceSystems { get; init; } =
        Array.Empty<string>();

    public IReadOnlyCollection<string> TargetSystems { get; init; } =
        Array.Empty<string>();

    public int FailureTypeCount { get; init; }

    public int RunStatusCount { get; init; }

    public int SourceSystemCount { get; init; }

    public int TargetSystemCount { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}


