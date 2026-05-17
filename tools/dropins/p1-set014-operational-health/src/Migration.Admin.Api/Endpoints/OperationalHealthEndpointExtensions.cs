using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalHealthEndpointExtensions
{
    public static WebApplication MapOperationalHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/health/live", () =>
            Results.Ok(new
            {
                status = OperationalHealthStatuses.Healthy,
                checkedUtc = DateTimeOffset.UtcNow
            }))
            .WithName("HealthLive")
            .WithTags("Health")
            .WithSummary("Liveness probe for process availability.");

        app.MapGet("/health/ready", (
                IConfiguration configuration,
                IWebHostEnvironment environment) =>
            {
                var health = BuildHealth(configuration, environment, includeCloudChecks: false);
                return Results.Ok(health);
            })
            .WithName("HealthReady")
            .WithTags("Health")
            .WithSummary("Readiness probe for local runtime dependencies.")
            .Produces<OperationalHealthDescriptor>(StatusCodes.Status200OK);

        app.MapGet("/health/cloud", (
                IConfiguration configuration,
                IWebHostEnvironment environment) =>
            {
                var health = BuildHealth(configuration, environment, includeCloudChecks: true);
                return Results.Ok(health);
            })
            .WithName("HealthCloud")
            .WithTags("Health")
            .WithSummary("Cloud-readiness operational health summary.")
            .Produces<OperationalHealthDescriptor>(StatusCodes.Status200OK);

        return app;
    }

    private static OperationalHealthDescriptor BuildHealth(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        bool includeCloudChecks)
    {
        var checks = new List<OperationalHealthCheckDescriptor>
        {
            CheckConfiguration(configuration),
            CheckControlPlaneStorage(configuration, environment, includeCloudChecks),
            CheckQueue(configuration, environment, includeCloudChecks),
            CheckCredentials(configuration, environment, includeCloudChecks),
            CheckArtifacts(configuration, environment, includeCloudChecks)
        };

        if (includeCloudChecks)
        {
            checks.Add(CheckDeployment(configuration, environment));
        }

        var warnings = checks
            .SelectMany(check => check.Warnings.Select(warning => $"{check.Name}: {warning}"))
            .ToArray();

        var status = checks.Any(check => string.Equals(check.Status, OperationalHealthStatuses.Unhealthy, StringComparison.OrdinalIgnoreCase))
            ? OperationalHealthStatuses.Unhealthy
            : checks.Any(check => string.Equals(check.Status, OperationalHealthStatuses.Degraded, StringComparison.OrdinalIgnoreCase))
                ? OperationalHealthStatuses.Degraded
                : OperationalHealthStatuses.Healthy;

        return new OperationalHealthDescriptor(
            Status: status,
            EnvironmentName: environment.EnvironmentName,
            CheckedUtc: DateTimeOffset.UtcNow,
            Checks: checks,
            Warnings: warnings);
    }

    private static OperationalHealthCheckDescriptor CheckConfiguration(IConfiguration configuration)
    {
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration["ControlPlane:StorageRoot"]))
        {
            warnings.Add("ControlPlane:StorageRoot is not configured.");
        }

        if (string.IsNullOrWhiteSpace(configuration["MigrationRunQueue:Provider"]))
        {
            warnings.Add("MigrationRunQueue:Provider is not configured.");
        }

        if (string.IsNullOrWhiteSpace(configuration["MigrationRunQueue:QueueName"]))
        {
            warnings.Add("MigrationRunQueue:QueueName is not configured.");
        }

        return Check(
            "configuration",
            warnings.Count == 0 ? OperationalHealthStatuses.Healthy : OperationalHealthStatuses.Degraded,
            "Required runtime configuration keys are present.",
            warnings);
    }

    private static OperationalHealthCheckDescriptor CheckControlPlaneStorage(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        bool includeCloudChecks)
    {
        var warnings = new List<string>();
        var root = configuration["ControlPlane:StorageRoot"];

        if (string.IsNullOrWhiteSpace(root))
        {
            warnings.Add("Control-plane storage root is not configured.");
            return Check("controlPlaneStorage", OperationalHealthStatuses.Degraded, "Control-plane storage root configuration.", warnings);
        }

        var isBlob = root.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
                     root.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (includeCloudChecks && !environment.IsDevelopment() && !isBlob)
        {
            warnings.Add("Non-development control-plane storage should be blob-backed.");
        }

        return Check(
            "controlPlaneStorage",
            warnings.Count == 0 ? OperationalHealthStatuses.Healthy : OperationalHealthStatuses.Degraded,
            "Control-plane storage configuration is available.",
            warnings);
    }

    private static OperationalHealthCheckDescriptor CheckQueue(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        bool includeCloudChecks)
    {
        var warnings = new List<string>();
        var provider = configuration["MigrationRunQueue:Provider"];
        var queueName = configuration["MigrationRunQueue:QueueName"];

        if (string.IsNullOrWhiteSpace(provider))
        {
            warnings.Add("Queue provider is not configured.");
        }

        if (string.IsNullOrWhiteSpace(queueName))
        {
            warnings.Add("Queue name is not configured.");
        }

        if (includeCloudChecks &&
            !environment.IsDevelopment() &&
            string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should not use in-memory queues.");
        }

        if (includeCloudChecks &&
            string.Equals(provider, "AzureQueue", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(FirstNonEmpty(
                configuration["MigrationRunQueue:StorageAccountName"],
                configuration["AzureQueue:StorageAccountName"],
                configuration["Cloud:QueueStorageAccountName"])))
        {
            warnings.Add("Azure Queue provider is selected but no storage account name is configured.");
        }

        return Check(
            "queue",
            warnings.Count == 0 ? OperationalHealthStatuses.Healthy : OperationalHealthStatuses.Degraded,
            "Run queue configuration is available.",
            warnings);
    }

    private static OperationalHealthCheckDescriptor CheckCredentials(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        bool includeCloudChecks)
    {
        var warnings = new List<string>();
        var credentialMode = FirstNonEmpty(
            configuration["Cloud:CredentialMode"],
            environment.IsDevelopment() ? "userSecrets" : string.Empty);

        if (string.IsNullOrWhiteSpace(credentialMode))
        {
            warnings.Add("Credential mode is not configured.");
        }

        if (includeCloudChecks &&
            !environment.IsDevelopment() &&
            (string.Equals(credentialMode, "userSecrets", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(credentialMode, "local", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("Non-development environments should use Key Vault or managed identity credential mode.");
        }

        return Check(
            "credentials",
            warnings.Count == 0 ? OperationalHealthStatuses.Healthy : OperationalHealthStatuses.Degraded,
            "Credential provider mode is configured.",
            warnings);
    }

    private static OperationalHealthCheckDescriptor CheckArtifacts(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        bool includeCloudChecks)
    {
        var warnings = new List<string>();
        var artifactMode = FirstNonEmpty(
            configuration["Cloud:ArtifactMode"],
            environment.IsDevelopment() ? "localFileSystem" : string.Empty);

        if (string.IsNullOrWhiteSpace(artifactMode))
        {
            warnings.Add("Artifact mode is not configured.");
        }

        if (includeCloudChecks &&
            !environment.IsDevelopment() &&
            (string.Equals(artifactMode, "local", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(artifactMode, "localFileSystem", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("Non-development environments should use Azure Blob artifact storage.");
        }

        return Check(
            "artifacts",
            warnings.Count == 0 ? OperationalHealthStatuses.Healthy : OperationalHealthStatuses.Degraded,
            "Artifact storage mode is configured.",
            warnings);
    }

    private static OperationalHealthCheckDescriptor CheckDeployment(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(configuration["Cloud:HostKind"]))
        {
            warnings.Add("Cloud:HostKind is not configured.");
        }

        if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(configuration["Cloud:Region"]))
        {
            warnings.Add("Cloud:Region is not configured.");
        }

        return Check(
            "deployment",
            warnings.Count == 0 ? OperationalHealthStatuses.Healthy : OperationalHealthStatuses.Degraded,
            "Deployment profile shape is configured.",
            warnings);
    }

    private static OperationalHealthCheckDescriptor Check(
        string name,
        string status,
        string description,
        IReadOnlyList<string> warnings) =>
        new(name, status, description, warnings);

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
