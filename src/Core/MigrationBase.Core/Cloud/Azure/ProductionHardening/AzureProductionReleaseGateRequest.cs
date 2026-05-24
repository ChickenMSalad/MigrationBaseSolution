using MigrationBase.Core.Cloud.Azure.EndToEndValidation;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReleaseGateRequest
{
    public required string ReleaseId { get; init; }

    public required AzureEndToEndEvidenceReport EvidenceReport { get; init; }

    public bool RequirePassedEvidenceReport { get; init; } = true;

    public bool AllowWarnings { get; init; }

    public bool OperatorOverrideGranted { get; init; }
}
