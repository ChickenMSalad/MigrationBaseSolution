using System;

namespace MigrationBase.Core.Cloud.Azure.Workers.Diagnostics;

public sealed class AzureWorkerDiagnosticSignal
{
    public string Code { get; init; } = string.Empty;

    public string Severity { get; init; } = "Information";

    public string Message { get; init; } = string.Empty;

    public DateTimeOffset ObservedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? RunId { get; init; }

    public string? WorkItemId { get; init; }
}
