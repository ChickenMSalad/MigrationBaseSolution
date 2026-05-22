using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.Readiness;

namespace Migration.Infrastructure.Sql.Operational.Readiness;

public sealed class SqlOperationalRuntimeReadinessService : IOperationalRuntimeReadinessService
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<SqlOperationalRuntimeReadinessOptions> _options;

    public SqlOperationalRuntimeReadinessService(
        IConfiguration configuration,
        IOptions<SqlOperationalRuntimeReadinessOptions> options)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<OperationalRuntimeReadinessReport> GetReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        var evaluatedUtc = DateTimeOffset.UtcNow;
        var checks = new List<OperationalRuntimeReadinessCheck>();
        var blockingIssues = new List<string>();
        var warnings = new List<string>();

        string connectionString;
        try
        {
            connectionString = ResolveConnectionString();
            checks.Add(new OperationalRuntimeReadinessCheck(
                "ConnectionString",
                "Ready",
                true,
                "SQL operational store connection string is configured."));
        }
        catch (Exception ex)
        {
            checks.Add(new OperationalRuntimeReadinessCheck(
                "ConnectionString",
                "Blocked",
                true,
                ex.Message));
            blockingIssues.Add(ex.Message);
            return BuildRuntimeReport(evaluatedUtc, checks, blockingIssues, warnings);
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            checks.Add(new OperationalRuntimeReadinessCheck(
                "SqlConnection",
                "Ready",
                true,
                "SQL operational store connection opened successfully."));

            foreach (var table in RequiredTables)
            {
                var exists = await TableExistsAsync(connection, table.Name, cancellationToken);
                checks.Add(new OperationalRuntimeReadinessCheck(
                    table.CheckName,
                    exists ? "Ready" : "Blocked",
                    true,
                    exists ? $"{table.Name} exists." : $"{table.Name} was not found."));

                if (!exists)
                {
                    blockingIssues.Add($"Required operational table '{table.Name}' was not found.");
                }
            }
        }
        catch (Exception ex)
        {
            checks.Add(new OperationalRuntimeReadinessCheck(
                "SqlConnection",
                "Blocked",
                true,
                ex.Message));
            blockingIssues.Add(ex.Message);
        }

        return BuildRuntimeReport(evaluatedUtc, checks, blockingIssues, warnings);
    }

    public async Task<OperationalRunReadinessReport> GetRunReadinessAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var evaluatedUtc = DateTimeOffset.UtcNow;
        var blockingIssues = new List<string>();
        var warnings = new List<string>();

        await using var connection = OpenConnection();

        var run = await connection.QuerySingleOrDefaultAsync<RunReadinessRow>(new CommandDefinition($@"
select RunId,
       Status
from {RunsTableName}
where RunId = @RunId;", new { RunId = runId }, cancellationToken: cancellationToken));

        if (run is null)
        {
            blockingIssues.Add($"Run '{runId}' was not found.");
            return new OperationalRunReadinessReport(
                runId,
                "NotFound",
                false,
                false,
                false,
                evaluatedUtc,
                new OperationalRunReadinessCounts(0, 0, 0, 0, 0, 0, 0, 0),
                blockingIssues,
                warnings);
        }

        var manifestCounts = await connection.QuerySingleAsync<ManifestReadinessCounts>(new CommandDefinition($@"
select count(1) as ManifestRowCount,
       sum(case when Status in ('Pending', 'Ready', 'Validated') then 1 else 0 end) as PendingManifestRowCount
from {ManifestRowsTableName}
where RunId = @RunId;", new { RunId = runId }, cancellationToken: cancellationToken));

        var workItemCounts = await connection.QuerySingleAsync<WorkItemReadinessCounts>(new CommandDefinition($@"
select count(1) as WorkItemCount,
       sum(case when Status = 'Pending' then 1 else 0 end) as PendingWorkItemCount,
       sum(case when Status = 'Leased' then 1 else 0 end) as LeasedWorkItemCount,
       sum(case when Status = 'Completed' then 1 else 0 end) as CompletedWorkItemCount,
       sum(case when Status = 'Failed' then 1 else 0 end) as FailedWorkItemCount,
       sum(case when Status = 'FailedRetryable' then 1 else 0 end) as RetryableFailedWorkItemCount
from {WorkItemsTableName}
where RunId = @RunId;", new { RunId = runId }, cancellationToken: cancellationToken));

        if (manifestCounts.ManifestRowCount == 0)
        {
            blockingIssues.Add("Run has no manifest rows loaded into the SQL operational store.");
        }

        if (string.Equals(run.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(run.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(run.Status, "Canceled", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Run is already terminal with status '{run.Status}'.");
        }

        var canStart = blockingIssues.Count == 0 &&
            (string.Equals(run.Status, "Created", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(run.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(run.Status, "Ready", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(run.Status, "Paused", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(run.Status, "RetryRequested", StringComparison.OrdinalIgnoreCase));

        var canDispatch = blockingIssues.Count == 0 &&
            manifestCounts.PendingManifestRowCount > 0 &&
            !string.Equals(run.Status, "CancelRequested", StringComparison.OrdinalIgnoreCase);

        var canExecute = blockingIssues.Count == 0 &&
            (workItemCounts.PendingWorkItemCount > 0 || workItemCounts.RetryableFailedWorkItemCount > 0);

        var status = blockingIssues.Count == 0 ? "Ready" : "Blocked";
        var counts = new OperationalRunReadinessCounts(
            manifestCounts.ManifestRowCount,
            manifestCounts.PendingManifestRowCount,
            workItemCounts.WorkItemCount,
            workItemCounts.PendingWorkItemCount,
            workItemCounts.LeasedWorkItemCount,
            workItemCounts.CompletedWorkItemCount,
            workItemCounts.FailedWorkItemCount,
            workItemCounts.RetryableFailedWorkItemCount);

        return new OperationalRunReadinessReport(
            runId,
            status,
            canStart,
            canDispatch,
            canExecute,
            evaluatedUtc,
            counts,
            blockingIssues,
            warnings);
    }

    private OperationalRuntimeReadinessReport BuildRuntimeReport(
        DateTimeOffset evaluatedUtc,
        IReadOnlyList<OperationalRuntimeReadinessCheck> checks,
        IReadOnlyList<string> blockingIssues,
        IReadOnlyList<string> warnings)
    {
        var ready = blockingIssues.Count == 0;
        return new OperationalRuntimeReadinessReport(
            ready ? "Ready" : "Blocked",
            ready,
            evaluatedUtc,
            checks,
            blockingIssues,
            warnings);
    }

    private async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        var objectName = $"[{_options.Value.SchemaName}].[{tableName}]";
        var exists = await connection.QuerySingleAsync<int>(new CommandDefinition(
            "select case when object_id(@ObjectName, 'U') is null then 0 else 1 end;",
            new { ObjectName = objectName },
            cancellationToken: cancellationToken));

        return exists == 1;
    }

    private SqlConnection OpenConnection()
    {
        var connectionString = ResolveConnectionString();
        var connection = new SqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    private string ResolveConnectionString()
    {
        var options = _options.Value;
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return options.ConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(options.ConnectionStringName))
        {
            var named = _configuration.GetConnectionString(options.ConnectionStringName);
            if (!string.IsNullOrWhiteSpace(named))
            {
                return named;
            }
        }

        var fallback = _configuration["SqlOperationalStore:ConnectionString"]
            ?? _configuration["OperationalStore:Sql:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        throw new InvalidOperationException(
            "SQL operational runtime readiness connection string is not configured. Configure ConnectionStrings:MigrationOperationalStore or SqlOperationalRuntimeReadiness:ConnectionString.");
    }

    private IReadOnlyList<RequiredTable> RequiredTables
    {
        get
        {
            var options = _options.Value;
            return new[]
            {
                new RequiredTable("ProjectsTable", options.ProjectsTableName),
                new RequiredTable("RunsTable", options.RunsTableName),
                new RequiredTable("ManifestRowsTable", options.ManifestRowsTableName),
                new RequiredTable("WorkItemsTable", options.WorkItemsTableName),
                new RequiredTable("FailuresTable", options.FailuresTableName),
                new RequiredTable("CheckpointsTable", options.CheckpointsTableName),
                new RequiredTable("IdentifierMappingsTable", options.IdentifierMappingsTableName)
            };
        }
    }

    private string RunsTableName => BracketTable(_options.Value.RunsTableName);

    private string ManifestRowsTableName => BracketTable(_options.Value.ManifestRowsTableName);

    private string WorkItemsTableName => BracketTable(_options.Value.WorkItemsTableName);

    private string BracketTable(string tableName) => $"[{_options.Value.SchemaName}].[{tableName}]";

    private sealed record RequiredTable(string CheckName, string Name);

    private sealed record RunReadinessRow(Guid RunId, string Status);

    private sealed record ManifestReadinessCounts(int ManifestRowCount, int PendingManifestRowCount);

    private sealed record WorkItemReadinessCounts(
        int WorkItemCount,
        int PendingWorkItemCount,
        int LeasedWorkItemCount,
        int CompletedWorkItemCount,
        int FailedWorkItemCount,
        int RetryableFailedWorkItemCount);
}
