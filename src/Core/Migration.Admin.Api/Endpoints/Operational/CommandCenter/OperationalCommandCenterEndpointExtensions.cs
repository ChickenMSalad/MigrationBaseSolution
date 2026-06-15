using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Migration.Admin.Api.Endpoints.Operational.CommandCenter;

public static class OperationalCommandCenterEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalCommandCenterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/command-center")
            .WithTags("Operational Command Center");

        group.MapGet("/summary", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var connectionString = ResolveOperationalConnectionString(configuration);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Results.Ok(CommandCenterSummaryResponse.Empty(
                    "Unknown",
                    "No operational SQL connection string is configured."));
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var hasRuns = await HasTableAsync(connection, "migration.Runs", cancellationToken).ConfigureAwait(false);
                var hasWorkItems = await HasTableAsync(connection, "migration.WorkItems", cancellationToken).ConfigureAwait(false);

                var activeRuns = hasRuns
                    ? await CountAsync(connection, "select count_big(1) from migration.Runs where lower(coalesce(Status, '')) in ('queued','dispatching','dispatched','running','inprogress','in-progress')", cancellationToken).ConfigureAwait(false)
                    : 0;

                var completedRunsToday = hasRuns
                    ? await CountAsync(connection, "select count_big(1) from migration.Runs where lower(coalesce(Status, '')) in ('completed','complete','succeeded','success') and CompletedAtUtc >= convert(date, sysutcdatetime())", cancellationToken).ConfigureAwait(false)
                    : 0;

                var failedRunsToday = hasRuns
                    ? await CountAsync(connection, "select count_big(1) from migration.Runs where lower(coalesce(Status, '')) in ('failed','completedwithfailures','completed-with-failures') and coalesce(CompletedAtUtc, StartedAtUtc) >= convert(date, sysutcdatetime())", cancellationToken).ConfigureAwait(false)
                    : 0;

                var queuedWorkItems = hasWorkItems
                    ? await CountAsync(connection, "select count_big(1) from migration.WorkItems where lower(coalesce(Status, '')) in ('queued','pending','ready')", cancellationToken).ConfigureAwait(false)
                    : 0;

                var dispatchedWorkItems = hasWorkItems
                    ? await CountAsync(connection, "select count_big(1) from migration.WorkItems where lower(coalesce(Status, '')) in ('dispatched','leased','running','inprogress','in-progress')", cancellationToken).ConfigureAwait(false)
                    : 0;

                var failedWorkItems = hasWorkItems
                    ? await CountAsync(connection, "select count_big(1) from migration.WorkItems where lower(coalesce(Status, '')) in ('failed','deadlettered','dead-lettered')", cancellationToken).ConfigureAwait(false)
                    : 0;

                var retryPendingWorkItems = hasWorkItems
                    ? await CountAsync(connection, "select count_big(1) from migration.WorkItems where lower(coalesce(Status, '')) in ('retrypending','retry-pending','retrying')", cancellationToken).ConfigureAwait(false)
                    : 0;

                var activeWorkers = hasWorkItems
                    ? await CountAsync(connection, "select count_big(1) from (select distinct WorkerId from migration.WorkItems where WorkerId is not null and lower(coalesce(Status, '')) in ('dispatched','leased','running','inprogress','in-progress')) workers", cancellationToken).ConfigureAwait(false)
                    : 0;

                var staleWorkers = hasWorkItems
                    ? await CountAsync(connection, "select count_big(1) from (select WorkerId, max(StartedAtUtc) as LastStartedUtc from migration.WorkItems where WorkerId is not null group by WorkerId having max(StartedAtUtc) < dateadd(minute, -15, sysutcdatetime())) workers", cancellationToken).ConfigureAwait(false)
                    : 0;

                var criticalAlerts = failedRunsToday + failedWorkItems;
                var status = DetermineStatus(hasRuns, hasWorkItems, failedWorkItems, retryPendingWorkItems, queuedWorkItems, dispatchedWorkItems);
                var recentEvents = hasRuns || hasWorkItems
                    ? await ReadRecentEventsAsync(connection, hasRuns, hasWorkItems, cancellationToken).ConfigureAwait(false)
                    : Array.Empty<CommandCenterEvent>();

                return Results.Ok(new CommandCenterSummaryResponse(
                    GeneratedUtc: DateTimeOffset.UtcNow,
                    RuntimeStatus: status,
                    ActiveRuns: activeRuns,
                    CompletedRunsToday: completedRunsToday,
                    FailedRunsToday: failedRunsToday,
                    QueuedWorkItems: queuedWorkItems,
                    DispatchedWorkItems: dispatchedWorkItems,
                    FailedWorkItems: failedWorkItems,
                    RetryPendingWorkItems: retryPendingWorkItems,
                    ActiveWorkers: activeWorkers,
                    StaleWorkers: staleWorkers,
                    PendingNotifications: 0,
                    CriticalAlerts: criticalAlerts,
                    RecentEvents: recentEvents,
                    Message: "Command Center summary is sourced from operational SQL runtime tables."));
            }
            catch (Exception ex)
            {
                return Results.Ok(CommandCenterSummaryResponse.Empty("Degraded", ex.Message));
            }
        })
        .WithName("GetOperationalCommandCenterSummary");

        group.MapGet("/health", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var checks = new List<CommandCenterHealthCheck>();
            var connectionString = ResolveOperationalConnectionString(configuration);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                checks.Add(new CommandCenterHealthCheck("Operational SQL connection", "Critical", "No operational SQL connection string is configured."));
                return Results.Ok(new CommandCenterHealthResponse(DateTimeOffset.UtcNow, "Degraded", checks));
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                checks.Add(new CommandCenterHealthCheck("Operational SQL connection", "Healthy", "SQL connection opened successfully."));

                await AddTableCheckAsync(connection, checks, "migration.Runs", cancellationToken).ConfigureAwait(false);
                await AddTableCheckAsync(connection, checks, "migration.ManifestRows", cancellationToken).ConfigureAwait(false);
                await AddTableCheckAsync(connection, checks, "migration.WorkItems", cancellationToken).ConfigureAwait(false);
                await AddTableCheckAsync(connection, checks, "dbo.AdminRuns", cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                checks.Add(new CommandCenterHealthCheck("Operational SQL query", "Critical", ex.Message));
            }

            var status = checks.Any(check => IsCritical(check.Status))
                ? "Degraded"
                : checks.Any(check => IsWarning(check.Status)) ? "Warning" : "Healthy";

            return Results.Ok(new CommandCenterHealthResponse(DateTimeOffset.UtcNow, status, checks));
        })
        .WithName("GetOperationalCommandCenterHealth");

        return endpoints;
    }

    private static string? ResolveOperationalConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("MigrationOperationalStore")
            ?? configuration.GetConnectionString("OperationalSql")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:MigrationOperationalStore"]
            ?? configuration["ConnectionStrings:OperationalSql"]
            ?? configuration["SqlOperationalStore:ConnectionString"];
    }

    private static async Task AddTableCheckAsync(SqlConnection connection, List<CommandCenterHealthCheck> checks, string tableName, CancellationToken cancellationToken)
    {
        var exists = await HasTableAsync(connection, tableName, cancellationToken).ConfigureAwait(false);
        checks.Add(new CommandCenterHealthCheck(
            tableName,
            exists ? "Healthy" : "Critical",
            exists ? "Required runtime table is present." : "Required runtime table is missing."));
    }

    private static async Task<bool> HasTableAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select case when object_id(@tableName, 'U') is null then 0 else 1 end";
        command.Parameters.Add(new SqlParameter("@tableName", SqlDbType.NVarChar, 256) { Value = tableName });
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(value, CultureInfo.InvariantCulture) == 1;
    }

    private static async Task<int> CountAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(value ?? 0, CultureInfo.InvariantCulture);
    }

    private static async Task<IReadOnlyList<CommandCenterEvent>> ReadRecentEventsAsync(SqlConnection connection, bool hasRuns, bool hasWorkItems, CancellationToken cancellationToken)
    {
        var unionParts = new List<string>();
        if (hasRuns)
        {
            unionParts.Add("select top (10) cast(RunKey as nvarchar(128)) as EventId, coalesce(CompletedAtUtc, StartedAtUtc, sysutcdatetime()) as CreatedUtc, case when lower(coalesce(Status, '')) like '%fail%' then 'Error' when lower(coalesce(Status, '')) in ('running','queued','dispatching','dispatched') then 'Info' else 'Info' end as Severity, 'Run' as Category, concat('Run ', coalesce(Status, 'Unknown')) as Title, coalesce(StatusReason, concat('Run ', RunKey, ' is ', coalesce(Status, 'Unknown'))) as Message, concat(coalesce(SourceSystem, 'source'), ' to ', coalesce(TargetSystem, 'target')) as Source from migration.Runs order by coalesce(CompletedAtUtc, StartedAtUtc, sysutcdatetime()) desc");
        }

        if (hasWorkItems)
        {
            unionParts.Add("select top (10) cast(WorkItemId as nvarchar(128)) as EventId, coalesce(CompletedAtUtc, StartedAtUtc, sysutcdatetime()) as CreatedUtc, case when lower(coalesce(Status, '')) like '%fail%' then 'Error' when lower(coalesce(Status, '')) in ('running','dispatched','leased') then 'Info' else 'Info' end as Severity, 'WorkItem' as Category, concat('Work item ', coalesce(Status, 'Unknown')) as Title, concat('Work item ', cast(WorkItemId as nvarchar(128)), ' for run ', cast(RunId as nvarchar(128)), ' is ', coalesce(Status, 'Unknown')) as Message, coalesce(WorkerId, WorkItemType, 'runtime') as Source from migration.WorkItems order by coalesce(CompletedAtUtc, StartedAtUtc, sysutcdatetime()) desc");
        }

        if (unionParts.Count == 0)
        {
            return Array.Empty<CommandCenterEvent>();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "select top (20) EventId, CreatedUtc, Severity, Category, Title, Message, Source from (" + string.Join(" union all ", unionParts) + ") events order by CreatedUtc desc";
        command.CommandTimeout = 30;

        var events = new List<CommandCenterEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            events.Add(new CommandCenterEvent(
                EventId: ReadString(reader, 0),
                CreatedUtc: ReadDateTimeOffset(reader, 1),
                Severity: ReadString(reader, 2),
                Category: ReadString(reader, 3),
                Title: ReadString(reader, 4),
                Message: ReadString(reader, 5),
                Source: ReadString(reader, 6)));
        }

        return events;
    }

    private static string DetermineStatus(bool hasRuns, bool hasWorkItems, int failedWorkItems, int retryPendingWorkItems, int queuedWorkItems, int dispatchedWorkItems)
    {
        if (!hasRuns || !hasWorkItems)
        {
            return "Degraded";
        }

        if (failedWorkItems > 0)
        {
            return "Degraded";
        }

        if (retryPendingWorkItems > 0 || queuedWorkItems > 0 || dispatchedWorkItems > 0)
        {
            return "Warning";
        }

        return "Healthy";
    }

    private static bool IsCritical(string? status)
    {
        return string.Equals(status, "Critical", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Degraded", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWarning(string? status)
    {
        return string.Equals(status, "Warning", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadString(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset? ReadDateTimeOffset(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => DateTimeOffset.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null
        };
    }
}

public sealed record CommandCenterSummaryResponse(
    DateTimeOffset GeneratedUtc,
    string RuntimeStatus,
    int ActiveRuns,
    int CompletedRunsToday,
    int FailedRunsToday,
    int QueuedWorkItems,
    int DispatchedWorkItems,
    int FailedWorkItems,
    int RetryPendingWorkItems,
    int ActiveWorkers,
    int StaleWorkers,
    int PendingNotifications,
    int CriticalAlerts,
    IReadOnlyList<CommandCenterEvent> RecentEvents,
    string? Message)
{
    public static CommandCenterSummaryResponse Empty(string status, string message)
    {
        return new CommandCenterSummaryResponse(
            DateTimeOffset.UtcNow,
            status,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            status.Equals("Degraded", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            Array.Empty<CommandCenterEvent>(),
            message);
    }
}

public sealed record CommandCenterEvent(
    string? EventId,
    DateTimeOffset? CreatedUtc,
    string? Severity,
    string? Category,
    string? Title,
    string? Message,
    string? Source);

public sealed record CommandCenterHealthResponse(
    DateTimeOffset GeneratedUtc,
    string Status,
    IReadOnlyList<CommandCenterHealthCheck> Checks);

public sealed record CommandCenterHealthCheck(
    string Name,
    string Status,
    string? Message);
