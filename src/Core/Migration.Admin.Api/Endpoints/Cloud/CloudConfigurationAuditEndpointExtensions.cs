using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class CloudConfigurationAuditEndpointExtensions
{
    public static RouteGroupBuilder MapCloudConfigurationAuditEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/configuration-audit", (
                IConfiguration configuration,
                IWebHostEnvironment environment) =>
            {
                var descriptor = BuildDescriptor(configuration, environment);
                return Results.Ok(descriptor);
            })
            .WithName("GetCloudConfigurationAudit")
            .WithTags("Cloud")
            .WithSummary("Gets a safe cloud configuration audit without exposing configuration values.")
            .Produces<CloudConfigurationAuditDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static CloudConfigurationAuditDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var keys = BuildKeys(environment)
            .Select(item => item with
            {
                IsConfigured = !string.IsNullOrWhiteSpace(configuration[item.Key])
            })
            .ToArray();

        var warnings = BuildWarnings(configuration, environment, keys);

        var configuredCount = keys.Count(x => x.IsConfigured);
        var missingCount = keys.Count(x => x.IsRequiredForCloud && !x.IsConfigured);

        return new CloudConfigurationAuditDescriptor(
            EnvironmentName: environment.EnvironmentName,
            MaturityLevel: InferMaturityLevel(environment, keys, warnings),
            ConfiguredCount: configuredCount,
            MissingCount: missingCount,
            WarningCount: warnings.Count,
            Keys: keys,
            Warnings: warnings);
    }

    private static IReadOnlyList<CloudConfigurationKeyAuditDescriptor> BuildKeys(
        IWebHostEnvironment environment) =>
    [
        Required("Cloud:DeploymentProfile", "deployment", "Set to local-dev/dev/test/prod."),
        Required("Cloud:HostKind", "deployment", "Set expected host kind such as azureAppService or azureContainerApps."),
        Required("Cloud:Region", "deployment", "Set the Azure region for non-development deployments."),
        Optional("Cloud:Sku", "deployment", "Set the intended Azure SKU for the deployment profile."),
        Optional("Cloud:RequiresHttps", "deployment", "Require HTTPS outside development."),
        Optional("Cloud:RequiresAuth", "deployment", "Require auth outside development."),
        Optional("Cloud:RequiresPrivateNetworking", "deployment", "Declare private networking requirement."),
        Optional("Cloud:EnablesDiagnostics", "deployment", "Keep diagnostics enabled."),
        Optional("Cloud:EnablesHealthProbes", "deployment", "Keep health probes enabled."),

        Required("Workspace:WorkspaceId", "workspace", "Use explicit workspace id outside development."),
        Optional("Workspace:DisplayName", "workspace", "Human-readable workspace name."),
        Optional("Workspace:TenantMode", "workspace", "Set development/singleTenant/multiTenant."),
        Optional("Workspace:TenantEnforced", "workspace", "Enable when auth tenant validation exists."),
        Optional("Workspace:TenantId", "workspace", "Set once tenant enforcement is enabled."),

        Required("ControlPlane:StorageRoot", "storage", "Use local root in development and blob-backed root in cloud."),
        Required("Cloud:ArtifactMode", "artifacts", "Use localFileSystem in dev and azureBlob in cloud."),
        Optional("Cloud:ArtifactContainerName", "artifacts", "Azure Blob container for artifacts."),
        Optional("Cloud:ArtifactStorageAccountName", "artifacts", "Azure Storage account for artifacts."),
        Optional("Artifacts:BlobContainerName", "artifacts", "Alternative artifact container key."),

        Required("Cloud:CredentialMode", "credentials", "Use userSecrets locally and keyVault/managedIdentity in cloud."),
        Optional("Cloud:KeyVaultName", "credentials", "Azure Key Vault name."),
        Optional("Cloud:KeyVaultUri", "credentials", "Azure Key Vault URI."),
        Optional("KeyVault:Name", "credentials", "Alternative Key Vault name."),
        Optional("KeyVault:Uri", "credentials", "Alternative Key Vault URI."),

        Required("MigrationRunQueue:Provider", "queue", "Use InMemory/AzureQueue/ServiceBus."),
        Required("MigrationRunQueue:QueueName", "queue", "Logical run queue name."),
        Optional("MigrationRunQueue:StorageAccountName", "queue", "Azure Queue storage account name."),
        Optional("MigrationRunQueue:ServiceBusNamespace", "queue", "Azure Service Bus namespace."),
        Optional("Cloud:QueueStorageAccountName", "queue", "Alternative Azure Queue storage account name."),
        Optional("Cloud:ServiceBusNamespace", "queue", "Alternative Azure Service Bus namespace."),

        Optional("Auth:Authority", "auth", "Required once Cloud:RequiresAuth=true."),
        Optional("Auth:Audience", "auth", "Required once Cloud:RequiresAuth=true.")
    ];

    private static List<string> BuildWarnings(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IReadOnlyList<CloudConfigurationKeyAuditDescriptor> keys)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment())
        {
            foreach (var missing in keys.Where(x => x.IsRequiredForCloud && !x.IsConfigured))
            {
                warnings.Add($"Required cloud configuration key '{missing.Key}' is missing.");
            }
        }

        var requiresAuth = ReadBool(configuration, "Cloud:RequiresAuth", !environment.IsDevelopment());
        if (requiresAuth)
        {
            if (string.IsNullOrWhiteSpace(configuration["Auth:Authority"]))
            {
                warnings.Add("Cloud:RequiresAuth is true but Auth:Authority is not configured.");
            }

            if (string.IsNullOrWhiteSpace(configuration["Auth:Audience"]))
            {
                warnings.Add("Cloud:RequiresAuth is true but Auth:Audience is not configured.");
            }
        }

        var credentialMode = configuration["Cloud:CredentialMode"];
        if (!environment.IsDevelopment() &&
            (string.Equals(credentialMode, "userSecrets", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(credentialMode, "local", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("Non-development environments should not use local/user-secrets credential modes.");
        }

        var artifactMode = configuration["Cloud:ArtifactMode"];
        if (!environment.IsDevelopment() &&
            (string.Equals(artifactMode, "local", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(artifactMode, "localFileSystem", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("Non-development environments should not use local file-system artifact mode.");
        }

        var queueProvider = configuration["MigrationRunQueue:Provider"];
        if (!environment.IsDevelopment() &&
            string.Equals(queueProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should not use in-memory queues.");
        }

        if (!string.IsNullOrWhiteSpace(configuration["Cloud:KeyVaultName"]) &&
            !string.IsNullOrWhiteSpace(configuration["KeyVault:Name"]))
        {
            warnings.Add("Both Cloud:KeyVaultName and KeyVault:Name are configured. Prefer one canonical key.");
        }

        if (!string.IsNullOrWhiteSpace(configuration["Cloud:ArtifactContainerName"]) &&
            !string.IsNullOrWhiteSpace(configuration["Artifacts:BlobContainerName"]))
        {
            warnings.Add("Both Cloud:ArtifactContainerName and Artifacts:BlobContainerName are configured. Prefer one canonical key.");
        }

        return warnings;
    }

    private static string InferMaturityLevel(
        IWebHostEnvironment environment,
        IReadOnlyList<CloudConfigurationKeyAuditDescriptor> keys,
        IReadOnlyList<string> warnings)
    {
        if (environment.IsDevelopment())
        {
            return CloudConfigurationMaturityLevels.LocalDevelopment;
        }

        var missingRequired = keys.Any(x => x.IsRequiredForCloud && !x.IsConfigured);
        if (missingRequired)
        {
            return CloudConfigurationMaturityLevels.CloudPlanned;
        }

        return warnings.Count == 0
            ? CloudConfigurationMaturityLevels.CloudReady
            : CloudConfigurationMaturityLevels.CloudPlanned;
    }

    private static CloudConfigurationKeyAuditDescriptor Required(
        string key,
        string category,
        string recommendation) =>
        new(key, category, IsConfigured: false, IsRequiredForCloud: true, Recommendation: recommendation);

    private static CloudConfigurationKeyAuditDescriptor Optional(
        string key,
        string category,
        string recommendation) =>
        new(key, category, IsConfigured: false, IsRequiredForCloud: false, Recommendation: recommendation);

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


