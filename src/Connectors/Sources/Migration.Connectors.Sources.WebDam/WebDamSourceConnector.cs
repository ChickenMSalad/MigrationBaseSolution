using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Connectors.Sources.WebDam.Clients;
using Migration.Connectors.Sources.WebDam.Configuration;
using Migration.Connectors.Sources.WebDam.Models;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.WebDam;

public sealed class WebDamSourceConnector : IAssetSourceConnector
{
    private readonly WebDamApiClient? _apiClient;
    private readonly WebDamOptions _options;
    private readonly ILogger<WebDamSourceConnector>? _logger;

    public string Type => "WebDam";

    public WebDamSourceConnector()
    {
        _options = new WebDamOptions();
    }

    public WebDamSourceConnector(
        WebDamApiClient apiClient,
        IOptions<WebDamOptions> options,
        ILogger<WebDamSourceConnector> logger)
    {
        _apiClient = apiClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AssetEnvelope> GetAssetAsync(
        MigrationJobDefinition job,
        ManifestRow row,
        CancellationToken cancellationToken = default)
    {
        var sourceAssetId = ResolveWebDamId(row);
        var explicitSourcePath = ResolveExplicitSourcePath(row);
        var mode = GetSetting(job, "SourceBinaryMode", "BinaryAcquisitionMode", "WebDamBinaryMode") ?? "PreferManifestPath";
        var hasExplicitSourcePath = !string.IsNullOrWhiteSpace(explicitSourcePath);

        var metadata = new Dictionary<string, string>(row.Columns, StringComparer.OrdinalIgnoreCase)
        {
            ["_sourceConnector"] = "WebDam",
            ["_binaryAcquisitionMode"] = mode
        };

        WebDamAssetDto? webDamAsset = null;
        var apiClient = ResolveApiClient(job);

        if (!string.IsNullOrWhiteSpace(sourceAssetId) && ShouldLoadWebDamMetadata(mode, hasExplicitSourcePath))
        {
            EnsureApiClientAvailable(apiClient);
            webDamAsset = await apiClient!.GetAssetAsync(sourceAssetId, cancellationToken).ConfigureAwait(false);
            MergeWebDamMetadata(metadata, webDamAsset);
        }

        var binary = await ResolveBinaryAsync(
            job,
            row,
            sourceAssetId,
            explicitSourcePath,
            webDamAsset,
            apiClient,
            mode,
            cancellationToken).ConfigureAwait(false);

        return new AssetEnvelope
        {
            SourceAssetId = sourceAssetId ?? row.SourceAssetId ?? row.RowId,
            ExternalId = sourceAssetId,
            Path = explicitSourcePath ?? row.SourcePath,
            SourceType = ConnectorType.WebDam,
            Metadata = metadata,
            Binary = binary
        };
    }

    private async Task<AssetBinary?> ResolveBinaryAsync(
        MigrationJobDefinition job,
        ManifestRow row,
        string? sourceAssetId,
        string? explicitSourcePath,
        WebDamAssetDto? webDamAsset,
        WebDamApiClient? apiClient,
        string mode,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(explicitSourcePath) &&
            !mode.Equals("WebDamDownloadOnly", StringComparison.OrdinalIgnoreCase))
        {
            return new AssetBinary
            {
                FileName = ResolveFileName(row, webDamAsset, explicitSourcePath),
                ContentType = webDamAsset?.Filetype,
                Length = TryParseLength(webDamAsset?.Filesize),
                SourceUri = explicitSourcePath
            };
        }

        if (mode.Equals("ManifestPathOnly", StringComparison.OrdinalIgnoreCase) ||
            mode.Equals("StagedOnly", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(sourceAssetId))
        {
            return null;
        }

        EnsureApiClientAvailable(apiClient);

        var stagingDirectory = ResolveStagingDirectory(job);
        Directory.CreateDirectory(stagingDirectory);

        var fileName = ResolveFileName(row, webDamAsset, null);
        var localPath = BuildUniqueStagingPath(stagingDirectory, sourceAssetId, fileName);

        if (File.Exists(localPath) && !ShouldForceDownload(job) && new FileInfo(localPath).Length > 0)
        {
            _logger?.LogInformation("Using previously staged WebDam binary. AssetId={AssetId}; Path={Path}", sourceAssetId, localPath);
            return new AssetBinary
            {
                FileName = Path.GetFileName(localPath),
                ContentType = webDamAsset?.Filetype,
                Length = new FileInfo(localPath).Length,
                SourceUri = localPath
            };
        }

        _logger?.LogInformation("Downloading WebDam binary. AssetId={AssetId}; Destination={Destination}", sourceAssetId, localPath);
        await using var remote = await apiClient!.DownloadAssetAsync(sourceAssetId, cancellationToken).ConfigureAwait(false);
        await using var local = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 128, true);
        await remote.CopyToAsync(local, cancellationToken).ConfigureAwait(false);
        await local.FlushAsync(cancellationToken).ConfigureAwait(false);

        var length = new FileInfo(localPath).Length;
        if (length <= 0)
        {
            throw new InvalidOperationException($"WebDam download produced an empty file. AssetId={sourceAssetId}; Path={localPath}");
        }

        return new AssetBinary
        {
            FileName = Path.GetFileName(localPath),
            ContentType = webDamAsset?.Filetype,
            Length = length,
            SourceUri = localPath
        };
    }

