namespace Migration.Shared.Configuration.Infrastructure;

public sealed class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = "";
    public string ContainerName { get; set; } = "aem-export";
    public string AssetRootPrefix { get; set; } = "assets";
    public string MetadataRootPrefix { get; set; } = "metadata";
    public string LogsRootPrefix { get; set; } = "logs";
    public string ImportsRootPrefix { get; set; } = "imports";
}
