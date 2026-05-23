namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public interface IAzureRuntimeCompositionValidationReporter
{
    AzureRuntimeCompositionValidationReport CreateReport(
        AzureRuntimeCompositionPlan plan,
        string planName,
        string? environmentName,
        IEnumerable<AzureRuntimeCompositionValidationFinding> findings);
}
