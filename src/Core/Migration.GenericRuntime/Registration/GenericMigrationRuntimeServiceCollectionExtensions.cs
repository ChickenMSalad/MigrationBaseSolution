using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;
using Migration.Connectors.Sources.Aem.Registration;
using Migration.Connectors.Sources.AzureBlob;
using Migration.Connectors.Sources.LocalStorage.Registration;
using Migration.Connectors.Sources.S3;
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
    /// </summary>
    public static IServiceCollection AddGenericMigrationRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeConsoleProgress = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(GenericMigrationRuntimeOptions.SectionName);
        services.Configure<GenericMigrationRuntimeOptions>(section);

        var options = section.Get<GenericMigrationRuntimeOptions>() ?? new GenericMigrationRuntimeOptions();

        var enabledSources = NormalizeEnabledList(
            configuration,
            $"{GenericMigrationRuntimeOptions.SectionName}:EnabledSources",
            options.EnabledSources);

        var enabledTargets = NormalizeEnabledList(
            configuration,
            $"{GenericMigrationRuntimeOptions.SectionName}:EnabledTargets",
            options.EnabledTargets);

        var enabledManifests = NormalizeEnabledList(
            configuration,
            $"{GenericMigrationRuntimeOptions.SectionName}:EnabledManifests",
            options.EnabledManifests);

        services.AddMigrationOrchestration(configuration);

        if (includeConsoleProgress)
        {
            services.AddConsoleMigrationProgress();
        }

        AddManifestProviders(services, enabledManifests, options.RegisterAllWhenEmpty);
        AddSourceConnectors(services, configuration, enabledSources, options.RegisterAllWhenEmpty);
        AddTargetConnectors(services, configuration, enabledTargets, options.RegisterAllWhenEmpty);
        AddMappingAndValidation(services);
        services.AddMigrationPreflight();

        return services;
    }

    private static List<string> NormalizeEnabledList(
        IConfiguration configuration,
        string key,
        IReadOnlyCollection<string>? current)
    {
        var values = new List<string>();

        if (current is not null)
        {
            values.AddRange(current.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        var raw = configuration[key];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            values.AddRange(
                raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return values
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddManifestProviders(
        IServiceCollection services,
        IReadOnlyCollection<string> enabledManifests,
        bool registerAllWhenEmpty)
    {
        if (IsEnabled(enabledManifests, "Csv", registerAllWhenEmpty))
        {
            services.AddSingleton<IManifestProvider, CsvManifestProvider>();
        }

        if (IsEnabled(enabledManifests, "Excel", registerAllWhenEmpty))
        {
            services.AddSingleton<IManifestProvider, ExcelManifestProvider>();
        }

        if (IsEnabled(enabledManifests, "Sql", registerAllWhenEmpty))
        {
            services.AddSingleton<IManifestProvider, SqlManifestProvider>();
        }

        if (IsEnabled(enabledManifests, "Sqlite", registerAllWhenEmpty))
        {
            services.AddSingleton<IManifestProvider, SqliteManifestProvider>();
        }
    }

    private static void AddSourceConnectors(
        IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyCollection<string> enabledSources,
        bool registerAllWhenEmpty)
    {
        if (IsEnabled(enabledSources, "Aem", registerAllWhenEmpty))
        {
            services.AddAemSourceConnector(configuration);
        }

        if (IsEnabled(enabledSources, "Sitecore", registerAllWhenEmpty))
        {
            services.AddSingleton<IAssetSourceConnector, SitecoreSourceConnector>();
        }

        if (IsEnabled(enabledSources, "Bynder", registerAllWhenEmpty))
        {
            services.AddBynderSourceConnector(configuration);
        }

        if (IsEnabled(enabledSources, "WebDam", registerAllWhenEmpty))
        {
            services.AddWebDamSourceConnector(configuration);
        }

        if (IsEnabled(enabledSources, "AzureBlob", registerAllWhenEmpty))
        {
            services.AddSingleton<IAssetSourceConnector, AzureBlobSourceConnector>();
        }

        if (IsEnabled(enabledSources, "S3", registerAllWhenEmpty))
        {
            services.AddSingleton<IAssetSourceConnector, S3SourceConnector>();
        }

        if (IsEnabled(enabledSources, "SharePoint", registerAllWhenEmpty))
        {
            Migration.Connectors.Sources.SharePoint.Registration.SharePointSourceConnectorRegistration
                .AddSharePointSourceConnector(services, configuration);
        }

        if (IsEnabled(enabledSources, "LocalStorage", registerAllWhenEmpty))
        {
            services.AddLocalStorageSourceConnector(configuration);
        }
    }

    private static void AddTargetConnectors(
        IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyCollection<string> enabledTargets,
        bool registerAllWhenEmpty)
    {
        if (IsEnabled(enabledTargets, "Bynder", registerAllWhenEmpty))
        {
            services.AddBynderTargetConnector(configuration);
        }

        if (IsEnabled(enabledTargets, "Aprimo", registerAllWhenEmpty))
        {
            services.AddSingleton<IAssetTargetConnector, AprimoTargetConnector>();
        }

        if (IsEnabled(enabledTargets, "AzureBlob", registerAllWhenEmpty))
        {
            services.AddAzureBlobTargetConnector(configuration);
        }

        if (IsEnabled(enabledTargets, "Cloudinary", registerAllWhenEmpty))
        {
            services.AddSingleton<IAssetTargetConnector, CloudinaryTargetConnector>();
        }

        if (IsEnabled(enabledTargets, "LocalStorage", registerAllWhenEmpty))
        {
            services.AddLocalStorageTargetConnector(configuration);
        }
    }

    private static void AddMappingAndValidation(IServiceCollection services)
    {
        services.AddSingleton<IMappingProfileLoader, JsonMappingProfileLoader>();
        services.AddSingleton<IMappingValueTransformer, DefaultMappingValueTransformer>();
        services.AddSingleton<IMapper, CanonicalMapper>();
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
