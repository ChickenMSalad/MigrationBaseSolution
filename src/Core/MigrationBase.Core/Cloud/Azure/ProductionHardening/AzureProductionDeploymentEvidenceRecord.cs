using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionDeploymentEvidenceRecord
{
    public required string EvidenceId { get; init; }

    public required string ReleaseId { get; init; }

    public required AzureProductionDeploymentDecision Decision { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Evidence { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
