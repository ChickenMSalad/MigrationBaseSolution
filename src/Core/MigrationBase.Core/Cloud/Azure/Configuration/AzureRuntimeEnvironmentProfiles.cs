namespace MigrationBase.Core.Cloud.Azure.Configuration;

/// <summary>
/// Shared environment profile conventions for Azure-hosted migration runtimes.
/// </summary>
public static class AzureRuntimeEnvironmentProfiles
{
    public static AzureRuntimeEnvironmentProfile Local { get; } = new(
        "Local",
        "appsettings.Local.json",
        IsProductionLike: false,
        RequiresManagedIdentity: false,
        RequiresDurableSqlOperationalStore: false,
        RequiresTelemetry: false);

    public static AzureRuntimeEnvironmentProfile Development { get; } = new(
        "Development",
        "appsettings.Development.json",
        IsProductionLike: false,
        RequiresManagedIdentity: false,
        RequiresDurableSqlOperationalStore: true,
        RequiresTelemetry: false);

    public static AzureRuntimeEnvironmentProfile Test { get; } = new(
        "Test",
        "appsettings.Test.json",
        IsProductionLike: false,
        RequiresManagedIdentity: true,
        RequiresDurableSqlOperationalStore: true,
        RequiresTelemetry: true);

    public static AzureRuntimeEnvironmentProfile Production { get; } = new(
        "Production",
        "appsettings.Production.json",
        IsProductionLike: true,
        RequiresManagedIdentity: true,
        RequiresDurableSqlOperationalStore: true,
        RequiresTelemetry: true);

    public static IReadOnlyCollection<AzureRuntimeEnvironmentProfile> All { get; } =
        new[] { Local, Development, Test, Production };

    public static AzureRuntimeEnvironmentProfile Find(string environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return Development;
        }

        foreach (AzureRuntimeEnvironmentProfile profile in All)
        {
            if (string.Equals(profile.EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }
        }

        return new AzureRuntimeEnvironmentProfile(
            environmentName.Trim(),
            string.Format(AzureRuntimeConfigurationSourceNames.EnvironmentSettingsFilePattern, environmentName.Trim()),
            IsProductionLike: false,
            RequiresManagedIdentity: false,
            RequiresDurableSqlOperationalStore: true,
            RequiresTelemetry: false);
    }
}
