namespace MigrationBase.Core.Cloud.Azure.Deployment.Rollback;

/// <summary>
/// A single ordered rollback action. The ActionName is a logical operation name,
/// not a shell command, so execution adapters can remain environment-specific.
/// </summary>
public sealed class AzureDeploymentRollbackStep
{
    public int Order { get; init; }

    public string ActionName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool IsDestructive { get; init; }

    public bool RequiresManualConfirmation { get; init; }

    public string EvidenceKey { get; init; } = string.Empty;
}
