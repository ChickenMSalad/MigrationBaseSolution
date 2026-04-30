using Stylelabs.M.Sdk.WebClient.Authentication;

namespace Migration.Connectors.Sources.Sitecore.Configuration;

public class ContentHubOptions
{
    public required OAuthPasswordGrant Client { get; set; }
    public required string BaseUrl { get; set; }
}
