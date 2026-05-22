namespace Migration.ControlPlane.Storage;

public interface ICloudStoragePathResolver
{
    CloudStorageLocation ResolveWorkspaceRoot(string workspaceId);

    CloudStorageLocation ResolveProjectRoot(string workspaceId, string projectId);

    CloudStorageLocation ResolveRunRoot(string workspaceId, string runId);

    CloudStorageLocation ResolveArtifactRoot(string workspaceId, string artifactKind);

    CloudStorageLocation ResolveAuditRoot(string workspaceId);
}
