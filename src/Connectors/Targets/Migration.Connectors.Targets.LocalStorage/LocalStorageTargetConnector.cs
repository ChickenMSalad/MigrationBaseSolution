using System.Text.Json;
using Migration.Application.Abstractions;
using Migration.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Migration.Connectors.Targets.LocalStorage;

public sealed class LocalStorageTargetConnector : IAssetTargetConnector
{
    private readonly LocalStorageTargetOptions _options;
    private readonly ILogger<LocalStorageTargetConnector> _logger;

    public LocalStorageTargetConnector(
        IOptions<LocalStorageTargetOptions> options,
        ILogger<LocalStorageTargetConnector> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Type => "LocalStorage";

    public async Task<MigrationResult> UpsertAsync(MigrationJobDefinition job, AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (job.DryRun)
        {
            var dryRunPath = BuildDestinationFilePath(job, item);
            return new MigrationResult
            {
                WorkItemId = item.WorkItemId,
                Success = true,
                TargetAssetId = $"local:{dryRunPath}",
                Message = $"Dry run: would copy binary to {dryRunPath}."
            };
        }

        var sourcePath = ResolveBinarySourcePath(item);
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return new MigrationResult
            {
                WorkItemId = item.WorkItemId,
                Success = false,
                Message = $"Local target could not find binary source file: {sourcePath ?? "(null)"}."
            };
        }

        var destinationPath = BuildDestinationFilePath(job, item);
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            return new MigrationResult
            {
                WorkItemId = item.WorkItemId,
                Success = false,
                Message = $"Could not determine destination directory for {destinationPath}."
            };
        }

        if (!Directory.Exists(destinationDirectory))
        {
            if (!_options.CreateDirectoryIfMissing)
            {
                return new MigrationResult
                {
                    WorkItemId = item.WorkItemId,
                    Success = false,
                    Message = $"Destination directory does not exist: {destinationDirectory}."
                };
            }

            Directory.CreateDirectory(destinationDirectory);
        }

        if (File.Exists(destinationPath) && !_options.Overwrite)
        {
            return new MigrationResult
            {
                WorkItemId = item.WorkItemId,
                Success = false,
                TargetAssetId = $"local:{destinationPath}",
                Message = $"Destination file already exists and overwrite is false: {destinationPath}."
            };
        }

        await using (var source = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var destination = File.Open(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        string? metadataPath = null;
        if (_options.WriteMetadataSidecar)
        {
            metadataPath = BuildMetadataSidecarPath(destinationPath, item);
            var metadata = BuildMetadataDocument(item, sourcePath, destinationPath);
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
        }

        _logger.LogInformation("Copied local migration asset {WorkItemId} to {DestinationPath}", item.WorkItemId, destinationPath);

        return new MigrationResult
        {
            WorkItemId = item.WorkItemId,
            Success = true,
            TargetAssetId = $"local:{destinationPath}",
            Message = metadataPath is null
                ? $"Copied binary to {destinationPath}."
                : $"Copied binary to {destinationPath}; wrote metadata sidecar to {metadataPath}."
        };
    }

    private string BuildDestinationFilePath(MigrationJobDefinition job, AssetWorkItem item)
    {
        var root = GetSetting(job, "LocalStorageTargetRootDirectory")
                   ?? GetSetting(job, "LocalStorageRootDirectory")
                   ?? _options.RootDirectory;

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("LocalStorage target requires LocalStorage:Target:RootDirectory or job setting LocalStorageTargetRootDirectory.");
        }

        var basePath = GetSetting(job, "LocalStorageTargetBasePath") ?? _options.BasePath;
        var sourceFolder = _options.PreserveSourceFolderPath ? ResolveSourceFolder(item) : null;
        var fileName = BuildDestinationFileName(item);

        return Path.GetFullPath(CombinePathParts(root, basePath, sourceFolder, fileName));
    }

    private string BuildDestinationFileName(AssetWorkItem item)
    {
        var originalFileName = item.SourceAsset?.Binary?.FileName
                               ?? item.TargetPayload?.Name
                               ?? Path.GetFileName(ResolveBinarySourcePath(item) ?? string.Empty)
                               ?? item.WorkItemId;

        originalFileName = SanitizeFileName(Path.GetFileName(originalFileName));

        if (!_options.PrefixFileNameWithUniqueId)
        {
            return originalFileName;
        }

        var uniqueId = ResolveUniqueId(item);
        if (string.IsNullOrWhiteSpace(uniqueId))
        {
            return originalFileName;
        }

        return $"{SanitizeFileName(uniqueId)}_{originalFileName}";
    }

