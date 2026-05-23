namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

/// <summary>
/// Describes a controlled failure scenario used to validate real migration execution recovery behavior.
/// </summary>
public sealed record RealMigrationFaultInjectionScenario
{
    public string ScenarioId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string TargetStage { get; init; } = string.Empty;

    public string FaultType { get; init; } = string.Empty;

    public string ExpectedRecoveryBehavior { get; init; } = string.Empty;

    public bool RequiresOperatorApproval { get; init; }

    public bool EnabledByDefault { get; init; }
}
