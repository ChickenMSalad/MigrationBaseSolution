namespace Migration.Core.Azure.Topology;

/// <summary>
/// Names the cloud resources expected by an environment without carrying secrets.
/// </summary>
public sealed class AzureRuntimeResourceTopology
{
    public string? ResourceGroupName { get; set; }

    public string? SqlServerName { get; set; }

    public string? SqlDatabaseName { get; set; }

    public string? StorageAccountName { get; set; }

    public string? QueueNamespaceName { get; set; }

    public string? KeyVaultName { get; set; }

    public string? ApplicationInsightsName { get; set; }
}
