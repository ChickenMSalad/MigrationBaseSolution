namespace MigrationBase.Core.Cloud.Azure.Integration.Queues;

public sealed class AzureQueueBindingDescriptor
{
    public required string Name { get; init; }
    public required string QueueName { get; init; }
    public string? ConnectionName { get; init; }
    public string? StorageAccountName { get; init; }
    public string Purpose { get; init; } = string.Empty;
    public bool IsRequired { get; init; } = true;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
