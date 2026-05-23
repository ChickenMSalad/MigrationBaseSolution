using System;

namespace MigrationBase.Core.Cloud.Azure.Validation.Resumability;

public sealed record RealMigrationResumabilityValidationContract
{
    public string ContractId { get; init; } = "real-migration-resumability-validation";
    public string ScenarioName { get; init; } = string.Empty;
    public string MigrationRunId { get; init; } = string.Empty;
    public string ManifestId { get; init; } = string.Empty;
    public int MinimumCheckpointCount { get; init; }
    public TimeSpan MaximumAllowedRecoveryDelay { get; init; } = TimeSpan.FromMinutes(15);
    public bool RequiresDurableCursorVerification { get; init; } = true;
    public bool RequiresIdempotencyVerification { get; init; } = true;
    public bool RequiresOperatorEvidence { get; init; } = true;
}
