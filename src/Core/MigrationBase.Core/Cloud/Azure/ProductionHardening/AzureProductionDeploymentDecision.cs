using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionDeploymentDecision
{
    public required string DecisionId { get; init; }

    public required string ReleaseId { get; init; }

    public AzureProductionDeploymentDecisionStatus Status { get; init; } =
        AzureProductionDeploymentDecisionStatus.NotEvaluated;

    public DateTimeOffset DecidedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? Reason { get; init; }

    public IReadOnlyList<AzureProductionReleaseGateIssue> Issues { get; init; } =
        new List<AzureProductionReleaseGateIssue>();

    public bool CanDeploy =>
        Status is AzureProductionDeploymentDecisionStatus.Approved or
            AzureProductionDeploymentDecisionStatus.ApprovedWithWarnings;
}
