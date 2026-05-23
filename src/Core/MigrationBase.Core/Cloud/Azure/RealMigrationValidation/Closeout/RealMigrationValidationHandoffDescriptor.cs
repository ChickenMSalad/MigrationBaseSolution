namespace MigrationBase.Core.Cloud.Azure.RealMigrationValidation.Closeout;

public sealed class RealMigrationValidationHandoffDescriptor
{
    public string HandoffKey { get; set; } = string.Empty;

    public string SourceValidationRunId { get; set; } = string.Empty;

    public string TargetOperationalPhase { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public IReadOnlyCollection<string> RequiredArtifacts { get; set; } = Array.Empty<string>();

    public IReadOnlyCollection<string> OpenRisks { get; set; } = Array.Empty<string>();

    public bool IsApprovedForNextPhase { get; set; }
}
