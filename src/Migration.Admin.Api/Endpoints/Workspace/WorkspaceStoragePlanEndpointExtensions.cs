using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class WorkspaceStoragePlanEndpointExtensions
{
    public static RouteGroupBuilder MapWorkspaceStoragePlanEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/workspace/storage-plan", (
                IConfiguration configuration,
                IWebHostEnvironment environment,
                HttpContext httpContext) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var controlPlaneRoot = FirstNonEmpty(
                    configuration["ControlPlane:StorageRoot"],
                    ".migration-control-plane");

                var storageMode = InferStorageMode(controlPlaneRoot);
                var plan = BuildPlan(workspaceId, controlPlaneRoot, storageMode, environment);

                return Results.Ok(plan);
            })
            .WithName("GetWorkspaceStoragePlan")
            .WithTags("Workspace")
            .WithSummary("Gets planned workspace-scoped storage paths for cloud migration readiness.")
            .Produces<WorkspaceStoragePlanDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static WorkspaceStoragePlanDescriptor BuildPlan(
        string workspaceId,
        string controlPlaneRoot,
        string storageMode,
        IWebHostEnvironment environment)
    {
        var normalizedWorkspaceId = NormalizeSegment(workspaceId);
        var workspaceRoot = CombinePath(
            controlPlaneRoot,
            WorkspaceStoragePathSegments.Workspaces,
            normalizedWorkspaceId);

        var warnings = BuildWarnings(
            environment,
            workspaceId,
            controlPlaneRoot,
            storageMode);

        return new WorkspaceStoragePlanDescriptor(
            WorkspaceId: normalizedWorkspaceId,
            StorageMode: storageMode,
            ControlPlaneRoot: controlPlaneRoot,
            WorkspaceRoot: workspaceRoot,
            ProjectsRoot: CombinePath(workspaceRoot, WorkspaceStoragePathSegments.Projects),
            RunsRoot: CombinePath(workspaceRoot, WorkspaceStoragePathSegments.Runs),
            ArtifactsRoot: CombinePath(workspaceRoot, WorkspaceStoragePathSegments.Artifacts),
            CredentialsRoot: CombinePath(workspaceRoot, WorkspaceStoragePathSegments.Credentials),
            IsLocalFileSystem: string.Equals(storageMode, "local", StringComparison.OrdinalIgnoreCase),
            IsCloudBlob: string.Equals(storageMode, "azureBlob", StringComparison.OrdinalIgnoreCase),
            Warnings: warnings);
    }

    private static List<string> BuildWarnings(
        IWebHostEnvironment environment,
        string workspaceId,
        string controlPlaneRoot,
        string storageMode)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() &&
            string.Equals(workspaceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should not use the default workspace id.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(storageMode, "local", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should use blob-backed workspace storage.");
        }

        if (string.IsNullOrWhiteSpace(controlPlaneRoot))
        {
            warnings.Add("ControlPlane:StorageRoot is not configured.");
        }

        return warnings;
    }

    private static string InferStorageMode(string controlPlaneRoot)
    {
        if (controlPlaneRoot.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
            controlPlaneRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "azureBlob";
        }

        return "local";
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
