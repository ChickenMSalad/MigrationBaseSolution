namespace Migration.Domain.Models;

public class MappingHelperObject
{
    public string AemAssetId { get; set; } = string.Empty;
    public string AemAssetPath { get; set; } = string.Empty;
    public string AemCreatedDate { get; set; } = string.Empty;
    public string AemAssetName { get; set; } = string.Empty;
    public string AzureAssetPath { get; set; } = string.Empty;
    public string AzureAssetName { get; set; } = string.Empty;
    public List<string> ImageSets { get; set; } = new();
    public int ImageSetCount { get; set; }
    public string AprimoId { get; set; } = string.Empty;
}
