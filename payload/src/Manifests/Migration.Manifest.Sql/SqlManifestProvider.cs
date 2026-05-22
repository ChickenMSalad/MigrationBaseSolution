using System.Data;
using Microsoft.Data.SqlClient;
using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Manifest.Sql;

/// <summary>
/// SQL Server manifest provider backed by a caller-supplied query.
/// 
/// This provider intentionally keeps the first SQL manifest phase simple:
/// - no new manifest DTOs
/// - no schema assumptions beyond the query result set
/// - no operational-store coupling
/// - no source/target connector behavior
/// 
/// The manifest query must return one row per asset/work item.
/// Recommended columns:
/// - RowId
/// - SourceAssetId
/// - SourcePath
/// 
/// All returned columns are also copied into ManifestRow.Columns.
/// </summary>
public sealed class SqlManifestProvider : IManifestProvider
{
    public string Type => "Sql";

    public async Task<IReadOnlyList<ManifestRow>> ReadAsync(
        MigrationJobDefinition job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var connectionString = ResolveRequiredValue(
            job.ConnectionString,
            job.Settings,
            "ConnectionString",
            "SqlConnectionString",
            "ManifestConnectionString");

        var queryText = ResolveRequiredValue(
            job.QueryText,
            job.Settings,
            "QueryText",
            "SqlQuery",
            "ManifestQuery");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = queryText;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = ResolveCommandTimeoutSeconds(job.Settings);

        await using var reader = await command
            .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken)
            .ConfigureAwait(false);

        var rows = new List<ManifestRow>();
        var ordinalNames = GetOrdinalNames(reader);

        var rowNumber = 0;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rowNumber++;

            var columns = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var ordinalName in ordinalNames)
            {
                var value = await ReadValueAsStringAsync(reader, ordinalName.Ordinal, cancellationToken)
                    .ConfigureAwait(false);

                columns[ordinalName.Name] = value;
            }

            var rowId = GetFirstNonEmpty(columns, "RowId", "ManifestRowId", "Id")
                ?? rowNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);

            rows.Add(new ManifestRow
            {
                RowId = rowId,
                SourceAssetId = GetFirstNonEmpty(columns, "SourceAssetId", "AssetId", "SourceId"),
                SourcePath = GetFirstNonEmpty(columns, "SourcePath", "Path", "FilePath", "BlobPath"),
                Columns = columns
            });
        }

        return rows;
    }

    private static IReadOnlyList<(int Ordinal, string Name)> GetOrdinalNames(SqlDataReader reader)
    {
        var names = new List<(int Ordinal, string Name)>();

        for (var index = 0; index < reader.FieldCount; index++)
        {
            names.Add((index, reader.GetName(index)));
        }

        return names;
    }

    private static async Task<string?> ReadValueAsStringAsync(
        SqlDataReader reader,
        int ordinal,
        CancellationToken cancellationToken)
    {
        if (await reader.IsDBNullAsync(ordinal, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ResolveRequiredValue(
        string? directValue,
        IReadOnlyDictionary<string, string> settings,
        params string[] settingKeys)
    {
        if (!string.IsNullOrWhiteSpace(directValue))
        {
            return directValue;
        }

        foreach (var key in settingKeys)
        {
            if (settings.TryGetValue(key, out var configuredValue)
                && !string.IsNullOrWhiteSpace(configuredValue))
            {
                return configuredValue;
            }
        }

        throw new InvalidOperationException(
            "SQL manifest provider requires a connection string and query text. " +
            "Set MigrationJobDefinition.ConnectionString / QueryText or provide matching Settings values.");
    }

    private static int ResolveCommandTimeoutSeconds(IReadOnlyDictionary<string, string> settings)
    {
        const int defaultTimeoutSeconds = 300;

        if (!settings.TryGetValue("CommandTimeoutSeconds", out var configuredValue))
        {
            return defaultTimeoutSeconds;
        }

        if (!int.TryParse(
                configuredValue,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var timeoutSeconds))
        {
            return defaultTimeoutSeconds;
        }

        return timeoutSeconds <= 0 ? defaultTimeoutSeconds : timeoutSeconds;
    }

    private static string? GetFirstNonEmpty(
        IReadOnlyDictionary<string, string?> columns,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (columns.TryGetValue(key, out var value)
                && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
