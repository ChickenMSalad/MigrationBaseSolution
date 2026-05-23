namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureClassificationResult
{
    public required AzureFailureClassification Classification { get; init; }

    public AzureFailureSeverity Severity { get; init; } = AzureFailureSeverity.Error;

    public bool RetryRecommended { get; init; }

    public bool ReplayRecommended { get; init; }

    public string? Reason { get; init; }
}
