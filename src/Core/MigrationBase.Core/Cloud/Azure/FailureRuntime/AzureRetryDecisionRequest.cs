namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureRetryDecisionRequest
{
    public required AzureFailureSignal Signal { get; init; }

    public required AzureFailureClassificationResult Classification { get; init; }

    public AzureRetryPolicy Policy { get; init; } = new();

    public int CurrentAttemptNumber => Signal.AttemptNumber;
}
