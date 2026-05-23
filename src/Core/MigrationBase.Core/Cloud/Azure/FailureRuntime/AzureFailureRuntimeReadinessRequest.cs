namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureRuntimeReadinessRequest
{
    public required string RuntimeName { get; init; }

    public bool RequireFailureClassifier { get; init; } = true;

    public bool RequireRetryDecisionEngine { get; init; } = true;

    public bool RequireReplayEligibilityEvaluator { get; init; } = true;

    public bool RequireReplayAdmissionController { get; init; } = true;

    public bool RequireIncidentStore { get; init; } = true;
}
