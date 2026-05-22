namespace Migration.ControlPlane.Services;

public interface ICredentialResolver
{
    Task<IReadOnlyDictionary<string, string?>> ResolveAsync(string credentialSetId, CancellationToken cancellationToken = default);
}
