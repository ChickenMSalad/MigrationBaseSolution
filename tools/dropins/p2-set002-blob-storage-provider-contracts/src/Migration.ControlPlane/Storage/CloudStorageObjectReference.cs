namespace Migration.ControlPlane.Storage;

/// <summary>
/// Stable logical reference to an object/blob.
/// </summary>
public sealed record CloudStorageObjectReference(
    string WorkspaceId,
    string Category,
    string ObjectKey,
    CloudStorageLocation Location,
    string? ContentType = null,
    long? Length = null,
    string? ETag = null);
