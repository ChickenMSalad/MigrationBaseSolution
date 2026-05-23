using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

/// <summary>
/// Describes a real migration execution validation scenario that can be approved,
/// executed, observed, and audited without treating CSV or Excel files as the durable run store.
/// </summary>
public sealed class AzureRealMigrationValidationScenario
{
    public string ScenarioName { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string SourceSystem { get; init; } = string.Empty;
    public string TargetSystem { get; init; } = string.Empty;
    public string ManifestStoreName { get; init; } = string.Empty;
    public int ExpectedManifestRowCount { get; init; }
    public int MaximumAllowedFailures { get; init; }
    public bool RequiresOperatorApproval { get; init; } = true;
    public bool RequiresReplayValidation { get; init; } = true;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ScenarioName) &&
        !string.IsNullOrWhiteSpace(EnvironmentName) &&
        !string.IsNullOrWhiteSpace(SourceSystem) &&
        !string.IsNullOrWhiteSpace(TargetSystem) &&
        !string.IsNullOrWhiteSpace(ManifestStoreName) &&
        ExpectedManifestRowCount > 0 &&
        MaximumAllowedFailures >= 0;
}
