namespace MigrationBase.Core.Cloud.Azure.Observability;

public sealed class AzureHealthSignalDescriptor
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public AzureHealthSignalSeverity DefaultSeverity { get; init; } = AzureHealthSignalSeverity.Unknown;
    public bool BlocksDeployment { get; init; }
    public bool BlocksExecution { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}
