namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public interface IAzureProductionAbortController
{
    AzureProductionAbortDecision Decide(AzureProductionAbortRequest request);
}
