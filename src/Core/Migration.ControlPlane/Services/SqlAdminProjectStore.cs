using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Migration.ControlPlane.Models;
using System.Text.Json;

namespace Migration.ControlPlane.Services;

public sealed class SqlAdminProjectStore : IAdminProjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IConfiguration _configuration;

    public SqlAdminProjectStore(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<IReadOnlyList<MigrationProjectRecord>> ListProjectsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ProjectId,
       Name,
       SourceType,
       TargetType,
       ManifestType,
       ManifestArtifactId,
       MappingArtifactId,
       Json,
       CreatedUtc,
       UpdatedUtc
FROM dbo.AdminProjects
ORDER BY UpdatedUtc DESC;";

        var results = new List<MigrationProjectRecord>();

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var item = ReadProject(reader);
            if (item is not null)
            {
                results.Add(item);
            }
        }

        return results;
    }

    public async Task<MigrationProjectRecord?> GetProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ProjectId,
       Name,
       SourceType,
       TargetType,
       ManifestType,
       ManifestArtifactId,
       MappingArtifactId,
       Json,
       CreatedUtc,
       UpdatedUtc
FROM dbo.AdminProjects
WHERE ProjectId = @ProjectId;";

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProjectId", projectId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadProject(reader);
    }

    public async Task<MigrationProjectRecord> SaveProjectAsync(
        MigrationProjectRecord project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        const string sql = @"
MERGE dbo.AdminProjects AS target
USING (SELECT @ProjectId AS ProjectId) AS source
ON target.ProjectId = source.ProjectId
WHEN MATCHED THEN
    UPDATE SET
        Name = @Name,
        SourceType = @SourceType,
        TargetType = @TargetType,
        ManifestType = @ManifestType,
        ManifestArtifactId = @ManifestArtifactId,
        MappingArtifactId = @MappingArtifactId,
        Json = @Json,
        UpdatedUtc = @UpdatedUtc
WHEN NOT MATCHED THEN
    INSERT
    (
        ProjectId,
        Name,
        SourceType,
        TargetType,
        ManifestType,
        ManifestArtifactId,
        MappingArtifactId,
        Json,
        CreatedUtc,
        UpdatedUtc
    )
    VALUES
    (
        @ProjectId,
        @Name,
        @SourceType,
        @TargetType,
        @ManifestType,
        @ManifestArtifactId,
        @MappingArtifactId,
        @Json,
        @CreatedUtc,
        @UpdatedUtc
    );";

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        AddProjectParameters(command, project);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return project;
    }

    public async Task<bool> DeleteProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
DELETE FROM dbo.AdminProjects
WHERE ProjectId = @ProjectId;";

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProjectId", projectId);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    public async Task<IReadOnlyList<MigrationRunControlRecord>> ListRunsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT RunId,
       ProjectId,
       JobName,
       Status,
       ManifestArtifactId,
       MappingArtifactId,
       Json,
       CreatedUtc,
       UpdatedUtc,
       CompletedUtc
FROM dbo.AdminRuns
ORDER BY CreatedUtc DESC;";

        var results = new List<MigrationRunControlRecord>();

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var item = ReadRun(reader);
            if (item is not null)
            {
                results.Add(item);
            }
        }

        return results;
    }

    public async Task<MigrationRunControlRecord?> GetRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT RunId,
       ProjectId,
       JobName,
       Status,
       ManifestArtifactId,
       MappingArtifactId,
       Json,
       CreatedUtc,
       UpdatedUtc,
       CompletedUtc
FROM dbo.AdminRuns
WHERE RunId = @RunId;";

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadRun(reader);
    }

    public async Task<MigrationRunControlRecord> SaveRunAsync(
        MigrationRunControlRecord run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        const string sql = @"
MERGE dbo.AdminRuns AS target
USING (SELECT @RunId AS RunId) AS source
ON target.RunId = source.RunId
WHEN MATCHED THEN
    UPDATE SET
        ProjectId = @ProjectId,
        JobName = @JobName,
        Status = @Status,
        ManifestArtifactId = @ManifestArtifactId,
        MappingArtifactId = @MappingArtifactId,
        Json = @Json,
        UpdatedUtc = @UpdatedUtc,
        CompletedUtc = @CompletedUtc
WHEN NOT MATCHED THEN
    INSERT
    (
        RunId,
        ProjectId,
        JobName,
        Status,
        ManifestArtifactId,
        MappingArtifactId,
        Json,
        CreatedUtc,
        UpdatedUtc,
        CompletedUtc
    )
    VALUES
    (
        @RunId,
        @ProjectId,
        @JobName,
        @Status,
        @ManifestArtifactId,
        @MappingArtifactId,
        @Json,
        @CreatedUtc,
        @UpdatedUtc,
        @CompletedUtc
    );";

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        AddRunParameters(command, run);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return run;
    }

    public async Task<bool> DeleteRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
