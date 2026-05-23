namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Summarizes deployment readiness findings for a target Azure environment.
/// </summary>
public sealed class AzureDeploymentReadinessReport
{
    public AzureDeploymentReadinessReport(
        string environmentName,
        string deploymentRing,
        DateTimeOffset evaluatedAtUtc,
        IEnumerable<AzureDeploymentReadinessFinding> findings)
    {
        EnvironmentName = environmentName ?? throw new ArgumentNullException(nameof(environmentName));
        DeploymentRing = deploymentRing ?? throw new ArgumentNullException(nameof(deploymentRing));
        EvaluatedAtUtc = evaluatedAtUtc;
        Findings = (findings ?? throw new ArgumentNullException(nameof(findings))).ToArray();
    }

    public string EnvironmentName { get; }

    public string DeploymentRing { get; }

    public DateTimeOffset EvaluatedAtUtc { get; }

    public IReadOnlyCollection<AzureDeploymentReadinessFinding> Findings { get; }

    public bool IsReady => Findings.All(finding => finding.Passed || finding.Severity < AzureDeploymentReadinessSeverity.Error);

    public bool IsBlocked => Findings.Any(finding => finding.BlocksDeployment);

    public int ErrorCount => Findings.Count(finding => !finding.Passed && finding.Severity >= AzureDeploymentReadinessSeverity.Error);

    public int WarningCount => Findings.Count(finding => !finding.Passed && finding.Severity == AzureDeploymentReadinessSeverity.Warning);
}
