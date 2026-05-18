namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;

internal static class IdentifierMapStoreSql
{
    public const string GetBySourceId = """
SELECT
    IdentifierMapId,
    RunId,
    ManifestRecordId,
    SourceId,
    TargetId,
    TargetPath,
    CreatedAt
FROM migration.MigrationIdentifierMaps
WHERE RunId = @RunId
  AND SourceId = @SourceId;
""";

    public const string GetByManifestRecordId = """
SELECT
    IdentifierMapId,
    RunId,
    ManifestRecordId,
    SourceId,
    TargetId,
    TargetPath,
    CreatedAt
FROM migration.MigrationIdentifierMaps
WHERE ManifestRecordId = @ManifestRecordId;
""";

    public const string Insert = """
INSERT INTO migration.MigrationIdentifierMaps
(
    IdentifierMapId,
    RunId,
    ManifestRecordId,
    SourceId,
    TargetId,
    TargetPath,
    CreatedAt
)
VALUES
(
    @IdentifierMapId,
    @RunId,
    @ManifestRecordId,
    @SourceId,
    @TargetId,
    @TargetPath,
    @CreatedAt
);
""";
}
