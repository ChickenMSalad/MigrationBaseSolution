using System;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionSignoffRecord
{
    public required string SignoffId { get; init; }

    public required string ReleaseId { get; init; }

    public required string SignedOffBy { get; init; }

    public DateTimeOffset SignedOffAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? Notes { get; init; }

    public bool Approved { get; init; }
}
