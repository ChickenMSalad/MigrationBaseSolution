namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;

internal static class ManifestStoreSql
{
    public const string GetById = """
SELECT
    ManifestRecordId,
    RunId,
    SequenceNumber,
    SourceId,
    SourcePath,
    SourceName,
    ContentType,
    ContentLength,
    Status,
    CreatedAt,
    UpdatedAt
FROM migration.MigrationManifestRecords
WHERE ManifestRecordId = @ManifestRecordId;
""";

    public const string GetByRun = """
SELECT
    ManifestRecordId,
    RunId,
    SequenceNumber,
    SourceId,
    SourcePath,
    SourceName,
    ContentType,
    ContentLength,
    Status,
    CreatedAt,
    UpdatedAt
FROM migration.MigrationManifestRecords
WHERE RunId = @RunId
ORDER BY SequenceNumber
OFFSET @Skip ROWS
FETCH NEXT @Take ROWS ONLY;
""";

    public const string Insert = """
INSERT INTO migration.MigrationManifestRecords
(
    ManifestRecordId,
    RunId,
    SequenceNumber,
    SourceId,
    SourcePath,
    SourceName,
    ContentType,
    ContentLength,
    Status,
    CreatedAt,
    UpdatedAt
)
VALUES
(
    @ManifestRecordId,
    @RunId,
    @SequenceNumber,
    @SourceId,
    @SourcePath,
    @SourceName,
    @ContentType,
    @ContentLength,
    @Status,
    @CreatedAt,
    @UpdatedAt
);
""";

    public const string UpdateStatus = """
UPDATE migration.MigrationManifestRecords
SET
    Status = @Status,
    UpdatedAt = @UpdatedAt
WHERE ManifestRecordId = @ManifestRecordId;
""";
}
