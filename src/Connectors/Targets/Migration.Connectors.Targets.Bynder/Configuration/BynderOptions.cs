
namespace Migration.Connectors.Targets.Bynder.Configuration;

public class BynderOptions
{
    public const string SectionName = "Bynder";
    public required global::Bynder.Sdk.Settings.Configuration Client { get; set; }
    public required string BrandStoreId { get; set; }
}