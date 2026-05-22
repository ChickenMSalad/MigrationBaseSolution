namespace Migration.Infrastructure.Sql.Operational.Readiness;

public sealed class SqlOperationalRuntimeReadinessOptions
{
    public const string SectionName = "SqlOperationalRuntimeReadiness";

    public string? ConnectionString { get; set; }

    public string? ConnectionStringName { get; set; } = "MigrationOperationalStore";

    public string SchemaName { get; set; } = "dbo";

    public string ProjectsTableName { get; set; } = "MigrationProjects";

    public string RunsTableName { get; set; } = "MigrationRuns";

    public string ManifestRowsTableName { get; set; } = "MigrationManifestRows";

    public string WorkItemsTableName { get; set; } = "MigrationWorkItems";

    public string FailuresTableName { get; set; } = "MigrationFailures";

    public string CheckpointsTableName { get; set; } = "MigrationRunCheckpoints";

    public string IdentifierMappingsTableName { get; set; } = "MigrationAssetMappings";
}
