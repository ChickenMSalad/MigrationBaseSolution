namespace Migration.ControlPlane.Credentials;

public sealed class CloudCredentialNameResolver : ICloudCredentialNameResolver
{
    private readonly string _prefix;

    public CloudCredentialNameResolver(string prefix)
    {
        _prefix = string.IsNullOrWhiteSpace(prefix)
            ? "migration"
            : Normalize(prefix);
    }

    public CloudCredentialReference Resolve(
        string workspaceId,
        string connectorRole,
        string connectorKey,
        string credentialSetId,
        string secretKind)
    {
        var normalizedWorkspace = Normalize(workspaceId, "default");
        var normalizedRole = Normalize(connectorRole, "connector");
        var normalizedConnector = Normalize(connectorKey, "unknown");
        var normalizedCredentialSet = Normalize(credentialSetId, "default");
        var normalizedSecretKind = Normalize(secretKind, "secret");

        var secretName = string.Join("--",
            _prefix,
            "workspace",
            normalizedWorkspace,
            "connector",
            normalizedRole,
            normalizedConnector,
            "credential",
            normalizedCredentialSet,
            normalizedSecretKind);

        return new CloudCredentialReference(
            WorkspaceId: normalizedWorkspace,
            ConnectorRole: normalizedRole,
            ConnectorKey: normalizedConnector,
            CredentialSetId: normalizedCredentialSet,
            SecretKind: normalizedSecretKind,
            SecretName: secretName);
    }

    private static string Normalize(string value, string fallback = "default")
    {
        var sanitized = new string((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' ? ch : '-')
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? fallback
            : sanitized.Trim('-');
    }
}
