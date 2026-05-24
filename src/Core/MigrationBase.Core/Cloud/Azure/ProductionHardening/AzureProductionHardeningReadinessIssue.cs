namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionHardeningReadinessIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Component { get; init; }
}
