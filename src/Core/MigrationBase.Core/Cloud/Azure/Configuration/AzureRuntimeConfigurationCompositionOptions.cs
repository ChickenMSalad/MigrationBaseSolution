namespace MigrationBase.Core.Cloud.Azure.Configuration;

/// <summary>
/// Describes how a cloud host should compose configuration before binding AzureRuntime options.
/// This is metadata/options only; it does not change host startup unless explicitly called from Program.cs.
/// </summary>
public sealed class AzureRuntimeConfigurationCompositionOptions
{
    public const string SectionName = "AzureRuntimeConfiguration";

    public string EnvironmentName { get; set; } = string.Empty;

    public bool IncludeBaseAppSettings { get; set; } = true;

    public bool IncludeEnvironmentAppSettings { get; set; } = true;

    public bool IncludeLocalAppSettings { get; set; }

    public bool IncludeEnvironmentVariables { get; set; } = true;

    public string EnvironmentVariablesPrefix { get; set; } = AzureRuntimeConfigurationSourceNames.EnvironmentVariablesPrefix;

    public bool RequireAzureRuntimeSection { get; set; }

    public bool RequireSqlOperationalStore { get; set; }

    public bool RequireTelemetry { get; set; }
}
