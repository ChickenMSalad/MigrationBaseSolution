using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class ArtifactStoragePlanEndpointExtensions
{
    public static RouteGroupBuilder MapArtifactStoragePlanEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/artifact-storage-plan", (
                IConfiguration configuration,
                IWebHostEnvironment environment,
                HttpContext httpContext) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var descriptor = BuildDescriptor(configuration, environment, workspaceId);
                return Results.Ok(descriptor);
            })
            .WithName("GetArtifactStoragePlan")
            .WithTags("Cloud")
            .WithSummary("Gets the safe artifact storage plan for cloud-readiness diagnostics.")
            .Produces<ArtifactStoragePlanDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static ArtifactStoragePlanDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string workspaceId)
    {
        var normalizedWorkspaceId = NormalizeSegment(workspaceId);

        var controlPlaneRoot = FirstNonEmpty(
            configuration["ControlPlane:StorageRoot"],
            "Runtime/admin-api");

        var artifactMode = Read(
            configuration,
            "Cloud:ArtifactMode",
            InferArtifactMode(controlPlaneRoot));

        var blobContainerName = FirstNonEmptyOrNull(
            configuration["Artifacts:BlobContainerName"],
            configuration["AzureBlob:ArtifactsContainer"],
            configuration["Cloud:ArtifactContainerName"]);

        var blobAccountName = FirstNonEmptyOrNull(
            configuration["Artifacts:BlobAccountName"],
            configuration["AzureBlob:AccountName"],
            configuration["Cloud:ArtifactStorageAccountName"]);

        var credentialMode = Read(
            configuration,
            "Cloud:CredentialMode",
            environment.IsDevelopment() ? "userSecrets" : "unknown");

        var providerKind = InferProviderKind(artifactMode, credentialMode, blobContainerName, blobAccountName);

        var artifactRoot = BuildArtifactRoot(controlPlaneRoot, normalizedWorkspaceId, artifactMode, blobContainerName);

        var warnings = BuildWarnings(
            environment,
            artifactMode,
            providerKind,
            controlPlaneRoot,
            blobContainerName,
            blobAccountName,
            normalizedWorkspaceId);

        return new ArtifactStoragePlanDescriptor(
            EnvironmentName: environment.EnvironmentName,
            WorkspaceId: normalizedWorkspaceId,
            ArtifactMode: artifactMode,
            ProviderKind: providerKind,
            ArtifactRoot: artifactRoot,
            ManifestRoot: CombinePath(artifactRoot, ArtifactStorageKinds.Manifest),
            MappingRoot: CombinePath(artifactRoot, ArtifactStorageKinds.Mapping),
            TaxonomyRoot: CombinePath(artifactRoot, ArtifactStorageKinds.Taxonomy),
            OtherRoot: CombinePath(artifactRoot, ArtifactStorageKinds.Other),
            BlobContainerName: blobContainerName,
            BlobAccountName: blobAccountName,
            UsesLocalFileSystem: string.Equals(providerKind, ArtifactStorageProviderKinds.LocalFileSystem, StringComparison.OrdinalIgnoreCase),
            UsesAzureBlob: providerKind.StartsWith("azureBlob", StringComparison.OrdinalIgnoreCase),
            RequiresManagedIdentity: string.Equals(providerKind, ArtifactStorageProviderKinds.AzureBlobManagedIdentity, StringComparison.OrdinalIgnoreCase),
            SupportedArtifactKinds:
            [
                ArtifactStorageKinds.Manifest,
                ArtifactStorageKinds.Mapping,
                ArtifactStorageKinds.Taxonomy,
                ArtifactStorageKinds.Other
            ],
            Warnings: warnings);
    }

    private static string BuildArtifactRoot(
        string controlPlaneRoot,
        string workspaceId,
        string artifactMode,
        string? blobContainerName)
    {
        if (string.Equals(artifactMode, "azureBlob", StringComparison.OrdinalIgnoreCase))
        {
            var container = string.IsNullOrWhiteSpace(blobContainerName)
                ? "migration-artifacts"
                : blobContainerName;

            return CombinePath($"az://{container}", "workspaces", workspaceId, "artifacts");
        }

        return CombinePath(controlPlaneRoot, "workspaces", workspaceId, "artifacts");
    }

    private static string InferArtifactMode(string controlPlaneRoot)
    {
        if (controlPlaneRoot.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
            controlPlaneRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "azureBlob";
        }

        return "localFileSystem";
    }

    private static string InferProviderKind(
        string artifactMode,
        string credentialMode,
        string? blobContainerName,
        string? blobAccountName)
    {
        if (string.Equals(artifactMode, "localFileSystem", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(artifactMode, "local", StringComparison.OrdinalIgnoreCase))
        {
            return ArtifactStorageProviderKinds.LocalFileSystem;
        }

        if (string.Equals(artifactMode, "azureBlob", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(credentialMode, "managedIdentity", StringComparison.OrdinalIgnoreCase)
                ? ArtifactStorageProviderKinds.AzureBlobManagedIdentity
                : ArtifactStorageProviderKinds.AzureBlobConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(blobContainerName) || !string.IsNullOrWhiteSpace(blobAccountName))
        {
            return ArtifactStorageProviderKinds.AzureBlobConnectionString;
        }

        return ArtifactStorageProviderKinds.Unknown;
    }

    private static List<string> BuildWarnings(
        IWebHostEnvironment environment,
        string artifactMode,
        string providerKind,
        string controlPlaneRoot,
        string? blobContainerName,
        string? blobAccountName,
        string workspaceId)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() &&
            string.Equals(providerKind, ArtifactStorageProviderKinds.LocalFileSystem, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should not use local file-system artifact storage.");
        }

        if (string.Equals(artifactMode, "azureBlob", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(blobContainerName))
        {
            warnings.Add("Azure Blob artifact mode is selected but no artifact container name is configured.");
        }

        if (string.Equals(artifactMode, "azureBlob", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(blobAccountName))
        {
            warnings.Add("Azure Blob artifact mode is selected but no artifact storage account name is configured.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(workspaceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development artifact roots should use an explicit workspace id.");
        }

        if (string.IsNullOrWhiteSpace(controlPlaneRoot))
        {
            warnings.Add("ControlPlane:StorageRoot is not configured.");
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

    private static string NormalizeSegment(string value)
    {
        var sanitized = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? "default"
            : sanitized.Trim('-');
    }

    private static string CombinePath(params string[] segments)
    {
        var cleaned = segments
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(segment => segment.Trim().Trim('/', '\\'))
            .ToArray();

        if (cleaned.Length == 0)
        {
            return string.Empty;
        }

        var first = cleaned[0];

        if (first.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
            first.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return first.TrimEnd('/') + "/" + string.Join("/", cleaned.Skip(1));
        }

        return string.Join("/", cleaned);
    }
}
