namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed class ProductionGovernanceCloseoutEvaluator : IProductionGovernanceCloseoutEvaluator
{
    public ProductionGovernanceCloseoutResult Evaluate(ProductionGovernanceCloseoutManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var blockingItems = manifest.HandoffItems
            .Where(item => item.Status is ProductionGovernanceCloseoutStatus.NotStarted or ProductionGovernanceCloseoutStatus.Blocked)
            .Select(item => item.Key)
            .ToArray();

        var warnings = manifest.HandoffItems
            .Where(item => string.IsNullOrWhiteSpace(item.EvidenceReference))
            .Select(item => $"Missing evidence reference: {item.Key}")
            .ToArray();

        var status = blockingItems.Length > 0
            ? ProductionGovernanceCloseoutStatus.Blocked
            : manifest.HandoffItems.All(item => item.Status == ProductionGovernanceCloseoutStatus.Accepted)
                ? ProductionGovernanceCloseoutStatus.Accepted
                : ProductionGovernanceCloseoutStatus.ReadyForOperationalHandoff;

        return new ProductionGovernanceCloseoutResult
        {
            Status = status,
            BlockingItems = blockingItems,
            Warnings = warnings
        };
    }
}
