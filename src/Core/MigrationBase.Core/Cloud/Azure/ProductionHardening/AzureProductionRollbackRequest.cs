using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionRollbackRequest
{
    public required string ReleaseId { get; init; }

    public AzureProductionRollbackTrigger Trigger { get; init; } =
        AzureProductionRollbackTrigger.Unknown;

    public AzureProductionReleaseGateResult? ReleaseGateResult { get; init; }

    public bool OperatorRequestedRollback { get; init; }

    public bool RequireRollbackOnBlockedGate { get; init; } = true;

    public bool AllowOperatorOverride { get; init; } = true;

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Evidence { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
