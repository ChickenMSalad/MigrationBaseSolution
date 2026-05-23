namespace MigrationBase.Core.Cloud.Azure;

public sealed class AzureArtifactStorageOptions
{
    public string AccountName { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "migration-artifacts";

    public string ImportPrefix { get; set; } = "imports";

    public string ExportPrefix { get; set; } = "exports";

    public string DiagnosticsPrefix { get; set; } = "diagnostics";

    public string TemporaryPrefix { get; set; } = "tmp";

    /// <summary>
    /// Explicitly documents that blob storage must not be used as the operational run system.
    /// </summary>
    public bool OperationalStateMustRemainInSql { get; set; } = true;
}
