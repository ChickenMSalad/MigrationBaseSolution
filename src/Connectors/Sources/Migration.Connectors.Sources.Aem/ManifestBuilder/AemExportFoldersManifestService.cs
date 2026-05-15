using System.Globalization;
using System.Text;
using Migration.Connectors.Sources.Aem.Clients;
using Migration.Connectors.Sources.Aem.Models;
using Migration.ControlPlane.ManifestBuilder;
using Microsoft.Extensions.Logging;

namespace Migration.Connectors.Sources.Aem.ManifestBuilder;

public sealed class AemExportFoldersManifestService : ISourceManifestService
{
    private const string Source = "aem";
    private const string Service = "export-folders";

    private readonly IAemClient _aemClient;
    private readonly ILogger<AemExportFoldersManifestService> _logger;

    public AemExportFoldersManifestService(
        IAemClient aemClient,
        ILogger<AemExportFoldersManifestService> logger)
    {
        _aemClient = aemClient ?? throw new ArgumentNullException(nameof(aemClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string SourceType => Source;

    public string ServiceName => Service;

    public ManifestBuilderServiceDescriptor GetDescriptor()
    {
        return new ManifestBuilderServiceDescriptor(
            Source,
            Service,
            "Export folders",
            "Builds an AEM manifest by exporting assets from one or more AEM DAM folders.",
            new[]
            {
                new ManifestBuilderOptionDescriptor(
                    "folders",
                    "Folders",
                    "One AEM DAM folder path per line. Example: /content/dam/site/folder.",
                    Required: true,
                    Placeholder: "/content/dam/example-folder"),
                new ManifestBuilderOptionDescriptor(
                    "recursive",
                    "Recursive",
                    "true/false. Defaults to true.",
                    Required: false,
                    Placeholder: "true")
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

        var options = request.Options ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var folders = GetFolders(options);

        if (folders.Count == 0)
        {
            throw new ArgumentException("At least one AEM folder is required. Enter one folder path per line.", nameof(request));
        }

        var recursive = GetBool(options, "recursive", defaultValue: true);
        var rows = new List<AemManifestRow>();
        var rowId = 1;

        foreach (var folder in folders)
        {
            await foreach (var asset in _aemClient.EnumerateAssetsAsync(folder, recursive, _logger, cancellationToken))
            {
                rows.Add(AemManifestRow.From(asset, rowId++, folder));
            }
        }

        var csv = BuildCsv(rows);
        var fileName = $"aem-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";

        return new BuildSourceManifestResult(
            Source,
            Service,
            fileName,
            "text/csv",
            csv,
            ContentBytes: null,
            rows.Count);
    }

    private static IReadOnlyList<string> GetFolders(IReadOnlyDictionary<string, string> options)
    {
        var raw = GetValue(options, "folders", "folderPaths", "exportFolders", "export.folders", "folder");

        return (raw ?? string.Empty)
            .Split(new[] { '\r', '\n', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeFolder)
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeFolder(string folder)
    {
        folder = folder.Trim();

        if (!folder.StartsWith('/'))
        {
            folder = "/" + folder;
        }

        return folder.TrimEnd('/');
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> options, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> options, string key, bool defaultValue)
    {
        var value = GetValue(options, key);
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static string BuildCsv(IReadOnlyList<AemManifestRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', new[]
        {
            "RowId",
            "SourceType",
            "ServiceName",
            "SourceAssetId",
            "SourcePath",
            "ExportFolder",
            "FileName",
            "FileNameWithoutExtension",
            "Extension",
            "MimeType",
            "SizeBytes",
            "Created",
            "LastModified"
        }.Select(EscapeCsv)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', new[]
            {
                row.RowId.ToString(CultureInfo.InvariantCulture),
                Source,
                Service,
                row.SourceAssetId,
                row.SourcePath,
                row.ExportFolder,
                row.FileName,
                row.FileNameWithoutExtension,
                row.Extension,
                row.MimeType,
                row.SizeBytes,
                row.Created,
                row.LastModified
            }.Select(EscapeCsv)));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n')
            ? $"\"{escaped}\""
            : escaped;
    }

    private sealed record AemManifestRow(
        int RowId,
        string SourceAssetId,
        string SourcePath,
        string ExportFolder,
        string FileName,
        string FileNameWithoutExtension,
        string Extension,
        string? MimeType,
        string? SizeBytes,
        string? Created,
        string? LastModified)
    {
        public static AemManifestRow From(AemAsset asset, int rowId, string exportFolder)
        {
            var path = asset.Path ?? string.Empty;
            var fileName = Path.GetFileName(path);

            return new AemManifestRow(
                rowId,
                asset.Id ?? path,
                path,
                exportFolder,
                fileName,
                Path.GetFileNameWithoutExtension(fileName),
                Path.GetExtension(fileName).TrimStart('.'),
                asset.MimeType,
                asset.SizeBytes?.ToString(CultureInfo.InvariantCulture),
                asset.Created?.ToString(),
                asset.LastModified?.ToString());
        }
    }
}
