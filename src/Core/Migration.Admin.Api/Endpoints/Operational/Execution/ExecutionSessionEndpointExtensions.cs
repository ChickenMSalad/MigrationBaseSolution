using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionSessionEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-sessions")
            .WithTags("Operational Execution Sessions");

        group.MapPost("/", async (
            IExecutionSessionStore store,
            CreateExecutionSessionRequest request,
            CancellationToken cancellationToken) =>
        {
            var session = await store.CreateAsync(request, cancellationToken);
            return Results.Ok(session);
        })
        .WithName("CreateExecutionSession");

        group.MapGet("/recent", async (
            IExecutionSessionStore store,
            IConfiguration configuration,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var safeTake = Math.Clamp(take.GetValueOrDefault(50), 1, 250);
            var sessions = await store.ReadRecentAsync(safeTake, cancellationToken);

            if (sessions.Count == 0)
            {
                sessions = await ReadRecentRuntimeRunsAsSessionsAsync(configuration, safeTake, cancellationToken);
            }

            return Results.Ok(new RecentExecutionSessionsResponse(
                Take: safeTake,
                Sessions: sessions));
        })
        .WithName("GetRecentExecutionSessions");

        return endpoints;
    }

    private static async Task<IReadOnlyList<ExecutionSessionRecord>> ReadRecentRuntimeRunsAsSessionsAsync(
        IConfiguration configuration,
        int take,
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Array.Empty<ExecutionSessionRecord>();
        }

        var sessions = new List<ExecutionSessionRecord>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    SELECT TOP (0)
        CAST(NULL AS nvarchar(128)) AS RunKey,
        CAST(NULL AS nvarchar(256)) AS SourceSystem,
        CAST(NULL AS nvarchar(256)) AS TargetSystem,
        CAST(NULL AS nvarchar(64)) AS Status,
        CAST(NULL AS nvarchar(max)) AS StatusReason,
        CAST(NULL AS datetimeoffset) AS StartedAtUtc,
        CAST(NULL AS datetimeoffset) AS CompletedAtUtc;
END
ELSE
BEGIN
    SELECT TOP (@Take)
        CAST(RunKey AS nvarchar(128)) AS RunKey,
        CAST(SourceSystem AS nvarchar(256)) AS SourceSystem,
        CAST(TargetSystem AS nvarchar(256)) AS TargetSystem,
        CAST(Status AS nvarchar(64)) AS Status,
        CAST(StatusReason AS nvarchar(max)) AS StatusReason,
        CAST(StartedAtUtc AS datetimeoffset) AS StartedAtUtc,
        CAST(CompletedAtUtc AS datetimeoffset) AS CompletedAtUtc
    FROM migration.Runs
    ORDER BY COALESCE(StartedAtUtc, CompletedAtUtc, SYSUTCDATETIME()) DESC;
END
";
        command.Parameters.AddWithValue("@Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var runKey = ReadNullableString(reader, "RunKey") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(runKey))
            {
                continue;
            }

            var sourceSystem = ReadNullableString(reader, "SourceSystem");
            var targetSystem = ReadNullableString(reader, "TargetSystem");
            var status = ReadNullableString(reader, "Status") ?? "Unknown";
            var statusReason = ReadNullableString(reader, "StatusReason");
            var startedUtc = ReadNullableDateTimeOffset(reader, "StartedAtUtc");
            var completedUtc = ReadNullableDateTimeOffset(reader, "CompletedAtUtc");

            sessions.Add(new ExecutionSessionRecord(
                ExecutionSessionId: StableGuidFromText(runKey),
                MigrationRunId: Guid.TryParse(runKey, out var migrationRunId) ? migrationRunId : null,
                Name: $"Runtime Run {runKey}",
                SourceConnector: sourceSystem,
                TargetConnector: targetSystem,
                Status: NormalizeStatus(status),
                CreatedUtc: startedUtc ?? completedUtc ?? DateTimeOffset.UtcNow,
                StartedUtc: startedUtc,
                CompletedUtc: completedUtc,
                Notes: BuildRuntimeNotes(runKey, statusReason)));
        }

        return sessions;
    }

    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("MigrationOperationalStore")
            ?? configuration.GetConnectionString("OperationalSql")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:MigrationOperationalStore"]
            ?? configuration["ConnectionStrings:OperationalSql"]
            ?? configuration["SqlOperationalStore:ConnectionString"];
    }

    private static string? ReadNullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        if (value is DateTimeOffset dto)
        {
            return dto;
        }

        if (value is DateTime dt)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
        }

        return null;
    }

    private static Guid StableGuidFromText(string text)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes("MigrationBaseSolution.ExecutionSession." + text));
        return new Guid(bytes);
    }

    private static string NormalizeStatus(string status)
    {
        if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            return "completed";
        }

        if (status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        {
            return "failed";
        }

        if (status.Equals("Running", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("Processing", StringComparison.OrdinalIgnoreCase))
        {
            return "running";
        }

        if (status.Equals("Queued", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            return "queued";
        }

        return status;
    }

    private static string BuildRuntimeNotes(string runKey, string? statusReason)
    {
        if (string.IsNullOrWhiteSpace(statusReason))
        {
            return "Synthesized from migration.Runs because no explicit execution session records were found.";
        }

        return $"RunKey: {runKey}. {statusReason}";
    }
}

public sealed record RecentExecutionSessionsResponse(
    int Take,
    IReadOnlyList<ExecutionSessionRecord> Sessions);
