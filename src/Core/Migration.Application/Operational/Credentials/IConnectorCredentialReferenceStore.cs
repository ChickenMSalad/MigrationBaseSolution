namespace Migration.Application.Operational.Credentials;

public interface IConnectorCredentialReferenceStore
{
    Task<IReadOnlyList<ConnectorCredentialReference>> ListAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ConnectorCredentialReference reference, CancellationToken cancellationToken = default);
}
