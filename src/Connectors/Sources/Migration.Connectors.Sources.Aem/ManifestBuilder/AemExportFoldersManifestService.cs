using System.Globalization;
using System.Text;
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
                    "Export folders",
                    "One or more AEM DAM folder paths to export. Enter one path per line, or separate paths with commas. Example: /content/dam/site/folder",
                    Required: true,
                    Placeholder: "/content/dam/site/folder-one\n/content/dam/site/folder-two"),
                new ManifestBuilderOptionDescriptor(
                    "recursive",
                    "Recursive",
                    "true/false. Defaults to true. When true, child folders are included.",
                    Required: false,
                    Placeholder: "true")
            ]);
    }

    public async Task<BuildSourceManifestResult> BuildAsync(
        BuildSourceManifestRequest request,
        CancellationToken cancellationToken = default)
    {
        var folders = GetFolders(request.Options).ToArray();

        if (folders.Length == 0)
        {
            throw new ArgumentException("At least one AEM export folder is required.");
        }

        var recursive = GetBooleanOption(request.Options, "recursive", defaultValue: true);
        var rows = new List<AemManifestRow>();

        foreach (var folder in folders)
        {
            _logger.LogInformation("Building AEM manifest rows for folder {Folder} (recursive={Recursive}).", folder, recursive);

            await foreach (var asset in _aemClient.EnumerateAssetsAsync(folder, recursive, _logger, cancellationToken, useLastModifiedOnly: false))
            {
                rows.Add(new AemManifestRow(
                    SourceAssetId: string.IsNullOrWhiteSpace(asset.Id) ? asset.Path : asset.Id,
                    SourcePath: asset.Path,
                    FileName: asset.Name,
                    FolderPath: GetFolderPath(asset.Path),
                    MimeType: asset.MimeType,
                    SizeBytes: asset.SizeBytes?.ToString(CultureInfo.InvariantCulture),
                    Created: asset.Created,
                    LastModified: asset.LastModified,
                    ExportFolder: folder));
            }
        }

        return new BuildSourceManifestResult(
            SourceType,
            ServiceName,
            $"aem-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv",
            "text/csv",
            Content: null,
            ContentBytes: WriteCsv(rows),
            rows.Count);
    }

    private static IEnumerable<string> GetFolders(IReadOnlyDictionary<string, string>? options)
    {
        if (options is null)
        {
            yield break;
        }

        if (options.TryGetValue("folders", out var folders) && !string.IsNullOrWhiteSpace(folders))
        {
            foreach (var folder in SplitFolderList(folders))
            {
                yield return NormalizeFolder(folder);
            }
        }

        if (options.TryGetValue("folder", out var folderValue) && !string.IsNullOrWhiteSpace(folderValue))
        {
            yield return NormalizeFolder(folderValue);
        }
    }

    private static IEnumerable<string> SplitFolderList(string value)
    {
        return value
            .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Distinct(StringComparer.OrdinalIgnoreCase);
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

    private static bool GetBooleanOption(IReadOnlyDictionary<string, string>? options, string name, bool defaultValue)
    {
        if (options is null || !options.TryGetValue(name, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        if (bool.TryParse(raw.Trim(), out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static string GetFolderPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Replace('\\', '/');
        var index = normalized.LastIndexOf('/');

        return index <= 0 ? string.Empty : normalized[..index];
    }

    private static byte[] WriteCsv(IReadOnlyCollection<AemManifestRow> rows)
    {
        var sb = new StringBuilder();

        AppendCsvLine(sb,
            "SourceAssetId",
            "SourcePath",
            "FileName",
            "FolderPath",
            "MimeType",
            "SizeBytes",
            "Created",
            "LastModified",
            "ExportFolder");

        foreach (var row in rows)
        {
            AppendCsvLine(sb,
                row.SourceAssetId,
                row.SourcePath,
                row.FileName,
                row.FolderPath,
                row.MimeType,
                row.SizeBytes,
                row.Created,
                row.LastModified,
                row.ExportFolder);
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void AppendCsvLine(StringBuilder sb, params string?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(EscapeCsv(values[i]));
        }

        sb.AppendLine();
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;

        if (value.Contains('"') || value.Contains(',') || value.Contains('\r') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private sealed record AemManifestRow(
        string? SourceAssetId,
        string? SourcePath,
        string? FileName,
        string? FolderPath,
        string? MimeType,
        string? SizeBytes,
        string? Created,
        string? LastModified,
        string ExportFolder);
}
