namespace Migration.Infrastructure.Sql.Operational.Readiness;

public sealed class SqlOperationalRuntimeReadinessOptions
{
    public const string SectionName = "SqlOperationalRuntimeReadiness";

    public string? ConnectionString { get; set; }

    public string? ConnectionStringName { get; set; } = "MigrationOperationalStore";

    public string SchemaName { get; set; } = "dbo";

    public string ProjectsTableName { get; set; } = "MigrationProjects";

    public string RunsTableName { get; set; } = "Runs";

    public string ManifestRowsTableName { get; set; } = "ManifestRows";

    public string WorkItemsTableName { get; set; } = "WorkItems";

    public string FailuresTableName { get; set; } = "MigrationFailures";

    public string CheckpointsTableName { get; set; } = "MigrationRunCheckpoints";

    public string IdentifierMappingsTableName { get; set; } = "MigrationAssetMappings";
}
