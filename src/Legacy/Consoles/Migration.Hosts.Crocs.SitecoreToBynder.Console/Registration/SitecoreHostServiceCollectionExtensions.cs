using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Connectors.Sources.Sitecore.Clients;
using Migration.Connectors.Sources.Sitecore.Configuration;
using Migration.Connectors.Sources.Sitecore.Services;
using Migration.Shared.Runtime;
using Migration.Shared.Storage;
using Stylelabs.M.Sdk.WebClient;
using Stylelabs.M.Sdk.WebClient.Authentication;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Console.Registration;

public static class SitecoreHostServiceCollectionExtensions
{
    public static IServiceCollection AddSitecoreHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var conn = configuration.GetSection("AzureWebJobsStorage").Value
                       ?? configuration.GetConnectionString("AzureWebJobsStorage")
                       ?? throw new InvalidOperationException("AzureWebJobsStorage is required.");
            return new BlobServiceClient(conn);
        });

        services.AddSingleton<IAzureBlobWrapperFactory>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<List<BlobStorageSettings>>>().Value;
            var logger = sp.GetRequiredService<ILogger<AzureBlobWrapperAsync>>();
            return new AzureBlobWrapperFactory(settings, logger);
        });

        services.AddSingleton<IWebMClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ContentHubOptions>>().Value;
            var endpoint = new Uri(options.BaseUrl);
            var oauth = new OAuthPasswordGrant
            {
                ClientId = options.Client.ClientId,
                ClientSecret = options.Client.ClientSecret,
                UserName = options.Client.UserName,
                Password = options.Client.Password
            };
            return MClientFactory.CreateMClient(endpoint, oauth);
        });

        services.AddSingleton<INodeBynderAssetClient, NodeBynderAssetClient>();
        services.AddSingleton<ContentHubAssetService>();
        services.AddSingleton<ContentHubDataMigrationService>();
        services.AddSingleton<SitecoreHostPathResolver>();
        return services;
    }
}
