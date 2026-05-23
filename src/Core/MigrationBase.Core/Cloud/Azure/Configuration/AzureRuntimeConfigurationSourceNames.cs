namespace MigrationBase.Core.Cloud.Azure.Configuration;

/// <summary>
/// Conventional configuration source names used by Azure-hosted migration runtimes.
/// This class is intentionally constants-only so hosts can adopt the conventions
/// without pulling in Azure SDK dependencies.
/// </summary>
public static class AzureRuntimeConfigurationSourceNames
{
    public const string BaseSettingsFile = "appsettings.json";
    public const string EnvironmentSettingsFilePattern = "appsettings.{0}.json";
    public const string LocalSettingsFile = "appsettings.Local.json";
    public const string CloudRuntimeSection = "AzureRuntime";
    public const string EnvironmentVariablesPrefix = "MIGRATIONBASE_";
}
