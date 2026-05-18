namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalSqlSchemaSmokeTestResult
{
    public bool Success { get; init; }

    public bool ConnectionSucceeded { get; init; }

    public bool RunsTableExists { get; init; }

    public bool ManifestTableExists { get; init; }

    public bool WorkItemsTableExists { get; init; }

    public bool FailuresTableExists { get; init; }

    public bool CheckpointsTableExists { get; init; }

    public bool IdentifierMapsTableExists { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}
