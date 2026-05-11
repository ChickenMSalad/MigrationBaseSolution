using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.ControlPlane.Options;

namespace Migration.ControlPlane.Services;

public sealed record ControlPlaneDeleteResult(
    string EntityType,
    string EntityId,
    int DeletedFiles,
    int DeletedDirectories,
    IReadOnlyList<string> DeletedPaths,
    IReadOnlyList<string> Warnings);

public sealed class ControlPlaneDeleteService
{
    private readonly AdminApiOptions _options;
    private readonly ILogger<ControlPlaneDeleteService> _logger;

    public ControlPlaneDeleteService(
        IOptions<AdminApiOptions> options,
        ILogger<ControlPlaneDeleteService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<ControlPlaneDeleteResult> DeleteProjectAsync(
        string projectId,
        bool includeRuns,
        CancellationToken cancellationToken = default)
    {
        var roots = includeRuns
            ? new[] { "projects", "runs", "work-items", "events", "progress", "state" }
            : new[] { "projects" };

        return DeleteByIdAsync("project", projectId, roots, cancellationToken);
    }

    public Task<ControlPlaneDeleteResult> DeleteRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        return DeleteByIdAsync(
            "run",
            runId,
            new[] { "runs", "work-items", "events", "progress", "state" },
            cancellationToken);
    }

    public Task<ControlPlaneDeleteResult> DeleteCredentialAsync(
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        return DeleteByIdAsync(
            "credential",
            credentialId,
            new[] { "credentials", "credential-sets", "secrets" },
            cancellationToken);
    }

    public Task<ControlPlaneDeleteResult> DeleteArtifactAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        return DeleteByIdAsync(
            "artifact",
            artifactId,
            new[] { "artifacts", "manifests", "mappings" },
            cancellationToken);
    }

    private Task<ControlPlaneDeleteResult> DeleteByIdAsync(
        string entityType,
        string entityId,
        IReadOnlyList<string> preferredFolders,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity id is required.", nameof(entityId));
        }

        var storageRoot = GetStorageRoot();
        var deletedPaths = new List<string>();
        var warnings = new List<string>();
        var deletedFiles = 0;
        var deletedDirectories = 0;

        if (!Directory.Exists(storageRoot))
        {
            return Task.FromResult(new ControlPlaneDeleteResult(
                entityType,
                entityId,
                0,
                0,
                Array.Empty<string>(),
                new[] { $"Control-plane storage root does not exist: {storageRoot}" }));
        }

        foreach (var folder in preferredFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var folderPath = Path.Combine(storageRoot, folder);
            if (!Directory.Exists(folderPath))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories).ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsSafeChildPath(storageRoot, file))
                {
                    warnings.Add($"Skipped unsafe file path: {file}");
                    continue;
                }

                if (!MatchesEntityId(file, entityId))
                {
                    continue;
                }

                try
                {
                    File.Delete(file);
                    deletedFiles++;
                    deletedPaths.Add(file);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Could not delete file '{file}': {ex.Message}");
                }
            }

            foreach (var directory in Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories)
                         .OrderByDescending(x => x.Length)
                         .ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsSafeChildPath(storageRoot, directory))
                {
                    warnings.Add($"Skipped unsafe directory path: {directory}");
                    continue;
                }

                if (!Directory.Exists(directory))
                {
                    continue;
                }

                var dirNameMatches = Path.GetFileName(directory).Contains(entityId, StringComparison.OrdinalIgnoreCase);
                var isEmpty = !Directory.EnumerateFileSystemEntries(directory).Any();

                if (!dirNameMatches && !isEmpty)
                {
                    continue;
                }

                try
                {
                    if (dirNameMatches)
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                    else
                    {
                        Directory.Delete(directory, recursive: false);
                    }

                    deletedDirectories++;
                    deletedPaths.Add(directory);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Could not delete directory '{directory}': {ex.Message}");
                }
            }
        }

        _logger.LogInformation(
            "Deleted {DeletedFiles} files and {DeletedDirectories} directories for {EntityType} {EntityId}.",
            deletedFiles,
            deletedDirectories,
            entityType,
            entityId);

        return Task.FromResult(new ControlPlaneDeleteResult(
            entityType,
            entityId,
            deletedFiles,
            deletedDirectories,
            deletedPaths,
            warnings));
    }

    private string GetStorageRoot()
    {
        var rawRoot = string.IsNullOrWhiteSpace(_options.StorageRoot)
            ? "Runtime/admin-api"
            : _options.StorageRoot;

        return Path.GetFullPath(rawRoot);
    }

    private static bool IsSafeChildPath(string root, string candidate)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullCandidate = Path.GetFullPath(candidate);
        return fullCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesEntityId(string filePath, string entityId)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName.Contains(entityId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            // Most control-plane files are small JSON documents. Searching exact id text keeps this
            // independent from specific store implementation details while remaining limited to
            // ControlPlane:StorageRoot.
            var info = new FileInfo(filePath);
            if (info.Length > 2_000_000)
            {
                return false;
            }

            var text = File.ReadAllText(filePath);
            return text.Contains(entityId, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
