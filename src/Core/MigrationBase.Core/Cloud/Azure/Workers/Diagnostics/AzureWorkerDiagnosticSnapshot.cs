using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Workers.Diagnostics;

/// <summary>
/// Captures a point-in-time operational view of an Azure worker instance.
/// This is a contract only; persistence and telemetry emission are added later.
/// </summary>
public sealed class AzureWorkerDiagnosticSnapshot
{
    public string WorkerInstanceId { get; init; } = string.Empty;

    public string HostRole { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public string DeploymentRing { get; init; } = string.Empty;

    public string? ActiveRunId { get; init; }

    public string? ActiveWorkItemId { get; init; }

    public AzureWorkerDiagnosticSnapshotStatus Status { get; init; } = AzureWorkerDiagnosticSnapshotStatus.Unknown;

    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public TimeSpan? WorkerUptime { get; init; }

    public TimeSpan? TimeSinceLastHeartbeat { get; init; }

    public TimeSpan? TimeSinceLastLeaseRenewal { get; init; }

    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
