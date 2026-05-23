namespace MigrationBase.Core.Cloud.Azure.Integration.Readiness;

public sealed class AzureIntegrationBoundaryReadinessCheck
{
    public required string Name { get; init; }
    public required string Boundary { get; init; }
    public string Description { get; init; } = string.Empty;
    public AzureIntegrationBoundaryReadinessLevel Level { get; init; } = AzureIntegrationBoundaryReadinessLevel.Required;
    public IReadOnlyCollection<string> RequiredConfigurationKeys { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> RequiredCapabilities { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
