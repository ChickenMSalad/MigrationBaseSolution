namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReadinessChecklistItem
{
    public required string ItemId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public bool Required { get; init; } = true;

    public AzureProductionReadinessItemStatus Status { get; init; } =
        AzureProductionReadinessItemStatus.NotEvaluated;

    public string? Evidence { get; init; }

    public string? WaiverReason { get; init; }
}
