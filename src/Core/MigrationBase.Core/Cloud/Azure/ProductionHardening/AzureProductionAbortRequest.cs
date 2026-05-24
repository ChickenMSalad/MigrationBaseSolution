using System;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionAbortRequest
{
    public required string ReleaseId { get; init; }

    public required string Reason { get; init; }

    public string? RequestedBy { get; init; }

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool Confirmed { get; init; }
}
