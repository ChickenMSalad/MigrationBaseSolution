using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Represents the closeout decision for the observability baseline of an Azure-hosted migration environment.
/// </summary>
public sealed class AzureObservabilityCloseoutReport
{
    public AzureObservabilityCloseoutReport(
        string environmentName,
        DateTimeOffset evaluatedAtUtc,
        IEnumerable<AzureObservabilityCloseoutGateResult>? gateResults = null)
    {
        EnvironmentName = string.IsNullOrWhiteSpace(environmentName) ? throw new ArgumentException("Environment name is required.", nameof(environmentName)) : environmentName.Trim();
        EvaluatedAtUtc = evaluatedAtUtc;
        GateResults = new ReadOnlyCollection<AzureObservabilityCloseoutGateResult>((gateResults ?? Array.Empty<AzureObservabilityCloseoutGateResult>()).ToArray());
    }

    public string EnvironmentName { get; }

    public DateTimeOffset EvaluatedAtUtc { get; }

    public IReadOnlyList<AzureObservabilityCloseoutGateResult> GateResults { get; }

    public bool IsReadyForOperationalHandoff => GateResults.All(result => result.IsSatisfied || result.Severity != AzureObservabilityCloseoutGateSeverity.Blocking);
}
