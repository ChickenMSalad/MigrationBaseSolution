using System.Text;
using System.Text.Json;
using Migration.ControlPlane.Storage;

namespace Migration.ControlPlane.Audit;

public sealed class ArtifactAuditPersistenceProvider : IAuditPersistenceProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IArtifactStorageService _artifactStorage;
    private readonly IArtifactManifestIndexService _manifestIndex;
    private readonly ArtifactAuditPersistenceOptions _options;
    private readonly object _gate = new();
    private readonly List<AuditRecord> _recent = [];

    public ArtifactAuditPersistenceProvider(
        IArtifactStorageService artifactStorage,
        IArtifactManifestIndexService manifestIndex,
        ArtifactAuditPersistenceOptions options)
    {
        _artifactStorage = artifactStorage;
        _manifestIndex = manifestIndex;
        _options = options;
    }

    public AuditPersistenceProviderDescriptor Descriptor { get; } = new(
        ProviderKind: "artifactStorage",
        IsConfigured: true,
        IsDurable: true,
        SupportsQuery: true,
        SupportsArtifactLinking: true,
        Warnings:
        [
            "Recent audit query is backed by the current process cache; durable historical query will be added later."
        ]);

    public async Task<AuditWriteResult> WriteAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var fileName = BuildFileName(record);

        var request = new ArtifactStorageRequest(
            WorkspaceId: record.WorkspaceId,
            ArtifactKind: _options.ArtifactKind,
            ArtifactId: _options.ArtifactId,
            FileName: fileName,
            ContentType: "application/json");

        var payload = JsonSerializer.Serialize(record, JsonOptions);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var artifact = await _artifactStorage.WriteAsync(
            request,
            content,
            cancellationToken).ConfigureAwait(false);

        await _manifestIndex.AddAsync(artifact, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            _recent.Add(record);

            if (_recent.Count > Math.Max(10, _options.RecentQueryLimit))
            {
                _recent.RemoveRange(0, _recent.Count - _options.RecentQueryLimit);
            }
        }

        return new AuditWriteResult(
            Accepted: true,
            ProviderKind: Descriptor.ProviderKind,
            AuditId: record.AuditId,
            WrittenUtc: DateTimeOffset.UtcNow,
            ArtifactObjectKey: artifact.ObjectKey);
    }

    public Task<IReadOnlyList<AuditRecord>> QueryRecentAsync(
        string workspaceId,
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var records = _recent
                .Where(x => string.Equals(x.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CreatedUtc)
                .Take(Math.Clamp(take, 1, _options.RecentQueryLimit))
                .ToArray();

            return Task.FromResult<IReadOnlyList<AuditRecord>>(records);
        }
    }

    private string BuildFileName(AuditRecord record)
    {
        var timestamp = record.CreatedUtc.UtcDateTime.ToString("yyyyMMddTHHmmssfffZ");
        return $"{Normalize(_options.FileNamePrefix)}-{timestamp}-{Normalize(record.EventName)}-{record.AuditId}.json";
    }

    private static string Normalize(string value)
    {
        var sanitized = new string((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '-')
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? "audit"
            : sanitized.Trim('-');
    }
}
