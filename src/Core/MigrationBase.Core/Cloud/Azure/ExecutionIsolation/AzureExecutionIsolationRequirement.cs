namespace MigrationBase.Core.Cloud.Azure.ExecutionIsolation;

public sealed record AzureExecutionIsolationRequirement(
    string RequirementKey,
    string Description,
    bool RequiredForProduction,
    string EvidenceKey);
