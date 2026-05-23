namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public interface IAzureReplayAdmissionController
{
    AzureReplayAdmissionDecision Decide(AzureReplayAdmissionRequest request);
}
