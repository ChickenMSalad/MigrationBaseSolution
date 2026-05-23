namespace MigrationBase.Core.Cloud.Azure.Observability;

public interface IAzureHealthSignalRegistry
{
    IReadOnlyCollection<AzureHealthSignalDescriptor> GetSignals();
    AzureHealthSignalDescriptor? FindByName(string name);
}
