using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Migration.Core.Azure.Configuration;

/// <summary>
/// Optional configuration composition helpers for hosts that want the shared Azure runtime config layer.
/// This is intentionally additive and does not alter any host unless Program.cs opts in.
/// </summary>
public static class AzureRuntimeConfigurationCompositionExtensions
{
    public static IConfigurationBuilder AddAzureRuntimeConfigurationComposition(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment hostEnvironment,
        bool includeLocalSettings = false,
        Action<AzureRuntimeConfigurationCompositionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        var options = new AzureRuntimeConfigurationCompositionOptions
        {
            IncludeLocalConfiguration = includeLocalSettings
        };

        configure?.Invoke(options);

        if (options.IncludeSampleConfiguration)
        {
            configurationBuilder.AddJsonFile(
                AzureRuntimeConfigurationSources.GetSamplePath(options.ConfigurationRootPath),
                optional: options.OptionalFiles,
                reloadOnChange: false);
        }

        if (options.IncludeEnvironmentConfiguration)
        {
            configurationBuilder.AddJsonFile(
                AzureRuntimeConfigurationSources.GetEnvironmentPath(options.ConfigurationRootPath, hostEnvironment.EnvironmentName),
                optional: options.OptionalFiles,
                reloadOnChange: false);
        }

        if (options.IncludeLocalConfiguration)
        {
            configurationBuilder.AddJsonFile(
                AzureRuntimeConfigurationSources.GetLocalPath(options.ConfigurationRootPath),
                optional: options.OptionalFiles,
                reloadOnChange: true);
        }

        return configurationBuilder;
    }
}
