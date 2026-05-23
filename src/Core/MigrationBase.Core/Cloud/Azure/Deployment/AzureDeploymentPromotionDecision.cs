namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Represents the result of evaluating deployment promotion evidence against a policy.
/// </summary>
public sealed class AzureDeploymentPromotionDecision
{
    public bool IsAllowed { get; set; }

    public bool RequiresManualApproval { get; set; }

    public IList<string> PassedGates { get; set; } = new List<string>();

    public IList<string> FailedGates { get; set; } = new List<string>();

    public IList<string> Warnings { get; set; } = new List<string>();
}
