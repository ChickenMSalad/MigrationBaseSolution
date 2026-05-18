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
FROM dbo.MigrationRuns
WHERE RunId = @RunId;
""";

    public const string Insert = """
INSERT INTO dbo.MigrationRuns
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
UPDATE dbo.MigrationRuns
SET
    Status = @Status,
    StartedAt = @StartedAt
WHERE RunId = @RunId;
""";

    public const string MarkCompleted = """
UPDATE dbo.MigrationRuns
SET
    Status = @Status,
    CompletedAt = @CompletedAt
WHERE RunId = @RunId;
""";

    public const string MarkFailed = """
UPDATE dbo.MigrationRuns
SET
    Status = @Status,
    FailedAt = @FailedAt,
    FailureReason = @FailureReason
WHERE RunId = @RunId;
""";
}
