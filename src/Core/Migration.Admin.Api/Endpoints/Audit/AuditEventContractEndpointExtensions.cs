using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class AuditEventContractEndpointExtensions
{
    public static RouteGroupBuilder MapAuditEventContractEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/audit/event-contract", (
                IConfiguration configuration,
                IWebHostEnvironment environment,
                HttpContext httpContext) =>
            {
                var descriptor = BuildDescriptor(configuration, environment, httpContext);
                return Results.Ok(descriptor);
            })
            .WithName("GetAuditEventContract")
            .WithTags("Cloud")
            .WithSummary("Gets the safe audit event contract for cloud operations.")
            .Produces<AuditEventContractDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static AuditEventContractDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        HttpContext httpContext)
    {
        var workspaceId = FirstNonEmpty(
            httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
            configuration["Workspace:WorkspaceId"],
            "default");

        var tenantId = FirstNonEmptyOrNull(
            httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault(),
            configuration["Workspace:TenantId"]);

        var auditMode = Read(
            configuration,
            "Cloud:AuditMode",
            environment.IsDevelopment() ? "none" : "azureBlob");

        var auditStorageRoot = FirstNonEmptyOrNull(
            configuration["Cloud:AuditStorageRoot"],
            configuration["Audit:StorageRoot"]);

        var providerKind = InferProviderKind(auditMode, auditStorageRoot);
        var persistenceEnabled = !string.Equals(providerKind, AuditEventProviderKinds.None, StringComparison.OrdinalIgnoreCase);

        var warnings = BuildWarnings(environment, auditMode, providerKind, auditStorageRoot, workspaceId, tenantId);

        return new AuditEventContractDescriptor(
            EnvironmentName: environment.EnvironmentName,
            AuditMode: auditMode,
            WorkspaceId: NormalizeSegment(workspaceId),
            TenantId: tenantId,
            PersistenceEnabled: persistenceEnabled,
            ProviderKind: providerKind,
            AuditStorageRoot: auditStorageRoot,
            SupportedEventTypes:
            [
                AuditEventTypes.ProjectCreated,
                AuditEventTypes.ProjectUpdated,
                AuditEventTypes.ProjectDeleted,
                AuditEventTypes.RunQueued,
                AuditEventTypes.RunCanceled,
                AuditEventTypes.CredentialCreated,
                AuditEventTypes.CredentialDeleted,
                AuditEventTypes.ArtifactUploaded,
                AuditEventTypes.ArtifactDeleted,
                AuditEventTypes.WorkspaceContextResolved
            ],
            RequiredProperties:
            [
                "eventId",
                "eventType",
                "occurredUtc",
                "environmentName",
                "workspaceId",
                "tenantId",
                "correlationId",
                "actorId",
                "resourceType",
                "resourceId",
                "outcome"
            ],
            RedactedProperties:
            [
                "password",
                "clientSecret",
                "apiSecret",
                "connectionString",
                "accessKey",
                "secretAccessKey",
                "bearerToken",
                "refreshToken"
            ],
            Warnings: warnings);
    }

    private static string InferProviderKind(string auditMode, string? auditStorageRoot)
    {
        if (string.Equals(auditMode, "none", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(auditMode, "disabled", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventProviderKinds.None;
        }

        if (string.Equals(auditMode, "applicationInsights", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventProviderKinds.ApplicationInsights;
        }

        if (string.Equals(auditMode, "azureBlob", StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(auditStorageRoot) &&
             (auditStorageRoot.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
              auditStorageRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase))))
        {
            return AuditEventProviderKinds.AzureBlob;
        }

        if (string.Equals(auditMode, "localFile", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventProviderKinds.LocalFile;
        }

        return AuditEventProviderKinds.Unknown;
    }

    private static List<string> BuildWarnings(
        IWebHostEnvironment environment,
        string auditMode,
        string providerKind,
        string? auditStorageRoot,
        string workspaceId,
        string? tenantId)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() &&
            string.Equals(providerKind, AuditEventProviderKinds.None, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Audit persistence should be enabled outside development.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(providerKind, AuditEventProviderKinds.LocalFile, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development audit persistence should not use local files.");
        }

        if ((string.Equals(providerKind, AuditEventProviderKinds.AzureBlob, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(providerKind, AuditEventProviderKinds.LocalFile, StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(auditStorageRoot))
        {
            warnings.Add("Audit persistence provider requires Cloud:AuditStorageRoot or Audit:StorageRoot.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(workspaceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development audit events should include an explicit workspace id.");
        }

        if (!environment.IsDevelopment() &&
            string.IsNullOrWhiteSpace(tenantId))
        {
            warnings.Add("Non-development audit events should include tenant id once tenant enforcement is enabled.");
        }

        if (string.Equals(providerKind, AuditEventProviderKinds.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Audit mode '{auditMode}' is not recognized.");
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
}
