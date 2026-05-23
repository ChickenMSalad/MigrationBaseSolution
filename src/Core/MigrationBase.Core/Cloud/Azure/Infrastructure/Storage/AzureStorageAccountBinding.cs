namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Storage;

public sealed class AzureStorageAccountBinding
{
    public required string Name { get; init; }
    public required string AccountName { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? EndpointSuffix { get; init; }
    public bool UseManagedIdentity { get; init; } = true;
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
