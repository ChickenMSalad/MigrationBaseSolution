using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class CloudPlatformEndpointExtensions
{
    public static RouteGroupBuilder MapCloudPlatformEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/environment", (
                IConfiguration configuration,
                IWebHostEnvironment environment) =>
            {
                var descriptor = BuildDescriptor(configuration, environment);
                return Results.Ok(descriptor);
            })
            .WithName("GetCloudEnvironment")
            .WithTags("Cloud")
            .WithSummary("Gets safe cloud/runtime environment shape for diagnostics and cloud readiness checks.")
            .Produces<CloudEnvironmentDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static CloudEnvironmentDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var environmentName = environment.EnvironmentName;
        var queueProvider = Read(configuration, "MigrationRunQueue:Provider", "unknown");
        var queueName = EmptyToNull(configuration["MigrationRunQueue:QueueName"]);
        var storageRoot = EmptyToNull(configuration["ControlPlane:StorageRoot"]);

        var hostKind = Read(configuration, "Cloud:HostKind", InferHostKind(environment));
        var storageMode = Read(configuration, "Cloud:StorageMode", InferStorageMode(storageRoot));
        var credentialMode = Read(configuration, "Cloud:CredentialMode", InferCredentialMode(environment));
        var artifactMode = Read(configuration, "Cloud:ArtifactMode", InferArtifactMode(storageMode));

        var warnings = BuildWarnings(
            environmentName,
            hostKind,
            storageMode,
            queueProvider,
            queueName,
            credentialMode,
            artifactMode,
            storageRoot);

        var isLocal =
            environment.IsDevelopment() ||
            string.Equals(hostKind, CloudHostKinds.LocalDevelopment, StringComparison.OrdinalIgnoreCase);

        return new CloudEnvironmentDescriptor(
            EnvironmentName: environmentName,
            HostKind: hostKind,
            StorageMode: storageMode,
            QueueProvider: queueProvider,
            QueueName: queueName,
            CredentialMode: credentialMode,
            ArtifactMode: artifactMode,
            ControlPlaneStorageRoot: storageRoot,
            IsLocal: isLocal,
            IsCloudReady: warnings.Count == 0,
            Warnings: warnings);
    }

    private static List<string> BuildWarnings(
        string environmentName,
        string hostKind,
        string storageMode,
        string queueProvider,
        string? queueName,
        string credentialMode,
        string artifactMode,
        string? storageRoot)
    {
        var warnings = new List<string>();

        var isDevelopment = string.Equals(
            environmentName,
            Environments.Development,
            StringComparison.OrdinalIgnoreCase);

        if (!isDevelopment &&
            string.Equals(storageMode, "local", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should not use local control-plane storage.");
        }

        if (!isDevelopment &&
            string.Equals(queueProvider, "inmemory", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should not use in-memory run queues.");
        }

        if (!isDevelopment &&
            string.IsNullOrWhiteSpace(queueName))
        {
            warnings.Add("Queue name should be configured for cloud environments.");
        }

        if (!isDevelopment &&
            string.Equals(credentialMode, CloudCredentialModes.Local, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should use Key Vault or Managed Identity credential resolution.");
        }

        if (!isDevelopment &&
            string.Equals(artifactMode, CloudArtifactModes.LocalFileSystem, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should use blob-backed artifacts.");
        }

        if (string.Equals(hostKind, CloudHostKinds.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Cloud host kind is not configured.");
        }

        if (string.IsNullOrWhiteSpace(storageRoot))
        {
            warnings.Add("ControlPlane:StorageRoot is not configured.");
        }

        return warnings;
    }

    private static string InferHostKind(IWebHostEnvironment environment) =>
        environment.IsDevelopment()
            ? CloudHostKinds.LocalDevelopment
            : CloudHostKinds.Unknown;

    private static string InferStorageMode(string? storageRoot)
    {
        if (string.IsNullOrWhiteSpace(storageRoot))
        {
            return "unknown";
        }

        return storageRoot.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
               storageRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "azureBlob"
            : "local";
    }

    private static string InferCredentialMode(IWebHostEnvironment environment) =>
        environment.IsDevelopment()
            ? CloudCredentialModes.UserSecrets
            : CloudCredentialModes.Unknown;

    private static string InferArtifactMode(string storageMode) =>
        string.Equals(storageMode, "azureBlob", StringComparison.OrdinalIgnoreCase)
            ? CloudArtifactModes.AzureBlob
            : CloudArtifactModes.LocalFileSystem;

    private static string Read(
        IConfiguration configuration,
        string key,
        string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
