namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public interface IAzureProductionReadinessChecklistBuilder
{
    AzureProductionReadinessChecklist Build(AzureProductionReadinessChecklistRequest request);
}
