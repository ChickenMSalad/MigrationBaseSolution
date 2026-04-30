
using CsvHelper.Configuration;

namespace Migration.Connectors.Sources.Aem.Models;

public class AemAsset {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string MimeType { get; set; }
    public long? SizeBytes { get; set; }
    public string Created { get; set; }
    public string LastModified { get; set; }
}

public sealed class AemAssetMap : ClassMap<AemAsset>
{
    public AemAssetMap()
    {
        // Header names match property names exactly
        Map(m => m.Id).Name(nameof(AemAsset.Id));
        Map(m => m.Name).Name(nameof(AemAsset.Name));
        Map(m => m.Path).Name(nameof(AemAsset.Path));
        Map(m => m.MimeType).Name(nameof(AemAsset.MimeType));

        // Nullable long with safe conversion
        Map(m => m.SizeBytes)
            .Name(nameof(AemAsset.SizeBytes))
            .Optional();

        Map(m => m.Created).Name(nameof(AemAsset.Created));
        Map(m => m.LastModified).Name(nameof(AemAsset.LastModified));
    }
}