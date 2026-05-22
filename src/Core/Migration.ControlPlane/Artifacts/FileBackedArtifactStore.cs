using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Migration.ControlPlane.Options;

namespace Migration.ControlPlane.Artifacts;

public sealed class FileBackedArtifactStore : IArtifactStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly AdminApiOptions _options;

    public FileBackedArtifactStore(IOptions<AdminApiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ControlPlaneArtifactRecord> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        ArtifactKind kind,
        string? projectId = null,
        string? description = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name is required.", nameof(fileName));
        }

        var artifactId = $"artifact-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var safeFileName = MakeSafeFileName(Path.GetFileName(fileName));
        var artifactDirectory = Path.Combine(GetArtifactsRoot(), artifactId);
        Directory.CreateDirectory(artifactDirectory);

        var filePath = Path.Combine(artifactDirectory, safeFileName);
        await using (var destination = File.Create(filePath))
        {
            await content.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        }

        var fileInfo = new FileInfo(filePath);
        var record = new ControlPlaneArtifactRecord
        {
            ArtifactId = artifactId,
            Kind = kind,
            FileName = safeFileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Length = fileInfo.Length,
            RelativePath = Path.Combine("artifacts", artifactId, safeFileName).Replace('\\', '/'),
            AbsolutePath = fileInfo.FullName,
            CreatedUtc = DateTimeOffset.UtcNow,
            ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId,
            Description = description,
            Metadata = metadata is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase)
        };

        await SaveRecordAsync(record, cancellationToken).ConfigureAwait(false);
        return record;
    }

    public async Task<IReadOnlyList<ControlPlaneArtifactRecord>> ListAsync(
        ArtifactKind? kind = null,
        string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var root = GetArtifactsRoot();
        if (!Directory.Exists(root))
        {
            return Array.Empty<ControlPlaneArtifactRecord>();
        }

        var records = new List<ControlPlaneArtifactRecord>();
        foreach (var metadataPath in Directory.EnumerateFiles(root, "artifact.json", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = File.OpenRead(metadataPath);
            var record = await JsonSerializer.DeserializeAsync<ControlPlaneArtifactRecord>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (record is null)
            {
                continue;
            }

            if (kind.HasValue && record.Kind != kind.Value)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(projectId) && !string.Equals(record.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            records.Add(record);
        }

        return records
            .OrderByDescending(x => x.CreatedUtc)
            .ToArray();
    }

    public async Task<ControlPlaneArtifactRecord?> GetAsync(string artifactId, CancellationToken cancellationToken = default)
    {
        var metadataPath = GetMetadataPath(artifactId);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(metadataPath);
        return await JsonSerializer.DeserializeAsync<ControlPlaneArtifactRecord>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream> OpenReadAsync(string artifactId, CancellationToken cancellationToken = default)
    {
        var record = await GetAsync(artifactId, cancellationToken).ConfigureAwait(false)
            ?? throw new FileNotFoundException($"Artifact '{artifactId}' was not found.");

        return File.OpenRead(record.AbsolutePath);
    }

    public Task<bool> DeleteAsync(string artifactId, CancellationToken cancellationToken = default)
    {
        var artifactDirectory = Path.Combine(GetArtifactsRoot(), MakeSafeFileName(artifactId));
        if (!Directory.Exists(artifactDirectory))
        {
            return Task.FromResult(false);
        }

        Directory.Delete(artifactDirectory, recursive: true);
        return Task.FromResult(true);
    }

    public async Task<ManifestPreview> PreviewManifestAsync(string artifactId, int take = 10, CancellationToken cancellationToken = default)
    {
        var record = await GetAsync(artifactId, cancellationToken).ConfigureAwait(false)
            ?? throw new FileNotFoundException($"Artifact '{artifactId}' was not found.");

        if (record.Kind != ArtifactKind.Manifest)
        {
            throw new InvalidOperationException($"Artifact '{artifactId}' is not a manifest artifact.");
        }

        var extension = Path.GetExtension(record.FileName);
        if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return new ManifestPreview
            {
                ArtifactId = record.ArtifactId,
                FileName = record.FileName,
                Columns = Array.Empty<string>(),
                SampleRows = Array.Empty<IReadOnlyDictionary<string, string>>()
            };
        }

        await using var file = File.OpenRead(record.AbsolutePath);
        using var reader = new StreamReader(file, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var headerLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return new ManifestPreview
            {
                ArtifactId = record.ArtifactId,
                FileName = record.FileName,
                Columns = Array.Empty<string>(),
                SampleRows = Array.Empty<IReadOnlyDictionary<string, string>>()
            };
        }

        var columns = ParseCsvLine(headerLine).ToArray();
        var rows = new List<IReadOnlyDictionary<string, string>>();

        while (rows.Count < Math.Clamp(take, 1, 100))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            var values = ParseCsvLine(line).ToArray();
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < columns.Length; i++)
            {
                row[columns[i]] = i < values.Length ? values[i] : string.Empty;
            }

            rows.Add(row);
        }

        return new ManifestPreview
        {
            ArtifactId = record.ArtifactId,
            FileName = record.FileName,
            Columns = columns,
            SampleRows = rows
        };
    }

    private async Task SaveRecordAsync(ControlPlaneArtifactRecord record, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.Combine(GetArtifactsRoot(), record.ArtifactId));
        await using var stream = File.Create(GetMetadataPath(record.ArtifactId));
        await JsonSerializer.SerializeAsync(stream, record, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private string GetArtifactsRoot()
    {
        var root = string.IsNullOrWhiteSpace(_options.StorageRoot)
            ? "Runtime/admin-api"
            : _options.StorageRoot;

        var fullRoot = Path.GetFullPath(root);
        var artifactsRoot = Path.Combine(fullRoot, "artifacts");
        Directory.CreateDirectory(artifactsRoot);
        return artifactsRoot;
    }

    private string GetMetadataPath(string artifactId)
    {
        return Path.Combine(GetArtifactsRoot(), MakeSafeFileName(artifactId), "artifact.json");
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) : safe;
    }

    private static IEnumerable<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        values.Add(current.ToString());
        return values;
    }
}
