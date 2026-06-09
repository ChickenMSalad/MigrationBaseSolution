using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Services;
using System.Text.Json;

namespace Migration.ControlPlane.Services;

public sealed class SqlAdminProjectStore : IAdminProjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private readonly IConfiguration _configuration;

    public SqlAdminProjectStore(IConfiguration configuration) => _configuration = configuration;

    public async Task<IReadOnlyList<MigrationProjectRecord>> ListProjectsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Json FROM dbo.AdminProjects ORDER BY UpdatedUtc DESC;";
        var results = new List<MigrationProjectRecord>();

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var item = JsonSerializer.Deserialize<MigrationProjectRecord>(reader.GetString(0), JsonOptions);
            if (item is not null) results.Add(item);
        }

        return results;
    }

    public async Task<MigrationProjectRecord?> GetProjectAsync(string projectId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Json FROM dbo.AdminProjects WHERE ProjectId = @ProjectId;";

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProjectId", projectId);

        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<MigrationProjectRecord>(json, JsonOptions);
    }

    public async Task<MigrationProjectRecord> SaveProjectAsync(MigrationProjectRecord project, CancellationToken cancellationToken = default)
    {
        const string sql = """
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
                    ProjectId, Name, SourceType, TargetType, ManifestType,
                    ManifestArtifactId, MappingArtifactId, Json, CreatedUtc, UpdatedUtc
                )
                VALUES
                (
                    @ProjectId, @Name, @SourceType, @TargetType, @ManifestType,
                    @ManifestArtifactId, @MappingArtifactId, @Json, @CreatedUtc, @UpdatedUtc
                );
            """;

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        AddProjectParameters(command, project);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return project;
    }

    public async Task<bool> DeleteProjectAsync(string projectId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.AdminProjects WHERE ProjectId = @ProjectId;";

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProjectId", projectId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<MigrationRunControlRecord>> ListRunsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Json FROM dbo.AdminRuns ORDER BY CreatedUtc DESC;";
        var results = new List<MigrationRunControlRecord>();

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var item = JsonSerializer.Deserialize<MigrationRunControlRecord>(reader.GetString(0), JsonOptions);
            if (item is not null) results.Add(item);
        }

        return results;
    }

    public async Task<MigrationRunControlRecord?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Json FROM dbo.AdminRuns WHERE RunId = @RunId;";

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<MigrationRunControlRecord>(json, JsonOptions);
    }

    public async Task<MigrationRunControlRecord> SaveRunAsync(MigrationRunControlRecord run, CancellationToken cancellationToken = default)
    {
        const string sql = """
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
                    RunId, ProjectId, JobName, Status,
                    ManifestArtifactId, MappingArtifactId,
                    Json, CreatedUtc, UpdatedUtc, CompletedUtc
                )
                VALUES
                (
                    @RunId, @ProjectId, @JobName, @Status,
                    @ManifestArtifactId, @MappingArtifactId,
                    @Json, @CreatedUtc, @UpdatedUtc, @CompletedUtc
                );
            """;

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        AddRunParameters(command, run);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return run;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connectionString =
            _configuration.GetConnectionString("OperationalSql") ??
            _configuration["OperationalSql:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Operational SQL connection string is not configured.");

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
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
        command.Parameters.AddWithValue("@JobName", run.Job.JobName);
        command.Parameters.AddWithValue("@Status", run.Status);
        command.Parameters.AddWithValue("@ManifestArtifactId", (object?)run.ManifestArtifactId ?? DBNull.Value);
        command.Parameters.AddWithValue("@MappingArtifactId", (object?)run.MappingArtifactId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Json", JsonSerializer.Serialize(run, JsonOptions));
        command.Parameters.AddWithValue("@CreatedUtc", run.CreatedUtc);
        command.Parameters.AddWithValue("@UpdatedUtc", run.UpdatedUtc);
        command.Parameters.AddWithValue("@CompletedUtc", (object?)run.CompletedUtc ?? DBNull.Value);
    }
}