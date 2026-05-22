namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Safe cloud-facing deployment profile. This describes the expected hosting
/// profile for the current environment without exposing deployment secrets.
/// </summary>
public sealed record DeploymentProfileDescriptor(
    string EnvironmentName,
    string ProfileName,
    string HostingModel,
    string Region,
    string Sku,
    bool UsesManagedIdentity,
    bool RequiresHttps,
    bool RequiresAuth,
    bool RequiresPrivateNetworking,
    bool EnablesDiagnostics,
    bool EnablesHealthProbes,
    IReadOnlyList<string> RequiredConfigurationKeys,
    IReadOnlyList<string> OptionalConfigurationKeys,
    IReadOnlyList<string> Warnings);

public static class DeploymentHostingModels
{
    public const string LocalDevelopment = "localDevelopment";
    public const string AzureAppService = "azureAppService";
    public const string AzureContainerApps = "azureContainerApps";
    public const string AzureFunctions = "azureFunctions";
    public const string Unknown = "unknown";
}
