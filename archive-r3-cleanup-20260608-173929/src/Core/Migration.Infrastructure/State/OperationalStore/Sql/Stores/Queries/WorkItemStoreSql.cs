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
FROM migration.MigrationWorkItems
WHERE WorkItemId = @WorkItemId;
""";

    public const string Insert = """
INSERT INTO migration.MigrationWorkItems
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
UPDATE migration.MigrationWorkItems
SET
    LockedAt = @LockedAt,
    LockedBy = @LockedBy
WHERE WorkItemId = @WorkItemId;
""";

    public const string MarkCompleted = """
UPDATE migration.MigrationWorkItems
SET
    Status = @Status,
    CompletedAt = @CompletedAt
WHERE WorkItemId = @WorkItemId;
""";

    public const string MarkFailed = """
UPDATE migration.MigrationWorkItems
SET
    Status = @Status,
    FailedAt = @FailedAt,
    LastFailureReason = @LastFailureReason
WHERE WorkItemId = @WorkItemId;
""";
}
