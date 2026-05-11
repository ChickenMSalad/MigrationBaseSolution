using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Configuration.Workflows;
using Migration.Connectors.Sources.Aem.Clients;
using Migration.Connectors.Sources.Aem.Configuration;
using Migration.Connectors.Sources.Aem.Services;
using Migration.Connectors.Targets.Aprimo.Clients;
using Migration.Connectors.Targets.Aprimo.Configuration;
using Migration.Connectors.Targets.Aprimo.Services;
using Migration.Shared.Configuration.Hosts.Aem;
using Migration.Shared.Configuration.Hosts.Aprimo;
using Migration.Shared.Configuration.Infrastructure;
using Migration.Shared.Storage;
using Migration.Shared.Workflows.AemToAprimo.Models;
using AemHttpClientDiagnosticsHandler = Migration.Connectors.Sources.Aem.Utilities.HttpClientDiagnosticsHandler;
using AprimoHttpClientDiagnosticsHandler = Migration.Connectors.Targets.Aprimo.Utilities.HttpClientDiagnosticsHandler;

namespace Migration.Hosts.Ashley.AemToAprimo.Functions.Registration;

public static class FunctionServiceCollectionExtensions
{
    public static IServiceCollection AddAshleyAemToAprimoFunctions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AemOptions>(configuration.GetSection("Aem"));
        services.Configure<AprimoOptions>(configuration.GetSection("Aprimo"));
        services.Configure<AprimoHttpThrottlingOptions>(configuration.GetSection("AprimoHttpThrottlingOptions"));
        services.Configure<AemToAprimoWorkflowOptions>(configuration.GetSection("Workflow"));
        services.Configure<AzurePathOptions>(configuration.GetSection("Azure"));
        services.Configure<ExportOptions>(configuration.GetSection("Export"));
        services.Configure<List<BlobStorageSettings>>(configuration.GetSection("BlobStorage"));
        services.Configure<AemHostOptions>(configuration.GetSection("Ashley:Aem"));
        services.Configure<AprimoHostOptions>(configuration.GetSection("Ashley:Aprimo"));

        services.AddSingleton<IAzureBlobWrapperFactory>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<List<BlobStorageSettings>>>().Value;
            var logger = sp.GetRequiredService<ILogger<AzureBlobWrapperAsync>>();
            return new AzureBlobWrapperFactory(settings, logger);
        });

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AemOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AprimoOptions>>().Value);

        services.AddHttpClient<IAemClient, AemClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AemOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMinutes(60);
            client.DefaultRequestHeaders.Add("X-Client-Name", "AemClient");
        }).AddHttpMessageHandler(_ => new AemHttpClientDiagnosticsHandler("AemClient"));

        services.AddSingleton<IAprimoAuthClient, AprimoAuthClient>();
        services.AddHttpClient<IAprimoAssetClient, AprimoAssetClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AprimoOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMinutes(60);
            client.DefaultRequestHeaders.Add("X-Client-Name", "AprimoClient");
        }).AddHttpMessageHandler(_ => new AprimoHttpClientDiagnosticsHandler("AprimoClient"));

        services.AddSingleton<AemDataMigrationService>();
        services.AddSingleton<AprimoDataMigrationService>();
        services.AddScoped<Migration.Shared.Workflows.AemToAprimo.Models.ExecutionContextState>();
        return services;
    }
}
