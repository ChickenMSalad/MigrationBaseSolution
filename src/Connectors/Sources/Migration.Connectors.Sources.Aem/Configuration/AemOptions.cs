
namespace Migration.Connectors.Sources.Aem.Configuration;

public sealed class AemOptions
{
    public string BaseUrl { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string AssetsApiRoot { get; set; } = "/api/assets";
    public string OpenApiAssetRoot { get; set; } = "adobe/folders";
    public string OpenApiFolderRoot { get; set; } = "adobe/folders";
    public string QueryBuilderRoot { get; set; } = "bin/querybuilder.json";
    public string DamRoot { get; set; } = "/content/dam";
    public string AuthType { get; set; } = "Bearer";
    public string TokenOrUser { get; set; } = "";
    public string DeveloperTokenOrUser { get; set; } = "";
    public string? Password { get; set; }
    public int PageSize { get; set; } = 200;
}
