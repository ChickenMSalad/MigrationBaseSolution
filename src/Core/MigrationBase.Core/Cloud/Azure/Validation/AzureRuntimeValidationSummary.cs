namespace MigrationBase.Core.Cloud.Azure.Validation;

public sealed class AzureRuntimeValidationSummary
{
    public string EnvironmentName { get; init; } = string.Empty;

    public string HostRole { get; init; } = string.Empty;

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureRuntimeValidationGateResult> Results { get; init; } = Array.Empty<AzureRuntimeValidationGateResult>();

    public bool HasBlockingFailures => Results.Any(result =>
        result.Status == AzureRuntimeValidationGateStatus.Failed
        && result.Severity == AzureRuntimeValidationGateSeverity.Blocking);

    public bool IsDeployable => !HasBlockingFailures;
}
