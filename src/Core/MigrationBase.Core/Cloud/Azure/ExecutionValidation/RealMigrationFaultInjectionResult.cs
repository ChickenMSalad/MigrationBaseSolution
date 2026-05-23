namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

/// <summary>
/// Captures observed behavior from an executed fault-injection validation scenario.
/// </summary>
public sealed record RealMigrationFaultInjectionResult
{
    public string ScenarioId { get; init; } = string.Empty;

    public string RunId { get; init; } = string.Empty;

    public bool FaultInjected { get; init; }

    public bool RecoveryObserved { get; init; }

    public bool Passed { get; init; }

    public string EvidenceReference { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;
}
