using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Capacity;

/// <summary>
/// Describes conservative runtime capacity limits for an Azure-hosted migration environment.
/// These values are advisory contracts for deployment planning, readiness checks, and worker stabilization.
/// They do not change runtime behavior by themselves.
/// </summary>
public sealed class AzureRuntimeCapacityProfile
{
    public string EnvironmentName { get; set; } = string.Empty;

    public string DeploymentRing { get; set; } = string.Empty;

    public string WorkloadTier { get; set; } = string.Empty;

    public int MaximumConcurrentRuns { get; set; }

    public int MaximumActiveWorkers { get; set; }

    public int MaximumQueueReadersPerWorker { get; set; }

    public int MaximumManifestRowsPerRun { get; set; }

    public int TargetBatchSize { get; set; }

    public int MaximumBatchSize { get; set; }

    public TimeSpan ExpectedHeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan WorkerDrainTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
