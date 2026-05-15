using Migration.Connectors.Sources.Aem.Clients;
using Migration.Connectors.Sources.Aem.Models;
using Migration.ControlPlane.ManifestBuilder;
using Microsoft.Extensions.Logging;

namespace Migration.Connectors.Sources.Aem.ManifestBuilder;

public sealed class AemExportFoldersManifestService : ISourceManifestService
{
    private readonly IAemClient _aemClient;
    private readonly ILogger<AemExportFoldersManifestService> _logger;

    public AemExportFoldersManifestService(
        IAemClient aemClient,
        ILogger<AemExportFoldersManifestService> logger)
    {
        _aemClient = aemClient ?? throw new ArgumentNullException(nameof(aemClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string SourceType => "aem";

    public string ServiceName => "export-folders";

    public ManifestBuilderServiceDescriptor GetDescriptor()
    {
        return new ManifestBuilderServiceDescriptor(
            SourceType,
            ServiceName,
            "Export Folders",
            "Exports one or more AEM DAM folders into a migration manifest file.",
            [
                new ManifestBuilderOptionDescriptor(
                    "folders",
                    "Folders",
                    "One or more AEM folder paths to export. Use one path per line, or separate paths with commas or semicolons.",
                    Required: true,
                    Placeholder: "/content/dam/example/folder-one\n/content/dam/example/folder-two"),
                new ManifestBuilderOptionDescriptor(
                    "recursive",
                    "Recursive",
                    "Whether child folders should be included. Defaults to true.",
                    Required: false,
                    Placeholder: "true or false")
            ]);
    }

    public async Task<BuildSourceManifestResult> BuildAsync(
        BuildSourceManifestRequest request,
        CancellationToken cancellationToken = default)
    {
        var folders = GetFolders(request);
        var recursive = GetBooleanOption(request, "recursive", defaultValue: true);
        var useLastModifiedOnly = GetBooleanOption(request, "useLastModifiedOnly", defaultValue: false);

        if (folders.Count == 0)
        {
            throw new ArgumentException("At least one AEM folder path is required. Populate the folders option with one or more /content/dam paths.");
        }

        var assets = new List<AemAsset>();

        foreach (var folder in folders)
        {
            _logger.LogInformation("Building AEM manifest for folder {Folder} (recursive={Recursive}).", folder, recursive);

            await foreach (var asset in _aemClient.EnumerateAssetsAsync(folder, recursive, _logger, cancellationToken, useLastModifiedOnly))
            {
                assets.Add(asset);
            }
        }

        var distinctAssets = assets
            .GroupBy(asset => !string.IsNullOrWhiteSpace(asset.Id) ? asset.Id : asset.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        return new BuildSourceManifestResult(
            SourceType,
            ServiceName,
            $"aem-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv",
            "text/csv",
            Content: null,
            ContentBytes: AemManifestCsvWriter.WriteManifestCsv(distinctAssets),
            distinctAssets.Count);
    }

    private static IReadOnlyList<string> GetFolders(BuildSourceManifestRequest request)
    {
        var folders = new List<string>();

        if (request.Options is not null)
        {
            AddOptionValues(request.Options, "folders", folders);
            AddOptionValues(request.Options, "folder", folders);
        }

        return folders
            .Select(NormalizeFolder)
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddOptionValues(
        IReadOnlyDictionary<string, string> options,
        string key,
        ICollection<string> folders)
    {
        if (!options.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        foreach (var folder in raw.Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(folder))
            {
                folders.Add(folder);
            }
        }
    }

    private static string NormalizeFolder(string folder)
    {
        folder = folder.Trim();

        if (folder.Length == 0)
        {
            return string.Empty;
        }

        if (!folder.StartsWith("/", StringComparison.Ordinal))
        {
            folder = "/" + folder;
        }

        return folder.Length == 1 ? folder : folder.TrimEnd('/');
    }

    private static bool GetBooleanOption(
        BuildSourceManifestRequest request,
        string key,
        bool defaultValue)
    {
        if (request.Options is null ||
            !request.Options.TryGetValue(key, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        if (bool.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        if (string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "y", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(raw, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "no", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "n", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return defaultValue;
    }
}
