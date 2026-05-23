namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public interface IAzureFailureClassifier
{
    AzureFailureClassificationResult Classify(AzureFailureSignal signal);
}
