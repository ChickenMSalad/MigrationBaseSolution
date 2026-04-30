using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;
using Migration.Connectors.Sources.Aem;
using Migration.Connectors.Sources.AzureBlob;
using Migration.Connectors.Sources.LocalStorage.Registration;
using Migration.Connectors.Sources.S3;
using Migration.Connectors.Sources.SharePoint;
using Migration.Connectors.Sources.Sitecore;
using Migration.Connectors.Sources.WebDam.Registration;
using Migration.Connectors.Targets.Aprimo;
using Migration.Connectors.Targets.AzureBlob.Registration;
using Migration.Connectors.Targets.Bynder.Registration;
using Migration.Connectors.Targets.Cloudinary;
using Migration.Connectors.Targets.LocalStorage.Registration;
using Migration.GenericRuntime.Options;
using Migration.Infrastructure.Mapping;
using Migration.Infrastructure.Profiles;
using Migration.Infrastructure.Validation;
using Migration.Manifest.Csv;
using Migration.Manifest.Excel;
using Migration.Manifest.Sql;
using Migration.Manifest.Sqlite;
using Migration.Orchestration.Extensions;

namespace Migration.GenericRuntime.Registration;

public static class GenericMigrationRuntimeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the concrete runtime services required by the generic migration execution path.
    ///
    /// This intentionally lives outside the GenericMigration console host so non-console processes
    /// such as Migration.Admin.Api and Migration.Workers.QueueExecutor can run or validate generic
    /// jobs without depending on a console host project.
    ///
    /// Connector/provider registration can be narrowed by configuration:
    ///
    /// GenericMigrationRuntime:RegisterAllWhenEmpty = false
    /// GenericMigrationRuntime:EnabledSources = [ "LocalStorage" ]
    /// GenericMigrationRuntime:EnabledTargets = [ "LocalStorage" ]
    /// GenericMigrationRuntime:EnabledManifests = [ "Csv" ]
    ///
    /// This is important for workers and API hosts because they should not require Bynder/WebDam/etc.
    /// secrets just to start when the current run only uses LocalStorage.
    /// </summary>
    public static IServiceCollection AddGenericMigrationRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeConsoleProgress = false)
    {
        services.Configure<GenericMigrationRuntimeOptions>(configuration.GetSection(GenericMigrationRuntimeOptions.SectionName));

        var options = configuration
            .GetSection(GenericMigrationRuntimeOptions.SectionName)
            .Get<GenericMigrationRuntimeOptions>() ?? new GenericMigrationRuntimeOptions();

        services.AddMigrationOrchestration(configuration);

        if (includeConsoleProgress)
        {
            services.AddConsoleMigrationProgress();
        }

        AddManifestProviders(services, options);
        AddSourceConnectors(services, configuration, options);
        AddTargetConnectors(services, configuration, options);
        AddMappingAndValidation(services);
        services.AddMigrationPreflight();

        return services;
    }

    private static void AddManifestProviders(IServiceCollection services, GenericMigrationRuntimeOptions options)
    {
        if (IsEnabled(options.EnabledManifests, "Csv", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IManifestProvider, CsvManifestProvider>();
        }

        if (IsEnabled(options.EnabledManifests, "Excel", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IManifestProvider, ExcelManifestProvider>();
        }

        if (IsEnabled(options.EnabledManifests, "Sql", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IManifestProvider, SqlManifestProvider>();
        }

        if (IsEnabled(options.EnabledManifests, "Sqlite", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IManifestProvider, SqliteManifestProvider>();
        }
    }

    private static void AddSourceConnectors(IServiceCollection services, IConfiguration configuration, GenericMigrationRuntimeOptions options)
    {
        if (IsEnabled(options.EnabledSources, "Aem", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IAssetSourceConnector, AemSourceConnector>();
        }

        if (IsEnabled(options.EnabledSources, "Sitecore", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IAssetSourceConnector, SitecoreSourceConnector>();
        }

        if (IsEnabled(options.EnabledSources, "WebDam", options.RegisterAllWhenEmpty))
        {
            services.AddWebDamSourceConnector(configuration);
        }

        if (IsEnabled(options.EnabledSources, "AzureBlob", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IAssetSourceConnector, AzureBlobSourceConnector>();
        }

        if (IsEnabled(options.EnabledSources, "S3", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IAssetSourceConnector, S3SourceConnector>();
        }

        if (IsEnabled(options.EnabledSources, "SharePoint", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IAssetSourceConnector, SharePointSourceConnector>();
        }

        if (IsEnabled(options.EnabledSources, "LocalStorage", options.RegisterAllWhenEmpty))
        {
            services.AddLocalStorageSourceConnector(configuration);
        }
    }

    private static void AddTargetConnectors(IServiceCollection services, IConfiguration configuration, GenericMigrationRuntimeOptions options)
    {
        if (IsEnabled(options.EnabledTargets, "Bynder", options.RegisterAllWhenEmpty))
        {
            services.AddBynderTargetConnector(configuration);
        }

        if (IsEnabled(options.EnabledTargets, "Aprimo", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IAssetTargetConnector, AprimoTargetConnector>();
        }

        if (IsEnabled(options.EnabledTargets, "AzureBlob", options.RegisterAllWhenEmpty))
        {
            services.AddAzureBlobTargetConnector(configuration);
        }

        if (IsEnabled(options.EnabledTargets, "Cloudinary", options.RegisterAllWhenEmpty))
        {
            services.AddSingleton<IAssetTargetConnector, CloudinaryTargetConnector>();
        }

        if (IsEnabled(options.EnabledTargets, "LocalStorage", options.RegisterAllWhenEmpty))
        {
            services.AddLocalStorageTargetConnector(configuration);
        }
    }

    private static void AddMappingAndValidation(IServiceCollection services)
    {
        services.AddSingleton<IMappingProfileLoader, JsonMappingProfileLoader>();
        services.AddSingleton<IMappingValueTransformer, DefaultMappingValueTransformer>();
        services.AddSingleton<IMapper, CanonicalMapper>();

        // This is the legacy/shared application validation step. Orchestration-specific
        // validation steps are registered by AddMigrationOrchestration(...).
        services.AddSingleton<IValidationStep, RequiredFieldValidationStep>();
    }

    private static bool IsEnabled(IReadOnlyCollection<string> enabledNames, string name, bool registerAllWhenEmpty)
    {
        if (enabledNames.Count == 0)
        {
            return registerAllWhenEmpty;
        }

        return enabledNames.Contains(name, StringComparer.OrdinalIgnoreCase);
    }
}
