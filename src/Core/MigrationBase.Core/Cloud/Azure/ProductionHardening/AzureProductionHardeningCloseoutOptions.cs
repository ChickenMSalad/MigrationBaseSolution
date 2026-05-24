namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionHardeningCloseoutOptions
{
    public const string SectionName = "AzureRuntime:ProductionHardeningCloseout";

    public bool Enabled { get; set; } = true;

    public bool RequireReleaseGate { get; set; } = true;

    public bool RequireRollbackGate { get; set; } = true;

    public bool RequireReadinessChecklist { get; set; } = true;

    public bool RequireDeploymentDecision { get; set; } = true;
}
