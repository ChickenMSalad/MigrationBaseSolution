namespace Migration.Core.Azure.ResourceNaming;

public sealed class AzureResourceNamingOptions
{
    public const string SectionName = "AzureRuntime:ResourceNaming";

    public string Organization { get; set; } = "migration";

    public string Application { get; set; } = "base";

    public string Environment { get; set; } = "dev";

    public string RegionCode { get; set; } = "eus";

    public string Separator { get; set; } = "-";

    public bool IncludeRegionInNames { get; set; } = true;

    public Dictionary<string, string> ResourceTypeTokens { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["resourceGroup"] = "rg",
        ["appServicePlan"] = "asp",
        ["webApp"] = "app",
        ["functionApp"] = "func",
        ["containerApp"] = "ca",
        ["containerAppsEnvironment"] = "cae",
        ["storageAccount"] = "st",
        ["keyVault"] = "kv",
        ["sqlServer"] = "sql",
        ["sqlDatabase"] = "sqldb",
        ["applicationInsights"] = "appi",
        ["logAnalyticsWorkspace"] = "law",
        ["serviceBusNamespace"] = "sb",
        ["queue"] = "q"
    };
}
