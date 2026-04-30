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
using Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;
using Migration.Hosts.Ashley.AemToAprimo.Console.Plugins;
using Migration.Hosts.Ashley.AemToAprimo.Console.Runtime;
using Migration.Shared.Configuration.Hosts.Aem;
using Migration.Shared.Configuration.Hosts.Aprimo;
using Migration.Shared.Configuration.Infrastructure;
using Migration.Shared.Files;
using Migration.Shared.Storage;
using Migration.Shared.Workflows.AemToAprimo.Models;
using AemHttpClientDiagnosticsHandler   = Migration.Connectors.Sources.Aem.Utilities.HttpClientDiagnosticsHandler;
using AprimoHttpClientDiagnosticsHandler = Migration.Connectors.Targets.Aprimo.Utilities.HttpClientDiagnosticsHandler;

namespace Migration.Hosts.Ashley.AemToAprimo.Console.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAshleyAemToAprimoHost(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = false;
                options.TimestampFormat = "HH:mm:ss ";
            });
        });

        // Shared plugin/menu infrastructure - consistent with the Crocs and WebDam hosts.
        services.AddSingleton<IConsoleReaderService, ConsoleReaderService>();
        services.AddSingleton<PluginMenuBuilder>();

        // Azure blob service client for the workflow/host to share.
        services.AddSingleton(sp =>
        {
            var cs = configuration["AzureWebJobsStorage"]
                     ?? configuration.GetConnectionString("AzureWebJobsStorage")
                     ?? throw new InvalidOperationException("Missing AzureWebJobsStorage configuration.");
            return new BlobServiceClient(cs);
        });

        // Connector-owned options (bound from top-level sections "Aem" and "Aprimo").
        services.Configure<AemOptions>(configuration.GetSection("Aem"));
        services.Configure<AprimoOptions>(configuration.GetSection("Aprimo"));
        services.Configure<AprimoHttpThrottlingOptions>(configuration.GetSection("AprimoHttpThrottlingOptions"));

        // Workflow + infrastructure options (shared, not connector-specific).
        services.Configure<AemToAprimoWorkflowOptions>(configuration.GetSection("Workflow"));
        services.Configure<AzurePathOptions>(configuration.GetSection("Azure"));
        services.Configure<ExportOptions>(configuration.GetSection("Export"));
        services.Configure<List<BlobStorageSettings>>(configuration.GetSection("BlobStorage"));

        // Host-scoped options - client-prefixed sections via HostSections constants.
        services.Configure<AemHostOptions>(configuration.GetSection(HostSections.AshleyAemSection));
        services.Configure<AprimoHostOptions>(configuration.GetSection(HostSections.AshleyAprimoSection));

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

        services.AddScoped<IPlugin, AemDataMigrationPlugin>();
        services.AddScoped<IPlugin, AprimoDataMigrationPlugin>();

        services.AddScoped<ExecutionContextState>();

        return services;
    }
}
