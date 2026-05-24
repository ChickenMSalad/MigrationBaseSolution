namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReleaseGateOptions
{
    public const string SectionName = "AzureRuntime:ProductionReleaseGate";

    public bool Enabled { get; set; } = true;

    public bool RequirePassedEvidenceReport { get; set; } = true;

    public bool AllowWarnings { get; set; }

    public bool AllowOperatorOverride { get; set; } = true;
}
