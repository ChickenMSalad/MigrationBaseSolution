namespace MigrationBase.Core.Cloud.Azure.Observability;

public sealed class AzureHealthSignalRegistry : IAzureHealthSignalRegistry
{
    private readonly IReadOnlyList<AzureHealthSignalDescriptor> _signals;

    public AzureHealthSignalRegistry(IEnumerable<AzureHealthSignalDescriptor> signals)
    {
        _signals = signals?.Where(signal => !string.IsNullOrWhiteSpace(signal.Name)).ToArray()
            ?? Array.Empty<AzureHealthSignalDescriptor>();
    }

    public IReadOnlyCollection<AzureHealthSignalDescriptor> GetSignals() => _signals;

    public AzureHealthSignalDescriptor? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _signals.FirstOrDefault(signal => string.Equals(signal.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
