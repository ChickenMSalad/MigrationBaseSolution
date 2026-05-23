using System;

namespace MigrationBase.Core.Cloud.Azure.Workers.Diagnostics;

public sealed class AzureWorkerDiagnosticSnapshotOptions
{
    public const string SectionName = "AzureRuntime:Workers:Diagnostics";

    public bool Enabled { get; set; } = true;

    public TimeSpan SnapshotInterval { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan StaleHeartbeatThreshold { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan StaleLeaseThreshold { get; set; } = TimeSpan.FromMinutes(5);

    public bool IncludeHostAttributes { get; set; } = true;
}
