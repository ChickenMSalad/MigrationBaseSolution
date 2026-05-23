namespace MigrationBase.Core.Cloud.Azure.Readiness;

public sealed record AzureOperationalizationReadinessSnapshot(
    string EnvironmentName,
    string DeploymentRing,
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyCollection<AzureOperationalizationReadinessItem> Items)
{
    public bool IsProductionReady => Items.Count > 0 && Items
        .Where(item => item.RequiredForProduction)
        .All(item => item.Status == AzureOperationalizationReadinessStatus.Accepted || item.Status == AzureOperationalizationReadinessStatus.Validated);

    public IReadOnlyCollection<AzureOperationalizationReadinessItem> BlockingItems => Items
        .Where(item => item.RequiredForProduction && item.Status == AzureOperationalizationReadinessStatus.Blocked)
        .ToArray();
}
