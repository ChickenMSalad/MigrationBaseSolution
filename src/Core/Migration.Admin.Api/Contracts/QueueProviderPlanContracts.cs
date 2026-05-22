namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Safe cloud-facing queue provider plan. This describes intended queue topology
/// without exposing connection strings or changing current queue behavior.
/// </summary>
public sealed record QueueProviderPlanDescriptor(
    string EnvironmentName,
    string WorkspaceId,
    string QueueProvider,
    string ProviderKind,
    string LogicalQueueName,
    string WorkspaceQueueName,
    string? ServiceBusNamespace,
    string? StorageAccountName,
    bool UsesInMemory,
    bool UsesAzureStorageQueue,
    bool UsesServiceBus,
    bool RequiresManagedIdentity,
    bool SupportsDeadLettering,
    bool SupportsSessions,
    bool SupportsScheduledMessages,
    IReadOnlyList<string> RecommendedMessageProperties,
    IReadOnlyList<string> Warnings);

public static class QueueProviderKinds
{
    public const string InMemory = "inMemory";
    public const string AzureStorageQueue = "azureStorageQueue";
    public const string AzureServiceBusQueue = "azureServiceBusQueue";
    public const string Unknown = "unknown";
}