DELETE FROM dbo.AdminRuns
WHERE RunId = @RunId;";

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString();
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private string ResolveConnectionString()
    {
        var connectionString =
            _configuration.GetConnectionString("OperationalSql") ??
            _configuration.GetConnectionString("MigrationOperationalStore") ??
            _configuration["OperationalSql:ConnectionString"] ??
            _configuration["SqlOperationalStore:ConnectionString"] ??
            _configuration["ConnectionStrings:OperationalSql"] ??
            _configuration["ConnectionStrings:MigrationOperationalStore"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Operational SQL connection string is not configured. Configure ConnectionStrings:OperationalSql or ConnectionStrings:MigrationOperationalStore.");
        }

        return connectionString;
    }

    private static MigrationProjectRecord? ReadProject(SqlDataReader reader)
    {
        var json = GetNullableString(reader, "Json");
        var project = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<MigrationProjectRecord>(json, JsonOptions);

        if (project is null)
        {
            return null;
        }

        return project with
        {
            ProjectId = reader.GetString(reader.GetOrdinal("ProjectId")),
            DisplayName = reader.GetString(reader.GetOrdinal("Name")),
            SourceType = reader.GetString(reader.GetOrdinal("SourceType")),
            TargetType = reader.GetString(reader.GetOrdinal("TargetType")),
            ManifestType = reader.GetString(reader.GetOrdinal("ManifestType")),
            ManifestArtifactId = GetNullableString(reader, "ManifestArtifactId"),
            MappingArtifactId = GetNullableString(reader, "MappingArtifactId"),
            CreatedUtc = GetDateTimeOffset(reader, "CreatedUtc"),
            UpdatedUtc = GetDateTimeOffset(reader, "UpdatedUtc")
        };
    }

    private static MigrationRunControlRecord? ReadRun(SqlDataReader reader)
    {
        var json = GetNullableString(reader, "Json");
        var run = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<MigrationRunControlRecord>(json, JsonOptions);

        if (run is null)
        {
            return null;
        }

        return run with
        {
            RunId = reader.GetString(reader.GetOrdinal("RunId")),
            ProjectId = reader.GetString(reader.GetOrdinal("ProjectId")),
            JobName = reader.GetString(reader.GetOrdinal("JobName")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            ManifestArtifactId = GetNullableString(reader, "ManifestArtifactId"),
            MappingArtifactId = GetNullableString(reader, "MappingArtifactId"),
            CreatedUtc = GetDateTimeOffset(reader, "CreatedUtc"),
            UpdatedUtc = GetDateTimeOffset(reader, "UpdatedUtc"),
            CompletedUtc = GetNullableDateTimeOffset(reader, "CompletedUtc")
        };
    }

    private static void AddProjectParameters(SqlCommand command, MigrationProjectRecord project)
    {
        command.Parameters.AddWithValue("@ProjectId", project.ProjectId);
        command.Parameters.AddWithValue("@Name", project.DisplayName);
        command.Parameters.AddWithValue("@SourceType", project.SourceType);
        command.Parameters.AddWithValue("@TargetType", project.TargetType);
        command.Parameters.AddWithValue("@ManifestType", (object?)project.ManifestType ?? DBNull.Value);
        command.Parameters.AddWithValue("@ManifestArtifactId", (object?)project.ManifestArtifactId ?? DBNull.Value);
        command.Parameters.AddWithValue("@MappingArtifactId", (object?)project.MappingArtifactId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Json", JsonSerializer.Serialize(project, JsonOptions));
        command.Parameters.AddWithValue("@CreatedUtc", project.CreatedUtc);
        command.Parameters.AddWithValue("@UpdatedUtc", project.UpdatedUtc);
    }

    private static void AddRunParameters(SqlCommand command, MigrationRunControlRecord run)
    {
        command.Parameters.AddWithValue("@RunId", run.RunId);
        command.Parameters.AddWithValue("@ProjectId", run.ProjectId);
        command.Parameters.AddWithValue("@JobName", run.JobName);
        command.Parameters.AddWithValue("@Status", run.Status);
        command.Parameters.AddWithValue("@ManifestArtifactId", (object?)run.ManifestArtifactId ?? DBNull.Value);
        command.Parameters.AddWithValue("@MappingArtifactId", (object?)run.MappingArtifactId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Json", JsonSerializer.Serialize(run, JsonOptions));
        command.Parameters.AddWithValue("@CreatedUtc", run.CreatedUtc);
        command.Parameters.AddWithValue("@UpdatedUtc", run.UpdatedUtc);
        command.Parameters.AddWithValue("@CompletedUtc", (object?)run.CompletedUtc ?? DBNull.Value);
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset GetDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetFieldValue<DateTimeOffset>(ordinal);
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }
}
