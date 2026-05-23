using System;

namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Captures the result of a single observability closeout gate evaluation.
/// </summary>
public sealed class AzureObservabilityCloseoutGateResult
{
    public AzureObservabilityCloseoutGateResult(
        string gateCode,
        AzureObservabilityCloseoutGateSeverity severity,
        bool isSatisfied,
        string? detail = null)
    {
        GateCode = string.IsNullOrWhiteSpace(gateCode) ? throw new ArgumentException("Gate code is required.", nameof(gateCode)) : gateCode.Trim();
        Severity = severity;
        IsSatisfied = isSatisfied;
        Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();
    }

    public string GateCode { get; }

    public AzureObservabilityCloseoutGateSeverity Severity { get; }

    public bool IsSatisfied { get; }

    public string? Detail { get; }
}
