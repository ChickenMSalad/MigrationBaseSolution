using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Rollback;

/// <summary>
/// Describes the operator-approved rollback posture for an Azure deployment target.
/// This contract is intentionally SDK-free so deployment tooling, workers, and operator UI
/// can share the same rollback model without taking Azure runtime dependencies.
/// </summary>
public sealed class AzureDeploymentRollbackPlan
{
    public string PlanId { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public string DeploymentTarget { get; init; } = string.Empty;

    public string ReleaseVersion { get; init; } = string.Empty;

    public string PreviousReleaseVersion { get; init; } = string.Empty;

    public AzureDeploymentRollbackStrategy Strategy { get; init; } = AzureDeploymentRollbackStrategy.Manual;

    public AzureDeploymentRollbackApprovalRequirement ApprovalRequirement { get; init; } = AzureDeploymentRollbackApprovalRequirement.Required;

    public bool RequiresDatabaseRestorePoint { get; init; }

    public bool RequiresArtifactRestorePoint { get; init; }

    public IReadOnlyCollection<AzureDeploymentRollbackStep> Steps { get; init; } = Array.Empty<AzureDeploymentRollbackStep>();

    public IReadOnlyCollection<string> RequiredEvidenceKeys { get; init; } = Array.Empty<string>();
}
