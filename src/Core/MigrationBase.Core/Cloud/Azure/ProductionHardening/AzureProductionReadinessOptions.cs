namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReadinessOptions
{
    public const string SectionName = "AzureRuntime:ProductionReadiness";

    public bool Enabled { get; set; } = true;

    public bool RequireReleaseGate { get; set; } = true;

    public bool RequireRollbackDecision { get; set; } = true;

    public bool RequireOperatorSignoff { get; set; } = true;

    public bool AllowWaivers { get; set; }
}
