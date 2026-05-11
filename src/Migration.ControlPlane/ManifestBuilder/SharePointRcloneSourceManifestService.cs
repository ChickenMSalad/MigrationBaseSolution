using System.Diagnostics;
using System.Globalization;
using System.Text;

using Microsoft.Extensions.Configuration;

using Migration.ControlPlane.Services;

namespace Migration.ControlPlane.ManifestBuilder;

public sealed class SharePointRcloneSourceManifestService : ISourceManifestService
{
    private const string Source = "SharePoint";
    private const string Service = "Rclone";

    private readonly IConfiguration _configuration;
    private readonly ICredentialResolver _credentialResolver;

    public SharePointRcloneSourceManifestService(
        IConfiguration configuration,
        ICredentialResolver credentialResolver)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _credentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
    }

    public string SourceType => Source;

    public string ServiceName => Service;

    public ManifestBuilderServiceDescriptor GetDescriptor()
    {
        return new ManifestBuilderServiceDescriptor(
            Source,
            Service,
            "rclone folder/file manifest",
            "Builds a SharePoint manifest by listing files through the rclone remote defined on the selected SharePoint Source credentials. Metadata is derived from folder path, folder depth, and file naming convention.",
            new[]
            {
                new ManifestBuilderOptionDescriptor(
                    "sourcePath",
                    "Source path",
                    "Path inside the selected credential's rclone remote. Example: images or Documents/images.",
                    Required: false,
                    Placeholder: "images"),
                new ManifestBuilderOptionDescriptor(
                    "includeFolderMetadata",
                    "Include folder metadata",
                    "true/false. Adds folder path, depth, parent folder, top folder, and folder level columns.",
                    Required: false,
                    Placeholder: "true"),
                new ManifestBuilderOptionDescriptor(
                    "includeFileNameMetadata",
                    "Include file name metadata",
                    "true/false. Adds file name, base name, extension, and simple filename token columns.",
                    Required: false,
                    Placeholder: "true"),
                new ManifestBuilderOptionDescriptor(
                    "maxFolderDepth",
                    "Max folder depth",
                    "Optional maximum folder depth to include.",
                    Required: false,
                    Placeholder: string.Empty),
                new ManifestBuilderOptionDescriptor(
                    "includeHidden",
                    "Include hidden folders/files",
                    "true/false. Defaults to false. Hidden paths are paths with any segment beginning with a dot.",
                    Required: false,
                    Placeholder: "false"),
                new ManifestBuilderOptionDescriptor(
                    "includeExtensions",
                    "Include extensions",
                    "Optional comma-separated extension allow-list. Example: jpg,png,tif. Leave blank to include all.",
                    Required: false,
                    Placeholder: "jpg,png,tif"),
                new ManifestBuilderOptionDescriptor(
                    "excludeExtensions",
                    "Exclude extensions",
                    "Optional comma-separated extension deny-list. Example: tmp,db.",
                    Required: false,
                    Placeholder: "tmp,db")
            });
    }

    public async Task<BuildSourceManifestResult> BuildAsync(
        BuildSourceManifestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requestOptions = request.Options ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var credentialValues = await ResolveCredentialValuesAsync(request.CredentialSetId, cancellationToken).ConfigureAwait(false);
        var manifestOptions = SharePointRcloneManifestBuildOptions.From(requestOptions, credentialValues, _configuration);

        var paths = await ListFilesAsync(manifestOptions, cancellationToken).ConfigureAwait(false);
        var rows = paths
            .Where(path => ShouldIncludePath(path, manifestOptions))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select((path, index) => SharePointRcloneManifestRow.From(path, index + 1, manifestOptions))
            .ToList();

        var csv = BuildCsv(rows, manifestOptions);
        var fileName = $"sharepoint-rclone-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";

        return new BuildSourceManifestResult(
            Source,
            Service,
            fileName,
            "text/csv",
            csv,
            ContentBytes: null,
            rows.Count);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveCredentialValuesAsync(
        string? credentialSetId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(credentialSetId))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var values = await _credentialResolver.ResolveAsync(credentialSetId, cancellationToken).ConfigureAwait(false);
        return new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
    }

    private static bool ShouldIncludePath(string path, SharePointRcloneManifestBuildOptions options)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = NormalizePath(path);
        var segments = GetSegments(normalized);

        if (!options.IncludeHidden && segments.Any(segment => segment.StartsWith(".", StringComparison.Ordinal)))
        {
            return false;
        }

        var folderDepth = Math.Max(0, segments.Count - 1);
        if (options.MaxFolderDepth is > 0 && folderDepth > options.MaxFolderDepth)
        {
            return false;
        }

        var extension = Path.GetExtension(normalized).TrimStart('.').ToLowerInvariant();

        if (options.IncludeExtensions.Count > 0 && !options.IncludeExtensions.Contains(extension))
        {
            return false;
        }

        if (options.ExcludeExtensions.Count > 0 && options.ExcludeExtensions.Contains(extension))
        {
            return false;
        }

        return true;
    }

    private async Task<IReadOnlyList<string>> ListFilesAsync(
        SharePointRcloneManifestBuildOptions options,
        CancellationToken cancellationToken)
    {
        var source = BuildRemotePath(options.RemoteName, options.SourcePath);

        var arguments = new List<string>
        {
            "lsf",
            QuoteArgument(source),
            "--recursive",
            "--files-only"
        };

        if (!string.IsNullOrWhiteSpace(options.RcloneConfigPath))
        {
            arguments.Add("--config");
            arguments.Add(QuoteArgument(options.RcloneConfigPath));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = options.RcloneExecutablePath,
            Arguments = string.Join(" ", arguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("Unable to start rclone process.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var output = await standardOutputTask.ConfigureAwait(false);
        var error = await standardErrorTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"rclone lsf failed with exit code {process.ExitCode}. Source: {source}. Error: {error}".Trim());
        }

        return output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static string BuildRemotePath(string remoteName, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            throw new InvalidOperationException(
                "SharePoint rclone manifest requires a remote name. Add remoteName to the selected SharePoint Source credentials, or configure SharePointSource:Rclone:RemoteName.");
        }

        var remote = remoteName.Trim().TrimEnd(':');
        var path = NormalizePath(sourcePath ?? string.Empty).Trim('/');

        return string.IsNullOrWhiteSpace(path)
            ? $"{remote}:"
            : $"{remote}:{path}";
    }

    private static string BuildCsv(
        IReadOnlyList<SharePointRcloneManifestRow> rows,
        SharePointRcloneManifestBuildOptions options)
    {
        var columns = new List<(string Header, Func<SharePointRcloneManifestRow, string?> Value)>
        {
            ("RowId", row => row.RowId.ToString(CultureInfo.InvariantCulture)),
            ("SourceType", row => Source),
            ("ServiceName", row => Service),
            ("SourceAssetId", row => row.SourceAssetId),
            ("SourcePath", row => row.SourcePath),
            ("RcloneRemote", row => row.RcloneRemote),
            ("RcloneSourcePath", row => row.RcloneSourcePath)
        };

        if (options.IncludeFolderMetadata)
        {
            columns.Add(("FolderPath", row => row.FolderPath));
            columns.Add(("FolderDepth", row => row.FolderDepth.ToString(CultureInfo.InvariantCulture)));
            columns.Add(("TopFolder", row => row.TopFolder));
            columns.Add(("ParentFolder", row => row.ParentFolder));
            columns.Add(("FolderLevel1", row => row.FolderLevel1));
            columns.Add(("FolderLevel2", row => row.FolderLevel2));
            columns.Add(("FolderLevel3", row => row.FolderLevel3));
            columns.Add(("FolderLevel4", row => row.FolderLevel4));
            columns.Add(("FolderLevel5", row => row.FolderLevel5));
        }

        if (options.IncludeFileNameMetadata)
        {
            columns.Add(("FileName", row => row.FileName));
            columns.Add(("FileNameWithoutExtension", row => row.FileNameWithoutExtension));
            columns.Add(("Extension", row => row.Extension));
            columns.Add(("FileNameToken1", row => row.FileNameToken1));
            columns.Add(("FileNameToken2", row => row.FileNameToken2));
            columns.Add(("FileNameToken3", row => row.FileNameToken3));
            columns.Add(("FileNameToken4", row => row.FileNameToken4));
        }

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', columns.Select(column => EscapeCsv(column.Header))));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', columns.Select(column => EscapeCsv(column.Value(row)))));
        }

        return builder.ToString();
    }

    private static IReadOnlyList<string> GetSegments(string path)
    {
        return NormalizePath(path)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string NormalizePath(string? path)
    {
        return (path ?? string.Empty).Replace('\\', '/');
    }

    private static string QuoteArgument(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var requiresQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n');
        var escaped = value.Replace("\"", "\"\"");

        return requiresQuotes ? $"\"{escaped}\"" : escaped;
    }

    private sealed record SharePointRcloneManifestBuildOptions(
        string RcloneExecutablePath,
        string? RcloneConfigPath,
        string RemoteName,
        string? SourcePath,
        bool IncludeFolderMetadata,
        bool IncludeFileNameMetadata,
        bool IncludeHidden,
        int? MaxFolderDepth,
        ISet<string> IncludeExtensions,
        ISet<string> ExcludeExtensions)
    {
        public static SharePointRcloneManifestBuildOptions From(
            IReadOnlyDictionary<string, string> requestOptions,
            IReadOnlyDictionary<string, string> credentialValues,
            IConfiguration configuration)
        {
            var rcloneSection = configuration.GetSection("SharePointSource:Rclone");

            return new SharePointRcloneManifestBuildOptions(
                GetValue(requestOptions, credentialValues, rcloneSection, "rcloneExecutablePath", "rclonePath", "executablePath", "path") ?? "rclone",
                GetValue(requestOptions, credentialValues, rcloneSection, "rcloneConfigPath", "configPath", "configurationPath"),
                GetValue(requestOptions, credentialValues, rcloneSection, "remoteName", "rcloneRemoteName", "remote") ?? string.Empty,
                GetValue(requestOptions, credentialValues, rcloneSection, "sourcePath", "rootPath", "pathInsideRemote", "folderPath") ??
                    rcloneSection["SourcePath"] ??
                    rcloneSection["RootPath"],
                GetBool(requestOptions, "includeFolderMetadata", defaultValue: true),
                GetBool(requestOptions, "includeFileNameMetadata", defaultValue: true),
                GetBool(requestOptions, "includeHidden", defaultValue: false),
                GetInt(requestOptions, "maxFolderDepth"),
                ParseExtensions(GetValue(requestOptions, credentialValues, rcloneSection, "includeExtensions")),
                ParseExtensions(GetValue(requestOptions, credentialValues, rcloneSection, "excludeExtensions")));
        }

        private static string? GetValue(
            IReadOnlyDictionary<string, string> requestOptions,
            IReadOnlyDictionary<string, string> credentialValues,
            IConfigurationSection configurationSection,
            params string[] keys)
        {
            foreach (var key in keys)
            {
                var requestValue = GetDictionaryValue(requestOptions, key);
                if (!string.IsNullOrWhiteSpace(requestValue))
                {
                    return requestValue;
                }

                var credentialValue = GetDictionaryValue(credentialValues, key);
                if (!string.IsNullOrWhiteSpace(credentialValue))
                {
                    return credentialValue;
                }

                var configurationValue = configurationSection[key];
                if (!string.IsNullOrWhiteSpace(configurationValue))
                {
                    return configurationValue.Trim();
                }
            }

            return null;
        }

        private static string? GetDictionaryValue(IReadOnlyDictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
        }

        private static bool GetBool(IReadOnlyDictionary<string, string> options, string key, bool defaultValue)
        {
            var value = GetDictionaryValue(options, key);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        private static int? GetInt(IReadOnlyDictionary<string, string> options, string key)
        {
            var value = GetDictionaryValue(options, key);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }

        private static ISet<string> ParseExtensions(string? value)
        {
            return (value ?? string.Empty)
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(extension => extension.Trim().TrimStart('.').ToLowerInvariant())
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }

    private sealed record SharePointRcloneManifestRow(
        int RowId,
        string SourceAssetId,
        string SourcePath,
        string RcloneRemote,
        string? RcloneSourcePath,
        string FolderPath,
        int FolderDepth,
        string? TopFolder,
        string? ParentFolder,
        string? FolderLevel1,
        string? FolderLevel2,
        string? FolderLevel3,
        string? FolderLevel4,
        string? FolderLevel5,
        string FileName,
        string FileNameWithoutExtension,
        string Extension,
        string? FileNameToken1,
        string? FileNameToken2,
        string? FileNameToken3,
        string? FileNameToken4)
    {
        public static SharePointRcloneManifestRow From(
            string path,
            int rowId,
            SharePointRcloneManifestBuildOptions options)
        {
            var normalizedPath = NormalizePath(path).Trim('/');
            var segments = GetSegments(normalizedPath);
            var fileName = segments.LastOrDefault() ?? normalizedPath;
            var folders = segments.Take(Math.Max(0, segments.Count - 1)).ToList();
            var folderPath = string.Join('/', folders);
            var extension = Path.GetExtension(fileName).TrimStart('.');
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var tokens = nameWithoutExtension
                .Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            return new SharePointRcloneManifestRow(
                rowId,
                normalizedPath,
                normalizedPath,
                options.RemoteName,
                options.SourcePath,
                folderPath,
                folders.Count,
                folders.ElementAtOrDefault(0),
                folders.LastOrDefault(),
                folders.ElementAtOrDefault(0),
                folders.ElementAtOrDefault(1),
                folders.ElementAtOrDefault(2),
                folders.ElementAtOrDefault(3),
                folders.ElementAtOrDefault(4),
                fileName,
                nameWithoutExtension,
                extension,
                tokens.ElementAtOrDefault(0),
                tokens.ElementAtOrDefault(1),
                tokens.ElementAtOrDefault(2),
                tokens.ElementAtOrDefault(3));
        }
    }
}
