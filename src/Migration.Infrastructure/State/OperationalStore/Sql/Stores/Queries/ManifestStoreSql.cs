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
FROM dbo.MigrationManifestRecords
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
FROM dbo.MigrationManifestRecords
WHERE RunId = @RunId
ORDER BY SequenceNumber
OFFSET @Skip ROWS
FETCH NEXT @Take ROWS ONLY;
""";

    public const string Insert = """
INSERT INTO dbo.MigrationManifestRecords
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
UPDATE dbo.MigrationManifestRecords
SET
    Status = @Status,
    UpdatedAt = @UpdatedAt
WHERE ManifestRecordId = @ManifestRecordId;
""";
}
