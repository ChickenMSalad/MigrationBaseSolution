using System.Text;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Sources.Aem.Clients;
using Migration.Connectors.Sources.Aem.Models;
using Migration.ControlPlane.ManifestBuilder;

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
                    "Export folders",
                    "One or more AEM DAM folder paths to export. Use one path per line, or comma-separated values.",
                    Required: true,
                    Placeholder: "/content/dam/site/folder-one\n/content/dam/site/folder-two"),
                new ManifestBuilderOptionDescriptor(
                    "recursive",
                    "Recursive",
                    "true/false. Defaults to true. Includes assets in child folders when true.",
                    Required: false,
                    Placeholder: "true")
            ]);
    }

    public async Task<BuildSourceManifestResult> BuildAsync(
        BuildSourceManifestRequest request,
        CancellationToken cancellationToken = default)
    {
        var folders = GetFolders(request);
        if (folders.Count == 0)
        {
            throw new ArgumentException("At least one AEM export folder is required.");
        }

        var recursive = GetBooleanOption(request, "recursive", defaultValue: true);
        var rows = new List<AemAsset>();

        foreach (var folder in folders)
        {
            _logger.LogInformation("Building AEM manifest for folder {Folder} recursive={Recursive}.", folder, recursive);

            await foreach (var asset in _aemClient.EnumerateAssetsAsync(folder, recursive, _logger, cancellationToken))
            {
                rows.Add(asset);
            }
        }

        var contentBytes = WriteCsv(rows);

        return new BuildSourceManifestResult(
            SourceType,
            ServiceName,
            $"aem-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv",
            "text/csv",
            Content: null,
            ContentBytes: contentBytes,
            rows.Count);
    }

    private static IReadOnlyList<string> GetFolders(BuildSourceManifestRequest request)
    {
        if (request.Options is null)
        {
            return Array.Empty<string>();
        }

        var rawValues = new List<string>();

        if (request.Options.TryGetValue("folders", out var folders) && !string.IsNullOrWhiteSpace(folders))
        {
            rawValues.Add(folders);
        }

        if (request.Options.TryGetValue("folder", out var folder) && !string.IsNullOrWhiteSpace(folder))
        {
            rawValues.Add(folder);
        }

        return rawValues
            .SelectMany(value => value.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeFolder)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool GetBooleanOption(BuildSourceManifestRequest request, string name, bool defaultValue)
    {
        if (request.Options is null ||
            !request.Options.TryGetValue(name, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static string NormalizeFolder(string folder)
    {
        var normalized = folder.Trim();

        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }

    private static byte[] WriteCsv(IReadOnlyCollection<AemAsset> assets)
    {
        var builder = new StringBuilder();
        builder.AppendLine("SourceAssetId,SourcePath,Name,MimeType,SizeBytes,Created,LastModified");

        foreach (var asset in assets)
        {
            builder.Append(Csv(asset.Id));
            builder.Append(',');
            builder.Append(Csv(asset.Path));
            builder.Append(',');
            builder.Append(Csv(asset.Name));
            builder.Append(',');
            builder.Append(Csv(asset.MimeType));
            builder.Append(',');
            builder.Append(Csv(asset.SizeBytes?.ToString()));
            builder.Append(',');
            builder.Append(Csv(asset.Created));
            builder.Append(',');
            builder.Append(Csv(asset.LastModified));
            builder.AppendLine();
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
