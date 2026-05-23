using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Observability.Anomalies;

/// <summary>
/// Describes an operational anomaly rule used to detect unhealthy Azure migration runtime behavior.
/// </summary>
public sealed class AzureAnomalyDetectionRuleDescriptor
{
    public string RuleId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string SignalName { get; init; } = string.Empty;

    public AzureAnomalyDetectionRuleSeverity Severity { get; init; } = AzureAnomalyDetectionRuleSeverity.Warning;

    public AzureAnomalyDetectionWindow EvaluationWindow { get; init; } = new();

    public AzureAnomalyDetectionThreshold Threshold { get; init; } = new();

    public IReadOnlyList<string> AppliesToHostRoles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
