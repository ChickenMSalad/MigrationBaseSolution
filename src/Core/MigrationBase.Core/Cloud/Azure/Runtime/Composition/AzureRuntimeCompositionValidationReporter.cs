namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public sealed class AzureRuntimeCompositionValidationReporter : IAzureRuntimeCompositionValidationReporter
{
    public AzureRuntimeCompositionValidationReport CreateReport(
        AzureRuntimeCompositionPlan plan,
        string planName,
        string? environmentName,
        IEnumerable<AzureRuntimeCompositionValidationFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(planName);
        ArgumentNullException.ThrowIfNull(findings);

        var report = new AzureRuntimeCompositionValidationReport
        {
            PlanName = planName,
            EnvironmentName = environmentName
        };

        report.AddFindings(findings);
        return report;
    }
}
