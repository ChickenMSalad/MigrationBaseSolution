using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Describes a required observability capability that should be present before a migration runtime is promoted.
/// </summary>
public sealed class AzureObservabilityCloseoutGate
{
    public AzureObservabilityCloseoutGate(
        string gateCode,
        string displayName,
        AzureObservabilityCloseoutGateSeverity severity,
        IEnumerable<string>? requiredEvidenceKeys = null)
    {
        GateCode = string.IsNullOrWhiteSpace(gateCode) ? throw new ArgumentException("Gate code is required.", nameof(gateCode)) : gateCode.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Display name is required.", nameof(displayName)) : displayName.Trim();
        Severity = severity;
        RequiredEvidenceKeys = new ReadOnlyCollection<string>((requiredEvidenceKeys ?? Array.Empty<string>())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }

    public string GateCode { get; }

    public string DisplayName { get; }

    public AzureObservabilityCloseoutGateSeverity Severity { get; }

    public IReadOnlyList<string> RequiredEvidenceKeys { get; }
}
