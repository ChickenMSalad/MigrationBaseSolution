namespace MigrationBase.Core.Cloud.Azure.Observability;

public interface IAzureStructuredLogEventRegistry
{
    IReadOnlyCollection<AzureStructuredLogEventDescriptor> GetDescriptors();

    AzureStructuredLogEventDescriptor? FindByName(string eventName);
}
