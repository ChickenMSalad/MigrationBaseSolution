namespace MigrationBase.Core.Cloud.Azure.Readiness;

public interface IAzureOperationalizationReadinessRegistry
{
    IReadOnlyCollection<AzureOperationalizationReadinessItem> GetRequiredItems();
}
