using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

/// <summary>
/// SQL-backed credential-set metadata store.
///
/// This stores credential metadata and secret references, not raw production secrets.
/// Values for secret keys should already be references such as kv://secret-name.
/// A file-backed fallback is used only as a one-time migration source so existing
/// local Admin API records can be promoted into the shared SQL store.
/// </summary>
public sealed class SqlCredentialSetStore : ICredentialSetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly string? _connectionString;
    private readonly FileBackedCredentialSetStore _fileFallback;
    private readonly SemaphoreSlim _schemaLock = new(1, 1);
    private volatile bool _schemaEnsured;

    public SqlCredentialSetStore(
        IConfiguration configuration,
        FileBackedCredentialSetStore fileFallback)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _fileFallback = fileFallback ?? throw new ArgumentNullException(nameof(fileFallback));
        _connectionString = ResolveConnectionString(configuration);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<IReadOnlyList<CredentialSetRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return await _fileFallback.ListAsync(cancellationToken).ConfigureAwait(false);
        }

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await ImportFileFallbackAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT
                CredentialSetId,
                DisplayName,
                ConnectorType,
                ConnectorRole,
                ValuesJson,
                SecretKeysJson,
                CreatedUtc,
                UpdatedUtc
            FROM migration.CredentialSets
            ORDER BY UpdatedUtc DESC;
            """;

        var results = new List<CredentialSetRecord>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(ReadRecord(reader));
        }

        return results;
    }

    public async Task<CredentialSetRecord?> GetAsync(string credentialSetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(credentialSetId))
        {
            return null;
        }

        if (!IsConfigured)
        {
            return await _fileFallback.GetAsync(credentialSetId, cancellationToken).ConfigureAwait(false);
        }

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        var sqlRecord = await GetSqlAsync(credentialSetId, cancellationToken).ConfigureAwait(false);
        if (sqlRecord is not null)
        {
            return sqlRecord;
        }

        // One-time migration path for records created before credential metadata was shared.
        var fileRecord = await _fileFallback.GetAsync(credentialSetId, cancellationToken).ConfigureAwait(false);
        if (fileRecord is not null)
        {
            await SaveAsync(fileRecord, cancellationToken).ConfigureAwait(false);
        }

        return fileRecord;
    }

    public async Task SaveAsync(CredentialSetRecord credentialSet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentialSet);

        if (!IsConfigured)
        {
            await _fileFallback.SaveAsync(credentialSet, cancellationToken).ConfigureAwait(false);
            return;
        }

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            MERGE migration.CredentialSets AS target
            USING
            (
                SELECT
                    @CredentialSetId AS CredentialSetId,
                    @DisplayName AS DisplayName,
                    @ConnectorType AS ConnectorType,
                    @ConnectorRole AS ConnectorRole,
                    @ValuesJson AS ValuesJson,
                    @SecretKeysJson AS SecretKeysJson,
                    @CreatedUtc AS CreatedUtc,
                    @UpdatedUtc AS UpdatedUtc
            ) AS source
            ON target.CredentialSetId = source.CredentialSetId
            WHEN MATCHED THEN
                UPDATE SET
                    DisplayName = source.DisplayName,
                    ConnectorType = source.ConnectorType,
                    ConnectorRole = source.ConnectorRole,
                    ValuesJson = source.ValuesJson,
                    SecretKeysJson = source.SecretKeysJson,
                    UpdatedUtc = source.UpdatedUtc
            WHEN NOT MATCHED THEN
                INSERT
                (
                    CredentialSetId,
                    DisplayName,
                    ConnectorType,
                    ConnectorRole,
                    ValuesJson,
                    SecretKeysJson,
                    CreatedUtc,
                    UpdatedUtc
                )
                VALUES
                (
                    source.CredentialSetId,
                    source.DisplayName,
                    source.ConnectorType,
                    source.ConnectorRole,
                    source.ValuesJson,
                    source.SecretKeysJson,
                    source.CreatedUtc,
                    source.UpdatedUtc
                );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        AddParameter(command, "@CredentialSetId", credentialSet.CredentialSetId);
        AddParameter(command, "@DisplayName", credentialSet.DisplayName);
        AddParameter(command, "@ConnectorType", credentialSet.ConnectorType);
        AddParameter(command, "@ConnectorRole", credentialSet.ConnectorRole);
        AddParameter(command, "@ValuesJson", JsonSerializer.Serialize(credentialSet.Values, JsonOptions));
        AddParameter(command, "@SecretKeysJson", JsonSerializer.Serialize(credentialSet.SecretKeys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase), JsonOptions));
        AddParameter(command, "@CreatedUtc", credentialSet.CreatedUtc);
        AddParameter(command, "@UpdatedUtc", credentialSet.UpdatedUtc);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string credentialSetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(credentialSetId))
        {
            return false;
        }

        if (!IsConfigured)
        {
            return await _fileFallback.DeleteAsync(credentialSetId, cancellationToken).ConfigureAwait(false);
        }

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        const string sql = "DELETE FROM migration.CredentialSets WHERE CredentialSetId = @CredentialSetId;";
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        AddParameter(command, "@CredentialSetId", credentialSetId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // Also remove any old local fallback copy so it cannot be re-imported later.
        await _fileFallback.DeleteAsync(credentialSetId, cancellationToken).ConfigureAwait(false);
        return affected > 0;
    }

    private async Task<CredentialSetRecord?> GetSqlAsync(string credentialSetId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1)
                CredentialSetId,
                DisplayName,
                ConnectorType,
                ConnectorRole,
                ValuesJson,
                SecretKeysJson,
                CreatedUtc,
                UpdatedUtc
            FROM migration.CredentialSets
            WHERE CredentialSetId = @CredentialSetId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        AddParameter(command, "@CredentialSetId", credentialSetId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? ReadRecord(reader)
            : null;
    }

    private async Task ImportFileFallbackAsync(CancellationToken cancellationToken)
    {
        var fileRecords = await _fileFallback.ListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var record in fileRecords)
        {
            if (await GetSqlAsync(record.CredentialSetId, cancellationToken).ConfigureAwait(false) is null)
            {
                await SaveAsync(record, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaEnsured)
        {
            return;
        }

        await _schemaLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_schemaEnsured)
            {
                return;
            }

            const string sql = """
                IF SCHEMA_ID(N'migration') IS NULL
                    EXEC(N'CREATE SCHEMA migration');

                IF OBJECT_ID(N'migration.CredentialSets', N'U') IS NULL
                BEGIN
                    CREATE TABLE migration.CredentialSets
                    (
                        CredentialSetId nvarchar(200) NOT NULL CONSTRAINT PK_CredentialSets PRIMARY KEY,
                        DisplayName nvarchar(400) NOT NULL,
                        ConnectorType nvarchar(200) NOT NULL,
                        ConnectorRole nvarchar(100) NOT NULL,
                        ValuesJson nvarchar(max) NOT NULL,
                        SecretKeysJson nvarchar(max) NOT NULL,
                        CreatedUtc datetimeoffset NOT NULL,
                        UpdatedUtc datetimeoffset NOT NULL
                    );

                    CREATE INDEX IX_CredentialSets_Connector
                    ON migration.CredentialSets(ConnectorType, ConnectorRole, UpdatedUtc DESC);
                END;
                """;

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _schemaEnsured = true;
        }
        finally
        {
            _schemaLock.Release();
        }
    }

    private static CredentialSetRecord ReadRecord(SqlDataReader reader)
    {
        var valuesJson = reader.GetString(reader.GetOrdinal("ValuesJson"));
        var secretKeysJson = reader.GetString(reader.GetOrdinal("SecretKeysJson"));

        var values = JsonSerializer.Deserialize<Dictionary<string, string?>>(valuesJson, JsonOptions)
            ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var secretKeys = JsonSerializer.Deserialize<string[]>(secretKeysJson, JsonOptions)
            ?? Array.Empty<string>();

        return new CredentialSetRecord
        {
            CredentialSetId = reader.GetString(reader.GetOrdinal("CredentialSetId")),
            DisplayName = reader.GetString(reader.GetOrdinal("DisplayName")),
            ConnectorType = reader.GetString(reader.GetOrdinal("ConnectorType")),
            ConnectorRole = reader.GetString(reader.GetOrdinal("ConnectorRole")),
            Values = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase),
            SecretKeys = new HashSet<string>(secretKeys, StringComparer.OrdinalIgnoreCase),
            CreatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedUtc")),
            UpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("UpdatedUtc"))
        };
    }

    private static void AddParameter(SqlCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        return FirstNonEmpty(
            configuration["CredentialSetStore:ConnectionString"],
            configuration["SqlOperationalStore:ConnectionString"],
            configuration.GetConnectionString("MigrationOperationalStore"),
            configuration.GetConnectionString("OperationalSql"),
            configuration["ConnectionStrings:MigrationOperationalStore"],
            configuration["ConnectionStrings:OperationalSql"]);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
