using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MigrationBase.Core.Cloud.Azure.Configuration;

public static class AzureRuntimeConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds the repository's conventional appsettings and environment-variable sources.
    /// Call this from Program.cs only when the host is ready to adopt P5 environment composition.
    /// </summary>
    public static IConfigurationBuilder AddAzureRuntimeConfigurationComposition(
        this IConfigurationBuilder builder,
        IHostEnvironment environment,
        bool includeLocalSettings = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (environment is null)
        {
            throw new ArgumentNullException(nameof(environment));
        }

        string environmentName = string.IsNullOrWhiteSpace(environment.EnvironmentName)
            ? AzureRuntimeEnvironmentProfiles.Development.EnvironmentName
            : environment.EnvironmentName;

        builder
            .AddJsonFile(AzureRuntimeConfigurationSourceNames.BaseSettingsFile, optional: true, reloadOnChange: false)
            .AddJsonFile(string.Format(AzureRuntimeConfigurationSourceNames.EnvironmentSettingsFilePattern, environmentName), optional: true, reloadOnChange: false);

        if (includeLocalSettings)
        {
            builder.AddJsonFile(AzureRuntimeConfigurationSourceNames.LocalSettingsFile, optional: true, reloadOnChange: false);
        }

        builder.AddEnvironmentVariables(AzureRuntimeConfigurationSourceNames.EnvironmentVariablesPrefix);

        return builder;
    }
}
