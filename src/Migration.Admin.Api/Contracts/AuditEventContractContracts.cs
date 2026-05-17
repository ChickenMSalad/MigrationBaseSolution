namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Safe audit event contract for cloud operations. This is a schema/plan only;
/// it does not persist audit events yet.
/// </summary>
public sealed record AuditEventContractDescriptor(
    string EnvironmentName,
    string AuditMode,
    string WorkspaceId,
    string? TenantId,
    bool PersistenceEnabled,
    string ProviderKind,
    string? AuditStorageRoot,
    IReadOnlyList<string> SupportedEventTypes,
    IReadOnlyList<string> RequiredProperties,
    IReadOnlyList<string> RedactedProperties,
    IReadOnlyList<string> Warnings);

public static class AuditEventProviderKinds
{
    public const string None = "none";
    public const string LocalFile = "localFile";
    public const string AzureBlob = "azureBlob";
    public const string ApplicationInsights = "applicationInsights";
    public const string Unknown = "unknown";
}

public static class AuditEventTypes
{
    public const string ProjectCreated = "project.created";
    public const string ProjectUpdated = "project.updated";
    public const string ProjectDeleted = "project.deleted";
    public const string RunQueued = "run.queued";
    public const string RunCanceled = "run.canceled";
    public const string CredentialCreated = "credential.created";
    public const string CredentialDeleted = "credential.deleted";
    public const string ArtifactUploaded = "artifact.uploaded";
    public const string ArtifactDeleted = "artifact.deleted";
    public const string WorkspaceContextResolved = "workspace.contextResolved";
}
