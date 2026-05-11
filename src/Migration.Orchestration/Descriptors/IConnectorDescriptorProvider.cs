namespace Migration.Orchestration.Descriptors;

public interface IConnectorDescriptorProvider
{
    IReadOnlyList<ConnectorDescriptor> GetAll();
    ConnectorDescriptor? Find(string type);
}
