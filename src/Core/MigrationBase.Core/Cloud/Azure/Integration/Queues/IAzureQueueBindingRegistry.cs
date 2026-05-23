namespace MigrationBase.Core.Cloud.Azure.Integration.Queues;

public interface IAzureQueueBindingRegistry
{
    IReadOnlyCollection<AzureQueueBindingDescriptor> GetBindings();
    AzureQueueBindingDescriptor? FindByName(string name);
    AzureQueueBindingValidationResult Validate();
}
