namespace MigrationBase.Core.Cloud.Azure.Integration.Readiness;

public sealed class AzureIntegrationBoundaryReadinessFinding
{
    public required string Name { get; init; }
    public required string Boundary { get; init; }
    public AzureIntegrationBoundaryReadinessLevel Level { get; init; } = AzureIntegrationBoundaryReadinessLevel.Required;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
