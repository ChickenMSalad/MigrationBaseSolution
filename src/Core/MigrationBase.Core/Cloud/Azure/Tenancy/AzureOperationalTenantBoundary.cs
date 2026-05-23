namespace MigrationBase.Core.Cloud.Azure.Tenancy;

/// <summary>
/// Describes the Azure tenant boundary that an operational runtime is allowed to use.
/// This is intentionally SDK-free so it can be shared by workers, APIs, deployment checks,
/// and readiness tooling without pulling Azure client packages into the core model.
/// </summary>
public sealed class AzureOperationalTenantBoundary
{
    public string Name { get; init; } = string.Empty;

    public string TenantId { get; init; } = string.Empty;

    public string SubscriptionId { get; init; } = string.Empty;

    public string? ManagementGroupId { get; init; }

    public string EnvironmentName { get; init; } = string.Empty;

    public string DeploymentRing { get; init; } = string.Empty;

    public bool AllowsProductionWorkloads { get; init; }

    public bool AllowsReplayWorkloads { get; init; }

    public bool RequiresManagedIdentity { get; init; } = true;

    public IReadOnlyCollection<string> AllowedResourceGroups { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, string> RequiredTags { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
