using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Describes one deployable host role in an Azure runtime environment.
/// </summary>
public sealed class AzureDeploymentHostManifest
{
    public string HostName { get; init; } = string.Empty;
    public string HostRole { get; init; } = string.Empty;
    public string DeploymentTargetProfile { get; init; } = string.Empty;
    public string CapacityProfile { get; init; } = string.Empty;
    public string ExecutionIsolationProfile { get; init; } = string.Empty;
    public bool RequiresManagedIdentity { get; init; } = true;
    public bool RequiresSqlOperationalStore { get; init; } = true;
    public bool RequiresArtifactStorage { get; init; } = true;
    public IReadOnlyList<string> RequiredAppSettings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredSecretReferences { get; init; } = Array.Empty<string>();
}
