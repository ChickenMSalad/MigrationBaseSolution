using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

public interface ICredentialSetStore
{
    Task<IReadOnlyList<CredentialSetRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task<CredentialSetRecord?> GetAsync(string credentialSetId, CancellationToken cancellationToken = default);
    Task SaveAsync(CredentialSetRecord credentialSet, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string credentialSetId, CancellationToken cancellationToken = default);
}
