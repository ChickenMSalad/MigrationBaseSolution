namespace Migration.ControlPlane.Queues;

public sealed class AzureQueueDispatchOptions
{
    public const string SectionName = "AzureQueue";

    public string? AccountName { get; init; }

    public string? ServiceUri { get; init; }

    public string? ConnectionString { get; init; }

    public string QueueName { get; init; } = "migration-runs";

    public bool UseManagedIdentity { get; init; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString) ||
        !string.IsNullOrWhiteSpace(ServiceUri) ||
        !string.IsNullOrWhiteSpace(AccountName);
}
