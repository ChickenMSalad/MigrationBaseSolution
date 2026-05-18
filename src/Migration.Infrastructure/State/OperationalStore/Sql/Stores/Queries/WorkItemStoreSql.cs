namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;

internal static class WorkItemStoreSql
{
    public const string GetById = """
SELECT
    WorkItemId,
    RunId,
    ManifestRecordId,
    Status,
    AttemptCount,
    CreatedAt,
    LockedAt,
    LockedBy,
    CompletedAt,
    FailedAt,
    LastFailureReason
FROM dbo.MigrationWorkItems
WHERE WorkItemId = @WorkItemId;
""";

    public const string Insert = """
INSERT INTO dbo.MigrationWorkItems
(
    WorkItemId,
    RunId,
    ManifestRecordId,
    Status,
    AttemptCount,
    CreatedAt,
    LockedAt,
    LockedBy,
    CompletedAt,
    FailedAt,
    LastFailureReason
)
VALUES
(
    @WorkItemId,
    @RunId,
    @ManifestRecordId,
    @Status,
    @AttemptCount,
    @CreatedAt,
    @LockedAt,
    @LockedBy,
    @CompletedAt,
    @FailedAt,
    @LastFailureReason
);
""";

    public const string MarkLocked = """
UPDATE dbo.MigrationWorkItems
SET
    LockedAt = @LockedAt,
    LockedBy = @LockedBy
WHERE WorkItemId = @WorkItemId;
""";

    public const string MarkCompleted = """
UPDATE dbo.MigrationWorkItems
SET
    Status = @Status,
    CompletedAt = @CompletedAt
WHERE WorkItemId = @WorkItemId;
""";

    public const string MarkFailed = """
UPDATE dbo.MigrationWorkItems
SET
    Status = @Status,
    FailedAt = @FailedAt,
    LastFailureReason = @LastFailureReason
WHERE WorkItemId = @WorkItemId;
""";
}
