using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

public sealed class CredentialTestService
{
    private readonly ICredentialResolver _credentialResolver;

    public CredentialTestService(ICredentialResolver credentialResolver)
    {
        _credentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
    }

    public async Task<CredentialTestResult> TestAsync(
        CredentialSetRecord credentialSet,
        CancellationToken cancellationToken = default)
    {
        var missing = credentialSet.Values
            .Where(x => string.IsNullOrWhiteSpace(x.Value))
            .Select(x => x.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missing.Length > 0)
        {
            return new CredentialTestResult(
                credentialSet.CredentialSetId,
                credentialSet.ConnectorType,
                credentialSet.ConnectorRole,
                false,
                $"Credential set has empty values for: {string.Join(", ", missing)}.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            await _credentialResolver.ResolveAsync(credentialSet.CredentialSetId, cancellationToken).ConfigureAwait(false);
            return new CredentialTestResult(
                credentialSet.CredentialSetId,
                credentialSet.ConnectorType,
                credentialSet.ConnectorRole,
                true,
                "Credential references resolved successfully.",
                DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            return new CredentialTestResult(
                credentialSet.CredentialSetId,
                credentialSet.ConnectorType,
                credentialSet.ConnectorRole,
                false,
                ex.Message,
                DateTimeOffset.UtcNow);
        }
    }
}
