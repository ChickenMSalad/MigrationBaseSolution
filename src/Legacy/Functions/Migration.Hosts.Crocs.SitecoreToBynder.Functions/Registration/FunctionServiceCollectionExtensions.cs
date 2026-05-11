using Azure.Storage.Blobs;
using Bynder.Sdk.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Configuration.Workflows;
using Migration.Connectors.Sources.Sitecore.Clients;
using Migration.Connectors.Sources.Sitecore.Configuration;
using Migration.Connectors.Sources.Sitecore.Services;
using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Targets.Bynder.Services;
using Migration.Shared.Configuration.Hosts.Bynder;
using Migration.Shared.Configuration.Hosts.Sitecore;
using Migration.Shared.Runtime;
using Migration.Shared.Storage;
using Stylelabs.M.Sdk.WebClient;
using Stylelabs.M.Sdk.WebClient.Authentication;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Functions.Registration;

public static class FunctionServiceCollectionExtensions
{
    public static IServiceCollection AddCrocsSitecoreToBynderFunctions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BynderOptions>(configuration.GetSection(BynderOptions.SectionName));
        services.Configure<ContentHubOptions>(configuration.GetSection("ContentHub"));
        services.Configure<NodeBynderScriptOptions>(configuration.GetSection("NodeBynderScript"));
        services.Configure<SitecoreHostOptions>(configuration.GetSection("Sitecore"));
        services.Configure<BynderHostOptions>(configuration.GetSection("BynderHost"));
        services.Configure<SitecoreToBynderWorkflowOptions>(configuration.GetSection("Workflow"));
        services.Configure<List<BlobStorageSettings>>(configuration.GetSection("BlobStorage"));

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

        services.AddSingleton<IBynderClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BynderOptions>>().Value;
            return ClientFactory.Create(options.Client);
        });

        services.AddMemoryCache();
        services.AddScoped<Migration.Connectors.Targets.Bynder.Models.ExecutionContextState>();
        services.AddSingleton<INodeBynderAssetClient, NodeBynderAssetClient>();
        services.AddSingleton<ContentHubAssetService>();
        services.AddSingleton<ContentHubDataMigrationService>();
        services.AddSingleton<SitecoreHostPathResolver>();
        services.AddSingleton<AssetResiliencyService>();
        services.AddSingleton<MetapropertyOptionBuilderFactory>();
        services.AddSingleton<MetapropertyOptionBuilderFactoryApi>();
        services.AddSingleton<BynderMetadataPropertiesService>();
        services.AddScoped<BynderDataMigrationBatchService>();
        services.AddScoped<BynderUpdateDataService>();
        services.AddScoped<BynderReportingService>();
        services.AddSingleton<BynderHostPathResolver>();
        return services;
    }
}