    private WebDamApiClient? ResolveApiClient(MigrationJobDefinition job)
    {
        var runtimeOptions = BuildRuntimeOptions(job);
        if (!HasUsableRuntimeCredentials(runtimeOptions))
        {
            return _apiClient;
        }

        var httpClient = new HttpClient { BaseAddress = new Uri(runtimeOptions.BaseUrl, UriKind.Absolute) };
        var options = Options.Create(runtimeOptions);
        var tokenStore = new RuntimeWebDamTokenStore();
        var authClient = new WebDamAuthClient(httpClient, options, tokenStore);
        return new WebDamApiClient(httpClient, authClient, options);
    }

    private WebDamOptions BuildRuntimeOptions(MigrationJobDefinition job)
    {
        return new WebDamOptions
        {
            BaseUrl = FirstNonEmpty(
                GetSetting(job, "WebDamBaseUrl", "SourceCredential_BaseUrl", "SourceBaseUrl"),
                _options.BaseUrl)!,
            ClientId = FirstNonEmpty(
                GetSetting(job, "WebDamClientId", "SourceCredential_ClientId", "SourceClientId"),
                _options.ClientId) ?? string.Empty,
            ClientSecret = FirstNonEmpty(
                GetSetting(job, "WebDamClientSecret", "SourceCredential_ClientSecret", "SourceClientSecret"),
                _options.ClientSecret) ?? string.Empty,
            RedirectUri = FirstNonEmpty(
                GetSetting(job, "WebDamRedirectUri", "SourceCredential_RedirectUri", "SourceRedirectUri"),
                _options.RedirectUri) ?? string.Empty,
            Username = FirstNonEmpty(
                GetSetting(job, "WebDamUsername", "SourceCredential_Username", "SourceUsername"),
                _options.Username),
            Password = FirstNonEmpty(
                GetSetting(job, "WebDamPassword", "SourceCredential_Password", "SourcePassword"),
                _options.Password),
            RefreshToken = FirstNonEmpty(
                GetSetting(job, "WebDamRefreshToken", "SourceCredential_RefreshToken", "SourceRefreshToken"),
                _options.RefreshToken),
            AccessToken = FirstNonEmpty(
                GetSetting(job, "WebDamAccessToken", "SourceCredential_AccessToken", "SourceAccessToken"),
                _options.AccessToken),
            AccessTokenExpiresAtUtc = TryParseDateTimeOffset(
                GetSetting(job, "WebDamAccessTokenExpiresAtUtc", "SourceCredential_AccessTokenExpiresAtUtc", "SourceAccessTokenExpiresAtUtc"))
                ?? _options.AccessTokenExpiresAtUtc,
            PageSize = _options.PageSize,
            TokenRefreshSkew = _options.TokenRefreshSkew
        };
    }

