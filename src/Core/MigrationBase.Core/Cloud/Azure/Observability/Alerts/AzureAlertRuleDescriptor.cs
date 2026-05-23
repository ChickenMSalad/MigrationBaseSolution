using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Observability.Alerts;

public sealed class AzureAlertRuleDescriptor
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SignalName { get; init; } = string.Empty;
    public AzureAlertSeverity Severity { get; init; } = AzureAlertSeverity.Warning;
    public AzureAlertEvaluationWindow EvaluationWindow { get; init; } = new();
    public AzureAlertThreshold Threshold { get; init; } = new();
    public IReadOnlyList<string> AppliesToHostRoles { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Dimensions { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public bool EnabledByDefault { get; init; } = true;
}
