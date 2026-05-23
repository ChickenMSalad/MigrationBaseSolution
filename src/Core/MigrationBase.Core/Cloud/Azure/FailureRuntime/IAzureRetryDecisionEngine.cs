namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public interface IAzureRetryDecisionEngine
{
    AzureRetryDecision Decide(AzureRetryDecisionRequest request);
}
