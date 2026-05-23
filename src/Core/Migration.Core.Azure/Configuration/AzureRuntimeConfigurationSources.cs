namespace Migration.Core.Azure.Configuration;

/// <summary>
/// Shared file names and configuration source conventions for Azure runtime composition.
/// </summary>
public static class AzureRuntimeConfigurationSources
{
    public const string DefaultConfigurationRootPath = "config/azure-runtime";
    public const string SampleFileName = "appsettings.AzureRuntime.sample.json";
    public const string LocalFileName = "appsettings.AzureRuntime.Local.json";

    public static string GetSamplePath(string rootPath) => Combine(rootPath, SampleFileName);

    public static string GetLocalPath(string rootPath) => Combine(rootPath, LocalFileName);

    public static string GetEnvironmentPath(string rootPath, string environmentName)
    {
        var safeEnvironmentName = string.IsNullOrWhiteSpace(environmentName) ? "Production" : environmentName.Trim();
        return Combine(rootPath, $"appsettings.AzureRuntime.{safeEnvironmentName}.json");
    }

    private static string Combine(string rootPath, string fileName)
    {
        var safeRoot = string.IsNullOrWhiteSpace(rootPath) ? DefaultConfigurationRootPath : rootPath.Trim();
        return safeRoot.TrimEnd('/', '\\') + "/" + fileName;
    }
}
