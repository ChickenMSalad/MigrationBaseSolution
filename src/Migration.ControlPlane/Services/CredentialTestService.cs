using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

public sealed class CredentialTestService
{
    public Task<CredentialTestResult> TestAsync(CredentialSetRecord credentialSet, CancellationToken cancellationToken = default)
    {
        var missing = credentialSet.Values
            .Where(x => string.IsNullOrWhiteSpace(x.Value))
            .Select(x => x.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missing.Length > 0)
        {
            return Task.FromResult(new CredentialTestResult(
                credentialSet.CredentialSetId,
                credentialSet.ConnectorType,
                credentialSet.ConnectorRole,
                false,
                $"Credential set has empty values for: {string.Join(", ", missing)}.",
                DateTimeOffset.UtcNow));
        }

        return Task.FromResult(new CredentialTestResult(
            credentialSet.CredentialSetId,
            credentialSet.ConnectorType,
            credentialSet.ConnectorRole,
            true,
            "Local credential set validation passed. Provider-specific live credential tests can be added connector by connector without changing legacy hosts.",
            DateTimeOffset.UtcNow));
    }
}
