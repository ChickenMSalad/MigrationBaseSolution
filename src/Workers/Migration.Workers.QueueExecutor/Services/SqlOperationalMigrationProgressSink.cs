using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Progress;

namespace Migration.Workers.QueueExecutor.Services;

public sealed class SqlOperationalMigrationProgressSink : IMigrationProgressSink
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlOperationalMigrationProgressSink> _logger;

    public SqlOperationalMigrationProgressSink(
        IConfiguration configuration,
        ILogger<SqlOperationalMigrationProgressSink> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ReportAsync(MigrationProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(progressEvent);

        var migrationRunId = ResolveMigrationRunId(progressEvent);
        if (migrationRunId is null)
        {
            return;
        }

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = @"
IF OBJECT_ID(N'dbo.MigrationOperationalEvents', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.MigrationOperationalEvents
    (
        OperationalEventId,
        EventType,
        Severity,
        Category,
        Source,
        Message,
        PayloadJson,
        CreatedUtc,
        ExecutionSessionId,
        MigrationRunId
    )
    VALUES
    (
        @OperationalEventId,
        @EventType,
        @Severity,
        @Category,
        @Source,
        @Message,
        @PayloadJson,
        @CreatedUtc,
        NULL,
        @MigrationRunId
    );
END";

            command.Parameters.Add(new SqlParameter("@OperationalEventId", SqlDbType.UniqueIdentifier) { Value = Guid.NewGuid() });
            command.Parameters.Add(new SqlParameter("@EventType", SqlDbType.NVarChar, 128) { Value = progressEvent.EventName });
            command.Parameters.Add(new SqlParameter("@Severity", SqlDbType.NVarChar, 32) { Value = ResolveSeverity(progressEvent.EventName) });
            command.Parameters.Add(new SqlParameter("@Category", SqlDbType.NVarChar, 128) { Value = "MigrationProgress" });
            command.Parameters.Add(new SqlParameter("@Source", SqlDbType.NVarChar, 256) { Value = "SqlOperationalQueueExecutor" });
            command.Parameters.Add(new SqlParameter("@Message", SqlDbType.NVarChar, -1) { Value = (object?)progressEvent.Message ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@PayloadJson", SqlDbType.NVarChar, -1) { Value = BuildPayloadJson(progressEvent) });
            command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTimeOffset) { Value = progressEvent.TimestampUtc });
            command.Parameters.Add(new SqlParameter("@MigrationRunId", SqlDbType.UniqueIdentifier) { Value = migrationRunId.Value });

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Unable to persist migration progress event {EventName} for run {RunId}.",
                progressEvent.EventName,
                progressEvent.RunId);
        }
    }

    private static Guid? ResolveMigrationRunId(MigrationProgressEvent progressEvent)
    {
        if (progressEvent.Properties.TryGetValue("SqlOperationalRunId", out var sqlRunId) &&
            Guid.TryParse(sqlRunId, out var parsedSqlRunId))
        {
            return parsedSqlRunId;
        }

        if (Guid.TryParse(progressEvent.RunId, out var parsedRunId))
        {
            return parsedRunId;
        }

        return null;
    }

    private string? ResolveConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("MigrationOperationalStore");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _configuration.GetConnectionString("OperationalSql");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _configuration["SqlOperationalRuntimeReadiness:ConnectionString"];
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _configuration["OperationalSql:ConnectionString"];
        }

        return connectionString;
    }

    private static string ResolveSeverity(string eventName)
    {
        return eventName.Contains("Failed", StringComparison.OrdinalIgnoreCase)
            ? "Error"
            : "Info";
    }

    private static string BuildPayloadJson(MigrationProgressEvent progressEvent)
    {
        return JsonSerializer.Serialize(new
        {
            progressEvent.RunId,
            progressEvent.JobName,
            progressEvent.EventName,
            progressEvent.WorkItemId,
            progressEvent.Completed,
            progressEvent.Total,
            progressEvent.Message,
            progressEvent.TimestampUtc,
            progressEvent.Properties
        }, JsonOptions);
    }
}
