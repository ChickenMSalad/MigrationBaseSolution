using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReleaseGateResult
{
    public required string ReleaseId { get; init; }

    public AzureProductionReleaseGateStatus Status { get; init; } =
        AzureProductionReleaseGateStatus.NotEvaluated;

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureProductionReleaseGateIssue> Issues { get; init; } =
        new List<AzureProductionReleaseGateIssue>();

    public bool CanRelease =>
        Status is AzureProductionReleaseGateStatus.Passed or
            AzureProductionReleaseGateStatus.PassedWithWarnings;
}
