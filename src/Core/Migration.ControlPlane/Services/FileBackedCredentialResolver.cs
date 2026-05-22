namespace Migration.ControlPlane.Services;

public sealed class FileBackedCredentialResolver : ICredentialResolver
{
    private readonly ICredentialSetStore _store;

    public FileBackedCredentialResolver(ICredentialSetStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyDictionary<string, string?>> ResolveAsync(string credentialSetId, CancellationToken cancellationToken = default)
    {
        var credentialSet = await _store.GetAsync(credentialSetId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Credential set '{credentialSetId}' was not found.");

        return credentialSet.Values;
    }
}
