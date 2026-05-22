namespace Migration.Application.Operational.Credentials;

public sealed record ConnectorCredentialReference(
    string ConnectorKey,
    string SecretProvider,
    string SecretReferenceName,
    string? Description);
