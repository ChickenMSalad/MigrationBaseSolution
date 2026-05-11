using Microsoft.Extensions.Logging;
using Migration.Connectors.Sources.SharePoint.Configuration;
using Migration.Connectors.Sources.SharePoint.Services;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.SharePoint.Rclone;

public sealed class RcloneSharePointSourceService
{
    private readonly RcloneProcessRunner _runner;
    private readonly ILogger<RcloneSharePointSourceService> _logger;

    public RcloneSharePointSourceService(RcloneProcessRunner runner, ILogger<RcloneSharePointSourceService> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    public async Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, SharePointSourceOptions options, CancellationToken cancellationToken)
    {
        var relativePath = ResolvePath(row, options);
        var remotePath = SharePointPathUtilities.CombineRemotePath(GetSetting(job, "SharePointRootPath", "RcloneRootPath") ?? options.Rclone.RootPath, relativePath);
        var remote = $"{Resolve(job, "RcloneRemoteName", options.Rclone.RemoteName)}:{remotePath}";
        var metadata = new Dictionary<string, string>(row.Columns, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in SharePointPathUtilities.BuildPathMetadata(relativePath)) metadata.TryAdd(kvp.Key, kvp.Value);
        metadata["_sourceConnector"] = "SharePoint";
        metadata["_sharepointMode"] = "Rclone";
        metadata["rclone_remote_path"] = remote;

        var fileName = Path.GetFileName(relativePath);
        string? sourceUri = remote;
        long? length = null;

        if (GetBool(job, "SharePointRcloneCopyToLocalStaging") ?? options.Rclone.CopyToLocalStaging)
        {
            var stagingDirectory = ResolveStagingDirectory(job, options, relativePath);
            Directory.CreateDirectory(stagingDirectory);
            var localPath = Path.Combine(stagingDirectory, SharePointPathUtilities.SanitizeFileName(fileName));

            if (!(File.Exists(localPath) && (GetBool(job, "SharePointRcloneReuseExistingStagedFile") ?? options.Rclone.ReuseExistingStagedFile)))
            {
                _logger.LogInformation("Staging SharePoint file with rclone. Source={Source}; Destination={Destination}", remote, localPath);
                await _runner.RunAsync(options.Rclone, $"copyto {RcloneProcessRunner.Quote(remote)} {RcloneProcessRunner.Quote(localPath)} --create-empty-src-dirs", cancellationToken).ConfigureAwait(false);
            }

            sourceUri = localPath;
            length = File.Exists(localPath) ? new FileInfo(localPath).Length : null;
        }

        return new AssetEnvelope
        {
            SourceAssetId = row.SourceAssetId ?? relativePath,
            ExternalId = row.SourceAssetId ?? relativePath,
            Path = relativePath,
            SourceType = ConnectorType.SharePoint,
            Metadata = metadata,
            Binary = new AssetBinary
            {
                FileName = fileName,
                ContentType = SharePointPathUtilities.GuessContentType(fileName),
                Length = length,
                SourceUri = sourceUri
            }
        };
    }

    public async Task<IReadOnlyList<string>> ListFilesAsync(MigrationJobDefinition job, SharePointSourceOptions options, CancellationToken cancellationToken)
    {
        var rootPath = GetSetting(job, "SharePointRootPath", "RcloneRootPath") ?? options.Rclone.RootPath;
        var remote = $"{Resolve(job, "RcloneRemoteName", options.Rclone.RemoteName)}:{SharePointPathUtilities.NormalizeRelativePath(rootPath)}";
        var output = await _runner.RunAsync(options.Rclone, $"lsf {RcloneProcessRunner.Quote(remote)} --recursive --files-only", cancellationToken).ConfigureAwait(false);
        return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(SharePointPathUtilities.NormalizeRelativePath)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static string ResolvePath(ManifestRow row, SharePointSourceOptions options)
    {
        if (!string.IsNullOrWhiteSpace(row.SourcePath)) return row.SourcePath!;
        foreach (var field in options.PathFields)
            if (row.Columns.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        throw new InvalidOperationException($"Manifest row {row.RowId} does not contain a SharePoint source path.");
    }

    private static string ResolveStagingDirectory(MigrationJobDefinition job, SharePointSourceOptions options, string relativePath)
    {
        var configured = GetSetting(job, "SharePointStagingDirectory", "RcloneStagingDirectory") ?? options.Rclone.StagingDirectory;
        var baseDir = !string.IsNullOrWhiteSpace(configured) ? configured : Path.Combine(AppContext.BaseDirectory, "runs", Sanitize(job.JobName), "sharepoint-rclone");
        var folder = Path.GetDirectoryName(SharePointPathUtilities.NormalizeRelativePath(relativePath))?.Replace('\\', Path.DirectorySeparatorChar) ?? string.Empty;
        return Path.GetFullPath(Path.Combine(baseDir, folder));
    }

    private static string Sanitize(string value) => string.Concat(value.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
    private static string Resolve(MigrationJobDefinition job, string key, string fallback) => GetSetting(job, key) ?? fallback;
    private static string? GetSetting(MigrationJobDefinition job, params string[] keys) => keys.Select(k => job.Settings.TryGetValue(k, out var v) ? v : null).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    private static bool? GetBool(MigrationJobDefinition job, string key) => job.Settings.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : null;
}