    private static bool HasUsableRuntimeCredentials(WebDamOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.RefreshToken) ||
               (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password)) ||
               (!string.IsNullOrWhiteSpace(options.AccessToken) && options.AccessTokenExpiresAtUtc.HasValue);
    }

    private static DateTimeOffset? TryParseDateTimeOffset(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private static void EnsureApiClientAvailable(WebDamApiClient? apiClient)
    {
        if (apiClient is null)
        {
            throw new InvalidOperationException("WebDam API services are not registered or no WebDam credentials were provided by the job. Bind source credentials to the project before running WebDam API/download workflows.");
        }
    }

    private static bool ShouldLoadWebDamMetadata(string mode, bool hasExplicitSourcePath)
    {
        if (mode.Equals("ManifestPathOnly", StringComparison.OrdinalIgnoreCase) ||
            mode.Equals("StagedOnly", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (hasExplicitSourcePath && mode.Equals("PreferManifestPath", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool ShouldForceDownload(MigrationJobDefinition job)
    {
        return bool.TryParse(GetSetting(job, "ForceDownload", "WebDamForceDownload"), out var force) && force;
    }

    private static string ResolveStagingDirectory(MigrationJobDefinition job)
    {
        var configured = GetSetting(job, "BinaryStagingDirectory", "StagingDirectory", "WebDamStagingDirectory", "DownloadDirectory");
        return !string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "runs", SanitizePathSegment(job.JobName), "webdam-binaries"));
    }

    private static string BuildUniqueStagingPath(string stagingDirectory, string sourceAssetId, string fileName)
    {
        fileName = string.IsNullOrWhiteSpace(fileName) ? $"{sourceAssetId}.bin" : fileName;
        return Path.Combine(stagingDirectory, $"{SanitizePathSegment(sourceAssetId)}_{SanitizeFileName(fileName)}");
    }

    private static string ResolveFileName(ManifestRow row, WebDamAssetDto? webDamAsset, string? sourcePath)
    {
        return FirstNonEmpty(
            GetColumn(row, "FileName", "filename", "file_name", "OriginalFileName", "original_file_name"),
            webDamAsset?.Filename,
            webDamAsset?.Name,
            FileNameFromPath(sourcePath),
            FileNameFromPath(row.SourcePath),
            $"{ResolveWebDamId(row) ?? row.RowId}.bin") ?? $"{row.RowId}.bin";
    }

    private static string? ResolveWebDamId(ManifestRow row)
    {
        return FirstNonEmpty(row.SourceAssetId, GetColumn(row, "webdam_id", "webdamId", "WebDamId", "AssetId", "asset_id", "id", "Id"));
    }

    private static string? ResolveExplicitSourcePath(ManifestRow row)
    {
        return FirstNonEmpty(
            row.SourcePath,
            GetColumn(row, "SourcePath", "source_path", "sourcePath", "FilePath", "file_path", "filePath", "Path", "path", "SourceUri", "source_uri", "sourceUri", "DownloadUrl", "download_url", "downloadUrl", "Url", "url"));
    }

    private static void MergeWebDamMetadata(IDictionary<string, string> metadata, WebDamAssetDto asset)
    {
        AddIfNotBlank(metadata, "webdam_id", asset.Id);
        AddIfNotBlank(metadata, "id", asset.Id);
        AddIfNotBlank(metadata, "filename", asset.Filename);
        AddIfNotBlank(metadata, "name", asset.Name);
        AddIfNotBlank(metadata, "description", asset.Description);
        AddIfNotBlank(metadata, "filetype", asset.Filetype);
        AddIfNotBlank(metadata, "filesize", asset.Filesize);
        AddIfNotBlank(metadata, "folder_id", asset.Folder?.Id);
        AddIfNotBlank(metadata, "folder_name", asset.Folder?.Name);
        AddIfNotBlank(metadata, "status", asset.Status);
    }

    private static void AddIfNotBlank(IDictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !values.ContainsKey(key))
        {
            values[key] = value;
        }
    }

    private static string? GetColumn(ManifestRow row, params string[] names)
    {
        foreach (var name in names)
        {
            if (row.Columns.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetSetting(MigrationJobDefinition job, params string[] names)
    {
        foreach (var name in names)
        {
            if (job.Settings.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string? FileNameFromPath(string? pathOrUri)
    {
        if (string.IsNullOrWhiteSpace(pathOrUri))
        {
            return null;
        }

        if (Uri.TryCreate(pathOrUri, UriKind.Absolute, out var uri))
        {
            return Path.GetFileName(uri.LocalPath);
        }

        return Path.GetFileName(pathOrUri);
    }

    private static long? TryParseLength(string? value)
    {
        return long.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(value.Select(ch => invalid.Contains(ch) ? '_' : ch));
        return string.IsNullOrWhiteSpace(sanitized) ? "asset.bin" : sanitized;
    }

    private sealed class RuntimeWebDamTokenStore : IWebDamTokenStore
    {
        private WebDamTokenState? _current;

        public Task<WebDamTokenState?> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_current);
        }

        public Task SaveAsync(WebDamTokenState token, CancellationToken cancellationToken = default)
        {
            _current = token;
            return Task.CompletedTask;
        }
    }
}
