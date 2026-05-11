using Microsoft.Extensions.Options;
using Migration.Connectors.Sources.SharePoint.Configuration;
using Migration.ControlPlane.ManifestBuilder;

namespace Migration.Connectors.Sources.SharePoint.ManifestBuilder;

public sealed class SharePointRcloneManifestService : ISourceManifestService
{
    private readonly SharePointSourceOptions _configuredOptions;
    private readonly SharePointRcloneManifestRunner _runner;

    public SharePointRcloneManifestService(
        IOptions<SharePointSourceOptions> configuredOptions,
        SharePointRcloneManifestRunner runner)
    {
        _configuredOptions = configuredOptions.Value;
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public string SourceType => "SharePoint";

    public string ServiceName => "Rclone";

    public ManifestBuilderServiceDescriptor GetDescriptor()
    {
        return new ManifestBuilderServiceDescriptor(
            SourceType,
            ServiceName,
            "SharePoint - rclone",
            "Builds a migration manifest from a SharePoint document library using an existing rclone remote.",
            new[]
            {
                new ManifestBuilderOptionDescriptor(
                    "rcloneExecutablePath",
                    "rclone executable path",
                    "Path to rclone.exe, or leave blank when rclone is on PATH.",
                    Required: false,
                    Placeholder: "rclone"),

                new ManifestBuilderOptionDescriptor(
                    "rcloneConfigPath",
                    "rclone config path",
                    "Optional path to rclone.conf.",
                    Required: false,
                    Placeholder: "C:\\Users\\you\\AppData\\Roaming\\rclone\\rclone.conf"),

                new ManifestBuilderOptionDescriptor(
                    "remoteName",
                    "Remote name",
                    "Name of the configured rclone remote.",
                    Required: true,
                    Placeholder: "sp-test"),

                new ManifestBuilderOptionDescriptor(
                    "rootPath",
                    "Root path",
                    "Folder/path inside the remote to scan.",
                    Required: false,
                    Placeholder: "images"),

                new ManifestBuilderOptionDescriptor(
                    "includeExtensions",
                    "Include extensions",
                    "Optional comma-separated extensions to include.",
                    Required: false,
                    Placeholder: ".jpg,.jpeg,.png,.webp"),

                new ManifestBuilderOptionDescriptor(
                    "excludeExtensions",
                    "Exclude extensions",
                    "Optional comma-separated extensions to exclude.",
                    Required: false,
                    Placeholder: ".tmp,.bak"),

                new ManifestBuilderOptionDescriptor(
                    "includeHidden",
                    "Include hidden",
                    "true/false. When false, dot-prefixed path segments are skipped.",
                    Required: false,
                    Placeholder: "false"),

                new ManifestBuilderOptionDescriptor(
                    "includeFileNameMetadata",
                    "Include file name metadata",
                    "true/false.",
                    Required: false,
                    Placeholder: "true"),

                new ManifestBuilderOptionDescriptor(
                    "includeFolderMetadata",
                    "Include folder metadata",
                    "true/false.",
                    Required: false,
                    Placeholder: "true"),

                new ManifestBuilderOptionDescriptor(
                    "maxFolderDepth",
                    "Max folder depth",
                    "Optional maximum folder depth to include.",
                    Required: false)
            });
    }

    public async Task<BuildSourceManifestResult> BuildAsync(
        BuildSourceManifestRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = SharePointRcloneManifestOptions.FromRequest(request, _configuredOptions);

        var items = await _runner
            .ListFilesAsync(options, cancellationToken)
            .ConfigureAwait(false);

        var filtered = items
            .Where(item => ShouldInclude(item, options))
            .OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var content = SharePointRcloneManifestCsvWriter.Write(filtered, options);

        return new BuildSourceManifestResult(
            SourceType,
            ServiceName,
            $"sharepoint-rclone-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv",
            "text/csv",
            Content: content,
            ContentBytes: null,
            RowCount: filtered.Length);
    }

    private static bool ShouldInclude(
        SharePointRcloneManifestItem item,
        SharePointRcloneManifestOptions options)
    {
        var segments = item.RelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (!options.IncludeHidden &&
            segments.Any(segment => segment.StartsWith(".", StringComparison.Ordinal)))
        {
            return false;
        }

        if (options.MaxFolderDepth > 0 && item.FolderDepth > options.MaxFolderDepth)
        {
            return false;
        }

        var includeExtensions = ParseExtensions(options.IncludeExtensions);
        if (includeExtensions.Count > 0 &&
            !includeExtensions.Contains(NormalizeExtension(item.FileExtension)))
        {
            return false;
        }

        var excludeExtensions = ParseExtensions(options.ExcludeExtensions);
        if (excludeExtensions.Count > 0 &&
            excludeExtensions.Contains(NormalizeExtension(item.FileExtension)))
        {
            return false;
        }

        return true;
    }

    private static HashSet<string> ParseExtensions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return value
            .Split(new[] { ',', ';', '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeExtension(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var extension = value.Trim();
        return extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : "." + extension;
    }
}
