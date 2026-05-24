namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionRollbackOptions
{
    public const string SectionName = "AzureRuntime:ProductionRollback";

    public bool Enabled { get; set; } = true;

    public bool RequireRollbackOnBlockedGate { get; set; } = true;

    public bool AllowOperatorRollback { get; set; } = true;

    public bool RequireAbortConfirmation { get; set; } = true;
}
