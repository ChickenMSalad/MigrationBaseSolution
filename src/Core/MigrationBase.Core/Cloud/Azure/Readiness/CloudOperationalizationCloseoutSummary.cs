namespace MigrationBase.Core.Cloud.Azure.Readiness;

public sealed record CloudOperationalizationCloseoutSummary
{
    public string Phase { get; init; } = "P5";
    public string Name { get; init; } = "Cloud Operationalization & Real Migration Execution";
    public CloudOperationalizationCloseoutStatus Status { get; init; } = CloudOperationalizationCloseoutStatus.ReadyForImplementation;
    public IReadOnlyList<CloudOperationalizationCloseoutItem> Items { get; init; } = Array.Empty<CloudOperationalizationCloseoutItem>();

    public bool HasBlockingItems => Items.Any(static item => item.Status == CloudOperationalizationCloseoutStatus.Blocked);
    public bool IsReadyForP6 => !HasBlockingItems && Items.Where(static item => item.IsRequiredForP6).All(static item => item.Status is CloudOperationalizationCloseoutStatus.ReadyForImplementation or CloudOperationalizationCloseoutStatus.Completed);
}
