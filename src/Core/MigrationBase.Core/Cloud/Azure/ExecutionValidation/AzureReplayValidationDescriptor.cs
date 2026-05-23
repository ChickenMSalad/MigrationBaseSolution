namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

/// <summary>
/// Describes replay validation expectations for a real migration execution run.
/// This is a contract-only descriptor; it does not execute replay behavior.
/// </summary>
public sealed record AzureReplayValidationDescriptor
{
    public required string ValidationId { get; init; }

    public required string MigrationRunId { get; init; }

    public required string SourceSystem { get; init; }

    public required string TargetSystem { get; init; }

    public AzureReplayValidationMode Mode { get; init; } = AzureReplayValidationMode.DryRun;

    public bool RequireApprovalBeforeReplay { get; init; } = true;

    public bool RequireSourceTargetMappingVerification { get; init; } = true;

    public bool RequireIdempotencyEvidence { get; init; } = true;

    public IReadOnlyList<string> RequiredEvidenceKeys { get; init; } = Array.Empty<string>();
}
