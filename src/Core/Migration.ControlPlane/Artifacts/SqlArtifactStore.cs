using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Migration.Application.Abstractions;
using Migration.Application.Artifacts;

namespace Migration.ControlPlane.Artifacts;

public sealed class SqlArtifactStore : IArtifactStore, IArtifactContentResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private readonly IConfiguration _configuration;
    private bool _schemaReady;
    private readonly SemaphoreSlim _schemaLock = new(1, 1);

    public SqlArtifactStore(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public bool IsArtifactReference(string? value)
        => ControlPlaneArtifactReference.TryParse(value, out _)
           || (!string.IsNullOrWhiteSpace(value) && value.Trim().StartsWith("artifact-", StringComparison.OrdinalIgnoreCase));

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
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name is required.", nameof(fileName));
        }

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        var artifactId = $"artifact-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var safeFileName = MakeSafeFileName(Path.GetFileName(fileName));
        var contentBytes = await ReadAllBytesAsync(content, cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var metadataDictionary = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        metadataDictionary["Sha256"] = Convert.ToHexString(SHA256.HashData(contentBytes));

        var record = new ControlPlaneArtifactRecord
        {
            ArtifactId = artifactId,
            Kind = kind,
            FileName = safeFileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Length = contentBytes.LongLength,
            RelativePath = ControlPlaneArtifactReference.Create(artifactId),
            AbsolutePath = ControlPlaneArtifactReference.Create(artifactId),
            CreatedUtc = now,
            ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId,
            Description = description,
            Metadata = metadataDictionary
        };

        const string sql = """
MERGE dbo.ControlPlaneArtifacts AS target
USING (SELECT @ArtifactId AS ArtifactId) AS source
ON target.ArtifactId = source.ArtifactId
WHEN MATCHED THEN UPDATE SET
    Kind = @Kind,
    FileName = @FileName,
    ContentType = @ContentType,
    Length = @Length,
    RelativePath = @RelativePath,
    AbsolutePath = @AbsolutePath,
    CreatedUtc = @CreatedUtc,
    ProjectId = @ProjectId,
    Description = @Description,
    MetadataJson = @MetadataJson,
    Content = @Content
WHEN NOT MATCHED THEN INSERT
(
    ArtifactId,
    Kind,
    FileName,
    ContentType,
    Length,
    RelativePath,
    AbsolutePath,
    CreatedUtc,
    ProjectId,
    Description,
    MetadataJson,
    Content
)
VALUES
(
    @ArtifactId,
    @Kind,
    @FileName,
    @ContentType,
    @Length,
    @RelativePath,
    @AbsolutePath,
    @CreatedUtc,
    @ProjectId,
    @Description,
    @MetadataJson,
    @Content
);
""";

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        AddRecordParameters(command, record, contentBytes);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return record;
    }

    public async Task<IReadOnlyList<ControlPlaneArtifactRecord>> ListAsync(
        ArtifactKind? kind = null,
        string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        var sql = """
SELECT ArtifactId, Kind, FileName, ContentType, Length, RelativePath, AbsolutePath,
       CreatedUtc, ProjectId, Description, MetadataJson
FROM dbo.ControlPlaneArtifacts
WHERE (@Kind IS NULL OR Kind = @Kind)
  AND (@ProjectId IS NULL OR ProjectId = @ProjectId)
ORDER BY CreatedUtc DESC;
""";

        var results = new List<ControlPlaneArtifactRecord>();
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Kind", SqlDbType.Int).Value = kind.HasValue ? (int)kind.Value : DBNull.Value;
        command.Parameters.Add("@ProjectId", SqlDbType.NVarChar, 200).Value = string.IsNullOrWhiteSpace(projectId) ? DBNull.Value : projectId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(ReadRecord(reader));
        }

        return results;
    }

    public async Task<ControlPlaneArtifactRecord?> GetAsync(string artifactId, CancellationToken cancellationToken = default)
    {
        var normalizedArtifactId = NormalizeArtifactId(artifactId);
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
SELECT ArtifactId, Kind, FileName, ContentType, Length, RelativePath, AbsolutePath,
       CreatedUtc, ProjectId, Description, MetadataJson
FROM dbo.ControlPlaneArtifacts
WHERE ArtifactId = @ArtifactId;
""";

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ArtifactId", SqlDbType.NVarChar, 120).Value = normalizedArtifactId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? ReadRecord(reader)
            : null;
    }

    public async Task<Stream> OpenReadAsync(string artifactId, CancellationToken cancellationToken = default)
    {
        var resolved = await OpenContentCoreAsync(artifactId, cancellationToken).ConfigureAwait(false);
        return resolved.Content;
    }

    async Task<ResolvedArtifactContent> IArtifactContentResolver.OpenReadAsync(
        string artifactReferenceOrId,
        CancellationToken cancellationToken)
        => await OpenContentCoreAsync(artifactReferenceOrId, cancellationToken).ConfigureAwait(false);

    public async Task<bool> DeleteAsync(string artifactId, CancellationToken cancellationToken = default)
    {
        var normalizedArtifactId = NormalizeArtifactId(artifactId);
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        const string sql = "DELETE FROM dbo.ControlPlaneArtifacts WHERE ArtifactId = @ArtifactId;";
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ArtifactId", SqlDbType.NVarChar, 120).Value = normalizedArtifactId;
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    public async Task<ManifestPreview> PreviewManifestAsync(
        string artifactId,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        await using var resolved = await OpenContentCoreAsync(artifactId, cancellationToken).ConfigureAwait(false);

        if (!Path.GetExtension(resolved.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return new ManifestPreview
            {
                ArtifactId = resolved.ArtifactId,
                FileName = resolved.FileName,
                Columns = Array.Empty<string>(),
                SampleRows = Array.Empty<IReadOnlyDictionary<string, string?>>()
            };
        }

        using var reader = new StreamReader(resolved.Content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var headerLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return new ManifestPreview
            {
                ArtifactId = resolved.ArtifactId,
                FileName = resolved.FileName,
                Columns = Array.Empty<string>(),
                SampleRows = Array.Empty<IReadOnlyDictionary<string, string?>>()
            };
        }

        var columns = ParseCsvLine(headerLine).ToArray();
        var rows = new List<IReadOnlyDictionary<string, string?>>();

        while (rows.Count < Math.Clamp(take, 1, 100))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            var values = ParseCsvLine(line).ToArray();
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < columns.Length; i++)
            {
                row[columns[i]] = i < values.Length ? values[i] : string.Empty;
            }

            rows.Add(row);
        }

        return new ManifestPreview
        {
            ArtifactId = resolved.ArtifactId,
            FileName = resolved.FileName,
            Columns = columns,
            SampleRows = rows
        };
    }

    private async Task<ResolvedArtifactContent> OpenContentCoreAsync(
        string artifactReferenceOrId,
        CancellationToken cancellationToken)
    {
        var artifactId = NormalizeArtifactId(artifactReferenceOrId);
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
SELECT ArtifactId, FileName, ContentType, Content
FROM dbo.ControlPlaneArtifacts
WHERE ArtifactId = @ArtifactId;
""";

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ArtifactId", SqlDbType.NVarChar, 120).Value = artifactId;

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new FileNotFoundException($"Artifact '{artifactId}' was not found in SQL artifact storage.");
        }

        var id = reader.GetString(0);
        var fileName = reader.GetString(1);
        var contentType = reader.GetString(2);
        var contentBytes = (byte[])reader[3];
        return new ResolvedArtifactContent(id, fileName, contentType, new MemoryStream(contentBytes, writable: false));
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaReady)
        {
            return;
        }

        await _schemaLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_schemaReady)
            {
                return;
            }

            const string sql = """
IF OBJECT_ID(N'dbo.ControlPlaneArtifacts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ControlPlaneArtifacts
    (
        ArtifactId NVARCHAR(120) NOT NULL CONSTRAINT PK_ControlPlaneArtifacts PRIMARY KEY,
        Kind INT NOT NULL,
        FileName NVARCHAR(512) NOT NULL,
        ContentType NVARCHAR(256) NOT NULL,
        Length BIGINT NOT NULL,
        RelativePath NVARCHAR(1024) NOT NULL,
        AbsolutePath NVARCHAR(1024) NOT NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL,
        ProjectId NVARCHAR(200) NULL,
        Description NVARCHAR(MAX) NULL,
        MetadataJson NVARCHAR(MAX) NOT NULL,
        Content VARBINARY(MAX) NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ControlPlaneArtifacts_Kind_Project_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.ControlPlaneArtifacts')
)
BEGIN
    CREATE INDEX IX_ControlPlaneArtifacts_Kind_Project_CreatedUtc
    ON dbo.ControlPlaneArtifacts (Kind, ProjectId, CreatedUtc DESC);
END;
""";

            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _schemaReady = true;
        }
        finally
        {
            _schemaLock.Release();
        }
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("OperationalSql")
            ?? _configuration.GetConnectionString("MigrationOperationalStore")
            ?? _configuration["OperationalSql:ConnectionString"]
            ?? _configuration["SqlOperationalStore:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SQL artifact storage requires ConnectionStrings:OperationalSql or ConnectionStrings:MigrationOperationalStore.");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static void AddRecordParameters(SqlCommand command, ControlPlaneArtifactRecord record, byte[] content)
    {
        command.Parameters.Add("@ArtifactId", SqlDbType.NVarChar, 120).Value = record.ArtifactId;
        command.Parameters.Add("@Kind", SqlDbType.Int).Value = (int)record.Kind;
        command.Parameters.Add("@FileName", SqlDbType.NVarChar, 512).Value = record.FileName;
        command.Parameters.Add("@ContentType", SqlDbType.NVarChar, 256).Value = record.ContentType;
        command.Parameters.Add("@Length", SqlDbType.BigInt).Value = record.Length;
        command.Parameters.Add("@RelativePath", SqlDbType.NVarChar, 1024).Value = record.RelativePath;
        command.Parameters.Add("@AbsolutePath", SqlDbType.NVarChar, 1024).Value = record.AbsolutePath;
        command.Parameters.Add("@CreatedUtc", SqlDbType.DateTimeOffset).Value = record.CreatedUtc;
        command.Parameters.Add("@ProjectId", SqlDbType.NVarChar, 200).Value = (object?)record.ProjectId ?? DBNull.Value;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = (object?)record.Description ?? DBNull.Value;
        command.Parameters.Add("@MetadataJson", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(record.Metadata, JsonOptions);
        command.Parameters.Add("@Content", SqlDbType.VarBinary, -1).Value = content;
    }

    private static ControlPlaneArtifactRecord ReadRecord(SqlDataReader reader)
    {
        var metadataJson = reader.GetString(reader.GetOrdinal("MetadataJson"));
        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson, JsonOptions)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return new ControlPlaneArtifactRecord
        {
            ArtifactId = reader.GetString(reader.GetOrdinal("ArtifactId")),
            Kind = (ArtifactKind)reader.GetInt32(reader.GetOrdinal("Kind")),
            FileName = reader.GetString(reader.GetOrdinal("FileName")),
            ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
            Length = reader.GetInt64(reader.GetOrdinal("Length")),
            RelativePath = reader.GetString(reader.GetOrdinal("RelativePath")),
            AbsolutePath = reader.GetString(reader.GetOrdinal("AbsolutePath")),
            CreatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedUtc")),
            ProjectId = reader.IsDBNull(reader.GetOrdinal("ProjectId")) ? null : reader.GetString(reader.GetOrdinal("ProjectId")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            Metadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string NormalizeArtifactId(string artifactReferenceOrId)
    {
        if (string.IsNullOrWhiteSpace(artifactReferenceOrId))
        {
            throw new ArgumentException("Artifact id is required.", nameof(artifactReferenceOrId));
        }

        return ControlPlaneArtifactReference.TryParse(artifactReferenceOrId, out var artifactId)
            ? artifactId
            : artifactReferenceOrId.Trim();
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var segment))
        {
            return segment.AsSpan(0, (int)memoryStream.Length).ToArray();
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe)
            ? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)
            : safe;
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
