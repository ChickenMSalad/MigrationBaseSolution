namespace MigrationBase.Core.Cloud.Azure.Readiness;

public sealed record AzureOperationalizationReadinessItem(
    string Key,
    AzureOperationalizationReadinessCategory Category,
    AzureOperationalizationReadinessStatus Status,
    string Description,
    string EvidenceReference,
    bool RequiredForProduction);
