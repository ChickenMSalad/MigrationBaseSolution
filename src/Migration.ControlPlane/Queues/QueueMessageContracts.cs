namespace Migration.ControlPlane.Queues;

public sealed record QueueMessageEnvelope(
    string MessageId,
    string MessageType,
    string WorkspaceId,
    string? TenantId,
    string? ProjectId,
    string? RunId,
    string IdempotencyKey,
    DateTimeOffset CreatedUtc,
    IReadOnlyDictionary<string, string> Properties);

public sealed record QueueProviderDescriptor(
    string ProviderKind,
    bool SupportsDeadLettering,
    bool SupportsSessions,
    bool SupportsScheduledMessages,
    IReadOnlyList<string> RecommendedProperties,
    IReadOnlyList<string> Warnings);

public static class QueueMessagePropertyNames
{
    public const string RunId = "runId";
    public const string ProjectId = "projectId";
    public const string WorkspaceId = "workspaceId";
    public const string TenantId = "tenantId";
    public const string IdempotencyKey = "idempotencyKey";
    public const string LeaseResource = "leaseResource";
    public const string Attempt = "attempt";
    public const string CreatedUtc = "createdUtc";

    public static readonly IReadOnlyList<string> Recommended =
    [
        RunId,
        ProjectId,
        WorkspaceId,
        TenantId,
        IdempotencyKey,
        LeaseResource,
        Attempt,
        CreatedUtc
    ];
}
