namespace MigrationBase.Core.Cloud.Azure.Validation;

public sealed class AzureRuntimeValidationGateResult
{
    public string GateKey { get; init; } = string.Empty;

    public AzureRuntimeValidationGateStatus Status { get; init; } = AzureRuntimeValidationGateStatus.NotEvaluated;

    public AzureRuntimeValidationGateSeverity Severity { get; init; } = AzureRuntimeValidationGateSeverity.Blocking;

    public string Message { get; init; } = string.Empty;

    public string? EvidenceReference { get; init; }

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public static AzureRuntimeValidationGateResult Passed(string gateKey, string message, string? evidenceReference = null)
    {
        return new AzureRuntimeValidationGateResult
        {
            GateKey = gateKey,
            Status = AzureRuntimeValidationGateStatus.Passed,
            Severity = AzureRuntimeValidationGateSeverity.Informational,
            Message = message,
            EvidenceReference = evidenceReference
        };
    }

    public static AzureRuntimeValidationGateResult Failed(
        string gateKey,
        AzureRuntimeValidationGateSeverity severity,
        string message,
        string? evidenceReference = null)
    {
        return new AzureRuntimeValidationGateResult
        {
            GateKey = gateKey,
            Status = AzureRuntimeValidationGateStatus.Failed,
            Severity = severity,
            Message = message,
            EvidenceReference = evidenceReference
        };
    }
}
