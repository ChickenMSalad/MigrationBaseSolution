using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Describes the expected deployment shape for one Azure runtime environment.
/// This is an SDK-free contract used by deployment automation, readiness checks,
/// and operator-facing validation evidence.
/// </summary>
public sealed class AzureDeploymentEnvironmentManifest
{
    public string EnvironmentName { get; init; } = string.Empty;
    public string DeploymentRing { get; init; } = string.Empty;
    public string AzureTenantBoundary { get; init; } = string.Empty;
    public string SubscriptionAlias { get; init; } = string.Empty;
    public string ResourceGroupName { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string SqlOperationalStoreProfile { get; init; } = string.Empty;
    public string StorageProfile { get; init; } = string.Empty;
    public string QueueProfile { get; init; } = string.Empty;
    public string TelemetryProfile { get; init; } = string.Empty;
    public IReadOnlyList<AzureDeploymentHostManifest> Hosts { get; init; } = Array.Empty<AzureDeploymentHostManifest>();
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
