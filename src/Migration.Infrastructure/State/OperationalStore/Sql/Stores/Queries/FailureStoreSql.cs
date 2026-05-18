namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;

internal static class FailureStoreSql
{
    public const string Insert = """
INSERT INTO dbo.MigrationFailures
(
    FailureId,
    RunId,
    ManifestRecordId,
    WorkItemId,
    FailureType,
    Message,
    Details,
    IsRetriable,
    CreatedAt
)
VALUES
(
    @FailureId,
    @RunId,
    @ManifestRecordId,
    @WorkItemId,
    @FailureType,
    @Message,
    @Details,
    @IsRetriable,
    @CreatedAt
);
""";

    public const string GetByRun = """
SELECT
    FailureId,
    RunId,
    ManifestRecordId,
    WorkItemId,
    FailureType,
    Message,
    Details,
    IsRetriable,
    CreatedAt
FROM dbo.MigrationFailures
WHERE RunId = @RunId
ORDER BY CreatedAt DESC
OFFSET @Skip ROWS
FETCH NEXT @Take ROWS ONLY;
""";

    public const string GetByManifestRecord = """
SELECT
    FailureId,
    RunId,
    ManifestRecordId,
    WorkItemId,
    FailureType,
    Message,
    Details,
    IsRetriable,
    CreatedAt
FROM dbo.MigrationFailures
WHERE ManifestRecordId = @ManifestRecordId
ORDER BY CreatedAt DESC;
""";

    public const string GetByWorkItem = """
SELECT
    FailureId,
    RunId,
    ManifestRecordId,
    WorkItemId,
    FailureType,
    Message,
    Details,
    IsRetriable,
    CreatedAt
FROM dbo.MigrationFailures
WHERE WorkItemId = @WorkItemId
ORDER BY CreatedAt DESC;
""";
}
