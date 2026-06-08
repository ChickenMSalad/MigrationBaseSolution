namespace Migration.ControlPlane.Storage;

/// <summary>
/// Resolves stable workspace-scoped storage locations for local and future
/// Azure Blob-backed storage. This does not perform IO.
/// </summary>
public sealed class CloudStoragePathResolver : ICloudStoragePathResolver
{
    private readonly string _root;
    private readonly string _provider;

    public CloudStoragePathResolver(string root)
    {
        _root = string.IsNullOrWhiteSpace(root)
            ? "Runtime/admin-api"
            : root.Trim();

        _provider = IsBlobRoot(_root)
            ? CloudStorageProviders.AzureBlob
            : CloudStorageProviders.LocalFileSystem;
    }

    public CloudStorageLocation ResolveWorkspaceRoot(string workspaceId)
    {
        var relativePath = Combine("workspaces", NormalizeSegment(workspaceId));
        return Create(relativePath);
    }

    public CloudStorageLocation ResolveProjectRoot(string workspaceId, string projectId)
    {
        var relativePath = Combine("workspaces", NormalizeSegment(workspaceId), "projects", NormalizeSegment(projectId));
        return Create(relativePath);
    }

    public CloudStorageLocation ResolveRunRoot(string workspaceId, string runId)
    {
        var relativePath = Combine("workspaces", NormalizeSegment(workspaceId), "runs", NormalizeSegment(runId));
        return Create(relativePath);
    }

    public CloudStorageLocation ResolveArtifactRoot(string workspaceId, string artifactKind)
    {
        var relativePath = Combine("workspaces", NormalizeSegment(workspaceId), "artifacts", NormalizeSegment(artifactKind));
        return Create(relativePath);
    }

    public CloudStorageLocation ResolveAuditRoot(string workspaceId)
    {
        var relativePath = Combine("workspaces", NormalizeSegment(workspaceId), "audit");
        return Create(relativePath);
    }

    private CloudStorageLocation Create(string relativePath)
    {
        var uri = Combine(_root, relativePath);

        var workspaceId = relativePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(x => !string.Equals(x, "workspaces", StringComparison.OrdinalIgnoreCase))
            .Skip(1)
            .FirstOrDefault() ?? "default";

        return new CloudStorageLocation(
            Provider: _provider,
            Root: _root,
            WorkspaceId: workspaceId,
            RelativePath: relativePath,
            Uri: uri);
    }

    private static bool IsBlobRoot(string root) =>
        root.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
        root.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string Combine(params string[] segments)
    {
        var cleaned = segments
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(segment => segment.Trim().Trim('/', '\\'))
            .ToArray();

        if (cleaned.Length == 0)
        {
            return string.Empty;
        }

        var first = cleaned[0];

        if (first.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
            first.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return first.TrimEnd('/') + "/" + string.Join("/", cleaned.Skip(1));
        }

        return string.Join("/", cleaned);
    }

    private static string NormalizeSegment(string value)
    {
        var sanitized = new string((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? "default"
            : sanitized.Trim('-');
    }
}
