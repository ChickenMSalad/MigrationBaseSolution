namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

/// <summary>
/// Groups controlled fault-injection scenarios for a real migration validation run.
/// </summary>
public sealed record RealMigrationFaultInjectionPlan
{
    public string PlanId { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public string MigrationProfileName { get; init; } = string.Empty;

    public IReadOnlyList<RealMigrationFaultInjectionScenario> Scenarios { get; init; } = Array.Empty<RealMigrationFaultInjectionScenario>();
}
