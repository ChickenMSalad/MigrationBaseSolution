namespace Migration.Core.Azure.Configuration;

/// <summary>
/// Controls how Azure runtime configuration files are layered for hosts that opt in.
/// </summary>
public sealed class AzureRuntimeConfigurationCompositionOptions
{
    /// <summary>
    /// Base path used for shared Azure runtime configuration files.
    /// </summary>
    public string ConfigurationRootPath { get; set; } = AzureRuntimeConfigurationSources.DefaultConfigurationRootPath;

    /// <summary>
    /// Adds config/azure-runtime/appsettings.AzureRuntime.sample.json when present.
    /// Intended as a template/reference source, not a production secret source.
    /// </summary>
    public bool IncludeSampleConfiguration { get; set; }

    /// <summary>
    /// Adds config/azure-runtime/appsettings.AzureRuntime.Local.json when present.
    /// Intended for local development only.
    /// </summary>
    public bool IncludeLocalConfiguration { get; set; }

    /// <summary>
    /// Adds config/azure-runtime/appsettings.AzureRuntime.{Environment}.json when present.
    /// </summary>
    public bool IncludeEnvironmentConfiguration { get; set; } = true;

    /// <summary>
    /// Allows missing optional files without failing host startup.
    /// </summary>
    public bool OptionalFiles { get; set; } = true;
}
