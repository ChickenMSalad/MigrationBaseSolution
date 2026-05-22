namespace Migration.ControlPlane.Auth;

public sealed record EndpointPolicyInventoryItem(
    string Area,
    string RoutePrefix,
    string RecommendedPolicy,
    string RequiredScope,
    bool MutatesState,
    bool ExposesSecretsOrCredentials,
    bool OperationallySensitive,
    string Notes);

public sealed record EndpointPolicyInventorySnapshot(
    DateTimeOffset GeneratedUtc,
    IReadOnlyList<EndpointPolicyInventoryItem> Items,
    int ReadOnlyCount,
    int MutatingCount,
    int CredentialSensitiveCount,
    int OperationallySensitiveCount,
    IReadOnlyList<string> Warnings);
