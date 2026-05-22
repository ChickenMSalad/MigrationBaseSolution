using Migration.Application.Operational.Credentials;

namespace Migration.Infrastructure.Sql.Credentials;

public sealed class SqlConnectorCredentialReferenceStore : IConnectorCredentialReferenceStore
{
    public Task<IReadOnlyList<ConnectorCredentialReference>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ConnectorCredentialReference> references = Array.Empty<ConnectorCredentialReference>();
        return Task.FromResult(references);
    }

    public Task SaveAsync(ConnectorCredentialReference reference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return Task.CompletedTask;
    }
}
