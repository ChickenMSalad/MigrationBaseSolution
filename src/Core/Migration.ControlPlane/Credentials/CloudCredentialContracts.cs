namespace Migration.ControlPlane.Credentials;

public sealed record CloudCredentialReference(
    string WorkspaceId,
    string ConnectorRole,
    string ConnectorKey,
    string CredentialSetId,
    string SecretKind,
    string SecretName);

public sealed record CloudCredentialProviderDescriptor(
    string ProviderKind,
    bool IsConfigured,
    bool UsesManagedIdentity,
    string? KeyVaultUriConfigured,
    string SecretNamePrefix,
    IReadOnlyList<string> SupportedSecretKinds,
    IReadOnlyList<string> Warnings);

public static class CloudCredentialProviderKinds
{
    public const string UserSecrets = "userSecrets";
    public const string KeyVault = "keyVault";
    public const string Local = "local";
    public const string Unknown = "unknown";
}

public static class CloudCredentialSecretKinds
{
    public const string Username = "username";
    public const string Password = "password";
    public const string BearerToken = "bearerToken";
    public const string ApiKey = "apiKey";
    public const string ApiSecret = "apiSecret";
    public const string OAuthClientId = "oauthClientId";
    public const string OAuthClientSecret = "oauthClientSecret";
    public const string ConnectionString = "connectionString";
    public const string AccessKeyId = "accessKeyId";
    public const string SecretAccessKey = "secretAccessKey";

    public static readonly IReadOnlyList<string> All =
    [
        Username,
        Password,
        BearerToken,
        ApiKey,
        ApiSecret,
        OAuthClientId,
        OAuthClientSecret,
        ConnectionString,
        AccessKeyId,
        SecretAccessKey
    ];
}
