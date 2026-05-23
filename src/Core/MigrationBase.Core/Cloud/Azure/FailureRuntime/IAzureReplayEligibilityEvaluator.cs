namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public interface IAzureReplayEligibilityEvaluator
{
    AzureReplayEligibilityDecision Evaluate(AzureReplayEligibilityRequest request);
}