    private string? ResolveSourceFolder(AssetWorkItem item)
    {
        var configuredField = _options.SourceFolderPathField;
        if (!string.IsNullOrWhiteSpace(configuredField)
            && item.Manifest.Columns.TryGetValue(configuredField, out var configuredValue)
            && !string.IsNullOrWhiteSpace(configuredValue))
        {
            return SanitizeRelativePath(configuredValue);
        }

        var sourcePath = item.Manifest.SourcePath ?? item.SourceAsset?.Path ?? item.SourceAsset?.Binary?.SourceUri;
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return null;
        }

        var stripped = StripFileUri(sourcePath);
        var directory = Path.GetDirectoryName(stripped);
        return SanitizeRelativePath(directory);
    }

    private string ResolveUniqueId(AssetWorkItem item)
    {
        var field = _options.UniqueIdField;

        if (field.Equals("SourceAssetId", StringComparison.OrdinalIgnoreCase))
        {
            return item.SourceAsset?.SourceAssetId ?? item.Manifest.SourceAssetId ?? item.WorkItemId;
        }

        if (field.Equals("WorkItemId", StringComparison.OrdinalIgnoreCase))
        {
            return item.WorkItemId;
        }

        if (item.Manifest.Columns.TryGetValue(field, out var manifestValue) && !string.IsNullOrWhiteSpace(manifestValue))
        {
            return manifestValue!;
        }

        if (item.SourceAsset?.Metadata.TryGetValue(field, out var sourceMetadataValue) == true && !string.IsNullOrWhiteSpace(sourceMetadataValue))
        {
            return sourceMetadataValue!;
        }

        return item.SourceAsset?.SourceAssetId ?? item.Manifest.SourceAssetId ?? item.WorkItemId;
    }

    private string BuildMetadataSidecarPath(string destinationBinaryPath, AssetWorkItem item)
    {
        var uniqueId = SanitizeFileName(ResolveUniqueId(item));
        var name = string.IsNullOrWhiteSpace(uniqueId)
            ? $"{Path.GetFileNameWithoutExtension(destinationBinaryPath)}_metadata.json"
            : $"{uniqueId}_metadata.json";

        return Path.Combine(Path.GetDirectoryName(destinationBinaryPath)!, name);
    }

    private Dictionary<string, object?> BuildMetadataDocument(AssetWorkItem item, string sourcePath, string destinationPath)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (_options.IncludeSystemMetadata)
        {
            result["workItemId"] = item.WorkItemId;
            result["sourceAssetId"] = item.SourceAsset?.SourceAssetId ?? item.Manifest.SourceAssetId;
            result["sourcePath"] = sourcePath;
            result["targetPath"] = destinationPath;
            result["fileName"] = Path.GetFileName(destinationPath);
            result["writtenUtc"] = DateTimeOffset.UtcNow;
        }

        if (_options.MetadataSidecarMode is LocalStorageMetadataSidecarMode.ManifestColumns or LocalStorageMetadataSidecarMode.Both)
        {
            foreach (var kvp in FilterManifestColumns(item.Manifest.Columns))
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        if (_options.MetadataSidecarMode is LocalStorageMetadataSidecarMode.TargetPayloadFields or LocalStorageMetadataSidecarMode.Both)
        {
            if (item.TargetPayload?.Fields is not null)
            {
                foreach (var kvp in item.TargetPayload.Fields)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
        }

        return result;
    }

    private IEnumerable<KeyValuePair<string, string?>> FilterManifestColumns(Dictionary<string, string?> columns)
    {
        var include = new HashSet<string>(_options.MetadataIncludeColumns, StringComparer.OrdinalIgnoreCase);
        var exclude = new HashSet<string>(_options.MetadataExcludeColumns, StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in columns)
        {
            if (include.Count > 0 && !include.Contains(kvp.Key))
            {
                continue;
            }

            if (exclude.Contains(kvp.Key))
            {
                continue;
            }

            yield return kvp;
        }
    }

    private static string? ResolveBinarySourcePath(AssetWorkItem item)
    {
        var candidate = item.SourceAsset?.Binary?.SourceUri
                        ?? item.SourceAsset?.Path
                        ?? item.Manifest.SourcePath;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        return StripFileUri(candidate);
    }

    private static string? GetSetting(MigrationJobDefinition job, string key)
    {
        return job.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    private static string CombinePathParts(params string?[] parts)
    {
        var cleaned = parts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .ToArray();

        if (cleaned.Length == 0)
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(parts[0] ?? string.Empty))
        {
            var root = parts[0]!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(new[] { root }.Concat(cleaned.Skip(1)).ToArray());
        }

        return Path.Combine(cleaned);
    }

    private static string StripFileUri(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        return value;
    }

    private static string SanitizeRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var stripped = StripFileUri(value).Replace('\\', '/').Trim('/');
        var segments = stripped
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x != "." && x != "..")
            .Select(SanitizeFileName);

        return Path.Combine(segments.ToArray());
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unnamed";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars).Trim();
    }
}
