using System;
using System.Collections.Generic;

namespace Migration.Core.Azure.Topology;

/// <summary>
/// Describes the Azure-facing operational topology for one deployable migration environment.
/// This is intentionally provider-neutral enough to be validated before Azure SDK bindings are introduced.
/// </summary>
public sealed class AzureEnvironmentTopologyDescriptor
{
    public string EnvironmentName { get; init; } = string.Empty;

    public string DeploymentRing { get; init; } = string.Empty;

    public string Region { get; init; } = string.Empty;

    public string ResourceGroupName { get; init; } = string.Empty;

    public string SqlOperationalStoreName { get; init; } = string.Empty;

    public string ArtifactStorageAccountName { get; init; } = string.Empty;

    public string QueueNamespaceName { get; init; } = string.Empty;

    public string TelemetryResourceName { get; init; } = string.Empty;

    public bool AllowsRealMigrationExecution { get; init; }

    public bool AllowsReplayExecution { get; init; }

    public bool RequiresManagedIdentity { get; init; } = true;

    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
