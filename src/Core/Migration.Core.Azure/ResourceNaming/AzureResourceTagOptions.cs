namespace Migration.Core.Azure.ResourceNaming;

public sealed class AzureResourceTagOptions
{
    public const string SectionName = "AzureRuntime:ResourceTags";

    public string Owner { get; set; } = "migration-platform";

    public string CostCenter { get; set; } = "migration";

    public string DataClassification { get; set; } = "internal";

    public string OperationalStore { get; set; } = "sql-first";

    public Dictionary<string, string> AdditionalTags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
