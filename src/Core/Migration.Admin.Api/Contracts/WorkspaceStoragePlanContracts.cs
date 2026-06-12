namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Describes planned workspace-scoped storage roots without changing current storage behavior.
/// </summary>
public sealed record WorkspaceStoragePlanDescriptor(
    string WorkspaceId,
    string StorageMode,
    string ControlPlaneRoot,
    string WorkspaceRoot,
    string ProjectsRoot,
    string RunsRoot,
    string ArtifactsRoot,
    string CredentialsRoot,
    bool IsLocalFileSystem,
    bool IsCloudBlob,
    IReadOnlyList<string> Warnings);

public static class WorkspaceStoragePathSegments
{
    public const string Workspaces = "workspaces";
    public const string Projects = "projects";
    public const string Runs = "runs";
    public const string Artifacts = "artifacts";
    public const string Credentials = "credentials";
}


