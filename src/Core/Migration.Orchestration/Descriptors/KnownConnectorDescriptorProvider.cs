namespace Migration.Orchestration.Descriptors;

public sealed class KnownConnectorDescriptorProvider : IConnectorDescriptorProvider
{
    private readonly KnownConnectorCatalog _catalog;

    public KnownConnectorDescriptorProvider(KnownConnectorCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<ConnectorDescriptor> GetAll()
    {
        return _catalog.GetAll();
    }

    public ConnectorDescriptor? Find(string type)
    {
        return _catalog.Find(type);
    }
}
