using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class DeploymentProfileEndpointExtensions
{
    public static RouteGroupBuilder MapDeploymentProfileEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/deployment-profile", (
                IConfiguration configuration,
                IWebHostEnvironment environment) =>
            {
                var descriptor = BuildDescriptor(configuration, environment);
                return Results.Ok(descriptor);
            })
            .WithName("GetDeploymentProfile")
            .WithTags("Cloud")
            .WithSummary("Gets the safe deployment profile for cloud environment promotion planning.")
            .Produces<DeploymentProfileDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static DeploymentProfileDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var profileName = Read(
            configuration,
            "Cloud:DeploymentProfile",
            environment.IsDevelopment() ? "local-dev" : environment.EnvironmentName.ToLowerInvariant());

        var hostingModel = Read(
            configuration,
            "Cloud:HostKind",
            environment.IsDevelopment()
                ? DeploymentHostingModels.LocalDevelopment
                : DeploymentHostingModels.Unknown);

        var region = Read(configuration, "Cloud:Region", environment.IsDevelopment() ? "local" : "unknown");
        var sku = Read(configuration, "Cloud:Sku", environment.IsDevelopment() ? "local" : "unknown");

        var usesManagedIdentity = ReadBool(
            configuration,
            "Cloud:UsesManagedIdentity",
            string.Equals(configuration["Cloud:CredentialMode"], "managedIdentity", StringComparison.OrdinalIgnoreCase));

        var requiresHttps = ReadBool(configuration, "Cloud:RequiresHttps", !environment.IsDevelopment());
        var requiresAuth = ReadBool(configuration, "Cloud:RequiresAuth", !environment.IsDevelopment());
        var requiresPrivateNetworking = ReadBool(configuration, "Cloud:RequiresPrivateNetworking", false);
        var enablesDiagnostics = ReadBool(configuration, "Cloud:EnablesDiagnostics", true);
        var enablesHealthProbes = ReadBool(configuration, "Cloud:EnablesHealthProbes", true);

        var requiredKeys = BuildRequiredConfigurationKeys(environment, hostingModel, usesManagedIdentity, requiresAuth);
        var optionalKeys = BuildOptionalConfigurationKeys();

        var warnings = BuildWarnings(
            environment,
            profileName,
            hostingModel,
            region,
            sku,
            usesManagedIdentity,
            requiresHttps,
            requiresAuth,
            enablesDiagnostics,
            enablesHealthProbes,
            configuration,
            requiredKeys);

        return new DeploymentProfileDescriptor(
            EnvironmentName: environment.EnvironmentName,
            ProfileName: profileName,
            HostingModel: hostingModel,
            Region: region,
            Sku: sku,
            UsesManagedIdentity: usesManagedIdentity,
            RequiresHttps: requiresHttps,
            RequiresAuth: requiresAuth,
            RequiresPrivateNetworking: requiresPrivateNetworking,
            EnablesDiagnostics: enablesDiagnostics,
            EnablesHealthProbes: enablesHealthProbes,
            RequiredConfigurationKeys: requiredKeys,
            OptionalConfigurationKeys: optionalKeys,
            Warnings: warnings);
    }

    private static IReadOnlyList<string> BuildRequiredConfigurationKeys(
        IWebHostEnvironment environment,
        string hostingModel,
        bool usesManagedIdentity,
        bool requiresAuth)
    {
        if (environment.IsDevelopment())
        {
            return
            [
                "ControlPlane:StorageRoot",
                "MigrationRunQueue:Provider",
                "MigrationRunQueue:QueueName"
            ];
        }

        var keys = new List<string>
        {
            "Cloud:DeploymentProfile",
            "Cloud:HostKind",
            "Cloud:Region",
            "Cloud:ArtifactMode",
            "Cloud:CredentialMode",
            "ControlPlane:StorageRoot",
            "MigrationRunQueue:Provider",
            "MigrationRunQueue:QueueName",
            "Workspace:WorkspaceId"
        };

        if (usesManagedIdentity)
        {
            keys.Add("Cloud:KeyVaultUri");
        }

        if (requiresAuth)
        {
            keys.Add("Auth:Authority");
            keys.Add("Auth:Audience");
        }

        return keys;
    }

    private static IReadOnlyList<string> BuildOptionalConfigurationKeys() =>
    [
        "Cloud:Sku",
        "Cloud:RequiresHttps",
        "Cloud:RequiresAuth",
        "Cloud:RequiresPrivateNetworking",
        "Cloud:EnablesDiagnostics",
        "Cloud:EnablesHealthProbes",
        "Cloud:ServiceBusNamespace",
        "Cloud:QueueStorageAccountName",
        "Cloud:ArtifactContainerName",
        "Cloud:ArtifactStorageAccountName",
        "Workspace:TenantMode",
        "Workspace:TenantEnforced",
        "Workspace:TenantId",
        "Workspace:DisplayName"
    ];

    private static List<string> BuildWarnings(
        IWebHostEnvironment environment,
        string profileName,
        string hostingModel,
        string region,
        string sku,
        bool usesManagedIdentity,
        bool requiresHttps,
        bool requiresAuth,
        bool enablesDiagnostics,
        bool enablesHealthProbes,
        IConfiguration configuration,
        IReadOnlyList<string> requiredKeys)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() &&
            string.Equals(hostingModel, DeploymentHostingModels.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Cloud:HostKind should be configured for non-development deployment profiles.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(region, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Cloud:Region should be configured for non-development deployment profiles.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(sku, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Cloud:Sku should be configured for non-development deployment profiles.");
        }

        if (!environment.IsDevelopment() && !requiresHttps)
        {
            warnings.Add("HTTPS should be required outside development.");
        }

        if (!environment.IsDevelopment() && !requiresAuth)
        {
            warnings.Add("Authentication should be required outside development.");
        }

        if (!environment.IsDevelopment() && !usesManagedIdentity)
        {
            warnings.Add("Managed identity should be preferred outside development.");
        }

        if (!enablesDiagnostics)
        {
            warnings.Add("Diagnostics should be enabled for deployment profiles.");
        }

        if (!enablesHealthProbes)
        {
            warnings.Add("Health probes should be enabled for deployment profiles.");
        }

        foreach (var key in requiredKeys)
        {
            if (string.IsNullOrWhiteSpace(configuration[key]))
            {
                warnings.Add($"Required configuration key '{key}' is not configured.");
            }
        }

        if (string.IsNullOrWhiteSpace(profileName))
        {
            warnings.Add("Cloud:DeploymentProfile is empty.");
        }

        return warnings;
    }

    private static string Read(
        IConfiguration configuration,
        string key,
        string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool ReadBool(
        IConfiguration configuration,
        string key,
        bool fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }
}
