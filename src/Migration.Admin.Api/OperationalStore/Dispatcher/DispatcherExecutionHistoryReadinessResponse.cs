namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryReadinessResponse
{
    public bool Ready { get; init; }

    public bool ServiceRegistered { get; init; }

    public bool TableExists { get; init; }

    public bool RequiredColumnsExist { get; init; }

    public string SchemaName { get; init; } = string.Empty;

    public IReadOnlyCollection<string> MissingColumns { get; init; } =
        Array.Empty<string>();

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}
