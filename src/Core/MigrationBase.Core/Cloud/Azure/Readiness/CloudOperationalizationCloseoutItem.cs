namespace MigrationBase.Core.Cloud.Azure.Readiness;

public sealed record CloudOperationalizationCloseoutItem
{
    public string Area { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public CloudOperationalizationCloseoutStatus Status { get; init; } = CloudOperationalizationCloseoutStatus.NotStarted;
    public string OwnerRole { get; init; } = string.Empty;
    public string EvidenceReference { get; init; } = string.Empty;
    public bool IsRequiredForP6 { get; init; }
}
