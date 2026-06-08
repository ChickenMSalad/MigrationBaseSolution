using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class CloudReadinessEndpointExtensions
{
    public static RouteGroupBuilder MapCloudReadinessEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/readiness", (
                IConfiguration configuration,
                IWebHostEnvironment environment,
                HttpContext httpContext) =>
            {
                var descriptor = BuildDescriptor(configuration, environment, httpContext);
                return Results.Ok(descriptor);
            })
            .WithName("GetCloudReadiness")
            .WithTags("Cloud")
            .WithSummary("Gets aggregate cloud-readiness diagnostics without exposing secrets.")
            .Produces<CloudReadinessSummaryDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static CloudReadinessSummaryDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        HttpContext httpContext)
    {
        var workspaceId = FirstNonEmpty(
            httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
            configuration["Workspace:WorkspaceId"],
            "default");

        var environmentName = environment.EnvironmentName;
        var isDevelopment = environment.IsDevelopment();

        var checks = new List<CloudReadinessCheckDescriptor>
        {
            CheckEnvironment(configuration, environment),
            CheckWorkspace(configuration, environment, workspaceId),
            CheckStorage(configuration, environment, workspaceId),
            CheckCredentials(configuration, environment, workspaceId),
            CheckArtifacts(configuration, environment, workspaceId),
            CheckQueue(configuration, environment, workspaceId)
        };

        var warnings = checks
            .SelectMany(check => check.Warnings.Select(warning => $"{check.Name}: {warning}"))
            .ToArray();

        return new CloudReadinessSummaryDescriptor(
            EnvironmentName: environmentName,
            IsDevelopment: isDevelopment,
            IsCloudReady: warnings.Length == 0,
            WarningCount: warnings.Length,
            Checks: checks,
            Warnings: warnings);
    }

    private static CloudReadinessCheckDescriptor CheckEnvironment(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var warnings = new List<string>();
        var hostKind = Read(configuration, "Cloud:HostKind", environment.IsDevelopment() ? "localDevelopment" : "unknown");

        if (!environment.IsDevelopment() && string.Equals(hostKind, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Cloud:HostKind should be configured for non-development environments.");
        }

        return BuildCheck("environment", warnings);
    }

    private static CloudReadinessCheckDescriptor CheckWorkspace(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string workspaceId)
    {
        var warnings = new List<string>();
        var tenantMode = Read(configuration, "Workspace:TenantMode", environment.IsDevelopment() ? "development" : "singleTenant");
        var tenantEnforced = ReadBool(configuration, "Workspace:TenantEnforced", false);
        var tenantId = FirstNonEmptyOrNull(configuration["Workspace:TenantId"]);

        if (!environment.IsDevelopment() && string.Equals(workspaceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Use an explicit Workspace:WorkspaceId outside development.");
        }

        if (string.Equals(tenantMode, "multiTenant", StringComparison.OrdinalIgnoreCase) && !tenantEnforced)
        {
            warnings.Add("TenantMode is multiTenant but Workspace:TenantEnforced is false.");
        }

        if (tenantEnforced && string.IsNullOrWhiteSpace(tenantId))
        {
            warnings.Add("Tenant enforcement is enabled but Workspace:TenantId is not configured.");
        }

        return BuildCheck("workspace", warnings);
    }

    private static CloudReadinessCheckDescriptor CheckStorage(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string workspaceId)
    {
        var warnings = new List<string>();
        var root = configuration["ControlPlane:StorageRoot"];
        var isBlob = root.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
                     root.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (!environment.IsDevelopment() && !isBlob)
        {
            warnings.Add("ControlPlane:StorageRoot should be blob-backed outside development.");
        }

        if (!environment.IsDevelopment() && string.Equals(workspaceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Workspace-scoped storage should use an explicit workspace id outside development.");
        }

        return BuildCheck("storage", warnings);
    }

    private static CloudReadinessCheckDescriptor CheckCredentials(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string workspaceId)
    {
        var warnings = new List<string>();
        var credentialMode = Read(configuration, "Cloud:CredentialMode", environment.IsDevelopment() ? "userSecrets" : "unknown");
        var keyVaultName = FirstNonEmptyOrNull(configuration["Cloud:KeyVaultName"], configuration["KeyVault:Name"]);
        var keyVaultUri = FirstNonEmptyOrNull(configuration["Cloud:KeyVaultUri"], configuration["KeyVault:Uri"]);

        if (!environment.IsDevelopment() &&
            (string.Equals(credentialMode, "userSecrets", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(credentialMode, "local", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("Use Key Vault or managed identity credential mode outside development.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(credentialMode, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Cloud:CredentialMode is not configured.");
        }

        if ((string.Equals(credentialMode, "keyVault", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(credentialMode, "managedIdentity", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(keyVaultName) &&
            string.IsNullOrWhiteSpace(keyVaultUri))
        {
            warnings.Add("Key Vault mode is selected but Key Vault name/URI is not configured.");
        }

        return BuildCheck("credentials", warnings);
    }

    private static CloudReadinessCheckDescriptor CheckArtifacts(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string workspaceId)
    {
        var warnings = new List<string>();
        var artifactMode = Read(configuration, "Cloud:ArtifactMode", environment.IsDevelopment() ? "localFileSystem" : "unknown");
        var container = FirstNonEmptyOrNull(
            configuration["Artifacts:BlobContainerName"],
            configuration["AzureBlob:ArtifactsContainer"],
            configuration["Cloud:ArtifactContainerName"]);

        if (!environment.IsDevelopment() &&
            (string.Equals(artifactMode, "localFileSystem", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(artifactMode, "local", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("Use Azure Blob artifact storage outside development.");
        }

        if (string.Equals(artifactMode, "azureBlob", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(container))
        {
            warnings.Add("Azure Blob artifact mode is selected but no container name is configured.");
        }

        return BuildCheck("artifacts", warnings);
    }

    private static CloudReadinessCheckDescriptor CheckQueue(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string workspaceId)
    {
        var warnings = new List<string>();
        var provider = Read(configuration, "MigrationRunQueue:Provider", environment.IsDevelopment() ? "InMemory" : "unknown");
        var queueName = FirstNonEmptyOrNull(configuration["MigrationRunQueue:QueueName"]);
        var storageAccountName = FirstNonEmptyOrNull(
            configuration["MigrationRunQueue:StorageAccountName"],
            configuration["AzureQueue:StorageAccountName"],
            configuration["Cloud:QueueStorageAccountName"]);
        var serviceBusNamespace = FirstNonEmptyOrNull(
            configuration["MigrationRunQueue:ServiceBusNamespace"],
            configuration["ServiceBus:Namespace"],
            configuration["Cloud:ServiceBusNamespace"]);

        if (!environment.IsDevelopment() &&
            string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Use Azure Queue or Azure Service Bus outside development.");
        }

        if (string.IsNullOrWhiteSpace(queueName))
        {
            warnings.Add("MigrationRunQueue:QueueName is not configured.");
        }

        if ((string.Equals(provider, "AzureQueue", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(provider, "AzureStorageQueue", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(storageAccountName))
        {
            warnings.Add("Azure Queue provider is selected but no queue storage account name is configured.");
        }

        if ((string.Equals(provider, "ServiceBus", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(provider, "AzureServiceBus", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(serviceBusNamespace))
        {
            warnings.Add("Service Bus provider is selected but no namespace is configured.");
        }

        return BuildCheck("queue", warnings);
    }

    private static CloudReadinessCheckDescriptor BuildCheck(
        string name,
        IReadOnlyList<string> warnings)
    {
        var status = warnings.Count == 0
            ? CloudReadinessStatuses.Ready
            : CloudReadinessStatuses.Warning;

        return new CloudReadinessCheckDescriptor(name, status, warnings);
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

    private static string? FirstNonEmptyOrNull(params string?[] values)
    {
        var value = FirstNonEmpty(values);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
