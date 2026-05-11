using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Migration.Connectors.Sources.LocalStorage;

public sealed class LocalStorageSourceConnector : IAssetSourceConnector
{
    private readonly LocalStorageSourceOptions _options;
    private readonly ILogger<LocalStorageSourceConnector> _logger;

    public LocalStorageSourceConnector(
        IOptions<LocalStorageSourceOptions> options,
        ILogger<LocalStorageSourceConnector> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Type => "LocalStorage";

    public Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var configuredRoot = GetSetting(job, "LocalStorageSourceRootDirectory")
                             ?? GetSetting(job, "LocalStorageRootDirectory")
                             ?? _options.RootDirectory;

        var sourcePath = ResolveSourcePath(row);
        var absolutePath = ResolveAbsolutePath(configuredRoot, sourcePath);

        if (_options.RequireExistingFile && !File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Local source file was not found: {absolutePath}", absolutePath);
        }

        var fileInfo = File.Exists(absolutePath) ? new FileInfo(absolutePath) : null;
        var fileName = ResolveFileName(row, absolutePath);
        var sourceAssetId = row.SourceAssetId
                            ?? FirstNonEmpty(row, _options.IdFields)
                            ?? Path.GetFileNameWithoutExtension(fileName)
                            ?? row.RowId;

        var metadata = new Dictionary<string, string?>(row.Columns, StringComparer.OrdinalIgnoreCase)
        {
            ["local_source_path"] = absolutePath,
            ["local_source_file_name"] = fileName
        };

        _logger.LogDebug("Resolved local source asset {SourceAssetId} to {Path}", sourceAssetId, absolutePath);

        return Task.FromResult(new AssetEnvelope
        {
            SourceAssetId = sourceAssetId,
            ExternalId = sourceAssetId,
            Path = absolutePath,
            SourceType = ConnectorType.LocalStorage,
            Metadata = metadata,
            Binary = new AssetBinary
            {
                FileName = fileName,
                ContentType = GuessContentType(fileName),
                Length = fileInfo?.Length,
                SourceUri = absolutePath
            }
        });
    }

    private string ResolveSourcePath(ManifestRow row)
    {
        var path = row.SourcePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = FirstNonEmpty(row, _options.PathFields);
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"Manifest row {row.RowId} does not contain a local source path. Set SourcePath or one of: {string.Join(", ", _options.PathFields)}.");
        }

        return path!;
    }

    private static string ResolveAbsolutePath(string? rootDirectory, string sourcePath)
    {
        var normalized = StripFileUri(sourcePath.Trim());
        if (Path.IsPathRooted(normalized))
        {
            return Path.GetFullPath(normalized);
        }

        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            return Path.GetFullPath(normalized);
        }

        return Path.GetFullPath(Path.Combine(rootDirectory, normalized));
    }

    private string ResolveFileName(ManifestRow row, string absolutePath)
    {
        var fileName = FirstNonEmpty(row, _options.FileNameFields);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return Path.GetFileName(fileName);
        }

        return Path.GetFileName(absolutePath);
    }

    private static string? FirstNonEmpty(ManifestRow row, IEnumerable<string> fields)
    {
        foreach (var field in fields)
        {
            if (row.Columns.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetSetting(MigrationJobDefinition job, string key)
    {
        return job.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    private static string StripFileUri(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        return value;
    }

    private static string GuessContentType(string? fileName)
    {
        return Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".tif" or ".tiff" => "image/tiff",
            ".pdf" => "application/pdf",
            ".json" => "application/json",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };
    }
}
