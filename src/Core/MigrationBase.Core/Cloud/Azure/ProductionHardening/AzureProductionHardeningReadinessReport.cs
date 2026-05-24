using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionHardeningReadinessReport
{
    public AzureProductionHardeningReadinessStatus Status { get; init; } =
        AzureProductionHardeningReadinessStatus.Ready;

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureProductionHardeningReadinessIssue> Issues { get; init; } =
        new List<AzureProductionHardeningReadinessIssue>();

    public bool IsReady => Status == AzureProductionHardeningReadinessStatus.Ready;
}
