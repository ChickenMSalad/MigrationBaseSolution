using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Bynder.Sdk.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Migration.Application.Configuration.Workflows;
using Migration.Connectors.Sources.S3.Clients;
using Migration.Connectors.Sources.S3.Configuration;
using Migration.Connectors.Sources.WebDam.Clients;
using Migration.Connectors.Sources.WebDam.Configuration;
using Migration.Connectors.Sources.WebDam.Services;
using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Targets.Bynder.Services;
using Migration.Shared.Configuration.Hosts.Bynder;
using Migration.Shared.Configuration.Hosts.WebDam;
using Migration.Shared.Storage;

namespace Migration.Hosts.WebDamToBynder.Functions.Registration;

public static class FunctionServiceCollectionExtensions
{
    public static IServiceCollection AddWebDamToBynderFunctions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BynderOptions>(configuration.GetSection(BynderOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection("S3"));
        services.Configure<WebDamOptions>(configuration.GetSection(WebDamOptions.SectionName));
        services.Configure<WebDamHostOptions>(configuration.GetSection("WebDamHost"));
        services.Configure<WebDamToBynderWorkflowOptions>(configuration.GetSection("Workflow"));
        services.Configure<List<BlobStorageSettings>>(configuration.GetSection("BlobStorage"));
        services.Configure<BynderHostOptions>(configuration.GetSection("BynderHost"));

        services.AddMemoryCache();
        services.AddScoped<Migration.Connectors.Targets.Bynder.Models.ExecutionContextState>();

        services.AddSingleton<IWebDamTokenStore, InMemoryWebDamTokenStore>();
        services.AddSingleton<WebDamAuthClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WebDamOptions>>();
            var httpClient = new HttpClient { BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute) };
            return new WebDamAuthClient(httpClient, options, sp.GetRequiredService<IWebDamTokenStore>());
        });
        services.AddSingleton<WebDamApiClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WebDamOptions>>();
            var httpClient = new HttpClient { BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute) };
            return new WebDamApiClient(httpClient, sp.GetRequiredService<WebDamAuthClient>(), options);
        });
        services.AddSingleton<WebDamExportService>();
        services.AddSingleton<WebDamExcelExporter>();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<S3Options>>().Value;
            var creds = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region) };
            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                config.ServiceURL = options.ServiceUrl;
                config.ForcePathStyle = true;
            }
            return new AmazonS3Client(creds, config);
        });
        services.AddSingleton<IS3Storage, S3Storage>();
        services.AddSingleton<S3Storage>();

        services.AddSingleton<IBynderClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BynderOptions>>().Value;
            return ClientFactory.Create(options.Client);
        });
        services.AddSingleton<AssetResiliencyService>();
        services.AddSingleton<MetapropertyOptionBuilderFactory>();
        services.AddSingleton<MetapropertyOptionBuilderFactoryApi>();
        services.AddSingleton<BynderMetadataPropertiesService>();
        services.AddScoped<BynderDataMigrationBatchService>();
        services.AddScoped<BynderWebDamMigrationService>();
        services.AddScoped<BynderS3BatchOperationsService>();
        services.AddScoped<BynderUpdateDataService>();
        services.AddScoped<BynderS3UpdateOperationsService>();
        services.AddScoped<BynderS3MetadataOperationsService>();
        services.AddScoped<BynderReportingService>();
        return services;
    }
}
