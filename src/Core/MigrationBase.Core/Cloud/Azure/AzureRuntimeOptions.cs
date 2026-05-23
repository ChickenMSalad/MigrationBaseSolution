using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure;

/// <summary>
/// Root P5 cloud runtime topology options. This model is intentionally SDK-free and host-neutral.
/// </summary>
public sealed class AzureRuntimeOptions
{
    public const string SectionName = "AzureRuntime";

    public RuntimeEnvironmentOptions Environment { get; set; } = new();

    public AzureIdentityOptions Identity { get; set; } = new();

    /// <summary>
    /// SQL remains the durable operational system of record for runs, work items, manifests, mappings, failures, reruns, and state.
    /// </summary>
    public AzureSqlOperationalStoreOptions SqlOperationalStore { get; set; } = new();

    /// <summary>
    /// Storage is for migration artifacts, import/export files, logs, and temporary payloads; not the operational run database.
    /// </summary>
    public AzureArtifactStorageOptions ArtifactStorage { get; set; } = new();

    public AzureQueueTopologyOptions QueueTopology { get; set; } = new();

    public AzureTelemetryOptions Telemetry { get; set; } = new();

    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
