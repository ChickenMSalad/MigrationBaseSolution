namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionDeploymentOptions
{
    public const string SectionName = "AzureRuntime:ProductionDeployment";

    public bool Enabled { get; set; } = true;

    public bool RequireReadinessChecklist { get; set; } = true;

    public bool RequireRollbackDecision { get; set; } = true;

    public bool AllowOperatorOverride { get; set; } = true;

    public bool RequireDeploymentEvidenceRecord { get; set; } = true;
}
