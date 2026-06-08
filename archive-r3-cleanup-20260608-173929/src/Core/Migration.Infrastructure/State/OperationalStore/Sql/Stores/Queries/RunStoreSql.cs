namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;

internal static class RunStoreSql
{
    public const string GetById = """
SELECT
    RunId,
    SourceSystem,
    TargetSystem,
    Status,
    CreatedAt,
    StartedAt,
    CompletedAt,
    FailedAt,
    FailureReason
FROM migration.MigrationRuns
WHERE RunId = @RunId;
""";

    public const string Insert = """
INSERT INTO migration.MigrationRuns
(
    RunId,
    SourceSystem,
    TargetSystem,
    Status,
    CreatedAt,
    StartedAt,
    CompletedAt,
    FailedAt,
    FailureReason
)
VALUES
(
    @RunId,
    @SourceSystem,
    @TargetSystem,
    @Status,
    @CreatedAt,
    @StartedAt,
    @CompletedAt,
    @FailedAt,
    @FailureReason
);
""";

    public const string MarkStarted = """
UPDATE migration.MigrationRuns
SET
    Status = @Status,
    StartedAt = @StartedAt
WHERE RunId = @RunId;
""";

    public const string MarkCompleted = """
UPDATE migration.MigrationRuns
SET
    Status = @Status,
    CompletedAt = @CompletedAt
WHERE RunId = @RunId;
""";

    public const string MarkFailed = """
UPDATE migration.MigrationRuns
SET
    Status = @Status,
    FailedAt = @FailedAt,
    FailureReason = @FailureReason
WHERE RunId = @RunId;
""";
}
