namespace Migration.Shared.Configuration.Infrastructure;

public class AzurePathOptions
{
    public string AssetRootPrefix { get; set; } = "assets";
    public string MetadataRootPrefix { get; set; } = "metadata";
    public string LogsRootPrefix { get; set; } = "logs";
    public string ImportsRootPrefix { get; set; } = "imports";
    public string DeltasRootPrefix { get; set; } = "deltas";
    public string Deltas2RootPrefix { get; set; } = "deltas2";
    public string Deltas3RootPrefix { get; set; } = "deltas3";
    public string BaseRootPrefix { get; set; } = "base";
}
