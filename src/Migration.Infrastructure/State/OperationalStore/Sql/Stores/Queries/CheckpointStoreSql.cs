namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;

internal static class CheckpointStoreSql
{
    public const string Get = """
SELECT
    CheckpointId,
    RunId,
    CheckpointName,
    CheckpointValue,
    CreatedAt,
    UpdatedAt
FROM dbo.MigrationCheckpoints
WHERE RunId = @RunId
  AND CheckpointName = @CheckpointName;
""";

    public const string GetByRun = """
SELECT
    CheckpointId,
    RunId,
    CheckpointName,
    CheckpointValue,
    CreatedAt,
    UpdatedAt
FROM dbo.MigrationCheckpoints
WHERE RunId = @RunId
ORDER BY CheckpointName;
""";

    public const string Upsert = """
MERGE dbo.MigrationCheckpoints AS target
USING
(
    SELECT
        @RunId AS RunId,
        @CheckpointName AS CheckpointName
) AS source
ON target.RunId = source.RunId
AND target.CheckpointName = source.CheckpointName

WHEN MATCHED THEN
    UPDATE SET
        CheckpointValue = @CheckpointValue,
        UpdatedAt = @UpdatedAt

WHEN NOT MATCHED THEN
    INSERT
    (
        CheckpointId,
        RunId,
        CheckpointName,
        CheckpointValue,
        CreatedAt,
        UpdatedAt
    )
    VALUES
    (
        @CheckpointId,
        @RunId,
        @CheckpointName,
        @CheckpointValue,
        @CreatedAt,
        @UpdatedAt
    );
""";
}
